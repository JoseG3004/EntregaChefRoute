using Core.Estructuras;
using Core.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Grafos
{
    public class GrafoReceta
    {
        private Tarea[] vertices;
        private ListaSimple<Arista>[] adyacencias;
        public int CantidadVertices { get; private set; } = 0;
        public int Capacidad { get; private set; }

        public GrafoReceta(int capacidadInicial = 10)
        {
            if (capacidadInicial <= 0) throw new ArgumentOutOfRangeException(nameof(capacidadInicial));
            this.Capacidad = capacidadInicial;
            this.vertices = new Tarea[Capacidad];
            this.adyacencias = new ListaSimple<Arista>[Capacidad];
        }

        private void Redimensionar()
        {
            Tarea[] verticesRedimension = new Tarea[Capacidad*2];
            ListaSimple<Arista>[] adyacenciasRedimension = new ListaSimple<Arista>[Capacidad * 2];

            for(int i = 0; i < CantidadVertices; i++)
            {
                verticesRedimension[i] = this.vertices[i];
                adyacenciasRedimension[i] = this.adyacencias[i];
            }
            this.vertices = verticesRedimension;
            this.adyacencias = adyacenciasRedimension;
            this.Capacidad = Capacidad * 2;
        }

        public int ObtenerIndice(string idTarea)
        {
            for (int i = 0; i < CantidadVertices; i++)
            {
                if (string.Equals(vertices[i].Id,idTarea,StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
                else { continue; }
            }
            return -1;
        }

        public void AgregarTarea(Tarea tarea)
        {
            ArgumentNullException.ThrowIfNull(tarea);
            if (ObtenerIndice(tarea.Id) == -1)
            {
                if (CantidadVertices == Capacidad)
                {
                    Redimensionar();
                }
                vertices[CantidadVertices] = tarea;
                adyacencias[CantidadVertices] = new ListaSimple<Arista>();
                CantidadVertices++;
            } else
            {
                throw new InvalidOperationException("No puede haber tareas duplicadas");
            }
        }

        public void AgregarDependencia(string idOrigen, string idDestino)
        {
            int idxOrigen = ObtenerIndice(idOrigen);
            int idxDestino = ObtenerIndice(idDestino);

            if(idxOrigen == -1 || idxDestino == -1)
            {
                throw new ArgumentException("Una o ambas tareas no existen en el grafo.");
            }

            NodoLista<Arista>? actual = adyacencias[idxOrigen].cabeza;
            while(actual != null)
            {
                if (string.Equals(actual.Dato.Destino.Id, idDestino, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"La dependencia de '{idOrigen}' hacia '{idDestino}' ya existe.");
                }
                actual = actual.Siguiente;
            }
            Arista nuevaArista = new Arista(vertices[idxOrigen], vertices[idxDestino], vertices[idxOrigen].TiempoEstimado);
            adyacencias[idxOrigen].Agregar(nuevaArista);
        }

        public Tarea ObtenerTareaPorIndice(int indice)
        {
            if (indice < 0 || indice >= CantidadVertices) throw new ArgumentOutOfRangeException(nameof(indice));
            return vertices[indice];
        }

        public ListaSimple<Arista> ObtenerAdyacentes(int indice)
        {
            if(0 <= indice && indice < CantidadVertices) { return adyacencias[indice]; } else { throw new ArgumentOutOfRangeException(nameof(indice)); }
        }
    }
}
