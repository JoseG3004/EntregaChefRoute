using ChefRoute.Maui.Models;
using Microsoft.Maui.Graphics;

namespace ChefRoute.Maui.Graphics
{
    public class KitchenSceneDrawable : IDrawable
    {
        // Normalized chef coordinates (0.0 to 1.0 of kitchen floor)
        public float ChefNormX { get; set; } = 0.50f;
        public float ChefNormY { get; set; } = 0.62f;
        public bool ChefMirandoDerecha { get; set; } = true;
        public float FaseCaminata { get; set; } = 0f;
        public float FaseVapor { get; set; } = 0f;
        public EstadoAnimacionChef EstadoChef { get; set; } = EstadoAnimacionChef.Esperando;
        public TipoEstacionCocina? EstacionActiva { get; set; } = null;
        public string EtiquetaChef { get; set; } = "Listo en cocina";

        // Station counts & completion state for visual indicators on counters
        public Dictionary<TipoEstacionCocina, int> TotalPorEstacion { get; } = new();
        public Dictionary<TipoEstacionCocina, int> CompletadasPorEstacion { get; } = new();

        public static (float X, float Y) ObtenerPosicionFrenteEstacion(TipoEstacionCocina estacion)
        {
            return estacion switch
            {
                TipoEstacionCocina.Preparacion => (0.20f, 0.44f),
                TipoEstacionCocina.Coccion     => (0.50f, 0.44f),
                TipoEstacionCocina.Salsas      => (0.80f, 0.44f),
                TipoEstacionCocina.Corte       => (0.26f, 0.72f),
                TipoEstacionCocina.Emplatado   => (0.74f, 0.72f),
                _ => (0.50f, 0.60f)
            };
        }

        public static string ObtenerNombreEstacion(TipoEstacionCocina estacion)
        {
            return estacion switch
            {
                TipoEstacionCocina.Preparacion => "Mesa de Preparacion",
                TipoEstacionCocina.Corte       => "Estacion de Corte",
                TipoEstacionCocina.Coccion     => "Estacion de Coccion",
                TipoEstacionCocina.Salsas      => "Estacion de Salsas",
                TipoEstacionCocina.Emplatado   => "Estacion de Emplatado",
                _ => "Estacion General"
            };
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            float w = dirtyRect.Width;
            float h = dirtyRect.Height;
            if (w <= 10 || h <= 10) return;

            canvas.SaveState();

            // 1. Kitchen Back Wall & Backsplash (Top 16% for elevated 2.5D simulation depth)
            float wallHeight = h * 0.16f;
            DibujarParedCocina(canvas, w, wallHeight);

            // 2. Checkered Tiled Kitchen Floor
            DibujarSueloBaldosas(canvas, 0, wallHeight, w, h - wallHeight);

            // 3. Draw Back-Row Stations (Preparacion, Coccion, Salsas)
            DibujarEstacionPreparacion(canvas, w * 0.18f, h * 0.27f, w * 0.22f, h * 0.17f);
            DibujarEstacionCoccion(canvas, w * 0.50f, h * 0.27f, w * 0.25f, h * 0.17f);
            DibujarEstacionSalsas(canvas, w * 0.82f, h * 0.27f, w * 0.22f, h * 0.17f);

            // 4. Draw Front-Row Stations & Chef with Y-sorting for natural 2.5D depth!
            float chefPx = w * ChefNormX;
            float chefPy = h * ChefNormY;
            float frontStationsY = h * 0.79f;

            if (chefPy < frontStationsY - 15f)
            {
                DibujarChef2D(canvas, chefPx, chefPy);
                DibujarEstacionCorte(canvas, w * 0.24f, frontStationsY, w * 0.24f, h * 0.17f);
                DibujarEstacionEmplatado(canvas, w * 0.76f, frontStationsY, w * 0.24f, h * 0.17f);
            }
            else
            {
                DibujarEstacionCorte(canvas, w * 0.24f, frontStationsY, w * 0.24f, h * 0.17f);
                DibujarEstacionEmplatado(canvas, w * 0.76f, frontStationsY, w * 0.24f, h * 0.17f);
                DibujarChef2D(canvas, chefPx, chefPy);
            }

            canvas.RestoreState();
        }

        private void DibujarParedCocina(ICanvas canvas, float w, float wallH)
        {
            // Warm ceramic wall
            canvas.FillColor = Color.FromArgb("#EFE9DC");
            canvas.FillRectangle(0, 0, w, wallH);

            // Subway tile lines on wall
            canvas.StrokeColor = Color.FromArgb("#DDD4C2");
            canvas.StrokeSize = 1f;
            float brickH = wallH / 3f;
            for (int r = 1; r < 3; r++)
            {
                canvas.DrawLine(0, r * brickH, w, r * brickH);
            }

            // Stainless steel utensil rail
            float railY = wallH * 0.45f;
            canvas.StrokeColor = Color.FromArgb("#94A3B8");
            canvas.StrokeSize = 3f;
            canvas.DrawLine(w * 0.08f, railY, w * 0.92f, railY);

            // Hanging copper pan & utensils
            float[] hooks = { 0.14f, 0.22f, 0.78f, 0.86f };
            for (int i = 0; i < hooks.Length; i++)
            {
                float hx = w * hooks[i];
                canvas.StrokeColor = Color.FromArgb("#64748B");
                canvas.StrokeSize = 2f;
                canvas.DrawLine(hx, railY, hx, railY + 14);

                if (i % 2 == 0)
                {
                    // Hanging copper pan
                    canvas.FillColor = Color.FromArgb("#D96B27");
                    canvas.FillCircle(hx, railY + 24, 11);
                    canvas.FillColor = Color.FromArgb("#B45309");
                    canvas.FillCircle(hx, railY + 24, 7);
                }
                else
                {
                    // Hanging spatula
                    canvas.FillColor = Color.FromArgb("#CBD5E1");
                    canvas.FillRoundedRectangle(hx - 4, railY + 14, 8, 16, 2);
                }
            }

            // Baseboard trim
            canvas.FillColor = Color.FromArgb("#C89666");
            canvas.FillRectangle(0, wallH - 6, w, 6);
        }

        private void DibujarSueloBaldosas(ICanvas canvas, float x, float y, float w, float h)
        {
            int cols = 16;
            int rows = 9;
            float tileW = w / cols;
            float tileH = h / rows;

            Color c1 = Color.FromArgb("#FAF7F0");
            Color c2 = Color.FromArgb("#EFE8DA");

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    canvas.FillColor = ((r + c) % 2 == 0) ? c1 : c2;
                    canvas.FillRectangle(x + c * tileW, y + r * tileH, tileW + 0.5f, tileH + 0.5f);
                }
            }

            // Subtle grout lines
            canvas.StrokeColor = Color.FromArgb("#E2D9C8");
            canvas.StrokeSize = 1f;
            for (int c = 1; c < cols; c++)
                canvas.DrawLine(x + c * tileW, y, x + c * tileW, y + h);
            for (int r = 1; r < rows; r++)
                canvas.DrawLine(x, y + r * tileH, x + w, y + r * tileH);
        }

        private void DibujarBaseMesa(ICanvas canvas, float cx, float cy, float w, float h, Color topColor, Color frontColor, TipoEstacionCocina tipo, string titulo)
        {
            float left = cx - w / 2f;
            float top = cy - h / 2f;
            bool esActiva = EstacionActiva == tipo;

            // Active glow ring under station
            if (esActiva)
            {
                canvas.FillColor = Color.FromArgb("#402563EB");
                canvas.FillRoundedRectangle(left - 8, top - 8, w + 16, h + 22, 18);
                canvas.StrokeColor = Color.FromArgb("#2563EB");
                canvas.StrokeSize = 2.5f;
                canvas.DrawRoundedRectangle(left - 6, top - 6, w + 12, h + 18, 16);
            }

            // Drop shadow
            canvas.FillColor = Color.FromArgb("#281E293B");
            canvas.FillRoundedRectangle(left + 4, top + 12, w, h, 12);

            // Counter front depth (3D isometric feel)
            canvas.FillColor = frontColor;
            canvas.FillRoundedRectangle(left, top + 10, w, h, 10);

            // Counter top surface
            canvas.FillColor = topColor;
            canvas.FillRoundedRectangle(left, top, w, h - 8, 10);
            canvas.StrokeColor = Color.FromArgb("#94A3B8");
            canvas.StrokeSize = 1.2f;
            canvas.DrawRoundedRectangle(left, top, w, h - 8, 10);

            // Station Nameplate on front edge
            float badgeW = Math.Min(w * 0.84f, 150f);
            float badgeH = 18f;
            float bx = cx - badgeW / 2f;
            float by = top + h - 8f;

            canvas.FillColor = esActiva ? Color.FromArgb("#2563EB") : Color.FromArgb("#1E293B");
            canvas.FillRoundedRectangle(bx, by, badgeW, badgeH, 6);

            canvas.FontColor = Colors.White;
            canvas.FontSize = 10f;
            canvas.DrawString(titulo, bx, by, badgeW, badgeH, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Status Indicator Dot on right corner of nameplate
            int total = TotalPorEstacion.TryGetValue(tipo, out int t) ? t : 0;
            int comp = CompletadasPorEstacion.TryGetValue(tipo, out int c) ? c : 0;
            if (total > 0)
            {
                Color dotColor = (comp >= total)
                    ? Color.FromArgb("#16A34A")
                    : (esActiva ? Color.FromArgb("#38BDF8") : Color.FromArgb("#F59E0B"));
                canvas.FillColor = dotColor;
                canvas.FillCircle(bx + badgeW - 10, by + badgeH / 2f, 4f);
            }
        }

        private void DibujarEstacionPreparacion(ICanvas canvas, float cx, float cy, float w, float h)
        {
            DibujarBaseMesa(canvas, cx, cy, w, h,
                Color.FromArgb("#E6CCB2"), Color.FromArgb("#B08968"),
                TipoEstacionCocina.Preparacion, "PREPARACION");

            float top = cy - h / 2f;
            // Mixing bowl
            canvas.FillColor = Color.FromArgb("#CBD5E1");
            canvas.FillCircle(cx - w * 0.22f, top + h * 0.38f, 15);
            canvas.FillColor = Color.FromArgb("#FEF3C7");
            canvas.FillCircle(cx - w * 0.22f, top + h * 0.38f, 11);

            // Fresh leafy greens
            canvas.FillColor = Color.FromArgb("#2A9D8F");
            canvas.FillCircle(cx + w * 0.04f, top + h * 0.35f, 10);
            canvas.FillColor = Color.FromArgb("#52B788");
            canvas.FillCircle(cx + w * 0.11f, top + h * 0.40f, 9);

            // Recipe clipboard / rolling pin
            canvas.FillColor = Color.FromArgb("#D4A373");
            canvas.FillRoundedRectangle(cx + w * 0.20f, top + h * 0.25f, 24, 8, 3);
        }

        private void DibujarEstacionCoccion(ICanvas canvas, float cx, float cy, float w, float h)
        {
            DibujarBaseMesa(canvas, cx, cy, w, h,
                Color.FromArgb("#E2E8F0"), Color.FromArgb("#94A3B8"),
                TipoEstacionCocina.Coccion, "COCCION");

            float top = cy - h / 2f;
            float leftBurnerX = cx - w * 0.20f;
            float rightBurnerX = cx + w * 0.20f;
            float burnerY = top + h * 0.38f;

            // Burners + glowing flame rings
            canvas.FillColor = Color.FromArgb("#334155");
            canvas.FillCircle(leftBurnerX, burnerY, 18);
            canvas.FillCircle(rightBurnerX, burnerY, 18);

            canvas.StrokeColor = Color.FromArgb("#F97316");
            canvas.StrokeSize = 2.5f;
            canvas.DrawCircle(leftBurnerX, burnerY, 15);
            canvas.DrawCircle(rightBurnerX, burnerY, 15);

            // Stockpot on left burner
            canvas.FillColor = Color.FromArgb("#64748B");
            canvas.FillCircle(leftBurnerX, burnerY, 13);
            canvas.FillColor = Color.FromArgb("#E07A5F");
            canvas.FillCircle(leftBurnerX, burnerY, 9);

            // Animated rising steam puffs
            float puffOffset = (float)Math.Sin(FaseVapor) * 4f;
            canvas.FillColor = Color.FromArgb("#99FFFFFF");
            canvas.FillCircle(leftBurnerX - 4 + puffOffset, burnerY - 16, 4.5f);
            canvas.FillCircle(leftBurnerX + 5 - puffOffset, burnerY - 23, 5.5f);

            // Frying pan on right burner
            canvas.FillColor = Color.FromArgb("#1E293B");
            canvas.FillCircle(rightBurnerX, burnerY, 13);
            canvas.FillRoundedRectangle(rightBurnerX + 10, burnerY - 2, 16, 4, 2);
            // Egg / sear inside pan
            canvas.FillColor = Color.FromArgb("#FBBF24");
            canvas.FillCircle(rightBurnerX - 2, burnerY, 5);
        }

        private void DibujarEstacionSalsas(ICanvas canvas, float cx, float cy, float w, float h)
        {
            DibujarBaseMesa(canvas, cx, cy, w, h,
                Color.FromArgb("#F8FAFC"), Color.FromArgb("#CBD5E1"),
                TipoEstacionCocina.Salsas, "SALSAS");

            float top = cy - h / 2f;
            // Sauce bowls (Red tomato, Green herb, Golden oil)
            canvas.FillColor = Color.FromArgb("#E63946");
            canvas.FillCircle(cx - w * 0.22f, top + h * 0.36f, 11);

            canvas.FillColor = Color.FromArgb("#2A9D8F");
            canvas.FillCircle(cx, top + h * 0.36f, 11);

            canvas.FillColor = Color.FromArgb("#F4A261");
            canvas.FillCircle(cx + w * 0.22f, top + h * 0.36f, 11);

            // Ladle / Spoon
            canvas.StrokeColor = Color.FromArgb("#64748B");
            canvas.StrokeSize = 2.5f;
            canvas.DrawLine(cx - w * 0.15f, top + h * 0.52f, cx + w * 0.15f, top + h * 0.52f);
        }

        private void DibujarEstacionCorte(ICanvas canvas, float cx, float cy, float w, float h)
        {
            DibujarBaseMesa(canvas, cx, cy, w, h,
                Color.FromArgb("#DDB892"), Color.FromArgb("#9C6644"),
                TipoEstacionCocina.Corte, "ESTACION DE CORTE");

            float top = cy - h / 2f;
            // Wooden cutting board
            canvas.FillColor = Color.FromArgb("#EDE0D4");
            canvas.FillRoundedRectangle(cx - w * 0.28f, top + h * 0.18f, w * 0.38f, h * 0.44f, 6);

            // Chopped tomato & carrot slices
            canvas.FillColor = Color.FromArgb("#E63946");
            canvas.FillCircle(cx - w * 0.14f, top + h * 0.35f, 6);
            canvas.FillColor = Color.FromArgb("#F97316");
            canvas.FillCircle(cx - w * 0.04f, top + h * 0.42f, 5);
            canvas.FillColor = Color.FromArgb("#22C55E");
            canvas.FillCircle(cx - w * 0.18f, top + h * 0.46f, 5);

            // Chef Knife
            canvas.FillColor = Color.FromArgb("#94A3B8");
            canvas.FillRoundedRectangle(cx + w * 0.14f, top + h * 0.26f, 22, 6, 2);
            canvas.FillColor = Color.FromArgb("#1E293B");
            canvas.FillRoundedRectangle(cx + w * 0.14f, top + h * 0.32f, 8, 12, 2);
        }

        private void DibujarEstacionEmplatado(ICanvas canvas, float cx, float cy, float w, float h)
        {
            DibujarBaseMesa(canvas, cx, cy, w, h,
                Color.FromArgb("#F1F5F9"), Color.FromArgb("#94A3B8"),
                TipoEstacionCocina.Emplatado, "EMPLATADO");

            float top = cy - h / 2f;
            // Warm pass-through heat lamp strip
            canvas.FillColor = Color.FromArgb("#40FBBF24");
            canvas.FillRoundedRectangle(cx - w * 0.42f, top + 4, w * 0.84f, h * 0.62f, 8);

            // Two porcelain plates
            float p1x = cx - w * 0.18f;
            float p2x = cx + w * 0.18f;
            float py = top + h * 0.38f;

            canvas.FillColor = Colors.White;
            canvas.FillCircle(p1x, py, 16);
            canvas.FillCircle(p2x, py, 16);
            canvas.StrokeColor = Color.FromArgb("#CBD5E1");
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(p1x, py, 11);
            canvas.DrawCircle(p2x, py, 11);

            // Plated food garnish
            canvas.FillColor = Color.FromArgb("#E07A5F");
            canvas.FillCircle(p1x, py, 6);
            canvas.FillColor = Color.FromArgb("#2A9D8F");
            canvas.FillCircle(p1x + 4, py - 3, 3.5f);
        }

        private void DibujarChef2D(ICanvas canvas, float cx, float cy)
        {
            bool caminando = EstadoChef == EstadoAnimacionChef.Caminando;
            float swing = caminando ? (float)Math.Sin(FaseCaminata) * 7f : 0f;
            float bob = caminando ? Math.Abs((float)Math.Sin(FaseCaminata)) * 3.5f : 0f;
            float supervWave = (EstadoChef == EstadoAnimacionChef.Supervisando) ? (float)Math.Sin(FaseVapor * 2f) * 3f : 0f;

            float baseY = cy - bob;

            // 1. Ground Shadow
            canvas.FillColor = Color.FromArgb("#380F172A");
            canvas.FillEllipse(cx - 18, cy + 20, 36, 10);

            // 2. Legs & Kitchen Clogs (Animated stride)
            canvas.FillColor = Color.FromArgb("#1E293B");
            // Left leg
            canvas.FillRoundedRectangle(cx - 10 + swing * 0.5f, baseY + 6, 7, 16, 3);
            // Right leg
            canvas.FillRoundedRectangle(cx + 3 - swing * 0.5f, baseY + 6, 7, 16, 3);

            // Shoes
            canvas.FillColor = Color.FromArgb("#0F172A");
            canvas.FillRoundedRectangle(cx - 12 + swing * 0.6f, baseY + 19, 10, 6, 3);
            canvas.FillRoundedRectangle(cx + 2 - swing * 0.6f, baseY + 19, 10, 6, 3);

            // 3. White Chef Coat (Torso)
            canvas.FillColor = Colors.White;
            canvas.FillRoundedRectangle(cx - 14, baseY - 18, 28, 27, 8);
            canvas.StrokeColor = Color.FromArgb("#CBD5E1");
            canvas.StrokeSize = 1.2f;
            canvas.DrawRoundedRectangle(cx - 14, baseY - 18, 28, 27, 8);

            // Terracotta / Culinary Apron
            canvas.FillColor = Color.FromArgb("#E07A5F");
            canvas.FillRoundedRectangle(cx - 11, baseY - 4, 22, 14, 4);

            // Double-breasted coat buttons
            canvas.FillColor = Color.FromArgb("#334155");
            canvas.FillCircle(cx - 3, baseY - 12, 1.6f);
            canvas.FillCircle(cx + 3, baseY - 12, 1.6f);
            canvas.FillCircle(cx - 3, baseY - 7, 1.6f);
            canvas.FillCircle(cx + 3, baseY - 7, 1.6f);

            // 4. Arms & Hands (Swinging when walking, inspecting when supervising)
            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Color.FromArgb("#CBD5E1");
            canvas.StrokeSize = 1f;
            // Left arm
            canvas.FillRoundedRectangle(cx - 20, baseY - 15 - swing * 0.5f, 6, 16, 3);
            canvas.DrawRoundedRectangle(cx - 20, baseY - 15 - swing * 0.5f, 6, 16, 3);
            // Right arm
            canvas.FillRoundedRectangle(cx + 14, baseY - 15 + swing * 0.5f - supervWave, 6, 16, 3);
            canvas.DrawRoundedRectangle(cx + 14, baseY - 15 + swing * 0.5f - supervWave, 6, 16, 3);

            // Hands
            canvas.FillColor = Color.FromArgb("#FDBA74");
            canvas.FillCircle(cx - 17, baseY + 2 - swing * 0.5f, 3.5f);
            canvas.FillCircle(cx + 17, baseY + 2 + swing * 0.5f - supervWave, 3.5f);

            // 5. Head & Expressive Face
            float headY = baseY - 29;
            canvas.FillColor = Color.FromArgb("#FDBA74");
            canvas.FillCircle(cx, headY, 11);

            // Eyes looking in movement direction
            float eyeOffsetX = ChefMirandoDerecha ? 2.5f : -2.5f;
            canvas.FillColor = Color.FromArgb("#1E293B");
            canvas.FillCircle(cx - 3.5f + eyeOffsetX, headY - 1, 1.8f);
            canvas.FillCircle(cx + 3.5f + eyeOffsetX, headY - 1, 1.8f);

            // Friendly smile or alert expression
            if (EstadoChef == EstadoAnimacionChef.AlertaBloqueo)
            {
                canvas.FillColor = Color.FromArgb("#DC2626");
                canvas.FillCircle(cx + eyeOffsetX, headY + 4, 2.5f);
            }
            else
            {
                canvas.StrokeColor = Color.FromArgb("#9A3412");
                canvas.StrokeSize = 1.5f;
                canvas.DrawArc(cx - 3 + eyeOffsetX, headY + 1, 6, 4, 180, 180, false, false);
            }

            // 6. Iconic Chef Toque (Gorro de Cocina)
            float hatBaseY = headY - 10;
            canvas.FillColor = Colors.White;
            canvas.StrokeColor = Color.FromArgb("#CBD5E1");
            canvas.StrokeSize = 1.2f;

            // Puffy top clouds of toque
            canvas.FillCircle(cx - 7, hatBaseY - 10, 7);
            canvas.FillCircle(cx + 7, hatBaseY - 10, 7);
            canvas.FillCircle(cx, hatBaseY - 13, 9);

            // Cylindrical hat band
            canvas.FillRoundedRectangle(cx - 10, hatBaseY - 9, 20, 11, 3);
            canvas.DrawRoundedRectangle(cx - 10, hatBaseY - 9, 20, 11, 3);

            // Subtle hat pleat lines
            canvas.DrawLine(cx - 4, hatBaseY - 9, cx - 4, hatBaseY - 2);
            canvas.DrawLine(cx + 4, hatBaseY - 9, cx + 4, hatBaseY - 2);

            // 7. Floating Chef Status Pill above Toque
            Color pillColor = EstadoChef switch
            {
                EstadoAnimacionChef.Caminando       => Color.FromArgb("#2563EB"),
                EstadoAnimacionChef.Supervisando    => Color.FromArgb("#0D9488"),
                EstadoAnimacionChef.AlertaBloqueo   => Color.FromArgb("#DC2626"),
                EstadoAnimacionChef.RecetaTerminada => Color.FromArgb("#16A34A"),
                _ => Color.FromArgb("#334155")
            };

            float pillW = 128f;
            float pillH = 20f;
            float pillX = cx - pillW / 2f;
            float pillY = hatBaseY - 42f;

            canvas.FillColor = pillColor;
            canvas.FillRoundedRectangle(pillX, pillY, pillW, pillH, 10);
            canvas.FontColor = Colors.White;
            canvas.FontSize = 10f;
            canvas.DrawString(EtiquetaChef, pillX, pillY, pillW, pillH, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
