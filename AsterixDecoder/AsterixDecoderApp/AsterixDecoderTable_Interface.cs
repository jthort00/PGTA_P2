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

        // Color azul consistente con la interfaz de simulación (panel info CAT021)
        private readonly Color simulationBlue = Color.FromArgb(220, 180, 220, 255);
        private readonly Color darkPanel = Color.FromArgb(230, 30, 30, 30);

        // Máscaras para "oscurecer" las barras de desplazamiento del DataGridView
        private Panel? gridVScrollMask;
        private Panel? gridHScrollMask;
        private Panel? gridCornerMask;

        public AsterixDecoderTable_Interface(AsterixCombinedList combined)
        {
            InitializeComponent();

            // Aplicar tema visual similar a la simulación
            ApplyTheme();

            // Guardar la lista original
            originalCombinedList = combined;

            // Inicializar la lista filtrada como copia de la original
            currentFilteredList = new AsterixCombinedList();
            currentFilteredList.AddRange(combined.GetAll());

            // Configurar DataGridView
            ConfigureDataGridView();

            // Seleccionar automáticamente el tipo de datos a mostrar
            AutoSelectDataType();

            // Crear máscaras para oscurecer scrollbars del grid
            CreateScrollMasks();
            PositionScrollMasks();

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
            // Ocultar la primera columna de encabezados de fila (selector de fila)
            cat_dataGridView.RowHeadersVisible = false;

            // Estilos para tema oscuro
            cat_dataGridView.EnableHeadersVisualStyles = false;
            cat_dataGridView.BackgroundColor = Color.Black;
            cat_dataGridView.GridColor = Color.FromArgb(50, 50, 50);
            cat_dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            cat_dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            cat_dataGridView.DefaultCellStyle.BackColor = Color.Black;
            cat_dataGridView.DefaultCellStyle.ForeColor = Color.White;
            cat_dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 60, 60);
            cat_dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;

            // Aplicar colores de columnas tras el binding de datos
            cat_dataGridView.DataBindingComplete += (s, e) => ApplyGridColumnColors();
            // Reposicionar máscaras de scroll en eventos clave
            cat_dataGridView.DataBindingComplete += (s, e) => PositionScrollMasks();
            cat_dataGridView.Resize += (s, e) => PositionScrollMasks();
            cat_dataGridView.Scroll += (s, e) => PositionScrollMasks();
        }

        private void CreateScrollMasks()
        {
            if (cat_dataGridView == null) return;

            if (gridVScrollMask == null)
            {
                gridVScrollMask = new Panel
                {
                    BackColor = Color.Black,
                    Visible = false
                };
                cat_dataGridView.Controls.Add(gridVScrollMask);
                gridVScrollMask.BringToFront();
            }
            if (gridHScrollMask == null)
            {
                gridHScrollMask = new Panel
                {
                    BackColor = Color.Black,
                    Visible = false
                };
                cat_dataGridView.Controls.Add(gridHScrollMask);
                gridHScrollMask.BringToFront();
            }
            if (gridCornerMask == null)
            {
                gridCornerMask = new Panel
                {
                    BackColor = Color.Black,
                    Visible = false
                };
                cat_dataGridView.Controls.Add(gridCornerMask);
                gridCornerMask.BringToFront();
            }
        }

        private void PositionScrollMasks()
        {
            if (cat_dataGridView == null) return;

            try
            {
                var vbar = cat_dataGridView.Controls.OfType<VScrollBar>().FirstOrDefault();
                var hbar = cat_dataGridView.Controls.OfType<HScrollBar>().FirstOrDefault();

                if (vbar != null && gridVScrollMask != null)
                {
                    gridVScrollMask.Bounds = vbar.Bounds;
                    gridVScrollMask.Visible = vbar.Visible;
                    gridVScrollMask.BringToFront();
                }

                if (hbar != null && gridHScrollMask != null)
                {
                    gridHScrollMask.Bounds = hbar.Bounds;
                    gridHScrollMask.Visible = hbar.Visible;
                    gridHScrollMask.BringToFront();
                }

                if (gridCornerMask != null)
                {
                    bool showCorner = (vbar != null && vbar.Visible) && (hbar != null && hbar.Visible);
                    if (showCorner)
                    {
                        // Posicionar en la esquina inferior derecha, en el área no ocupada por celdas
                        int cornerWidth = SystemInformation.VerticalScrollBarWidth;
                        int cornerHeight = SystemInformation.HorizontalScrollBarHeight;
                        gridCornerMask.Bounds = new Rectangle(
                            cat_dataGridView.ClientSize.Width - cornerWidth,
                            cat_dataGridView.ClientSize.Height - cornerHeight,
                            cornerWidth,
                            cornerHeight);
                        gridCornerMask.Visible = true;
                        gridCornerMask.BringToFront();
                    }
                    else
                    {
                        gridCornerMask.Visible = false;
                    }
                }
            }
            catch
            {
                // Ignorar cualquier excepción de posicionamiento por estados intermedios del control durante layout
            }
        }

        private void ApplyTheme()
        {
            // Fondo negro de la ventana
            this.BackColor = Color.Black;

            // Títulos y textos en blanco
            this.ForeColor = Color.White;

            // Aplicar estilo a labels
            foreach (var lbl in new[] { label1, label2 })
            {
                if (lbl == null) continue;
                lbl.ForeColor = Color.White;
                lbl.BackColor = Color.Transparent;
            }

            // Checkboxes blancos sobre fondo negro
            foreach (var cb in new[] { CAT021_checkbox, CAT048_checkbox, combined_checkBox, Purewhite_checkBox, transponderfijo_checkBox, OnGround_checkBox })
            {
                if (cb == null) continue;
                cb.ForeColor = Color.White;
                cb.BackColor = Color.Transparent;
            }

            // Botones con texto blanco y fondo oscuro consistente
            foreach (var b in new[] { simulation_button, csv_button, close_button })
            {
                if (b == null) continue;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.UseVisualStyleBackColor = false;
                b.BackColor = darkPanel; // mismo tono usado en simulación para paneles
                b.ForeColor = Color.White;
                b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                b.FlatAppearance.MouseOverBackColor = darkPanel;
                b.FlatAppearance.MouseDownBackColor = darkPanel;
            }

            // Controles de filtro geográfico eliminados (textboxes/botón) -> no aplicar estilo
        }

        private void ApplyGridColumnColors()
        {
            if (cat_dataGridView == null || cat_dataGridView.Columns.Count == 0) return;

            // Primera columna blanca, texto negro
            var white = Color.White;
            var black = Color.Black;

            for (int i = 0; i < cat_dataGridView.Columns.Count; i++)
            {
                var col = cat_dataGridView.Columns[i];
                if (i == 0)
                {
                    col.DefaultCellStyle.BackColor = black;
                    col.DefaultCellStyle.ForeColor = simulationBlue;
                    col.HeaderCell.Style.BackColor = Color.FromArgb(30, 30, 30);
                    col.HeaderCell.Style.ForeColor = Color.White;
                }
                else
                {
                    col.DefaultCellStyle.BackColor = black;
                    col.DefaultCellStyle.ForeColor = simulationBlue;
                    col.HeaderCell.Style.BackColor = Color.FromArgb(30, 30, 30);
                    col.HeaderCell.Style.ForeColor = Color.White;
                }
            }

            // Asegurar selección visible sobre fondos personalizados
            cat_dataGridView.DefaultCellStyle.SelectionBackColor = black;
            cat_dataGridView.DefaultCellStyle.SelectionForeColor = Color.Yellow;
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
            ApplyAllFilters();
        }


        /// <summary>
        /// Aplica todos los filtros activos sobre la lista original
        /// </summary>
        private void ApplyAllFilters()
        {
            try
            {
                if (originalCombinedList == null)
                {
                    MessageBox.Show("No hay datos cargados para aplicar filtros.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

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

                // Aplicar filtro On Ground (I048/230: 1 o 3) solo a CAT048
                if (OnGround_checkBox.Checked)
                {
                    var cat048Records = currentFilteredList.GetCat048();
                    var filteredCat048 = Cat048List.FilterRemoveOnGroundFromI048_230(cat048Records);
                    var cat021Records = currentFilteredList.GetCat021();

                    currentFilteredList = new AsterixCombinedList();
                    foreach (var rec in filteredCat048) currentFilteredList.Add(rec);
                    foreach (var rec in cat021Records) currentFilteredList.Add(rec);
                }

                // Actualizar display
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar filtros: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Actualiza el DataGridView según el checkbox de categoría seleccionado
        /// </summary>
        private void UpdateDisplay()
        {
            try
            {
                if (currentFilteredList == null)
                {
                    cat_dataGridView.DataSource = null;
                    return;
                }

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

                // Asegurar que los colores de columnas se apliquen tras poblar los datos
                ApplyGridColumnColors();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar la vista: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                try { cat_dataGridView.DataSource = null; } catch { }
            }
        }

        private void DisplayCAT048Data()
        {
            var dataTable = new DataTable();

            // Definir columnas para que coincidan EXACTAMENTE con el CSV
            dataTable.Columns.Add("CAT", typeof(string));
            dataTable.Columns.Add("SAC", typeof(string));
            dataTable.Columns.Add("SIC", typeof(string));
            dataTable.Columns.Add("Time", typeof(string));
            dataTable.Columns.Add("Lat", typeof(string));
            dataTable.Columns.Add("Lon", typeof(string));
            dataTable.Columns.Add("H_wgs", typeof(string));          // H_m (WGS84 meters)
            dataTable.Columns.Add("h_ft", typeof(string));            // H in feet
            dataTable.Columns.Add("RHO", typeof(string));
            dataTable.Columns.Add("THETA", typeof(string));
            dataTable.Columns.Add("Mode_3A", typeof(string));
            dataTable.Columns.Add("Flight_level", typeof(string));
            dataTable.Columns.Add("ModeC_corrected", typeof(string));
            dataTable.Columns.Add("Target_address", typeof(string));
            dataTable.Columns.Add("Target_identification", typeof(string));
            dataTable.Columns.Add("Mode_S", typeof(string));          // BDS registers present
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
            dataTable.Columns.Add("Track_number", typeof(string));
            dataTable.Columns.Add("Ground_speedkt", typeof(string));
            dataTable.Columns.Add("Heading", typeof(string));
            dataTable.Columns.Add("SAT230", typeof(string));         // Communications capability

            // Llenar datos solo con CAT048
            var cat048Records = currentFilteredList.GetCat048();
            foreach (var r in cat048Records)
            {
                // Calcular campo Mode_S (BDS) igual que en el export CSV
                string modeS = "";
                var bdsList = new List<string>();
                if (r.BP.HasValue) bdsList.Add("BDS:4,0");
                if (r.RA.HasValue || r.TAR.HasValue || r.TAS.HasValue || r.GS.HasValue) bdsList.Add("BDS:5,0");
                if (r.HDG.HasValue || r.IAS.HasValue || r.MACH.HasValue || r.BAR.HasValue || r.IVV.HasValue) bdsList.Add("BDS:6,0");
                if (bdsList.Count > 0) modeS = string.Join(" ", bdsList);

                // Calcular campo SAT230 como en el CSV
                string sat230 = "";
                if (r.COM.HasValue || r.STAT_230.HasValue)
                {
                    var parts = new List<string>();
                    if (r.COM.HasValue) parts.Add($"COM:{r.COM}");
                    if (r.STAT_230.HasValue)
                    {
                        var flags = new List<string>();
                        if (r.SI.HasValue) flags.Add($"SI={(r.SI.Value ? 1 : 0)}");
                        if (r.MSSC.HasValue) flags.Add($"MSSC={(r.MSSC.Value ? 1 : 0)}");
                        if (r.ARC.HasValue) flags.Add($"ARC={(r.ARC.Value ? 1 : 0)}");
                        if (r.AIC.HasValue) flags.Add($"AIC={(r.AIC.Value ? 1 : 0)}");
                        if (flags.Count > 0) parts.Add("[" + string.Join(",", flags) + "]");
                    }
                    sat230 = string.Join(" ", parts);
                }

                var modecCorrectedStr = (r.FL.HasValue && r.FL.Value < 60.0 && r.H.HasValue)
                    ? r.H.Value.ToString("F3")
                    : string.Empty;

                dataTable.Rows.Add(
                    r.CAT ?? "N/A",
                    r.SAC?.ToString() ?? "N/A",
                    r.SIC?.ToString() ?? "N/A",
                    r.Time ?? "N/A",
                    r.LAT?.ToString("F8") ?? "N/A",
                    r.LON?.ToString("F8") ?? "N/A",
                    r.H_m?.ToString("F14") ?? "N/A",      // H_wgs
                    r.H?.ToString("F14") ?? "N/A",        // h_ft
                    r.RHO?.ToString("F6") ?? "N/A",
                    r.THETA?.ToString("F6") ?? "N/A",
                    r.Mode3A ?? "N/A",                      // Mode_3A
                    r.FL?.ToString("F3") ?? "N/A",        // Flight_level
                    modecCorrectedStr,                       // ModeC_corrected (pies) solo si corregida
                    r.TA ?? "N/A",
                    r.TI ?? "N/A",
                    string.IsNullOrEmpty(modeS) ? "N/A" : modeS,
                    r.BP?.ToString("F3") ?? "N/A",
                    r.RA?.ToString("F3") ?? "N/A",
                    r.TTA?.ToString("F3") ?? "N/A",
                    r.GS?.ToString("F3") ?? "N/A",
                    r.TAR?.ToString("F3") ?? "N/A",
                    r.TAS?.ToString("F3") ?? "N/A",
                    r.HDG?.ToString("F6") ?? "N/A",
                    r.IAS?.ToString("F3") ?? "N/A",
                    r.MACH?.ToString("F3") ?? "N/A",
                    r.BAR?.ToString("F3") ?? "N/A",
                    r.IVV?.ToString("F3") ?? "N/A",
                    r.TN?.ToString() ?? "N/A",
                    r.GSSD?.ToString("F3") ?? "N/A",
                    r.HDG2?.ToString("F4") ?? "N/A",
                    string.IsNullOrEmpty(sat230) ? "N/A" : sat230
                );
            }

            cat_dataGridView.DataSource = dataTable;
        }

        private void DisplayCAT021Data()
        {
            var dataTable = new DataTable();

            dataTable.Columns.Add("CAT", typeof(string));
            dataTable.Columns.Add("SAC", typeof(string));
            dataTable.Columns.Add("SIC", typeof(string));
            dataTable.Columns.Add("Time", typeof(string));
            dataTable.Columns.Add("LAT", typeof(string));
            dataTable.Columns.Add("LON", typeof(string));
            dataTable.Columns.Add("Mode3A_Code", typeof(string));
            dataTable.Columns.Add("FL", typeof(string));
            dataTable.Columns.Add("ModeC_Corrected", typeof(string));
            dataTable.Columns.Add("TA", typeof(string));
            dataTable.Columns.Add("TI", typeof(string));
            dataTable.Columns.Add("BP", typeof(string));
            dataTable.Columns.Add("OnGround", typeof(string));

            var cat021Records = currentFilteredList.GetCat021()
                .Where(r =>
                    !r.IsOnGround &&
                    r.Mode3A != "7777" &&
                    (string.IsNullOrEmpty(r.TI) || (!char.IsDigit(r.TI[0]) && r.TI.Length != 3))
                );

            foreach (var r in cat021Records)
            {
                // Formateo con CultureInfo.InvariantCulture y mismos formatos que CSV
                string latStr = r.LAT.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                string lonStr = r.LON.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                string flStr = r.FL.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string altStr = r.ModeC_Corrected.HasValue
                    ? r.ModeC_Corrected.Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)
                    : "N/A";

                string bpStr = "N/A";
                if (r.BP.HasValue && r.BP.Value >= 1000 && r.BP.Value <= 1030)
                    bpStr = r.BP.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

                dataTable.Rows.Add(
                    string.IsNullOrWhiteSpace(r.CAT) ? "N/A" : r.CAT,
                    r.SAC.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    r.SIC.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    string.IsNullOrWhiteSpace(r.Time) ? "N/A" : r.Time,
                    latStr,
                    lonStr,
                    string.IsNullOrWhiteSpace(r.Mode3A) ? "N/A" : r.Mode3A,
                    flStr,
                    altStr,
                    string.IsNullOrWhiteSpace(r.TA) ? "N/A" : r.TA,
                    string.IsNullOrWhiteSpace(r.TI) ? "N/A" : r.TI,
                    bpStr,
                    r.IsOnGround ? "True" : "False"
                );
            }

            cat_dataGridView.DataSource = dataTable;
        }

        private void DisplayCombinedData()
        {
            var dataTable = new DataTable();

            // Columnas alineadas con el CSV combinado (mismo orden y nombres)
            string[] cols = new[] {
                "CAT","SAC","SIC","Time","Lat","Lon","H_wgs","h_ft","RHO","THETA","Mode_3A","Flight_level","ModeC_corrected","Target_address","Target_identification","Mode_S","BP","RA","TTA","GS","TAR","TAS","HDG","IAS","MACH","BAR","IVV","Track_number","Ground_speedkt","Heading","SAT230"
            };
            foreach (var c in cols) dataTable.Columns.Add(c, typeof(string));

            // Helpers de formato
            Func<double?, string> FmtD = (v) => v.HasValue ? v.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<double?, string> FmtD2 = (v) => v.HasValue ? v.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<double?, string> FmtD3 = (v) => v.HasValue ? v.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<double?, string> FmtF6 = (v) => v.HasValue ? v.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<double?, string> FmtF8 = (v) => v.HasValue ? v.Value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<double?, string> FmtF14 = (v) => v.HasValue ? v.Value.ToString("F14", System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<int?, string> FmtI = (v) => v.HasValue ? v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "N/A";
            Func<string, string> FmtS = (s) => string.IsNullOrWhiteSpace(s) || s == "N/A" ? "N/A" : s;

            // Builders específicos de CAT048
            string BuildModeS(Cat048 r)
            {
                var bdsList = new List<string>();
                if (r.BP.HasValue) bdsList.Add("BDS:4,0");
                if (r.RA.HasValue || r.TAR.HasValue || r.TAS.HasValue || r.GS.HasValue) bdsList.Add("BDS:5,0");
                if (r.HDG.HasValue || r.IAS.HasValue || r.MACH.HasValue || r.BAR.HasValue || r.IVV.HasValue) bdsList.Add("BDS:6,0");
                return bdsList.Count > 0 ? string.Join(" ", bdsList) : "N/A";
            }
            string BuildSAT230(Cat048 r)
            {
                if (!r.COM.HasValue && !r.STAT_230.HasValue) return "N/A";
                return r.Stat ?? "N/A";
            }

            // Obtener todos los registros en orden de inserción
            var allRecords = currentFilteredList.GetAll();

            foreach (var record in allRecords)
            {
                if (record.Category == AsterixCombinedList.AsterixCategory.CAT048 && record.Cat048 != null)
                {
                    var r = record.Cat048;
                    var modecCorr048 = (r.FL.HasValue && r.FL.Value < 60.0 && r.H.HasValue)
                        ? r.H.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                        : string.Empty;

                    dataTable.Rows.Add(
                        "CAT048",
                        FmtI(r.SAC),
                        FmtI(r.SIC),
                        FmtS(r.Time),
                        FmtF8(r.LAT),
                        FmtF8(r.LON),
                        FmtF14(r.H_m),
                        FmtF14(r.H),
                        FmtF6(r.RHO),
                        FmtF6(r.THETA),
                        FmtS(r.Mode3A),
                        FmtD3(r.FL),
                        modecCorr048,
                        FmtS(r.TA),
                        FmtS(r.TI),
                        FmtS(BuildModeS(r)),
                        FmtD3(r.BP),
                        FmtD3(r.RA),
                        FmtD3(r.TTA),
                        FmtD3(r.GS),
                        FmtD3(r.TAR),
                        FmtD3(r.TAS),
                        r.HDG.HasValue ? r.HDG.Value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "N/A",
                        FmtD3(r.IAS),
                        FmtD3(r.MACH),
                        FmtD3(r.BAR),
                        FmtD3(r.IVV),
                        FmtI(r.TN),
                        FmtD3(r.GSSD),
                        r.HDG2.HasValue ? r.HDG2.Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) : "N/A",
                        FmtS(BuildSAT230(r))
                    );
                }
                else if (record.Category == AsterixCombinedList.AsterixCategory.CAT021 && record.Cat021 != null)
                {
                    var r = record.Cat021;
                    dataTable.Rows.Add(
                        "CAT021",
                        FmtI(r.SAC),
                        FmtI(r.SIC),
                        FmtS(r.Time),
                        r.LAT.ToString("F8", System.Globalization.CultureInfo.InvariantCulture),
                        r.LON.ToString("F8", System.Globalization.CultureInfo.InvariantCulture),
                        "N/A", // H_wgs
                        "N/A", // h_ft bruto
                        "N/A", // RHO
                        "N/A", // THETA
                        FmtS(r.Mode3A),
                        r.FL.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                        ((r.FL < 60 && r.ModeC_Corrected.HasValue) ? r.ModeC_Corrected.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : string.Empty),
                        FmtS(r.TA),
                        FmtS(r.TI),
                        "N/A", // Mode_S
                        r.BP.HasValue ? r.BP.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : "N/A",
                        "N/A", // RA
                        "N/A", // TTA
                        "N/A", // GS
                        "N/A", // TAR
                        "N/A", // TAS
                        "N/A", // HDG
                        "N/A", // IAS
                        "N/A", // MACH
                        "N/A", // BAR
                        "N/A", // IVV
                        "N/A", // Track_number
                        "N/A", // Ground_speedkt
                        "N/A", // Heading
                        "N/A"  // SAT230
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

        // Controles y handlers de filtro geográfico eliminados

        private void simulation_button_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar que hay datos para simular
                if (currentFilteredList.Count == 0)
                {
                    MessageBox.Show(
                        "No hay datos disponibles para simular.",
                        "Sin datos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                // Abrir la interfaz de simulación pasando los datos actuales
                var simulationInterface = new AsterixDecoderSimulation_Interface(currentFilteredList);

                // Ocultar esta ventana
                this.Hide();

                // Mostrar la simulación
                simulationInterface.ShowDialog();

                // Al cerrar la simulación, volver a mostrar esta ventana
                this.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al abrir la simulación:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void AsterixDecoderTable_Interface_Load(object sender, EventArgs e)
        {

        }
    }
}
