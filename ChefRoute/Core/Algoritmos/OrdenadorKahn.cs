using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Algoritmos
{
    public class OrdenadorKahn
    {
        public ListaSimple<Tarea> ObtenerOrdenTopologico(GrafoReceta grafo)
        {
            ArgumentNullException.ThrowIfNull(grafo);

            ListaSimple<Tarea> resultado = new ListaSimple<Tarea>();

            if (grafo.CantidadVertices == 0) { return resultado; }

            int[] gradosEntrada = new int[grafo.CantidadVertices];
            for (int u =0; u< grafo.CantidadVertices; u++)
            {
                ListaSimple<Arista> adyacentes = grafo.ObtenerAdyacentes(u);

                NodoLista<Arista>? nodoArista = adyacentes.cabeza;
                while (nodoArista != null)
                {
                    int v = grafo.ObtenerIndice(nodoArista.Dato.Destino.Id);
                    gradosEntrada[v]++;
                    nodoArista = nodoArista.Siguiente;
                }
            }

            ListaSimple<int> cola = new ListaSimple<int>();
            for(int i =0; i<grafo.CantidadVertices; i++)
            {
                if(gradosEntrada[i] == 0)
                {
                    cola.Agregar(i);
                }
            }

            while (!cola.EsVacia())
            {
                int u = cola.EliminarInicio();
                resultado.Agregar(grafo.ObtenerTareaPorIndice(u));
                ListaSimple<Arista> adyacentes = grafo.ObtenerAdyacentes(u);

                NodoLista<Arista>? nodoArista = adyacentes.cabeza;
                while (nodoArista != null)
                {
                    int v = grafo.ObtenerIndice(nodoArista.Dato.Destino.Id);
                    gradosEntrada[v]--;
                    if (gradosEntrada[v] == 0)
                    {
                        cola.Agregar(v);
                    }
                    nodoArista = nodoArista.Siguiente;
                }
            }
            if(resultado.cantidad != grafo.CantidadVertices) { throw new InvalidOperationException("No se puede generar un orden topológico porque la receta contiene un ciclo."); }
            return resultado;
        }
    }
}
