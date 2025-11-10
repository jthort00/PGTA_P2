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

namespace AsterixDecoderApp
{
    public partial class AsterixDecoderTable_Interface : Form
    {
        // Lista original sin modificar (recibida de DecoderFile)
        private AsterixCombinedList originalCombinedList;
        
        // Lista actual con filtros aplicados
        private AsterixCombinedList currentFilteredList;

        public AsterixDecoderTable_Interface(AsterixCombinedList combined)
        {
            InitializeComponent();

            // Guardar la lista original
            originalCombinedList = combined;
            
            // Inicializar la lista filtrada como copia de la original
            currentFilteredList = new AsterixCombinedList();
            currentFilteredList.AddRange(combined.GetAll());

            // Configurar DataGridView
            ConfigureDataGridView();

            // Seleccionar automáticamente el tipo de datos a mostrar
            AutoSelectDataType();

            // Mostrar datos iniciales
            UpdateDisplay();
        }

        private void ConfigureDataGridView()
        {
            cat_dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            cat_dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            cat_dataGridView.MultiSelect = false;
            cat_dataGridView.ReadOnly = true;
            cat_dataGridView.AllowUserToAddRows = false;
            cat_dataGridView.AllowUserToDeleteRows = false;
        }

        private void AutoSelectDataType()
        {
            // Determinar qué tipo de datos tenemos
            var (cat048Count, cat021Count) = originalCombinedList.GetCountsByCategory();

            if (cat048Count > 0 && cat021Count > 0)
            {
                // Tenemos ambos
                combined_checkBox.Checked = true;
            }
            else if (cat048Count > 0)
            {
                // Solo CAT048
                CAT048_checkbox.Checked = true;
            }
            else if (cat021Count > 0)
            {
                // Solo CAT021
                CAT021_checkbox.Checked = true;
            }
        }

        private void CAT021_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (CAT021_checkbox.Checked)
            {
                CAT048_checkbox.Checked = false;
                combined_checkBox.Checked = false;
                UpdateDisplay();
            }
        }

        private void CAT048_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            if (CAT048_checkbox.Checked)
            {
                CAT021_checkbox.Checked = false;
                combined_checkBox.Checked = false;
                UpdateDisplay();
            }
        }

        private void combined_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (combined_checkBox.Checked)
            {
                CAT021_checkbox.Checked = false;
                CAT048_checkbox.Checked = false;
                UpdateDisplay();
            }
        }

        private void Purewhite_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyAllFilters();
        }

        private void transponderfijo_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            ApplyAllFilters();
        }

        private void OnGround_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            // De momento vacía (sin acción)
        }

        private void Latitude_textBox_TextChanged(object sender, EventArgs e)
        {
            // No aplicar automáticamente, esperar al botón
        }

        private void Longitude_textBox_TextChanged(object sender, EventArgs e)
        {
            // No aplicar automáticamente, esperar al botón
        }

        /// <summary>
        /// Aplica todos los filtros activos sobre la lista original
        /// </summary>
        private void ApplyAllFilters()
        {
            // Resetear a la lista original
            currentFilteredList = new AsterixCombinedList();
            currentFilteredList.AddRange(originalCombinedList.GetAll());

            // Aplicar filtro de blancos puros (Mode S)
            if (Purewhite_checkBox.Checked)
            {
                var cat048Records = currentFilteredList.GetCat048();
                var filteredCat048 = Cat048List.FilterPureModeS(cat048Records);
                
                var cat021Records = currentFilteredList.GetCat021();
                
                // Reconstruir lista con solo los CAT048 filtrados
                currentFilteredList = new AsterixCombinedList();
                foreach (var rec in filteredCat048)
                {
                    currentFilteredList.Add(rec);
                }
                foreach (var rec in cat021Records)
                {
                    currentFilteredList.Add(rec);
                }
            }

            // Aplicar filtro de transponder fijo
            if (transponderfijo_checkBox.Checked)
            {
                var cat048Records = currentFilteredList.GetCat048();
                var filteredCat048 = Cat048List.FilterFixedTransponder(cat048Records);
                
                var cat021Records = currentFilteredList.GetCat021();
                
                // Reconstruir lista con solo los CAT048 filtrados
                currentFilteredList = new AsterixCombinedList();
                foreach (var rec in filteredCat048)
                {
                    currentFilteredList.Add(rec);
                }
                foreach (var rec in cat021Records)
                {
                    currentFilteredList.Add(rec);
                }
            }

            // Actualizar display
            UpdateDisplay();
        }

        /// <summary>
        /// Actualiza el DataGridView según el checkbox de categoría seleccionado
        /// </summary>
        private void UpdateDisplay()
        {
            if (CAT048_checkbox.Checked)
            {
                DisplayCAT048Data();
            }
            else if (CAT021_checkbox.Checked)
            {
                DisplayCAT021Data();
            }
            else if (combined_checkBox.Checked)
            {
                DisplayCombinedData();
            }
        }

        private void DisplayCAT048Data()
        {
            var dataTable = new DataTable();

            // Definir columnas según TODOS los campos de CAT048
            dataTable.Columns.Add("CAT", typeof(string));
            dataTable.Columns.Add("SAC", typeof(string));
            dataTable.Columns.Add("SIC", typeof(string));
            dataTable.Columns.Add("Time", typeof(string));
            dataTable.Columns.Add("LAT", typeof(string));
            dataTable.Columns.Add("LON", typeof(string));
            dataTable.Columns.Add("H_ft", typeof(string));
            dataTable.Columns.Add("H_m", typeof(string));
            dataTable.Columns.Add("RHO", typeof(string));
            dataTable.Columns.Add("THETA", typeof(string));
            dataTable.Columns.Add("Mode3A", typeof(string));
            dataTable.Columns.Add("FL", typeof(string));
            dataTable.Columns.Add("TA", typeof(string));
            dataTable.Columns.Add("TI", typeof(string));
            dataTable.Columns.Add("Stat", typeof(string));
            dataTable.Columns.Add("BP", typeof(string));
            dataTable.Columns.Add("RA", typeof(string));
            dataTable.Columns.Add("TTA", typeof(string));
            dataTable.Columns.Add("GS", typeof(string));
            dataTable.Columns.Add("TAR", typeof(string));
            dataTable.Columns.Add("TAS", typeof(string));
            dataTable.Columns.Add("HDG", typeof(string));
            dataTable.Columns.Add("IAS", typeof(string));
            dataTable.Columns.Add("MACH", typeof(string));
            dataTable.Columns.Add("BAR", typeof(string));
            dataTable.Columns.Add("IVV", typeof(string));
            dataTable.Columns.Add("TN", typeof(string));
            dataTable.Columns.Add("GSSD", typeof(string));
            dataTable.Columns.Add("HDG2", typeof(string));
            dataTable.Columns.Add("COM", typeof(string));
            dataTable.Columns.Add("STAT_230", typeof(string));
            dataTable.Columns.Add("SI", typeof(string));
            dataTable.Columns.Add("MSSC", typeof(string));
            dataTable.Columns.Add("ARC", typeof(string));
            dataTable.Columns.Add("AIC", typeof(string));

            // Llenar datos solo con CAT048
            var cat048Records = currentFilteredList.GetCat048();
            foreach (var r in cat048Records)
            {
                dataTable.Rows.Add(
                    r.CAT ?? "N/A",
                    r.SAC?.ToString() ?? "N/A",
                    r.SIC?.ToString() ?? "N/A",
                    r.Time ?? "N/A",
                    r.LAT?.ToString("F6") ?? "N/A",
                    r.LON?.ToString("F6") ?? "N/A",
                    r.H?.ToString("F0") ?? "N/A",
                    r.H_m?.ToString("F2") ?? "N/A",
                    r.RHO?.ToString("F3") ?? "N/A",
                    r.THETA?.ToString("F2") ?? "N/A",
                    r.Mode3A ?? "N/A",
                    r.FL?.ToString("F0") ?? "N/A",
                    r.TA ?? "N/A",
                    r.TI ?? "N/A",
                    r.Stat ?? "N/A",
                    r.BP?.ToString("F2") ?? "N/A",
                    r.RA?.ToString("F2") ?? "N/A",
                    r.TTA?.ToString("F3") ?? "N/A",
                    r.GS?.ToString("F3") ?? "N/A",
                    r.TAR?.ToString("F3") ?? "N/A",
                    r.TAS?.ToString("F3") ?? "N/A",
                    r.HDG?.ToString("F3") ?? "N/A",
                    r.IAS?.ToString("F3") ?? "N/A",
                    r.MACH?.ToString("F3") ?? "N/A",
                    r.BAR?.ToString("F3") ?? "N/A",
                    r.IVV?.ToString("F3") ?? "N/A",
                    r.TN?.ToString() ?? "N/A",
                    r.GSSD?.ToString("F3") ?? "N/A",
                    r.HDG2?.ToString("F3") ?? "N/A",
                    r.COM?.ToString() ?? "N/A",
                    r.STAT_230?.ToString() ?? "N/A",
                    r.SI.HasValue ? (r.SI.Value ? "True" : "False") : "N/A",
                    r.MSSC.HasValue ? (r.MSSC.Value ? "True" : "False") : "N/A",
                    r.ARC.HasValue ? (r.ARC.Value ? "True" : "False") : "N/A",
                    r.AIC.HasValue ? (r.AIC.Value ? "True" : "False") : "N/A"
                );
            }

            cat_dataGridView.DataSource = dataTable;
        }

        private void DisplayCAT021Data()
        {
            var dataTable = new DataTable();

            // Definir columnas según TODOS los campos de CAT021
            dataTable.Columns.Add("CAT", typeof(string));
            dataTable.Columns.Add("SAC", typeof(string));
            dataTable.Columns.Add("SIC", typeof(string));
            dataTable.Columns.Add("Time", typeof(string));
            dataTable.Columns.Add("LAT", typeof(string));
            dataTable.Columns.Add("LON", typeof(string));
            dataTable.Columns.Add("Mode3A", typeof(string));
            dataTable.Columns.Add("FL", typeof(string));
            dataTable.Columns.Add("TA", typeof(string));
            dataTable.Columns.Add("TI", typeof(string));
            dataTable.Columns.Add("BP", typeof(string));
            dataTable.Columns.Add("Real_Altitude_ft", typeof(string));
            dataTable.Columns.Add("IsOnGround", typeof(string));

            // Llenar datos solo con CAT021
            var cat021Records = currentFilteredList.GetCat021();
            foreach (var r in cat021Records)
            {
                dataTable.Rows.Add(
                    r.CAT ?? "N/A",
                    r.SAC.ToString(),
                    r.SIC.ToString(),
                    r.Time ?? "N/A",
                    r.LAT.ToString("F6"),
                    r.LON.ToString("F6"),
                    r.Mode3A ?? "N/A",
                    r.FL.ToString(),
                    r.TA ?? "N/A",
                    r.TI ?? "N/A",
                    r.BP.HasValue ? r.BP.Value.ToString("F2") : "N/A",
                    r.ModeC_Corrected.ToString("F0"),
                    r.IsOnGround ? "True" : "False"
                );
            }

            cat_dataGridView.DataSource = dataTable;
        }

        private void DisplayCombinedData()
        {
            var dataTable = new DataTable();

            // Columnas comunes
            dataTable.Columns.Add("CAT", typeof(string));
            dataTable.Columns.Add("Time", typeof(string));
            dataTable.Columns.Add("TA", typeof(string));
            dataTable.Columns.Add("TI", typeof(string));
            dataTable.Columns.Add("LAT", typeof(string));
            dataTable.Columns.Add("LON", typeof(string));
            dataTable.Columns.Add("FL", typeof(string));
            dataTable.Columns.Add("Alt_ft", typeof(string));
            dataTable.Columns.Add("Mode3A", typeof(string));

            // Obtener todos los registros en orden de inserción
            var allRecords = currentFilteredList.GetAll();

            foreach (var record in allRecords)
            {
                if (record.Category == AsterixCombinedList.AsterixCategory.CAT048 && record.Cat048 != null)
                {
                    var r = record.Cat048;
                    dataTable.Rows.Add(
                        "CAT048",
                        r.Time ?? "N/A",
                        r.TA ?? "N/A",
                        r.TI ?? "N/A",
                        r.LAT?.ToString("F6") ?? "N/A",
                        r.LON?.ToString("F6") ?? "N/A",
                        r.FL?.ToString("F0") ?? "N/A",
                        r.H?.ToString("F0") ?? "N/A",
                        r.Mode3A ?? "N/A"
                    );
                }
                else if (record.Category == AsterixCombinedList.AsterixCategory.CAT021 && record.Cat021 != null)
                {
                    var r = record.Cat021;
                    dataTable.Rows.Add(
                        "CAT021",
                        r.Time,
                        r.TA,
                        r.TI,
                        r.LAT.ToString("F6"),
                        r.LON.ToString("F6"),
                        r.FL.ToString(),
                        r.ModeC_Corrected.ToString("F0"),
                        r.Mode3A
                    );
                }
            }

            cat_dataGridView.DataSource = dataTable;
        }

        private void close_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void csv_button_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveDialog.FilterIndex = 1;
                    saveDialog.RestoreDirectory = true;
                    saveDialog.FileName = $"asterix_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string outputPath = saveDialog.FileName;

                        // Crear listas temporales para exportar
                        var cat048List = new Cat048List();
                        var cat021List = new Cat021List();

                        cat048List.AddRange(currentFilteredList.GetCat048());
                        cat021List.AddRange(currentFilteredList.GetCat021());

                        // Exportar según el tipo seleccionado
                        if (CAT048_checkbox.Checked)
                        {
                            string csv048 = cat048List.ExportToCSV();
                            File.WriteAllText(outputPath, csv048, Encoding.UTF8);
                        }
                        else if (CAT021_checkbox.Checked)
                        {
                            cat021List.ExportToCSV(outputPath);
                        }
                        else if (combined_checkBox.Checked)
                        {
                            // Exportar combinado usando la nueva función en AsterixCombinedList
                            currentFilteredList.ExportCombinedToCSV(outputPath);
                        }

                        MessageBox.Show(
                            $"Archivo CSV exportado exitosamente:\n{outputPath}",
                            "Exportación completada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar CSV:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void filtergeo_button_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que los 4 campos estén completos
                if (string.IsNullOrWhiteSpace(Latitude_textBox.Text) ||
                    string.IsNullOrWhiteSpace(Longitude_textBox.Text) ||
                    string.IsNullOrWhiteSpace(Latitude2_textbox.Text) ||
                    string.IsNullOrWhiteSpace(Longuitude2_textbox.Text))
                {
                    MessageBox.Show(
                        "Por favor, introduce valores en los cuatro campos de coordenadas.",
                        "Campos incompletos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Parsear coordenadas
                if (!double.TryParse(Latitude_textBox.Text, out double lat1) ||
                    !double.TryParse(Longitude_textBox.Text, out double lon1) ||
                    !double.TryParse(Latitude2_textbox.Text, out double lat2) ||
                    !double.TryParse(Longuitude2_textbox.Text, out double lon2))
                {
                    MessageBox.Show(
                        "Introduce coordenadas válidas en formato numérico.",
                        "Formato inválido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Aplicar filtro geográfico sobre CAT048
                var cat048Records = currentFilteredList.GetCat048();
                var cat021Records = currentFilteredList.GetCat021();

                var geoFilteredCat048 = Cat048List.FilterGeographic(cat048Records, lat1, lon1, lat2, lon2);

                // Reconstruir la lista filtrada con el filtro geográfico aplicado
                currentFilteredList = new AsterixCombinedList();
                foreach (var rec in geoFilteredCat048)
                {
                    currentFilteredList.Add(rec);
                }
                foreach (var rec in cat021Records)
                {
                    currentFilteredList.Add(rec);
                }

                // Actualizar display
                UpdateDisplay();

                MessageBox.Show(
                    $"Filtro geográfico aplicado.\nRegistros CAT048 restantes: {geoFilteredCat048.Count}",
                    "Filtro aplicado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al aplicar filtro geográfico:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void simulation_button_Click(object sender, EventArgs e)
        {
            // De momento vacío
        }
    }
}
