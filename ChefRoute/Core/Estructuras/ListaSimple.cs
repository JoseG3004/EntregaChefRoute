using System;
using System.Collections.Generic;
using System.Text;
namespace Core.Estructuras
{
    public class ListaSimple<T>
    {
        public NodoLista<T>? cabeza { get; private set; }
        public NodoLista<T>? ultimo { get; private set; }
        public int cantidad { get; private set; } = 0;

        public void Agregar(T dato)
        {
            NodoLista<T> nuevoNodo = new NodoLista<T>(dato);
            if (cantidad == 0)
            {
                this.cabeza = nuevoNodo;
                this.ultimo = nuevoNodo;
            }
            else
            {
                this.ultimo!.Siguiente = nuevoNodo;
                this.ultimo = nuevoNodo;
            }
            cantidad++;
        }
        public bool EsVacia() => cabeza == null || cantidad == 0; 

        public T EliminarInicio()
        {
            NodoLista<T> NodoEliminado = this.cabeza;

            if(EsVacia())
            {
                throw new InvalidOperationException("No se puede eliminar de una lista vacia");
            } else if(NodoEliminado == this.ultimo)
            {
                this.ultimo = null;
                this.cabeza = this.cabeza.Siguiente;
            }
            else
            {
                this.cabeza = this.cabeza.Siguiente;
            }
            cantidad--;
            return NodoEliminado.Dato;
        }

        public void Limpiar()
        {
            this.cabeza = null;
            this.ultimo = null;
            this.cantidad = 0; 
        }
    }
}
