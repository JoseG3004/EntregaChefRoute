using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Modelos
{
    public class Tarea
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int TiempoEstimado { get; set; }

        public Tarea(string id,string nombre,string descripcion,int tiempo)
        {
            if(string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("No se puede asignar un id nulo o vacio");
            }
            else
            {
                this.Id = id;
            }

            if(string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("No se puede asignar un nombre nulo o vacio");
            }
            else
            {
                this.Nombre = nombre;
            }

            if(string.IsNullOrWhiteSpace(descripcion))
            {
                throw new ArgumentException("No se ouede asignar una descripcion nula o vaca");
            }
            else
            {
                this.Descripcion = descripcion;
            }
            if(tiempo < 0)
            {
                throw new ArgumentException("No se puede asignar un tiempo nulo o menor a 0");
            }
            else
            {
                this.TiempoEstimado = tiempo;
            }
        }
    }
}
