using Core.Algoritmos;
using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using System;

namespace Core.Servicios
{
    public class SupervisorReceta
    {
        private readonly DetectorCicloDFS detectorCiclos;
        private readonly OrdenadorKahn ordenadorKahn;
        public ListaCircular<Tarea> RondaSupervision { get; private set; }
        public int TiempoTotalSecuencial { get; private set; }
        public bool RecetaIniciada { get; private set; }
        private GrafoReceta? grafoActivo;

        public SupervisorReceta()
        {
            this.detectorCiclos = new DetectorCicloDFS();
            this.ordenadorKahn = new OrdenadorKahn();
            this.RondaSupervision = new ListaCircular<Tarea>();
        }

        public ListaSimple<Tarea> PrepararReceta(GrafoReceta grafo)
        {
            ArgumentNullException.ThrowIfNull(grafo);

            if (grafo.CantidadVertices == 0)
            {
                throw new InvalidOperationException("La receta no tiene tareas registradas.");
            }

            if (detectorCiclos.TieneCiclo(grafo) == true)
            {
                throw new InvalidOperationException("La receta contiene dependencias circulares y no puede ejecutarse.");
            }

            ListaSimple<Tarea> planOrdenado = ordenadorKahn.ObtenerOrdenTopologico(grafo);

            this.grafoActivo = grafo;
            RondaSupervision.Limpiar();
            TiempoTotalSecuencial = 0;

            NodoLista<Tarea>? actual = planOrdenado.cabeza;
            while (actual != null)
            {
                RondaSupervision.Agregar(actual.Dato);
                TiempoTotalSecuencial += actual.Dato.TiempoEstimado;
                actual = actual.Siguiente;
            }

            RecetaIniciada = true;
            return planOrdenado;
        }

        public Tarea SiguienteTareaEnRonda()
        {
            if (!RecetaIniciada || RondaSupervision.EsVacia())
            {
                throw new InvalidOperationException("No hay tareas pendientes en la ronda de supervisión.");
            }
            return RondaSupervision.Avanzar();
        }

        public bool CompletarTarea(Tarea tarea)
        {
            ArgumentNullException.ThrowIfNull(tarea);

            if (!RecetaIniciada || grafoActivo == null)
            {
                throw new InvalidOperationException("No hay ninguna receta activa en supervisión.");
            }

            ListaSimple<Tarea> pendientes = new ListaSimple<Tarea>();

            for (int i = 0; i < grafoActivo.CantidadVertices; i++)
            {
                ListaSimple<Arista> adyacentes = grafoActivo.ObtenerAdyacentes(i);
                NodoLista<Arista>? nodoArista = adyacentes.cabeza;

                while (nodoArista != null)
                {
                    if (nodoArista.Dato.Destino.Equals(tarea))
                    {
                        Tarea prerrequisito = nodoArista.Dato.Origen;
                        if (RondaSupervision.Contiene(prerrequisito))
                        {
                            pendientes.Agregar(prerrequisito);
                        }
                    }
                    nodoArista = nodoArista.Siguiente;
                }
            }

            if (!pendientes.EsVacia())
            {
                string listaFaltantes = "";
                NodoLista<Tarea>? actual = pendientes.cabeza;

                while (actual != null)
                {
                    listaFaltantes += $"[{actual.Dato.Id}] {actual.Dato.Nombre}";
                    if (actual.Siguiente != null)
                    {
                        listaFaltantes += ", ";
                    }
                    actual = actual.Siguiente;
                }

                throw new InvalidOperationException(
                    $"No puedes completar '{tarea.Nombre}' [{tarea.Id}] porque aún están pendientes sus prerrequisitos: {listaFaltantes}.");
            }

            return RondaSupervision.Eliminar(tarea);
        }

        public bool RecetaCompletada()
        {
            return RecetaIniciada && RondaSupervision.EsVacia();
        }
    }
}