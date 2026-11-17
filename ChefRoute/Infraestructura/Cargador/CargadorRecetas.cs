using Core.Estructuras;
using Core.Grafos;
using Core.Modelos;
using Infraestructura.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infraestructura.Cargador
{
    public class CargadorRecetas
    {
        public void GuardarReceta(string rutaArchivo, string nombreReceta, GrafoReceta grafo)
        {
            if (string.IsNullOrWhiteSpace(rutaArchivo))
            {
                throw new ArgumentException("La ruta no puede ser nula o vacia");
            }

            if (string.IsNullOrWhiteSpace(nombreReceta))
            {
                throw new ArgumentException("el nombre no puede ser nulo o vacio");
            }

            ArgumentNullException.ThrowIfNull(grafo);

            TareaDto[] tareasDto = new TareaDto[grafo.CantidadVertices];
            int totalAristas = 0;
            for (int i = 0; i < grafo.CantidadVertices; i++)
            {
                totalAristas += grafo.ObtenerAdyacentes(i).cantidad;
            }
            DependenciaDto[] dependenciasDto = new DependenciaDto[totalAristas];

            int idxDep = 0;
            for (int i = 0; i < grafo.CantidadVertices; i++)
            {
                Tarea t = grafo.ObtenerTareaPorIndice(i);
                tareasDto[i] = new TareaDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Descripcion = t.Descripcion,
                    TiempoEstimado = t.TiempoEstimado
                };

                NodoLista<Arista>? nodoArista = grafo.ObtenerAdyacentes(i).cabeza;
                while (nodoArista != null)
                {
                    dependenciasDto[idxDep] = new DependenciaDto
                    {
                        IdOrigen = nodoArista.Dato.Origen.Id,
                        IdDestino = nodoArista.Dato.Destino.Id
                    };
                    idxDep++;
                    nodoArista = nodoArista.Siguiente;
                }
            }

            RecetaDto recetaDto = new RecetaDto
            {
                NombreReceta = nombreReceta,
                Tareas = tareasDto,
                Dependencias = dependenciasDto
            };

            JsonSerializerOptions opciones = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(recetaDto, opciones);
            File.WriteAllText(rutaArchivo, json);
        }

        public GrafoReceta CargarReceta(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo)) throw new FileNotFoundException("No se encontro el archivo");

            string json = File.ReadAllText(rutaArchivo);
            RecetaDto? dto = JsonSerializer.Deserialize<RecetaDto>(json);

            if (dto == null)
            {
                throw new InvalidDataException("El archivo JSON no tiene un formato de receta válido.");
            }

            GrafoReceta grafo = new GrafoReceta();
            for (int i = 0; i < dto.Tareas.Length; i++)
            {
                TareaDto tDto = dto.Tareas[i];
                Tarea nuevaTarea = new Tarea(tDto.Id, tDto.Nombre, tDto.Descripcion, tDto.TiempoEstimado);
                grafo.AgregarTarea(nuevaTarea);
            }

            for (int i = 0; i < dto.Dependencias.Length; i++)
            {
                DependenciaDto dDto = dto.Dependencias[i];
                grafo.AgregarDependencia(dDto.IdOrigen, dDto.IdDestino);
            }

            return grafo;
        }
    }
}
