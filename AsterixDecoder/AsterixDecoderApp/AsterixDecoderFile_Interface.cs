using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using AsterixDecoder.Models.CAT048;
using AsterixDecoder.Models.CAT021;
using AsterixDecoder.Models;
using AsterixDecoder.IO;
using MultiCAT6.Utils;

namespace AsterixDecoderApp
{
    public partial class AsterixDecoderFile_Interface : Form
    {
        // Coordenadas del Radar BCN
        private static readonly CoordinatesWGS84 RadarBCN = new CoordinatesWGS84(
            GeoUtils.LatLon2Radians(41, 18, 2.5284, 0),
            GeoUtils.LatLon2Radians(2, 6, 7.4095, 0),
            27.257
        );

        public AsterixDecoderFile_Interface()
        {
            InitializeComponent();

            // Adapt form to full screen behavior
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.Padding = new Padding(12);
            this.Resize += (s, e) => {
                try { drop_panel?.Invalidate(); } catch { }
            };

            InitializeDragDrop();
        }

        private void InitializeDragDrop()
        {
            // Configurar el panel para drag & drop
            drop_panel.AllowDrop = true;
            drop_panel.DragEnter += Drop_panel_DragEnter;
            drop_panel.DragDrop += Drop_panel_DragDrop;
            drop_panel.Paint += Drop_panel_Paint;

            // Hacer el panel más visible
            drop_panel.BorderStyle = BorderStyle.FixedSingle;
            drop_panel.BackColor = Color.WhiteSmoke;

            // Adaptar el panel a pantalla completa
            drop_panel.Dock = DockStyle.Fill;
            drop_panel.Margin = new Padding(0);
        }

        private void Drop_panel_Paint(object sender, PaintEventArgs e)
        {
            // Dibujar instrucciones en el panel
            string message = "Arrastra aquí tu archivo ASTERIX (.ast)\n\n" +
                           "Formatos soportados:\n" +
                           "• CAT021 (ADS-B)\n" +
                           "• CAT048 (Radar)\n" +
                           "• Archivos combinados";

            using (Font font = new Font("Segoe UI", 14, FontStyle.Bold))
            using (Font fontSmall = new Font("Segoe UI", 10))
            {
                SizeF titleSize = e.Graphics.MeasureString("Arrastra aquí tu archivo ASTERIX (.ast)", font);

                // Título
                e.Graphics.DrawString(
                    "Arrastra aquí tu archivo ASTERIX (.ast)",
                    font,
                    Brushes.DarkSlateGray,
                    new PointF((drop_panel.Width - titleSize.Width) / 2, drop_panel.Height / 2 - 60)
                );

                // Subtítulo
                string subtitle = "Formatos soportados:";
                SizeF subtitleSize = e.Graphics.MeasureString(subtitle, fontSmall);
                e.Graphics.DrawString(
                    subtitle,
                    fontSmall,
                    Brushes.Gray,
                    new PointF((drop_panel.Width - subtitleSize.Width) / 2, drop_panel.Height / 2 - 10)
                );

                // Lista de formatos
                string format1 = "• CAT021 (ADS-B)";
                string format2 = "• CAT048 (Radar)";
                string format3 = "• Archivos combinados";

                SizeF format1Size = e.Graphics.MeasureString(format1, fontSmall);
                SizeF format2Size = e.Graphics.MeasureString(format2, fontSmall);
                SizeF format3Size = e.Graphics.MeasureString(format3, fontSmall);

                e.Graphics.DrawString(format1, fontSmall, Brushes.DarkGray,
                    new PointF((drop_panel.Width - format1Size.Width) / 2, drop_panel.Height / 2 + 15));
                e.Graphics.DrawString(format2, fontSmall, Brushes.DarkGray,
                    new PointF((drop_panel.Width - format2Size.Width) / 2, drop_panel.Height / 2 + 35));
                e.Graphics.DrawString(format3, fontSmall, Brushes.DarkGray,
                    new PointF((drop_panel.Width - format3Size.Width) / 2, drop_panel.Height / 2 + 55));
            }
        }

        private void Drop_panel_DragEnter(object sender, DragEventArgs e)
        {
            // Verificar si se está arrastrando un archivo
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                drop_panel.BackColor = Color.LightBlue;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Drop_panel_DragDrop(object sender, DragEventArgs e)
        {
            // Restaurar color original
            drop_panel.BackColor = Color.WhiteSmoke;

            try
            {
                // Obtener el archivo arrastrado
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    string filePath = files[0];

                    // Verificar extensión
                    if (!filePath.EndsWith(".ast", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            "Por favor, selecciona un archivo con extensión .ast",
                            "Formato de archivo incorrecto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    // Verificar existencia y accesibilidad
                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show(
                            "El archivo seleccionado no existe o no es accesible.",
                            "Archivo no encontrado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    // Procesar el archivo
                    ProcessAsterixFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al procesar el archivo:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void ProcessAsterixFile(string filePath)
        {
            // Mostrar diálogo de progreso
            using (var progressForm = new Form())
            {
                progressForm.Text = "Procesando archivo ASTERIX...";
                progressForm.Size = new Size(400, 150);
                progressForm.StartPosition = FormStartPosition.CenterParent;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;

                var label = new Label
                {
                    Text = "Leyendo y decodificando archivo...\nPor favor espere.",
                    AutoSize = false,
                    Size = new Size(360, 60),
                    Location = new Point(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                var progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    Size = new Size(360, 23),
                    Location = new Point(20, 80)
                };

                progressForm.Controls.Add(label);
                progressForm.Controls.Add(progressBar);
                progressForm.Show();
                progressForm.Refresh();

                try
                {
                    // Decodificar usando la nueva lista combinada
                    var combined = AsterixCombinedList.FromFile(filePath);

                    // Ocultar inmediatamente el cuadro de progreso tras decodificar
                    try { if (!progressForm.IsDisposed) progressForm.Hide(); } catch { }

                    // Mostrar resumen inicial
                    var counts = combined.GetCountsByCategory();
                    int c48 = counts.cat048;
                    int c21 = counts.cat021;
                    string summary = $"Decodificación completada:\n\n" +
                                     (c48 > 0 ? $"CAT048: {c48} registros\n" : string.Empty) +
                                     (c21 > 0 ? $"CAT021: {c21} registros\n" : string.Empty) +
                                     $"Total: {combined.Count} registros";

                    MessageBox.Show(
                        summary,
                        "Decodificación exitosa",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Aplicar filtro geográfico estricto obligatorio (sin opción de omitir)
                    var filtered = combined.FilterBarcelonaFIRStrict();

                    var fcounts = filtered.GetCountsByCategory();
                    int f48 = fcounts.cat048;
                    int f21 = fcounts.cat021;
                    MessageBox.Show($"Filtro geográfico (Barcelona FIR) aplicado automáticamente.\nCAT048: {f48}\nCAT021: {f21}\nTotal: {filtered.Count}", "Filtro aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Abrir la interfaz de tabla con los datos filtrados
                    OpenTableInterface(filtered);
                }
                catch (InvalidDataException ex)
                {
                    try { if (!progressForm.IsDisposed) progressForm.Hide(); } catch { }
                    MessageBox.Show(
                        ex.Message,
                        "Archivo no válido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
                catch (UnauthorizedAccessException ex)
                {
                    try { if (!progressForm.IsDisposed) progressForm.Hide(); } catch { }
                    MessageBox.Show(
                        $"Acceso denegado al archivo:\n{ex.Message}",
                        "Permisos insuficientes",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                catch (IOException ex)
                {
                    try { if (!progressForm.IsDisposed) progressForm.Hide(); } catch { }
                    MessageBox.Show(
                        $"No se pudo leer el archivo:\n{ex.Message}",
                        "Error de E/S",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                catch (Exception ex)
                {
                    try { if (!progressForm.IsDisposed) progressForm.Hide(); } catch { }
                    MessageBox.Show(
                        $"Se produjo un error inesperado al decodificar el archivo:\n{ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                finally
                {
                    try { progressForm.Close(); } catch { }
                }
            }
        }

        private AsterixCombinedList? PromptAndApplyGeographicFilter(AsterixCombinedList combined)
        {
            // Ventana de filtro geográfico inicial (aplicado justo tras decodificar)
            using (var dlg = new Form())
            {
                dlg.Text = "Filtro geográfico (aplicar antes de continuar)";
                dlg.Size = new Size(420, 260);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                // Etiquetas y cajas de texto para 2 vértices
                var lblInfo = new Label { Text = "Introduce el rectángulo geográfico (dos vértices opuestos)", AutoSize = false, Size = new Size(380, 30), Location = new Point(20, 10) };
                var lblLat1 = new Label { Text = "Lat 1:", Location = new Point(20, 50), Size = new Size(60, 23) };
                var txtLat1 = new TextBox { Location = new Point(80, 50), Size = new Size(100, 23) };
                var lblLon1 = new Label { Text = "Lon 1:", Location = new Point(200, 50), Size = new Size(60, 23) };
                var txtLon1 = new TextBox { Location = new Point(260, 50), Size = new Size(100, 23) };

                var lblLat2 = new Label { Text = "Lat 2:", Location = new Point(20, 90), Size = new Size(60, 23) };
                var txtLat2 = new TextBox { Location = new Point(80, 90), Size = new Size(100, 23) };
                var lblLon2 = new Label { Text = "Lon 2:", Location = new Point(200, 90), Size = new Size(60, 23) };
                var txtLon2 = new TextBox { Location = new Point(260, 90), Size = new Size(100, 23) };

                // Valores por defecto: caja alrededor del FIR BCN definida previamente en Cat021.IsWithinBarcelonaFIR
                txtLat1.Text = "40.90"; txtLon1.Text = "1.50";
                txtLat2.Text = "41.70"; txtLon2.Text = "2.60";

                var btnApply = new Button { Text = "Aplicar filtro", Location = new Point(80, 150), Size = new Size(120, 35) };
                var btnSkip = new Button { Text = "Omitir", Location = new Point(220, 150), Size = new Size(120, 35) };

                AsterixCombinedList? result = null;

                btnApply.Click += (s, e) =>
                {
                    if (!double.TryParse(txtLat1.Text, out double lat1) ||
                        !double.TryParse(txtLon1.Text, out double lon1) ||
                        !double.TryParse(txtLat2.Text, out double lat2) ||
                        !double.TryParse(txtLon2.Text, out double lon2))
                    {
                        MessageBox.Show("Introduce coordenadas válidas.", "Formato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    result = combined.FilterGeographic(lat1, lon1, lat2, lon2);

                    var (f48, f21) = result.GetCountsByCategory();
                    MessageBox.Show($"Filtro geográfico aplicado.\nCAT048: {f48}\nCAT021: {f21}\nTotal: {result.Count}", "Filtro aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };

                btnSkip.Click += (s, e) =>
                {
                    // El usuario decide no filtrar ahora
                    dlg.DialogResult = DialogResult.Cancel;
                    dlg.Close();
                };

                dlg.Controls.AddRange(new Control[] { lblInfo, lblLat1, txtLat1, lblLon1, txtLon1, lblLat2, txtLat2, lblLon2, txtLon2, btnApply, btnSkip });

                dlg.ShowDialog();
                return result; // null si se omite
            }
        }

        private void OpenTableInterface(AsterixCombinedList combined)
        {
            // Mostrar la interfaz de tabla con la lista combinada ya decodificada
            var tableInterface = new AsterixDecoderTable_Interface(combined);

            // Ocultar esta ventana
            this.Hide();

            // Mostrar la interfaz de tabla
            tableInterface.ShowDialog();

            // Al cerrar la tabla, volver a mostrar esta ventana
            this.Show();
        }

        private void AsterixDecoderFile_Interface_Load(object sender, EventArgs e)
        {

        }
    }
}
