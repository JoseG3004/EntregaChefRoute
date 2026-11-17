using Core.Modelos;

namespace ChefRoute.Maui.Models
{
    public enum TipoEstacionCocina
    {
        Preparacion,
        Corte,
        Coccion,
        Salsas,
        Emplatado
    }

    public enum EstadoAnimacionChef
    {
        Esperando,
        Caminando,
        Deteniendose,
        Supervisando,
        AlertaBloqueo,
        RecetaTerminada
    }

    public enum EstadoTareaVisual
    {
        Pendiente,
        EnEjecucion,
        Completada,
        Bloqueada
    }

    public class RecetaArchivoItem
    {
        public string RutaCompleta { get; set; } = string.Empty;
        public string NombreArchivo { get; set; } = string.Empty;
        public string NombreReceta { get; set; } = string.Empty;
        public int CantidadTareas { get; set; }
        public int CantidadDependencias { get; set; }
        public int TiempoTotalMinutos { get; set; }
        public bool TieneCiclo { get; set; }
    }

    public class TareaTicketItem
    {
        public int NumeroOrden { get; set; }
        public Tarea TareaOriginal { get; set; } = null!;
        public string Id => TareaOriginal.Id;
        public string Nombre => TareaOriginal.Nombre;
        public string Descripcion => TareaOriginal.Descripcion;
        public int TiempoEstimado => TareaOriginal.TiempoEstimado;
        public TipoEstacionCocina Estacion { get; set; }
        public string NombreEstacion { get; set; } = string.Empty;
        public string DependenciasTexto { get; set; } = "Sin dependencias";
        public EstadoTareaVisual Estado { get; set; } = EstadoTareaVisual.Pendiente;
        public bool EsTareaActual { get; set; }
    }

    public class NodoGrafoVisual
    {
        public int Indice { get; set; }
        public Tarea TareaOriginal { get; set; } = null!;
        public string Id => TareaOriginal.Id;
        public string Nombre => TareaOriginal.Nombre;
        public int Tiempo => TareaOriginal.TiempoEstimado;
        public TipoEstacionCocina Estacion { get; set; }
        public int NivelTopologico { get; set; }
        public int GradoEntradaActual { get; set; }
        public float NormX { get; set; }
        public float NormY { get; set; }
        // 0 = NoVisitado/Pendiente (Amarillo), 1 = EnProceso/Seleccionado (Azul), 2 = Visitado/Completado (Verde), 3 = Ciclo/Error (Rojo)
        public int EstadoAlgoritmo { get; set; }
    }

    public class AristaGrafoVisual
    {
        public int IndiceOrigen { get; set; }
        public int IndiceDestino { get; set; }
        public int PesoTiempo { get; set; }
        public bool Resaltada { get; set; }
        public bool EsCiclo { get; set; }
    }
}
