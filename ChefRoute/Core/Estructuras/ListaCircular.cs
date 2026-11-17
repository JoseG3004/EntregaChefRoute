using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Core.Estructuras
{
    public class ListaCircular<T>
    {
        public NodoLista<T>? cabeza { get; private set; }
        public NodoLista<T>? ultimo { get; private set; }
        public NodoLista<T>? actual { get; private set; }
        public int cantidad { get; private set; } = 0;

        public void Agregar(T dato)
        {
            NodoLista<T> nuevoNodo = new NodoLista<T>(dato);
            if (cantidad == 0)
            {
                this.cabeza = nuevoNodo;
                this.ultimo = this.cabeza;
                this.actual = this.cabeza;
                nuevoNodo.Siguiente = this.cabeza;
            }
            else
            {
                this.ultimo!.Siguiente = nuevoNodo;
                this.ultimo = nuevoNodo;
                this.ultimo!.Siguiente = this.cabeza;
            }
            cantidad++;
        }

        public T Avanzar()
        {
            if (EsVacia())
            {
                throw new InvalidOperationException("No se puede avanzar en lista vacia");
            }
            else
            {
                T dato = actual!.Dato;
                this.actual = this.actual.Siguiente;
                return dato;
            }
        }

        public void Limpiar()
        {
            this.cabeza = null;
            this.ultimo = null;
            this.actual = null;
            this.cantidad = 0;
        }

        public bool Eliminar(T dato)
        {
            if (EsVacia())
            {
                return false;
            }
            else
            {
                NodoLista<T> anterior = this.ultimo!;
                NodoLista<T> recorrido = this.cabeza!;
                for (int i = 0; i < cantidad; i++)
                {
                    if(EqualityComparer<T>.Default.Equals(recorrido.Dato, dato))
                    {
                        if (cantidad == 1)
                        {
                            this.cabeza = null;
                            this.ultimo = null;
                            this.actual = null;
                        }else if(cantidad > 1)
                        {
                            if (this.actual == recorrido)
                            {
                                this.actual = recorrido.Siguiente;
                            }
                            if(recorrido == this.cabeza)
                            {
                                this.cabeza = this.cabeza!.Siguiente;
                                this.ultimo!.Siguiente = this.cabeza;
                            }
                            anterior.Siguiente = recorrido.Siguiente;
                            if(recorrido == this.ultimo)
                            {
                                    this.ultimo = anterior;
                            }
                        }
                        cantidad--;
                        return true;
                    }
                    anterior = recorrido;
                    recorrido = recorrido.Siguiente!;
                }
                return false;
            }
        }

        public bool Contiene(T dato)
        {
            if (EsVacia() || dato == null)
            {
                return false;
            }

            NodoLista<T> recorrido = actual!;
            for (int i = 0; i < cantidad; i++)
            {
                if (recorrido.Dato!.Equals(dato))
                {
                    return true;
                }
                recorrido = recorrido.Siguiente!;
            }

            return false;
        }

        public bool EsVacia() => cabeza == null || cantidad == 0;
    }
}