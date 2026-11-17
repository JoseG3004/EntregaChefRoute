using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Modelos
{
    public class Arista
    {
        public Tarea Origen { get; set; }
        public Tarea Destino { get; set; }
        public int Peso { get; set; }

        public Arista(Tarea origen,Tarea destino,int peso)
        {
            ArgumentNullException.ThrowIfNull(origen);
            ArgumentNullException.ThrowIfNull(destino);
            if (peso < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(peso), "El peso no puede ser menor a 0");
            }

            this.Origen = origen;
            this.Destino = destino;
            this.Peso = peso;
        }
    }
}
