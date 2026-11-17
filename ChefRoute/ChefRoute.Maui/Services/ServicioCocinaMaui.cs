using Core.Algoritmos;
using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using Core.Servicios;
using ChefRoute.Maui.Graphics;
using ChefRoute.Maui.Models;
using Infraestructura.Cargador;
using Infraestructura.DTOs;
using System.Text.Json;

namespace ChefRoute.Maui.Services
{
    public class ServicioCocinaMaui
    {
        private readonly CargadorRecetas _cargador;
        private readonly SupervisorReceta _supervisor;
        private readonly DetectorCicloDFS _detectorCiclos;

        public GrafoReceta? GrafoActual { get; private set; }
        public string NombreRecetaActual { get; private set; } = "Sin receta seleccionada";
        public string DescripcionRecetaActual { get; private set; } = "Selecciona o crea una receta para comenzar la supervision.";
        public ListaSimple<Tarea>? PlanOrdenadoActual { get; private set; }
        public Tarea? TareaEnFocoActual { get; private set; }

        public bool HayRecetaCargada => GrafoActual != null && GrafoActual.CantidadVertices > 0;
        public bool SupervisionActiva => _supervisor.RecetaIniciada;
        public bool RecetaCompletada => _supervisor.RecetaCompletada();
        public int TiempoTotalSecuencial => _supervisor.TiempoTotalSecuencial;
        public int PendientesEnRonda => _supervisor.RondaSupervision.cantidad;

        public ServicioCocinaMaui()
        {
            _cargador = new CargadorRecetas();
            _supervisor = new SupervisorReceta();
            _detectorCiclos = new DetectorCicloDFS();
        }

        public string ObtenerCarpetaRecetas()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidataConsola = Path.Combine(dir.FullName, "Consola", "Recetas");
                if (Directory.Exists(candidataConsola))
                {
                    return candidataConsola;
                }

                string candidataRaiz = Path.Combine(dir.FullName, "Recetas");
                if (Directory.Exists(candidataRaiz) && File.Exists(Path.Combine(dir.FullName, "ChefRoute.slnx")))
                {
                    return candidataRaiz;
                }

                dir = dir.Parent;
            }

            string fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChefRoute",
                "Recetas");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        public static TipoEstacionCocina ClasificarEstacionDeTarea(Tarea tarea)
        {
            string texto = $"{tarea.Nombre} {tarea.Descripcion}".ToLowerInvariant();

            if (texto.Contains("emplat") || texto.Contains("servir") || texto.Contains("decorar") || texto.Contains("presentar"))
                return TipoEstacionCocina.Emplatado;

            if (texto.Contains("cortar") || texto.Contains("picar") || texto.Contains("pelar") || texto.Contains("rebanar") || texto.Contains("trocear"))
                return TipoEstacionCocina.Corte;

            if (texto.Contains("salsa") || texto.Contains("mezclar") || texto.Contains("licuar") || texto.Contains("batir") || texto.Contains("aderez") || texto.Contains("hogao"))
                return TipoEstacionCocina.Salsas;

            if (texto.Contains("cocin") || texto.Contains("freir") || texto.Contains("sofreir") || texto.Contains("hervir") || texto.Contains("hornear") || texto.Contains("asar") || texto.Contains("sellar") || texto.Contains("calentar"))
                return TipoEstacionCocina.Coccion;

            return TipoEstacionCocina.Preparacion;
        }

        public RecetaArchivoItem[] ObtenerCatalogoRecetas()
        {
            string carpeta = ObtenerCarpetaRecetas();
            Directory.CreateDirectory(carpeta);

            string[] archivos = Directory.GetFiles(carpeta, "*.json");
            RecetaArchivoItem[] resultado = new RecetaArchivoItem[archivos.Length];

            for (int i = 0; i < archivos.Length; i++)
            {
                string ruta = archivos[i];
                string nombreArchivo = Path.GetFileName(ruta);
                string nombreReceta = Path.GetFileNameWithoutExtension(ruta);
                int cantTareas = 0;
                int cantDeps = 0;
                int tiempoTotal = 0;
                bool tieneCiclo = false;

                try
                {
                    string json = File.ReadAllText(ruta);
                    RecetaDto? dto = JsonSerializer.Deserialize<RecetaDto>(json);
                    if (dto != null)
                    {
                        if (!string.IsNullOrWhiteSpace(dto.NombreReceta))
                            nombreReceta = dto.NombreReceta;

                        cantTareas = dto.Tareas?.Length ?? 0;
                        cantDeps = dto.Dependencias?.Length ?? 0;

                        if (dto.Tareas != null)
                        {
                            for (int t = 0; t < dto.Tareas.Length; t++)
                            {
                                tiempoTotal += dto.Tareas[t].TiempoEstimado;
                            }
                        }
                    }

                    GrafoReceta grafoTemp = _cargador.CargarReceta(ruta);
                    tieneCiclo = _detectorCiclos.TieneCiclo(grafoTemp);
                }
                catch
                {
                    tieneCiclo = true;
                }

                resultado[i] = new RecetaArchivoItem
                {
                    RutaCompleta = ruta,
                    NombreArchivo = nombreArchivo,
                    NombreReceta = nombreReceta,
                    CantidadTareas = cantTareas,
                    CantidadDependencias = cantDeps,
                    TiempoTotalMinutos = tiempoTotal,
                    TieneCiclo = tieneCiclo
                };
            }

            return resultado;
        }

        public void CargarRecetaDesdeArchivo(string rutaArchivo)
        {
            GrafoReceta grafo = _cargador.CargarReceta(rutaArchivo);
            string nombre = Path.GetFileNameWithoutExtension(rutaArchivo);

            try
            {
                string json = File.ReadAllText(rutaArchivo);
                RecetaDto? dto = JsonSerializer.Deserialize<RecetaDto>(json);
                if (dto != null && !string.IsNullOrWhiteSpace(dto.NombreReceta))
                {
                    nombre = dto.NombreReceta;
                }
            }
            catch { }

            GrafoActual = grafo;
            NombreRecetaActual = nombre;
            DescripcionRecetaActual = $"Flujo culinario con {grafo.CantidadVertices} estaciones de preparacion.";
            PlanOrdenadoActual = null;
            TareaEnFocoActual = null;
        }

        public void GuardarYActivarReceta(string nombreReceta, GrafoReceta grafo)
        {
            string carpeta = ObtenerCarpetaRecetas();
            Directory.CreateDirectory(carpeta);

            string nombreArchivo = nombreReceta.Trim().ToLower().Replace(" ", "_") + ".json";
            string ruta = Path.Combine(carpeta, nombreArchivo);

            _cargador.GuardarReceta(ruta, nombreReceta, grafo);

            GrafoActual = grafo;
            NombreRecetaActual = nombreReceta;
            DescripcionRecetaActual = $"Receta personalizada de {grafo.CantidadVertices} pasos.";
            PlanOrdenadoActual = null;
            TareaEnFocoActual = null;
        }

        public bool ValidarCicloActual()
        {
            if (GrafoActual == null) return false;
            return _detectorCiclos.TieneCiclo(GrafoActual);
        }

        public ListaSimple<Tarea> IniciarSupervisionEnCocina()
        {
            if (GrafoActual == null)
                throw new InvalidOperationException("Primero selecciona o crea una receta.");

            ListaSimple<Tarea> plan = _supervisor.PrepararReceta(GrafoActual);
            PlanOrdenadoActual = plan;
            TareaEnFocoActual = _supervisor.SiguienteTareaEnRonda();
            return plan;
        }

        public Tarea AvanzarNodoCircular()
        {
            TareaEnFocoActual = _supervisor.SiguienteTareaEnRonda();
            return TareaEnFocoActual;
        }

        public void CompletarTareaEnFoco()
        {
            if (TareaEnFocoActual == null)
                throw new InvalidOperationException("No hay ninguna tarea activa en supervision.");

            _supervisor.CompletarTarea(TareaEnFocoActual);

            if (_supervisor.RecetaCompletada())
            {
                TareaEnFocoActual = null;
            }
            else
            {
                TareaEnFocoActual = _supervisor.SiguienteTareaEnRonda();
            }
        }

        public string ObtenerDependenciasDeTarea(Tarea tarea)
        {
            if (GrafoActual == null) return "Ninguna";

            string texto = "";
            for (int i = 0; i < GrafoActual.CantidadVertices; i++)
            {
                ListaSimple<Arista> ady = GrafoActual.ObtenerAdyacentes(i);
                NodoLista<Arista>? nodo = ady.cabeza;
                while (nodo != null)
                {
                    if (nodo.Dato.Destino.Equals(tarea))
                    {
                        if (texto.Length > 0) texto += ", ";
                        texto += $"[{nodo.Dato.Origen.Id}] {nodo.Dato.Origen.Nombre}";
                    }
                    nodo = nodo.Siguiente;
                }
            }

            return texto.Length > 0 ? texto : "Ninguna (Inicio libre)";
        }

        public TareaTicketItem[] ObtenerItemsTicket()
        {
            if (GrafoActual == null) return Array.Empty<TareaTicketItem>();

            // If topological plan is active, list in Kahn execution order; otherwise vertex order
            if (PlanOrdenadoActual != null && !PlanOrdenadoActual.EsVacia())
            {
                TareaTicketItem[] items = new TareaTicketItem[PlanOrdenadoActual.cantidad];
                NodoLista<Tarea>? nodo = PlanOrdenadoActual.cabeza;
                int idx = 0;
                while (nodo != null)
                {
                    items[idx] = CrearTicketItem(nodo.Dato, idx + 1);
                    idx++;
                    nodo = nodo.Siguiente;
                }
                return items;
            }
            else
            {
                TareaTicketItem[] items = new TareaTicketItem[GrafoActual.CantidadVertices];
                for (int i = 0; i < GrafoActual.CantidadVertices; i++)
                {
                    items[i] = CrearTicketItem(GrafoActual.ObtenerTareaPorIndice(i), i + 1);
                }
                return items;
            }
        }

        private TareaTicketItem CrearTicketItem(Tarea tarea, int orden)
        {
            string deps = "";
            bool tienePrereqPendiente = false;

            if (GrafoActual != null)
            {
                for (int i = 0; i < GrafoActual.CantidadVertices; i++)
                {
                    ListaSimple<Arista> ady = GrafoActual.ObtenerAdyacentes(i);
                    NodoLista<Arista>? nodo = ady.cabeza;
                    while (nodo != null)
                    {
                        if (nodo.Dato.Destino.Equals(tarea))
                        {
                            if (deps.Length > 0) deps += ", ";
                            deps += nodo.Dato.Origen.Id;

                            if (_supervisor.RecetaIniciada && _supervisor.RondaSupervision.Contiene(nodo.Dato.Origen))
                            {
                                tienePrereqPendiente = true;
                            }
                        }
                        nodo = nodo.Siguiente;
                    }
                }
            }

            bool enRonda = _supervisor.RecetaIniciada && _supervisor.RondaSupervision.Contiene(tarea);
            bool completada = _supervisor.RecetaIniciada && !enRonda;
            bool esActual = TareaEnFocoActual != null && TareaEnFocoActual.Equals(tarea);

            EstadoTareaVisual estado;
            if (completada)
                estado = EstadoTareaVisual.Completada;
            else if (esActual)
                estado = EstadoTareaVisual.EnEjecucion;
            else if (tienePrereqPendiente)
                estado = EstadoTareaVisual.Bloqueada;
            else
                estado = EstadoTareaVisual.Pendiente;

            TipoEstacionCocina est = ClasificarEstacionDeTarea(tarea);

            return new TareaTicketItem
            {
                NumeroOrden = orden,
                TareaOriginal = tarea,
                Estacion = est,
                NombreEstacion = KitchenSceneDrawable.ObtenerNombreEstacion(est),
                DependenciasTexto = deps.Length > 0 ? $"Req: {deps}" : "Inicio libre",
                Estado = estado,
                EsTareaActual = esActual
            };
        }

        public (NodoGrafoVisual[] Nodos, AristaGrafoVisual[] Aristas) ConstruirGrafoVisual()
        {
            if (GrafoActual == null || GrafoActual.CantidadVertices == 0)
                return (Array.Empty<NodoGrafoVisual>(), Array.Empty<AristaGrafoVisual>());

            int n = GrafoActual.CantidadVertices;
            int[] gradosEntrada = new int[n];
            int[] niveles = new int[n];
            List<AristaGrafoVisual> listaAristas = new();

            for (int i = 0; i < n; i++)
            {
                ListaSimple<Arista> ady = GrafoActual.ObtenerAdyacentes(i);
                NodoLista<Arista>? nodo = ady.cabeza;
                while (nodo != null)
                {
                    int destIdx = -1;
                    for (int k = 0; k < n; k++)
                    {
                        if (GrafoActual.ObtenerTareaPorIndice(k).Equals(nodo.Dato.Destino))
                        {
                            destIdx = k;
                            break;
                        }
                    }

                    if (destIdx >= 0)
                    {
                        gradosEntrada[destIdx]++;
                        listaAristas.Add(new AristaGrafoVisual
                        {
                            IndiceOrigen = i,
                            IndiceDestino = destIdx,
                            PesoTiempo = nodo.Dato.Origen.TiempoEstimado
                        });
                    }
                    nodo = nodo.Siguiente;
                }
            }

            // Compute topological levels for clean left-to-right spatial layout
            for (int pass = 0; pass < n; pass++)
            {
                for (int a = 0; a < listaAristas.Count; a++)
                {
                    var ar = listaAristas[a];
                    if (niveles[ar.IndiceDestino] <= niveles[ar.IndiceOrigen])
                    {
                        niveles[ar.IndiceDestino] = Math.Min(n - 1, niveles[ar.IndiceOrigen] + 1);
                    }
                }
            }

            int maxNivel = 0;
            for (int i = 0; i < n; i++)
                if (niveles[i] > maxNivel) maxNivel = niveles[i];

            int[] conteoPorNivel = new int[maxNivel + 1];
            int[] usadosEnNivel = new int[maxNivel + 1];
            for (int i = 0; i < n; i++)
                conteoPorNivel[niveles[i]]++;

            NodoGrafoVisual[] nodos = new NodoGrafoVisual[n];
            for (int i = 0; i < n; i++)
            {
                Tarea t = GrafoActual.ObtenerTareaPorIndice(i);
                int niv = niveles[i];
                int fila = usadosEnNivel[niv]++;
                int totalEnCol = conteoPorNivel[niv];

                float normX = maxNivel == 0 ? 0.5f : 0.14f + (0.72f * niv / maxNivel);
                float normY = (fila + 1f) / (totalEnCol + 1f);

                int estadoAlgo = 0;
                if (_supervisor.RecetaIniciada)
                {
                    bool enRonda = _supervisor.RondaSupervision.Contiene(t);
                    if (!enRonda) estadoAlgo = 2;
                    else if (TareaEnFocoActual != null && TareaEnFocoActual.Equals(t)) estadoAlgo = 1;
                }

                nodos[i] = new NodoGrafoVisual
                {
                    Indice = i,
                    TareaOriginal = t,
                    Estacion = ClasificarEstacionDeTarea(t),
                    NivelTopologico = niv,
                    GradoEntradaActual = gradosEntrada[i],
                    NormX = normX,
                    NormY = normY,
                    EstadoAlgoritmo = estadoAlgo
                };
            }

            return (nodos, listaAristas.ToArray());
        }
    }
}
