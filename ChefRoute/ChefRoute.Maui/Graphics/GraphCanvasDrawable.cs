using ChefRoute.Maui.Models;
using Microsoft.Maui.Graphics;

namespace ChefRoute.Maui.Graphics
{
    public class GraphCanvasDrawable : IDrawable
    {
        public NodoGrafoVisual[] Nodos { get; set; } = Array.Empty<NodoGrafoVisual>();
        public AristaGrafoVisual[] Aristas { get; set; } = Array.Empty<AristaGrafoVisual>();
        public string TituloAlgoritmo { get; set; } = "Mapa de Dependencias (GrafoReceta)";

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float w = dirtyRect.Width;
            float h = dirtyRect.Height;
            if (w <= 20 || h <= 20) return;

            canvas.SaveState();

            // Subtle blueprint/culinary grid background
            canvas.FillColor = Color.FromArgb("#FAF7F0");
            canvas.FillRoundedRectangle(0, 0, w, h, 14);

            canvas.StrokeColor = Color.FromArgb("#E7E0D2");
            canvas.StrokeSize = 1f;
            for (float gx = 32; gx < w; gx += 32)
                canvas.DrawLine(gx, 0, gx, h);
            for (float gy = 32; gy < h; gy += 32)
                canvas.DrawLine(0, gy, w, gy);

            canvas.StrokeColor = Color.FromArgb("#D6CFC2");
            canvas.StrokeSize = 1.5f;
            canvas.DrawRoundedRectangle(0, 0, w, h, 14);

            if (Nodos.Length == 0)
            {
                canvas.FontColor = Color.FromArgb("#64748B");
                canvas.FontSize = 14f;
                canvas.DrawString("Selecciona o crea una receta para visualizar su grafo dirigido.",
                    0, 0, w, h, HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.RestoreState();
                return;
            }

            float nodeW = 148f;
            float nodeH = 66f;

            // 1. Draw Curved Directed Edges with Arrows and Weight Badges
            for (int i = 0; i < Aristas.Length; i++)
            {
                AristaGrafoVisual arista = Aristas[i];
                if (arista.IndiceOrigen < 0 || arista.IndiceOrigen >= Nodos.Length ||
                    arista.IndiceDestino < 0 || arista.IndiceDestino >= Nodos.Length)
                    continue;

                NodoGrafoVisual orig = Nodos[arista.IndiceOrigen];
                NodoGrafoVisual dest = Nodos[arista.IndiceDestino];

                float x1 = orig.NormX * w + nodeW / 2f;
                float y1 = orig.NormY * h;
                float x2 = dest.NormX * w - nodeW / 2f;
                float y2 = dest.NormY * h;

                // If backward/same-column edge, curve nicely
                float ctrlOffset = Math.Max(45f, Math.Abs(x2 - x1) * 0.45f);
                PathF path = new PathF();
                path.MoveTo(x1, y1);
                path.CurveTo(x1 + ctrlOffset, y1, x2 - ctrlOffset, y2, x2, y2);

                Color edgeColor = arista.EsCiclo
                    ? Color.FromArgb("#DC2626")
                    : (arista.Resaltada ? Color.FromArgb("#2563EB") : Color.FromArgb("#94A3B8"));

                canvas.StrokeColor = edgeColor;
                canvas.StrokeSize = (arista.Resaltada || arista.EsCiclo) ? 3f : 2f;
                canvas.DrawPath(path);

                // Arrowhead at (x2, y2)
                float angle = (float)Math.Atan2(y2 - (y2), x2 - (x2 - ctrlOffset));
                float arrowLen = 10f;
                PathF arrow = new PathF();
                arrow.MoveTo(x2, y2);
                arrow.LineTo(x2 - arrowLen * (float)Math.Cos(angle - 0.45f), y2 - arrowLen * (float)Math.Sin(angle - 0.45f));
                arrow.LineTo(x2 - arrowLen * (float)Math.Cos(angle + 0.45f), y2 - arrowLen * (float)Math.Sin(angle + 0.45f));
                arrow.Close();
                canvas.FillColor = edgeColor;
                canvas.FillPath(arrow);

                // Edge Weight Pill (Minutes)
                float midX = (x1 + x2) / 2f;
                float midY = (y1 + y2) / 2f - 10f;
                canvas.FillColor = Colors.White;
                canvas.FillRoundedRectangle(midX - 18, midY - 9, 36, 18, 6);
                canvas.StrokeColor = edgeColor;
                canvas.StrokeSize = 1f;
                canvas.DrawRoundedRectangle(midX - 18, midY - 9, 36, 18, 6);
                canvas.FontColor = Color.FromArgb("#334155");
                canvas.FontSize = 10f;
                canvas.DrawString($"{arista.PesoTiempo}m", midX - 18, midY - 9, 36, 18,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            // 2. Draw Station Nodes
            for (int i = 0; i < Nodos.Length; i++)
            {
                NodoGrafoVisual nodo = Nodos[i];
                float cx = nodo.NormX * w;
                float cy = nodo.NormY * h;
                float left = cx - nodeW / 2f;
                float top = cy - nodeH / 2f;

                Color borderColor = nodo.EstadoAlgoritmo switch
                {
                    1 => Color.FromArgb("#2563EB"), // Azul: En ejecucion / explorando
                    2 => Color.FromArgb("#16A34A"), // Verde: Completado / visitado
                    3 => Color.FromArgb("#DC2626"), // Rojo: Ciclo / bloqueado
                    _ => Color.FromArgb("#D97706")  // Amarillo: Pendiente
                };

                Color headerBg = nodo.EstadoAlgoritmo switch
                {
                    1 => Color.FromArgb("#DBEAFE"),
                    2 => Color.FromArgb("#DCFCE7"),
                    3 => Color.FromArgb("#FEE2E2"),
                    _ => Color.FromArgb("#FEF3C7")
                };

                // Shadow
                canvas.FillColor = Color.FromArgb("#201E293B");
                canvas.FillRoundedRectangle(left + 3, top + 4, nodeW, nodeH, 12);

                // Card body
                canvas.FillColor = Colors.White;
                canvas.FillRoundedRectangle(left, top, nodeW, nodeH, 12);

                // Top header band
                canvas.FillColor = headerBg;
                canvas.FillRoundedRectangle(left, top, nodeW, 24, 10);

                canvas.StrokeColor = borderColor;
                canvas.StrokeSize = nodo.EstadoAlgoritmo == 1 ? 2.8f : 1.8f;
                canvas.DrawRoundedRectangle(left, top, nodeW, nodeH, 12);

                // Task ID + In-Degree Badge
                canvas.FontColor = borderColor;
                canvas.FontSize = 11f;
                canvas.DrawString($"[{nodo.Id}]  Grado: {nodo.GradoEntradaActual}",
                    left + 8, top + 2, nodeW - 16, 20,
                    HorizontalAlignment.Left, VerticalAlignment.Center);

                // Task Name
                canvas.FontColor = Color.FromArgb("#1E293B");
                canvas.FontSize = 11.5f;
                string nombreCorto = nodo.Nombre.Length > 20 ? nodo.Nombre.Substring(0, 18) + "..." : nodo.Nombre;
                canvas.DrawString(nombreCorto,
                    left + 8, top + 26, nodeW - 16, 20,
                    HorizontalAlignment.Left, VerticalAlignment.Center);

                // Station & Time subtext
                canvas.FontColor = Color.FromArgb("#64748B");
                canvas.FontSize = 10f;
                canvas.DrawString($"{nodo.Estacion} • {nodo.Tiempo} min",
                    left + 8, top + 44, nodeW - 16, 18,
                    HorizontalAlignment.Left, VerticalAlignment.Center);
            }

            canvas.RestoreState();
        }
    }
}
