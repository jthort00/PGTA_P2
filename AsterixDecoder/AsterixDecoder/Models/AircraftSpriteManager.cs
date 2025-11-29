using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AsterixDecoderApp
{
    /// <summary>
    /// Gestor de sprites para aeronaves usando imágenes direccionales 
    /// </summary>
    public class AircraftSpriteManager
    {
        // Tamaño objetivo (px) para los iconos en el mapa
        private const int TARGET_ICON_SIZE = 22;
        
        // Almacén de imágenes direccionales por categoría
        private readonly Dictionary<string, Bitmap> images = new(StringComparer.OrdinalIgnoreCase);

        public AircraftSpriteManager() { }

        /// <summary>
        /// Carga imágenes direccionales (0° y 180°) por categoría (blue/red)
        /// </summary>
        public void LoadDirectionalSprites(
            string yellow0Path, string yellow180Path,
            string blue0Path, string blue180Path,
            string red0Path, string red180Path)
        {
            images.Clear();
            TryLoad("yellow_0", yellow0Path);
            TryLoad("yellow_180", yellow180Path);
            TryLoad("blue_0", blue0Path);
            TryLoad("blue_180", blue180Path);
            TryLoad("red_0", red0Path);
            TryLoad("red_180", red180Path);
        }

        // Conservamos la firma previa por compatibilidad, pero ya no se usa.
        public void LoadSpriteSheets(string yellowPath, string bluePath, string redPath)
        {
            // Intencionalmente vacío: mantenido para compatibilidad.
        }

        private void TryLoad(string key, string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    images[key] = new Bitmap(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading '{key}' from '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el sprite de aeronave según categoría y rumbo.
        /// </summary>
        public Bitmap GetAircraftSprite(string callsign, string category, double heading)
        {
            string color = category switch
            {
                "CAT021" => "blue",   // ADS-B = azul
                "CAT048" => "red",    // Radar = rojo
                _ => "yellow"
            };

            // Normalizar rumbo a [0,360)
            double norm = heading % 360.0; if (norm < 0) norm += 360.0;
            
            //  - Si 90 < rumbo < 270 -> usar versión _180 y ROTAR RELATIVO a 180° (tomar 180° como 0)
            //  - Si 270 < rumbo < 360 o 0 < rumbo < 90 -> usar versión _0 sin cálculos extra
            bool use180 = (norm > 90.0 && norm < 270.0);
            string key = use180 ? $"{color}_180" : $"{color}_0";

            if (!images.TryGetValue(key, out var baseImg) || baseImg == null)
                return CreateFallbackSprite(category, heading);

            float angle = use180 ? (float)(norm - 180.0) : (float)norm;
            var rotated = RotateBitmap(baseImg, angle);
            return ScaleToBox(rotated, TARGET_ICON_SIZE);
        }

        private Bitmap RotateBitmap(Bitmap src, float angle)
        {
            if (src == null) return null;
            // Calcular caja contenedora de la rotación
            double rad = angle * Math.PI / 180.0;
            double cos = Math.Abs(Math.Cos(rad));
            double sin = Math.Abs(Math.Sin(rad));
            int w = src.Width; int h = src.Height;
            int newW = (int)Math.Round(w * cos + h * sin);
            int newH = (int)Math.Round(w * sin + h * cos);
            if (newW <= 0) newW = w; if (newH <= 0) newH = h;

            Bitmap dest = new Bitmap(newW, newH);
            dest.MakeTransparent();
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Centrar y rotar
                g.TranslateTransform(newW / 2f, newH / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-w / 2f, -h / 2f);
                g.DrawImage(src, new Rectangle(0, 0, w, h));
            }
            return dest;
        }

        private Bitmap ScaleToBox(Bitmap src, int target)
        {
            if (src == null) return null;
            if (target <= 0) return src;
            int w = src.Width;
            int h = src.Height;
            if (w <= 0 || h <= 0) return src;

            // Crear un lienzo cuadrado de tamaño fijo
            Bitmap canvas = new Bitmap(target, target);
            canvas.MakeTransparent();

            // Calcular escala para encajar dentro del cuadro con un pequeño padding
            int padding = 1; // píxeles de margen para evitar recortes
            float maxW = target - 2f * padding;
            float maxH = target - 2f * padding;
            float scale = Math.Min(maxW / w, maxH / h);
            int drawW = Math.Max(1, (int)Math.Round(w * scale));
            int drawH = Math.Max(1, (int)Math.Round(h * scale));

            // Centrar en el lienzo
            int dx = (target - drawW) / 2;
            int dy = (target - drawH) / 2;

            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(src, new Rectangle(dx, dy, drawW, drawH));
            }

            // Liberar el bitmap fuente si fue intermedio
            if (!object.ReferenceEquals(src, canvas))
            {
                src.Dispose();
            }

            return canvas;
        }

        /// <summary>
        /// Crea un sprite de respaldo si no hay imagen disponible
        /// </summary>
        private Bitmap CreateFallbackSprite(string category, double heading)
        {
            Color color = category switch
            {
                "CAT021" => Color.FromArgb(255, 0, 120, 215),      // Azul ADS-B
                "CAT048" => Color.FromArgb(255, 220, 0, 0),        // Rojo Radar
                _ => Color.Gray
            };

            var bmp = CreateAircraftBitmapFallback(color, heading);
            return ScaleToBox(bmp, TARGET_ICON_SIZE);
        }

        private Bitmap CreateAircraftBitmapFallback(Color color, double heading)
        {
            int size = 24;
            Bitmap bmp = new Bitmap(size, size);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                g.TranslateTransform(size / 2, size / 2);
                g.RotateTransform((float)heading);

                using (SolidBrush brush = new SolidBrush(color))
                using (Pen outline = new Pen(Color.White, 1.5f))
                {
                    Point[] fuselage = new Point[]
                    {
                        new Point(0, -10), new Point(-2, -2), new Point(-2, 6),
                        new Point(0, 8), new Point(2, 6), new Point(2, -2)
                    };
                    g.FillPolygon(brush, fuselage);
                    g.DrawPolygon(outline, fuselage);

                    Point[] wings = new Point[]
                    {
                        new Point(-8, 0), new Point(-2, -2), new Point(2, -2),
                        new Point(8, 0), new Point(2, 2), new Point(-2, 2)
                    };
                    g.FillPolygon(brush, wings);
                    g.DrawPolygon(outline, wings);
                }
            }

            return bmp;
        }
    }
}
