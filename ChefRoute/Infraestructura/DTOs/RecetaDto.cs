using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
namespace Infraestructura.DTOs
{
    public class RecetaDto
    {
        public string NombreReceta { get; set; } = string.Empty;
        public TareaDto[] Tareas { get; set; } = Array.Empty<TareaDto>();
        public DependenciaDto[] Dependencias { get; set; } = Array.Empty<DependenciaDto>();
    }

    public class TareaDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int TiempoEstimado { get; set; }
    }

    public class DependenciaDto
    {
        public string IdOrigen { get; set; } = string.Empty;
        public string IdDestino { get; set; } = string.Empty;
    }
}
