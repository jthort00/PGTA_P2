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
        }

        private void Drop_panel_Paint(object sender, PaintEventArgs e)
        {
            // Dibujar instrucciones en el panel
            string message = "Arrastra aquí tu archivo ASTERIX (.ast)\n\n" +
                           "Formatos soportados:\n" +
                           "• CAT021 (ADS-B)\n" +
                           "• CAT048 (Radar SMR)\n" +
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
                string format2 = "• CAT048 (Radar SMR)";
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

                    progressForm.Close();

                    // Mostrar resumen
                    var (c48, c21) = combined.GetCountsByCategory();
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

                    // Abrir la interfaz de tabla con los datos decodificados
                    OpenTableInterface(combined);
                }
                catch (Exception)
                {
                    progressForm.Close();
                    throw;
                }
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
