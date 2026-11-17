using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Algoritmos
{
    public class DetectorCicloDFS
    {
        public bool TieneCiclo(GrafoReceta grafo)
        {
            ArgumentNullException.ThrowIfNull(grafo);

            if (grafo.CantidadVertices == 0)
            {
                return false;
            }
            else
            {
                EstadoVertice[] estados = new EstadoVertice[grafo.CantidadVertices];

                for (int i = 0; i < grafo.CantidadVertices; i++)
                {
                    if (estados[i] == EstadoVertice.NoVisitado)
                    {
                        if (ExplorarDFS(grafo, i, estados))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        private bool ExplorarDFS(GrafoReceta grafo, int indiceActual, EstadoVertice[] estados)
        {
            estados[indiceActual] = EstadoVertice.EnProceso;
            ListaSimple<Arista> adyacentes = grafo.ObtenerAdyacentes(indiceActual);

            NodoLista<Arista>? nodoArista = adyacentes.cabeza;
            while (nodoArista != null)
            {
                int indiceVecino = grafo.ObtenerIndice(nodoArista.Dato.Destino.Id);

                if(estados[indiceVecino] == EstadoVertice.EnProceso) { return true; }

                if (estados[indiceVecino] == EstadoVertice.NoVisitado)
                {
                    if (ExplorarDFS(grafo, indiceVecino, estados))
                    {
                        return true;
                    }
                }
                nodoArista = nodoArista.Siguiente;
            }
            estados[indiceActual] = EstadoVertice.Finalizado;
            return false;
        }
    }
}
