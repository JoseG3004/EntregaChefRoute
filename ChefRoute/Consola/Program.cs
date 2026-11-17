using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using Core.Servicios;
using Infraestructura.Cargador;
using System;
using System.IO;

namespace Consola
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CargadorRecetas cargador = new CargadorRecetas();
            SupervisorReceta supervisor = new SupervisorReceta();

            GrafoReceta? grafoActual = null;
            string nombreRecetaActual = "Ninguna";

            string carpetaRecetas = ObtenerCarpetaRecetasProyecto();
            Directory.CreateDirectory(carpetaRecetas);

            bool salir = false;

            while (!salir)
            {
                Console.Clear();
                DibujarEncabezado(nombreRecetaActual, grafoActual);

                Console.WriteLine("  [1] Cargar receta desde archivo JSON");
                Console.WriteLine("  [2] Crear nueva receta manualmente y guardar en JSON");
                Console.WriteLine("  [3] Ver estructura actual del grafo (Tareas y Dependencias)");
                Console.WriteLine("  [4] Validar (DFS), Ordenar (Kahn) e Iniciar Supervision en Cocina");
                Console.WriteLine("  [0] Salir");
                Console.WriteLine("=================================================================");
                Console.Write("Selecciona una opcion: ");

                string? opcion = Console.ReadLine();

                try
                {
                    switch (opcion)
                    {
                        case "1":
                            CargarDesdeJson(cargador, carpetaRecetas, ref grafoActual, ref nombreRecetaActual);
                            break;

                        case "2":
                            CrearRecetaManual(cargador, carpetaRecetas, ref grafoActual, ref nombreRecetaActual);
                            break;

                        case "3":
                            MostrarEstructuraGrafo(grafoActual, nombreRecetaActual);
                            break;

                        case "4":
                            EjecutarFlujoSupervision(supervisor, grafoActual, nombreRecetaActual);
                            break;

                        case "0":
                            salir = true;
                            Console.WriteLine("\nCerrando ChefRoute. Buen servicio en cocina!");
                            break;

                        default:
                            MostrarMensajeError("Opcion no valida. Intenta nuevamente.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Captura limpia de excepciones de dominio (ciclos DFS, archivos no encontrados, datos invalidos)
                    MostrarMensajeError(ex.Message);
                }
            }
        }

        private static void DibujarEncabezado(string nombreReceta, GrafoReceta? grafo)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=================================================================");
            Console.WriteLine("              CHEFROUTE - GESTOR DE FLUJO DE RECETAS             ");
            Console.WriteLine("=================================================================");
            Console.ResetColor();

            string infoTareas = grafo != null ? $"{grafo.CantidadVertices} tareas registradas" : "Sin cargar";
            Console.WriteLine($"  Receta activa : {nombreReceta} ({infoTareas})");
            Console.WriteLine("-----------------------------------------------------------------");
        }

        private static void CargarDesdeJson(
            CargadorRecetas cargador,
            string carpetaRecetas,
            ref GrafoReceta? grafoActual,
            ref string nombreRecetaActual)
        {
            Console.WriteLine("\n--- ARCHIVOS DISPONIBLES EN /Recetas ---");
            string[] archivos = Directory.GetFiles(carpetaRecetas, "*.json");

            if (archivos.Length == 0)
            {
                Console.WriteLine("No hay archivos .json en la carpeta Recetas.");
                Pausar();
                return;
            }

            for (int i = 0; i < archivos.Length; i++)
            {
                Console.WriteLine($"  [{i + 1}] {Path.GetFileName(archivos[i])}");
            }

            Console.Write("\nIngresa el numero del archivo (o escribe el nombre completo): ");
            string? entrada = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(entrada))
            {
                throw new ArgumentException("Debes indicar un archivo valido.");
            }

            string rutaSeleccionada;
            if (int.TryParse(entrada, out int indice) && indice >= 1 && indice <= archivos.Length)
            {
                rutaSeleccionada = archivos[indice - 1];
            }
            else
            {
                string nombreArchivo = entrada.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    ? entrada
                    : entrada + ".json";
                rutaSeleccionada = Path.Combine(carpetaRecetas, nombreArchivo);
            }

            GrafoReceta grafoCargado = cargador.CargarReceta(rutaSeleccionada);
            grafoActual = grafoCargado;
            nombreRecetaActual = Path.GetFileNameWithoutExtension(rutaSeleccionada);

            MostrarMensajeExito($"Receta '{nombreRecetaActual}' cargada con {grafoActual.CantidadVertices} tareas.");
        }

        private static void CrearRecetaManual(
    CargadorRecetas cargador,
    string carpetaRecetas,
    ref GrafoReceta? grafoActual,
    ref string nombreRecetaActual)
        {
            Console.WriteLine("\n--- CREACION FLEXIBLE DE RECETA ---");
            Console.Write("Nombre de la receta: ");
            string? nombreReceta = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(nombreReceta))
            {
                throw new ArgumentException("El nombre de la receta no puede estar vacio.");
            }

            GrafoReceta nuevoGrafo = new GrafoReceta();
            int contadorTareas = 1;
            bool agregandoTareas = true;

            Console.WriteLine("\nAgrega las tareas una por una (el ID se asigna automaticamente):");
            while (agregandoTareas)
            {
                string idAuto = $"T{contadorTareas}";
                Console.WriteLine($"\n--- Tarea [{idAuto}] ---");

                Console.Write("  Nombre de la tarea: ");
                string nombre = Console.ReadLine() ?? string.Empty;

                Console.Write("  Descripcion breve: ");
                string descripcion = Console.ReadLine() ?? string.Empty;

                Console.Write("  Tiempo estimado (minutos): ");
                if (!int.TryParse(Console.ReadLine(), out int tiempo) || tiempo < 0)
                {
                    MostrarMensajeError("Tiempo no valido. Intenta ingresar esta tarea de nuevo.");
                    continue;
                }

                try
                {
                    Tarea tarea = new Tarea(idAuto, nombre, descripcion, tiempo);
                    nuevoGrafo.AgregarTarea(tarea);
                    contadorTareas++;
                }
                catch (Exception ex)
                {
                    MostrarMensajeError(ex.Message);
                    continue;
                }

                Console.Write("Deseas agregar otra tarea? (S/N): ");
                string? resp = Console.ReadLine();
                if (!string.Equals(resp, "S", StringComparison.OrdinalIgnoreCase))
                {
                    agregandoTareas = false;
                }
            }

            if (nuevoGrafo.CantidadVertices == 0)
            {
                throw new InvalidOperationException("No se registro ninguna tarea para la receta.");
            }

            // Mostrar resumen de tareas creadas para facilitar la conexion de dependencias
            Console.WriteLine("\n--- DEFINIR DEPENDENCIAS (ORDEN DE PREPARACION) ---");
            Console.WriteLine("Tareas disponibles:");
            for (int i = 0; i < nuevoGrafo.CantidadVertices; i++)
            {
                Tarea t = nuevoGrafo.ObtenerTareaPorIndice(i);
                Console.WriteLine($"  [{t.Id}] {t.Nombre}");
            }

            Console.WriteLine("\nIndica que tarea debe hacerse ANTES que otra (Origen -> Destino).");
            Console.WriteLine("Escribe 'FIN' en el origen cuando hayas terminado de conectar pasos.");

            while (true)
            {
                Console.Write("\n  ID Tarea Previa (Origen) o 'FIN': ");
                string idOrigen = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();

                if (idOrigen == "FIN" || string.IsNullOrEmpty(idOrigen))
                {
                    break;
                }

                Console.Write($"  ID Tarea que depende de {idOrigen} (Destino): ");
                string idDestino = (Console.ReadLine() ?? string.Empty).Trim().ToUpper();

                try
                {
                    nuevoGrafo.AgregarDependencia(idOrigen, idDestino);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  -> Dependencia agregada: {idOrigen} antes que {idDestino}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    // Si el usuario se equivoca de ID, no pierde toda la receta: solo reintenta esa arista
                    MostrarMensajeError(ex.Message);
                }
            }

            string nombreArchivo = nombreReceta.Trim().ToLower().Replace(" ", "_") + ".json";
            string rutaGuardado = Path.Combine(carpetaRecetas, nombreArchivo);

            cargador.GuardarReceta(rutaGuardado, nombreReceta, nuevoGrafo);

            grafoActual = nuevoGrafo;
            nombreRecetaActual = nombreReceta;

            MostrarMensajeExito($"Receta '{nombreReceta}' creada con {nuevoGrafo.CantidadVertices} tareas y guardada en JSON.");
        }

        private static void MostrarEstructuraGrafo(GrafoReceta? grafo, string nombreReceta)
        {
            if (grafo == null || grafo.CantidadVertices == 0)
            {
                throw new InvalidOperationException("No hay ninguna receta cargada para inspeccionar.");
            }

            Console.WriteLine($"\n--- ESTRUCTURA DEL GRAFO: {nombreReceta.ToUpper()} ---");
            for (int i = 0; i < grafo.CantidadVertices; i++)
            {
                Tarea t = grafo.ObtenerTareaPorIndice(i);
                Console.Write($"  [{t.Id}] {t.Nombre} ({t.TiempoEstimado} min)  -->  Dependientes: ");

                ListaSimple<Arista> adyacentes = grafo.ObtenerAdyacentes(i);
                if (adyacentes.EsVacia())
                {
                    Console.WriteLine("Ninguna (Tarea final)");
                }
                else
                {
                    NodoLista<Arista>? actual = adyacentes.cabeza;
                    while (actual != null)
                    {
                        Console.Write($"{actual.Dato.Destino.Id}");
                        if (actual.Siguiente != null) Console.Write(", ");
                        actual = actual.Siguiente;
                    }
                    Console.WriteLine();
                }
            }

            Pausar();
        }

        private static void EjecutarFlujoSupervision(
            SupervisorReceta supervisor,
            GrafoReceta? grafo,
            string nombreReceta)
        {
            if (grafo == null)
            {
                throw new InvalidOperationException("Primero debes cargar o crear una receta.");
            }

            Console.WriteLine("\n[Paso 1] Ejecutando Detector de Ciclos DFS (3 estados)...");
            Console.WriteLine("[Paso 2] Calculando Orden Topologico (Algoritmo de Kahn)...");

            // Si hay ciclo, PrepararReceta lanzara InvalidOperationException y saltara al catch del Main
            ListaSimple<Tarea> planOrdenado = supervisor.PrepararReceta(grafo);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Validacion exitosa: El grafo es un DAG (sin dependencias circulares).");
            Console.ResetColor();

            Console.WriteLine($"\n--- PLAN DE EJECUCION SECUENCIAL ({nombreReceta}) ---");
            int paso = 1;
            NodoLista<Tarea>? nodoPlan = planOrdenado.cabeza;
            while (nodoPlan != null)
            {
                Tarea t = nodoPlan.Dato;
                Console.WriteLine($"  {paso}. [{t.Id}] {t.Nombre} - {t.TiempoEstimado} min ({t.Descripcion})");
                paso++;
                nodoPlan = nodoPlan.Siguiente;
            }

            Console.WriteLine($"\nTiempo Total Estimado (Secuencial): {supervisor.TiempoTotalSecuencial} minutos.");
            Console.Write("\nDeseas iniciar la Ronda Circular de Supervision en Cocina? (S/N): ");
            string? iniciar = Console.ReadLine();

            if (!string.Equals(iniciar, "S", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SubmenuRondaCircular(supervisor, nombreReceta);
        }

        private static void SubmenuRondaCircular(SupervisorReceta supervisor, string nombreReceta)
        {
            Tarea tareaEnFoco = supervisor.SiguienteTareaEnRonda();

            while (!supervisor.RecetaCompletada())
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("=================================================================");
                Console.WriteLine($"        RONDA DE SUPERVISION EN COCINA: {nombreReceta.ToUpper()}");
                Console.WriteLine("=================================================================");
                Console.ResetColor();

                Console.WriteLine($"  Tareas pendientes en ronda : {supervisor.RondaSupervision.cantidad}");
                Console.WriteLine("-----------------------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  TAREA ACTUAL EN ESTACION   : [{tareaEnFoco.Id}] {tareaEnFoco.Nombre}");
                Console.WriteLine($"  Instruccion                : {tareaEnFoco.Descripcion}");
                Console.WriteLine($"  Tiempo estimado            : {tareaEnFoco.TiempoEstimado} minutos");
                Console.ResetColor();
                Console.WriteLine("-----------------------------------------------------------------");
                Console.WriteLine("  [1] Avanzar al siguiente puesto en la ronda circular");
                Console.WriteLine("  [2] Marcar tarea actual como COMPLETADA");
                Console.WriteLine("  [0] Volver al menu principal");
                Console.Write("\nAccion del Chef: ");

                string? accion = Console.ReadLine();

                if (accion == "1")
                {
                    tareaEnFoco = supervisor.SiguienteTareaEnRonda();
                }
                else if (accion == "2")
                {
                    try
                    {
                        string idCompletado = tareaEnFoco.Id;
                        supervisor.CompletarTarea(tareaEnFoco);

                        if (supervisor.RecetaCompletada())
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\nTarea [{idCompletado}] completada.");
                            Console.WriteLine("FELICIDADES! Todas las tareas de la receta han sido completadas.");
                            Console.ResetColor();
                            Pausar();
                            return;
                        }

                        tareaEnFoco = supervisor.SiguienteTareaEnRonda();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MostrarMensajeError(ex.Message);
                    }
                }
                else if (accion == "0")
                {
                    return;
                }
            }
        }

        private static void MostrarMensajeExito(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[OK] {mensaje}");
            Console.ResetColor();
            Pausar();
        }

        private static void MostrarMensajeError(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] {mensaje}");
            Console.ResetColor();
            Pausar();
        }

        private static void Pausar()
        {
            Console.Write("\nPresiona ENTER para continuar...");
            Console.ReadLine();
        }

        private static string ObtenerCarpetaRecetasProyecto()
        {
            // AppContext.BaseDirectory apunta a .../Consola/bin/Debug/net10.0/
            DirectoryInfo? dirBase = new DirectoryInfo(AppContext.BaseDirectory);

            // Subimos 3 niveles (net10.0 -> Debug -> bin -> Consola)
            DirectoryInfo? raizProyecto = dirBase.Parent?.Parent?.Parent;

            // Si estamos ejecutando desde Visual Studio / dotnet run, usamos Consola/Recetas
            if (raizProyecto != null && File.Exists(Path.Combine(raizProyecto.FullName, "Consola.csproj")))
            {
                return Path.Combine(raizProyecto.FullName, "Recetas");
            }

            // Respaldo por si se ejecuta un binario publicado independiente
            return Path.Combine(AppContext.BaseDirectory, "Recetas");
        }
    }
}