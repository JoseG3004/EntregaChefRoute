using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Estructuras
{
    public class NodoLista<T>
    {
        public NodoLista<T>? Siguiente { get; set; }
        public T Dato { get; set; }

        public NodoLista(T dato) {
            this.Dato = dato;
            this.Siguiente = null;
        }
    }
}
