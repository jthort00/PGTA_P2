using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using AsterixDecoder.Models;
using AsterixDecoder.Models.CAT021;
using AsterixDecoder.Models.CAT048;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;

namespace AsterixDecoderApp
{
    public partial class AsterixDecoderSimulation_Interface : Form
    {

        private AircraftSpriteManager spriteManager;
        private const string SPRITES_FOLDER = "Sprites"; // Carpeta donde están las imágenes

        // Coordenadas de la estación Radar BCN
        private static readonly double RADAR_LAT = 41.0 + 18.0 / 60.0 + 2.5284 / 3600.0;  // 41.300702°
        private static readonly double RADAR_LON = 2.0 + 6.0 / 60.0 + 7.4095 / 3600.0;     // 2.102058°
        private static readonly double RADAR_ELEVATION = 27.257; // 2.007m terreno + 25.25m antena

        // Datos originales
        private AsterixCombinedList originalData;
        private AsterixCombinedList currentFilteredData;

        // Control del mapa
        private GMapControl gMapControl;
        private GMapOverlay aircraftOverlay;
        private GMapOverlay trajectoryOverlay;
        private GMapOverlay stationOverlay;

        // Control de simulación
        private System.Windows.Forms.Timer simulationTimer;
        private int currentTimeIndex = 0;
        private List<DateTime> timeStamps;
        private Dictionary<string, List<AircraftPosition>> aircraftTrajectories;
        private Dictionary<string, Queue<TrajectoryPoint>> recentTrajectories; // Para efecto fade
        private bool isPlaying = false;
        private int speedMultiplier = 1;
        private int lastAdvanceSteps = 1; // pasos avanzados en el último tick
        private int simulationDirection = 1; // 1 = adelante, -1 = atrás

        // Filtros de visualización
        private bool showCAT021 = true;
        private bool showCAT048 = true;

        // UI adicional
        private Label timeLabel;
        private Panel legendPanel;
        private Label speedLabel;

        // Panel de información de aeronave (panel derecho)
        private Panel aircraftInfoPanel;
        private Label aircraftInfoTitle;
        private Panel aircraftInfoContent;
        private Panel infoScrollBarMask; // máscara para unificar color de la scrollbar
        private readonly Color infoPanelBackColor = Color.FromArgb(255, 0, 0, 0); // color de fondo del panel de info (igual que scrollbar: negro opaco)
        private string selectedAircraftId;

        // Panel flotante (hover) para mostrar solo TI al pasar sobre el icono de aeronave
        private Panel hoverPanel;
        private Label hoverLabel;
        private GMapMarker currentHoverMarker;

        // Constantes de visualización
        private const int MAX_TRAJECTORY_POINTS = 300; // Límite de puntos para dibujar por aeronave (cap rendimiento)
        private const int TRAIL_DURATION_SECONDS = 180; // Duración de la traza visible en segundos

        // Eliminación de aeronaves estancadas cerca del borde del filtro geográfico
        private const int INACTIVITY_THRESHOLD_SECONDS = 30; // si una aeronave no se actualiza en N segundos y está cerca del borde -> no se dibuja
        private const double GEO_BORDER_MARGIN_DEG = 0.02;   // margen (~2 km aprox) desde el borde del filtro para considerar "cerca"

        // Límites del filtro geográfico actualmente aplicado (derivados de los datos filtrados)
        private double? geoMinLat = null, geoMaxLat = null, geoMinLon = null, geoMaxLon = null;

        // Clase auxiliar para posiciones de aeronaves
        private class AircraftPosition
        {
            public DateTime Time { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }
            public double? Altitude { get; set; }
            public string Callsign { get; set; }
            public string Category { get; set; } // "CAT021", "CAT048"
            public string Mode3A { get; set; }
            public double? Speed { get; set; }
            public double? Heading { get; set; }
        }

        // Clase para puntos de trayectoria con edad
        private class TrajectoryPoint
        {
            public PointLatLng Position { get; set; }
            public DateTime Timestamp { get; set; }
            public int Age { get; set; } // Edad en segundos
        }

        // Metadatos asociados a los marcadores de aeronaves
        private class MarkerMeta
        {
            public string AircraftId { get; set; }
            public string Callsign { get; set; }
            public string Category { get; set; }
        }

        // Constructor por defecto (para diseñador o apertura sin datos)
        public AsterixDecoderSimulation_Interface()
        {
            InitializeComponent();
            originalData = new AsterixCombinedList();
            currentFilteredData = new AsterixCombinedList();
            recentTrajectories = new Dictionary<string, Queue<TrajectoryPoint>>();
            InitializeSpriteManager();
            InitializeMap();
            InitializeSimulation();
            InitializeUI();
        }

        public AsterixDecoderSimulation_Interface(AsterixCombinedList data)
        {
            InitializeComponent();

            originalData = data;
            currentFilteredData = new AsterixCombinedList();
            currentFilteredData.AddRange(data.GetAll());

            recentTrajectories = new Dictionary<string, Queue<TrajectoryPoint>>();


            InitializeSpriteManager();

            InitializeMap();
            InitializeSimulation();
            InitializeUI();
            ProcessTrajectories();
            DrawRadarStation();
        }

        private void InitializeMap()
        {
            // Configurar GMap.NET
            gMapControl = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = GMapProviders.GoogleMap,
                Position = new PointLatLng(RADAR_LAT, RADAR_LON), // Centrar en el radar
                MinZoom = 5,
                MaxZoom = 18,
                Zoom = 12,
                ShowCenter = false,
                DragButton = MouseButtons.Left
            };

            // Configurar capas (orden importa para renderizado)
            stationOverlay = new GMapOverlay("stations");
            trajectoryOverlay = new GMapOverlay("trajectories");
            aircraftOverlay = new GMapOverlay("aircraft");

            gMapControl.Overlays.Add(stationOverlay);
            gMapControl.Overlays.Add(trajectoryOverlay);
            gMapControl.Overlays.Add(aircraftOverlay);

            // Añadir al panel
            map_panel.Controls.Clear();
            map_panel.Dock = DockStyle.Fill;
            map_panel.Controls.Add(gMapControl);

            // Inicializar panel flotante de hover y eventos de mapa
            InitializeHoverPanel();

            gMapControl.OnMarkerEnter += GMapControl_OnMarkerEnter;
            gMapControl.OnMarkerLeave += GMapControl_OnMarkerLeave;
            gMapControl.OnMapZoomChanged += GMapControl_OnMapZoomChanged;
            gMapControl.OnPositionChanged += GMapControl_OnPositionChanged;
            gMapControl.OnMarkerClick += GMapControl_OnMarkerClick;
        }

        private void DrawRadarStation()
        {
            // Crear marcador para la estación Radar/ADS-B
            GMapMarker radarMarker = new GMarkerGoogle(
                new PointLatLng(RADAR_LAT, RADAR_LON),
                CreateRadarStationBitmap()
            );

            // Usamos nuestro panel hover personalizado en lugar del tooltip integrado
            radarMarker.ToolTipMode = MarkerTooltipMode.Never;
            radarMarker.Tag = "RADAR";

            stationOverlay.Markers.Add(radarMarker);
        }

        private Bitmap CreateRadarStationBitmap()
        {
            Bitmap bmp = new Bitmap(40, 40);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Dibujar círculos concéntricos (señal radar)
                using (Pen radarPen = new Pen(Color.FromArgb(150, 255, 0, 0), 2))
                {
                    g.DrawEllipse(radarPen, 10, 10, 20, 20);
                    g.DrawEllipse(radarPen, 14, 14, 12, 12);
                }

                // Torre central
                using (SolidBrush brush = new SolidBrush(Color.Red))
                {
                    g.FillRectangle(brush, 18, 15, 4, 10);
                }

                // Antena
                using (Pen antennaPen = new Pen(Color.DarkRed, 2))
                {
                    g.DrawLine(antennaPen, 20, 15, 20, 8);
                }

                // Base
                using (SolidBrush baseBrush = new SolidBrush(Color.DarkGray))
                {
                    g.FillRectangle(baseBrush, 15, 25, 10, 3);
                }

                // Texto "BCN"
                using (Font font = new Font("Arial", 6, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("BCN", font, textBrush, new RectangleF(0, 30, 40, 10), sf);
                }
            }

            return bmp;
        }

        private void InitializeSimulation()
        {
            // Configurar timer
            simulationTimer = new System.Windows.Forms.Timer();
            simulationTimer.Interval = 1000; // 1 segundo por defecto
            simulationTimer.Tick += SimulationTimer_Tick;

            // Configurar trackbar de velocidad
            speed_trackBar.Minimum = 1;
            speed_trackBar.Maximum = 10;
            speed_trackBar.Value = 1;
            speed_trackBar.TickFrequency = 1;
            speed_trackBar.ValueChanged += Speed_trackBar_ValueChanged;
        }

        private void InitializeUI()
        {
            // Label de tiempo (mostrado como panel de tiempo de simulación)
            timeLabel = new Label
            {
                Location = new Point(10, 580), // posición inicial, se recalcula dinámicamente
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = infoPanelBackColor, // mismo color que scrollbar
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "00:00:00",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            this.Controls.Add(timeLabel);
            timeLabel.BringToFront();

            // Panel de leyenda
            CreateLegendPanel();

            // Panel de información de aeronave (derecha)
            CreateAircraftInfoPanel();

            // Activar doble buffer para suavizar refrescos
            EnableDoubleBuffering(this);
            EnableDoubleBuffering(map_panel);
            if (aircraftInfoPanel != null) EnableDoubleBuffering(aircraftInfoPanel);
            if (aircraftInfoContent != null) EnableDoubleBuffering(aircraftInfoContent);

            // Aplicar estilo consistente a los botones de la izquierda
            ApplyLeftButtonsStyle();

            // Reposicionar a la esquina inferior derecha e izquierda y reaccionar a cambios de tamaño/ventana completa
            this.Resize += (s, e) => { PositionInfoPanelsBottomRight(); PositionBottomLeftButtons(); PositionRightInfoPanel(); };
            PositionInfoPanelsBottomRight();
            PositionBottomLeftButtons();
            PositionRightInfoPanel();
        }

        // Aplica un estilo uniforme a los botones de la columna izquierda
        private void ApplyLeftButtonsStyle()
        {
            var buttons = new Button[]
            {
                avanzar_button,
                retroceder_button,
                auto_button,
                reset_button,
                Filter_button,
                csv_button,
                close_button
            };

            foreach (var b in buttons)
            {
                if (b == null) continue;
                ApplyButtonStyle(b);
            }
        }

        private void ApplyButtonStyle(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = infoPanelBackColor; // mismo color que scrollbar/panel
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            b.FlatAppearance.MouseOverBackColor = infoPanelBackColor; // mantener exactamente el mismo color
            b.FlatAppearance.MouseDownBackColor = infoPanelBackColor; // mantener exactamente el mismo color
        }

        private void InitializeHoverPanel()
        {
            hoverPanel = new Panel
            {
                Size = new Size(80, 22),
                BackColor = Color.FromArgb(230, 30, 30, 30),
                Visible = false
            };
            hoverPanel.Padding = new Padding(4, 2, 4, 2);
            hoverPanel.BorderStyle = BorderStyle.FixedSingle;

            hoverLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            hoverPanel.Controls.Add(hoverLabel);
            gMapControl.Controls.Add(hoverPanel);
            hoverPanel.BringToFront();
        }

        private void GMapControl_OnMarkerEnter(GMapMarker item)
        {
            if (item == null) return;

            // Aeronaves: mostrar TI/ID
            if (item.Overlay == aircraftOverlay)
            {
                currentHoverMarker = item;
                string ti = null;
                if (item.Tag is MarkerMeta meta)
                {
                    ti = string.IsNullOrWhiteSpace(meta.Callsign) ? meta.AircraftId : meta.Callsign;
                }
                else
                {
                    ti = item.Tag as string;
                }
                if (string.IsNullOrWhiteSpace(ti)) ti = "(sin TI)";
                hoverLabel.Text = ti;
                PositionHoverPanel();
                hoverPanel.Visible = true;
                return;
            }

            // Radar: mostrar "Radar"
            if (item.Overlay == stationOverlay)
            {
                currentHoverMarker = item;
                hoverLabel.Text = "Radar";
                PositionHoverPanel();
                hoverPanel.Visible = true;
                return;
            }
        }

        private void GMapControl_OnMarkerLeave(GMapMarker item)
        {
            if (currentHoverMarker == item)
            {
                hoverPanel.Visible = false;
                currentHoverMarker = null;
            }
        }

        private void GMapControl_OnMapZoomChanged()
        {
            if (currentHoverMarker != null && hoverPanel.Visible)
            {
                PositionHoverPanel();
            }
        }

        private void GMapControl_OnPositionChanged(PointLatLng point)
        {
            if (currentHoverMarker != null && hoverPanel.Visible)
            {
                PositionHoverPanel();
            }
        }

        private void PositionInfoPanelsBottomRight()
        {
            // Márgenes desde el borde inferior-derecho
            const int margin = 12;

            // Asegurar que los controles existen
            if (legendPanel != null)
            {
                int lx = Math.Max(margin, this.ClientSize.Width - legendPanel.Width - margin);
                int ly = Math.Max(margin, this.ClientSize.Height - legendPanel.Height - margin);
                legendPanel.Location = new Point(lx, ly);
            }

            if (timeLabel != null)
            {
                // Colocar el tiempo justo encima de la leyenda, alineado a la derecha
                int tx;
                int ty;
                if (legendPanel != null)
                {
                    tx = legendPanel.Left + legendPanel.Width - timeLabel.Width; // alineado a derecha de la leyenda
                    ty = legendPanel.Top - timeLabel.Height - 8; // 8px separación vertical
                }
                else
                {
                    tx = this.ClientSize.Width - timeLabel.Width - margin;
                    ty = this.ClientSize.Height - timeLabel.Height - margin;
                }

                // Asegurar que no se salga por arriba/izquierda
                tx = Math.Max(margin, tx);
                ty = Math.Max(margin, ty);

                timeLabel.Location = new Point(tx, ty);
            }
        }

        private void PositionBottomLeftButtons()
        {
            const int margin = 12;
            const int spacing = 8;

            // Asegurar que existan los tres botones
            if (Filter_button == null || csv_button == null || close_button == null)
                return;

            // Fijar anclaje inferior-izquierdo para mayor robustez
            Filter_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            csv_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            close_button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            int left = Math.Max(margin, 12); // margen izquierdo
            int bottom = this.ClientSize.Height - margin;

            // Colocar en orden: abajo Cerrar, encima CSV, encima Filtrar
            close_button.Location = new Point(left, bottom - close_button.Height);
            csv_button.Location = new Point(left, close_button.Top - spacing - csv_button.Height);
            Filter_button.Location = new Point(left, csv_button.Top - spacing - Filter_button.Height);
        }

        private void PositionHoverPanel()
        {
            if (currentHoverMarker == null) return;

            // Convertir posición geográfica del marcador a coordenadas locales del control
            GMap.NET.GPoint local = gMapControl.FromLatLngToLocal(currentHoverMarker.Position);

            // Ajustar por el offset del propio marcador para obtener el centro visual
            int centerX = (int)local.X + currentHoverMarker.Offset.X + (currentHoverMarker.Size.Width / 2);
            int centerY = (int)local.Y + currentHoverMarker.Offset.Y + (currentHoverMarker.Size.Height / 2);

            // Si es el radar, el icono es más alto: ajustar un poco más el Y
            bool isRadar = currentHoverMarker.Overlay == stationOverlay ||
                           (currentHoverMarker.Tag is string s && s == "RADAR");

            // Colocar el panel centrado horizontalmente por encima del icono
            int panelX = centerX - (hoverPanel.Width / 2);
            int extraOffsetY = isRadar ? 10 : 0;
            int panelY = centerY - currentHoverMarker.Size.Height / 2 - hoverPanel.Height - 6 - extraOffsetY; // margen

            // Asegurar que no salga de los límites del control
            panelX = Math.Max(0, Math.Min(panelX, gMapControl.Width - hoverPanel.Width));
            panelY = Math.Max(0, Math.Min(panelY, gMapControl.Height - hoverPanel.Height));

            hoverPanel.Location = new Point(panelX, panelY);
        }

        // --- Anti-parpadeo y helpers de UI ---
        private const int WM_SETREDRAW = 0x000B;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private void SuspendRedraw(Control c)
        {
            if (c == null || !c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        private void ResumeRedraw(Control c)
        {
            if (c == null || !c.IsHandleCreated) return;
            SendMessage(c.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            c.Invalidate();
            c.Update();
        }

        private void EnableDoubleBuffering(Control c)
        {
            if (c == null) return;
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch { /* ignore */ }
        }

        private void CreateAircraftInfoPanel()
        {
            if (aircraftInfoPanel != null) return;

            aircraftInfoPanel = new Panel
            {
                Size = new Size(320, 440),
                BackColor = infoPanelBackColor, // semi-transparente oscuro
                BorderStyle = BorderStyle.None,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Estilo moderno con esquinas redondeadas y sombra ligera (borde pintado)
            aircraftInfoPanel.Padding = new Padding(12);
            aircraftInfoPanel.Paint += (s, e) =>
            {
                using (var path = new GraphicsPath())
                {
                    int radius = 10;
                    var rect = new Rectangle(0, 0, aircraftInfoPanel.Width - 1, aircraftInfoPanel.Height - 1);
                    int d = radius * 2;
                    path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                    path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                    path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                    path.CloseAllFigures();
                    aircraftInfoPanel.Region = new Region(path);

                    using (Pen borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.DrawPath(borderPen, path);
                    }
                }
            };

            // Título y botón cerrar
            var titleContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.FromArgb(0, 0, 0, 0)
            };

            aircraftInfoTitle = new Label
            {
                Text = "Información de Aeronave",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Padding = new Padding(8, 8, 8, 0)
            };

            Button closeInfoBtn = new Button
            {
                Text = "✕",
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = infoPanelBackColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right,
                Margin = new Padding(0)
            };
            closeInfoBtn.FlatAppearance.BorderSize = 0;
            closeInfoBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(Math.Max(0, infoPanelBackColor.A - 10), infoPanelBackColor);
            closeInfoBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(Math.Max(0, infoPanelBackColor.A - 20), infoPanelBackColor);
            closeInfoBtn.Click += (s, e) => { aircraftInfoPanel.Visible = false; selectedAircraftId = null; };

            // Línea separadora sutil
            var divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(60, 255, 255, 255)
            };

            titleContainer.Controls.Add(aircraftInfoTitle);
            titleContainer.Controls.Add(closeInfoBtn);
            titleContainer.Controls.Add(divider);

            // Contenido scrollable
            aircraftInfoContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(8)
            };

            // Máscara para ocultar la scrollbar clara y unificar color
            infoScrollBarMask = new Panel
            {
                Dock = DockStyle.Right,
                Width = SystemInformation.VerticalScrollBarWidth,
                BackColor = infoPanelBackColor,
                Visible = true
            };

            aircraftInfoPanel.Controls.Add(infoScrollBarMask);
            aircraftInfoPanel.Controls.Add(aircraftInfoContent);
            aircraftInfoPanel.Controls.Add(titleContainer);

            // Mantener máscara al frente en cambios de tamaño
            aircraftInfoPanel.Resize += (s, e) => { if (infoScrollBarMask != null) infoScrollBarMask.BringToFront(); };

            this.Controls.Add(aircraftInfoPanel);
            aircraftInfoPanel.BringToFront();
            infoScrollBarMask.BringToFront();

            PositionRightInfoPanel();
        }

        private void PositionRightInfoPanel()
        {
            if (aircraftInfoPanel == null) return;

            int margin = 12;
            int width = 300;
            int height = this.ClientSize.Height - 2 * margin;
            int x = this.ClientSize.Width - width - margin;
            int y = margin;
            aircraftInfoPanel.Bounds = new Rectangle(x, y, width, height);
        }

        private void GMapControl_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (item == null) return;

            // Click en estación radar -> mostrar info de radar
            if (item.Overlay == stationOverlay)
            {
                selectedAircraftId = null; // no hay aeronave seleccionada
                BuildRadarInfoContent();
                aircraftInfoPanel.Visible = true;
                PositionRightInfoPanel();
                return;
            }

            // Click en aeronave -> mostrar info de aeronave
            if (item.Overlay != aircraftOverlay)
                return;

            string aircraftId = null;
            string callsign = null;
            string category = null;
            if (item.Tag is MarkerMeta meta)
            {
                aircraftId = meta.AircraftId;
                callsign = meta.Callsign;
                category = meta.Category;
            }
            else
            {
                callsign = item.Tag as string;
                // Intentar deducir aircraftId como callsign
                aircraftId = callsign;
            }

            if (string.IsNullOrWhiteSpace(aircraftId)) return;

            selectedAircraftId = aircraftId;
            var currentTime = (currentTimeIndex >= 0 && timeStamps != null && currentTimeIndex < timeStamps.Count)
                ? timeStamps[currentTimeIndex]
                : DateTime.MinValue;

            BuildAircraftInfoContent(selectedAircraftId, currentTime);
            aircraftInfoPanel.Visible = true;
            PositionRightInfoPanel();
        }

        private void BuildAircraftInfoContent(string aircraftId, DateTime atTime)
        {
            if (aircraftInfoContent == null) return;

            // Evitar parpadeo al reconstruir
            SuspendRedraw(aircraftInfoContent);
            aircraftInfoContent.SuspendLayout();
            aircraftInfoContent.Controls.Clear();

            // Encabezado y datos actuales básicos
            string baseId = aircraftId.Contains("|") ? aircraftId.Split('|')[0] : aircraftId;
            string titleText = "Aeronave: " + baseId;
            string callsignForSearch = baseId;

            if (aircraftTrajectories != null && aircraftTrajectories.ContainsKey(aircraftId))
            {
                var latest = aircraftTrajectories[aircraftId]
                    .Where(p => atTime == DateTime.MinValue || p.Time <= atTime)
                    .OrderByDescending(p => p.Time)
                    .FirstOrDefault();

                if (latest != null)
                {
                    string callsign = string.IsNullOrWhiteSpace(latest.Callsign) ? baseId : latest.Callsign;
                    callsignForSearch = callsign;
                    titleText = $"Aeronave: {callsign}";
                    aircraftInfoTitle.Text = titleText;

                    AddSectionHeader(aircraftInfoContent, "Instantáneo actual");
                    AddInfoRow(aircraftInfoContent, "Sistema", GetSystemName(latest.Category));
                    AddInfoRow(aircraftInfoContent, "Tiempo sim.", latest.Time.ToString("HH:mm:ss"));
                    AddInfoRow(aircraftInfoContent, "Lat", latest.Lat.ToString("F6"));
                    AddInfoRow(aircraftInfoContent, "Lon", latest.Lon.ToString("F6"));
                    if (latest.Altitude.HasValue)
                        AddInfoRow(aircraftInfoContent, "Altitud", latest.Altitude.Value.ToString("F0") + " ft");
                    if (latest.Speed.HasValue)
                        AddInfoRow(aircraftInfoContent, "Velocidad", latest.Speed.Value.ToString("F1") + " kt");
                    if (latest.Heading.HasValue)
                        AddInfoRow(aircraftInfoContent, "Rumbo", latest.Heading.Value.ToString("F0") + "°");
                    if (!string.IsNullOrWhiteSpace(latest.Mode3A))
                        AddInfoRow(aircraftInfoContent, "Mode_3A", latest.Mode3A);
                }
                else
                {
                    aircraftInfoTitle.Text = titleText;
                    AddInfoRow(aircraftInfoContent, "Estado", "Sin datos en este instante");
                }
            }
            else
            {
                aircraftInfoTitle.Text = titleText;
            }

            // Buscar registros completos CAT021 y CAT048 más cercanos al tiempo
            var rec021 = FindBestCat021Record(baseId, callsignForSearch, atTime);
            var rec048 = FindBestCat048Record(baseId, callsignForSearch, atTime);

            if (rec021 != null)
            {
                AddSectionHeader(aircraftInfoContent, "Datos CAT021 (ADS-B)");
                AddInfoRow(aircraftInfoContent, "SAC", rec021.SAC.ToString());
                AddInfoRow(aircraftInfoContent, "SIC", rec021.SIC.ToString());
                AddInfoRow(aircraftInfoContent, "Hora", rec021.Time ?? "N/A");
                AddInfoRow(aircraftInfoContent, "LAT", rec021.LAT.ToString("F6"));
                AddInfoRow(aircraftInfoContent, "LON", rec021.LON.ToString("F6"));
                AddInfoRow(aircraftInfoContent, "Mode3A_Code", string.IsNullOrWhiteSpace(rec021.Mode3A) ? "N/A" : rec021.Mode3A);
                AddInfoRow(aircraftInfoContent, "FL", rec021.FL.ToString("F0"));
                AddInfoRow(aircraftInfoContent, "ModeC_Corrected", (rec021.FL < 60 && rec021.ModeC_Corrected.HasValue) ? rec021.ModeC_Corrected.Value.ToString("F0") + " ft" : string.Empty);
                AddInfoRow(aircraftInfoContent, "TA", string.IsNullOrWhiteSpace(rec021.TA) ? "N/A" : rec021.TA);
                AddInfoRow(aircraftInfoContent, "TI", string.IsNullOrWhiteSpace(rec021.TI) ? "N/A" : rec021.TI);
                {
                    string bpStr = "NV";
                    if (rec021.BP.HasValue)
                    {
                        if (rec021.BP.Value >= 1000 && rec021.BP.Value <= 1030)
                            bpStr = rec021.BP.Value.ToString("F2") + " hPa";
                    }
                    AddInfoRow(aircraftInfoContent, "BP", bpStr);
                }
                AddInfoRow(aircraftInfoContent, "OnGround", rec021.IsOnGround ? "True" : "False");
            }

            if (rec048 != null)
            {
                AddSectionHeader(aircraftInfoContent, "Datos CAT048 (Radar)");
                AddInfoRow(aircraftInfoContent, "SAC", rec048.SAC.HasValue ? rec048.SAC.Value.ToString() : "N/A");
                AddInfoRow(aircraftInfoContent, "SIC", rec048.SIC.HasValue ? rec048.SIC.Value.ToString() : "N/A");
                AddInfoRow(aircraftInfoContent, "Hora", rec048.Time ?? "N/A");
                AddInfoRow(aircraftInfoContent, "Lat", FmtDouble(rec048.LAT, "F8"));
                AddInfoRow(aircraftInfoContent, "Lon", FmtDouble(rec048.LON, "F8"));
                AddInfoRow(aircraftInfoContent, "H_wgs", FmtDouble(rec048.H_m, "F14"));
                AddInfoRow(aircraftInfoContent, "h_ft", FmtDouble(rec048.H, "F14"));
                AddInfoRow(aircraftInfoContent, "RHO", FmtDouble(rec048.RHO, "F6"));
                AddInfoRow(aircraftInfoContent, "THETA", FmtDouble(rec048.THETA, "F6"));
                AddInfoRow(aircraftInfoContent, "Mode_3A", string.IsNullOrWhiteSpace(rec048.Mode3A) ? "N/A" : rec048.Mode3A);
                AddInfoRow(aircraftInfoContent, "Flight_level", FmtDouble(rec048.FL, "F3"));
                AddInfoRow(aircraftInfoContent, "ModeC_corrected", (rec048.FL.HasValue && rec048.FL.Value < 60.0 && rec048.H.HasValue) ? rec048.H.Value.ToString("F3") : string.Empty);
                AddInfoRow(aircraftInfoContent, "Target_address", string.IsNullOrWhiteSpace(rec048.TA) ? "N/A" : rec048.TA);
                AddInfoRow(aircraftInfoContent, "Target_identification", string.IsNullOrWhiteSpace(rec048.TI) ? "N/A" : rec048.TI);
                // Mode_S (BDS registers)
                {
                    var bdsList = new List<string>();
                    if (rec048.BP.HasValue) bdsList.Add("BDS:4,0");
                    if (rec048.RA.HasValue || rec048.TAR.HasValue || rec048.TAS.HasValue || rec048.GS.HasValue)
                        bdsList.Add("BDS:5,0");
                    if (rec048.HDG.HasValue || rec048.IAS.HasValue || rec048.MACH.HasValue || rec048.BAR.HasValue || rec048.IVV.HasValue)
                        bdsList.Add("BDS:6,0");
                    string modeS = bdsList.Count > 0 ? string.Join(" ", bdsList) : "N/A";
                    AddInfoRow(aircraftInfoContent, "Mode_S", modeS);
                }
                // QNH BP
                AddInfoRow(aircraftInfoContent, "BP", FmtDouble(rec048.BP, "F3"));
                AddInfoRow(aircraftInfoContent, "RA", FmtDouble(rec048.RA, "F3"));
                AddInfoRow(aircraftInfoContent, "TTA", FmtDouble(rec048.TTA, "F3"));
                AddInfoRow(aircraftInfoContent, "GS", FmtDouble(rec048.GS, "F3"));
                AddInfoRow(aircraftInfoContent, "TAR", FmtDouble(rec048.TAR, "F3"));
                AddInfoRow(aircraftInfoContent, "TAS", FmtDouble(rec048.TAS, "F3"));
                AddInfoRow(aircraftInfoContent, "HDG", FmtDouble(rec048.HDG, "F6"));
                AddInfoRow(aircraftInfoContent, "IAS", FmtDouble(rec048.IAS, "F3"));
                AddInfoRow(aircraftInfoContent, "MACH", FmtDouble(rec048.MACH, "F3"));
                AddInfoRow(aircraftInfoContent, "BAR", FmtDouble(rec048.BAR, "F3"));
                AddInfoRow(aircraftInfoContent, "IVV", FmtDouble(rec048.IVV, "F3"));
                AddInfoRow(aircraftInfoContent, "Track_number", FmtInt(rec048.TN));
                AddInfoRow(aircraftInfoContent, "Ground_speedkt", FmtDouble(rec048.GSSD, "F3"));
                AddInfoRow(aircraftInfoContent, "Heading", FmtDouble(rec048.HDG2, "F4"));
                // SAT230 (communications capability)
                {
                    string sat230 = "";
                    if (rec048.COM.HasValue || rec048.STAT_230.HasValue)
                    {
                        var parts = new List<string>();
                        if (rec048.COM.HasValue) parts.Add($"COM:{rec048.COM}");
                        if (rec048.STAT_230.HasValue)
                        {
                            var flags = new List<string>();
                            if (rec048.SI.HasValue) flags.Add($"SI={(rec048.SI.Value ? 1 : 0)}");
                            if (rec048.MSSC.HasValue) flags.Add($"MSSC={(rec048.MSSC.Value ? 1 : 0)}");
                            if (rec048.ARC.HasValue) flags.Add($"ARC={(rec048.ARC.Value ? 1 : 0)}");
                            if (rec048.AIC.HasValue) flags.Add($"AIC={(rec048.AIC.Value ? 1 : 0)}");
                            if (flags.Count > 0) parts.Add("[" + string.Join(",", flags) + "]");
                        }
                        sat230 = string.Join(" ", parts);
                    }
                    AddInfoRow(aircraftInfoContent, "SAT230", string.IsNullOrEmpty(sat230) ? "N/A" : sat230);
                }
                // Campos adicionales informativos
                AddInfoRow(aircraftInfoContent, "TypDesc", FmtInt(rec048.TypDesc));
                AddInfoRow(aircraftInfoContent, "RAB", rec048.RABPresent.HasValue ? (rec048.RABPresent.Value ? "Sí" : "No") : "N/A");
            }

            if (rec021 == null && rec048 == null)
            {
                AddInfoRow(aircraftInfoContent, "Estado", "No se encontraron registros cercanos al instante actual");
            }

            aircraftInfoContent.ResumeLayout();
            if (infoScrollBarMask != null) infoScrollBarMask.BringToFront();
            ResumeRedraw(aircraftInfoContent);
        }

        private Cat021 FindBestCat021Record(string aircraftId, string callsign, DateTime atTime)
        {
            if (currentFilteredData == null) return null;
            var list = currentFilteredData.GetCat021();
            if (list == null || list.Count == 0) return null;
            string id = aircraftId;
            string ti = callsign;
            DateTime target = atTime == DateTime.MinValue ? DateTime.MaxValue : atTime;

            var candidates = list.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.TA) && r.TA == id) ||
                    (!string.IsNullOrWhiteSpace(r.TI) && r.TI == ti) ||
                    (!string.IsNullOrWhiteSpace(r.TI) && r.TI == id)
                )
                .Select(r => new { rec = r, t = ParseTime(r.Time) })
                .Where(x => x.t != DateTime.MinValue)
                .ToList();
            if (candidates.Count == 0) return null;

            // Preferir <= atTime, más cercano hacia atrás; si no, el más cercano absoluto
            var before = candidates.Where(x => x.t <= target).OrderByDescending(x => x.t).FirstOrDefault();
            if (before != null) return before.rec;
            return candidates.OrderBy(x => Math.Abs((x.t - target).TotalSeconds)).First().rec;
        }

        private Cat048 FindBestCat048Record(string aircraftId, string callsign, DateTime atTime)
        {
            if (currentFilteredData == null) return null;
            var list = currentFilteredData.GetCat048();
            if (list == null || list.Count == 0) return null;
            string id = aircraftId;
            string ti = callsign;
            DateTime target = atTime == DateTime.MinValue ? DateTime.MaxValue : atTime;

            var candidates = list.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.TA) && r.TA == id) ||
                    (!string.IsNullOrWhiteSpace(r.TI) && r.TI == ti) ||
                    (!string.IsNullOrWhiteSpace(r.TI) && r.TI == id)
                )
                .Select(r => new { rec = r, t = ParseTime(r.Time) })
                .Where(x => x.t != DateTime.MinValue)
                .ToList();
            if (candidates.Count == 0) return null;

            var before = candidates.Where(x => x.t <= target).OrderByDescending(x => x.t).FirstOrDefault();
            if (before != null) return before.rec;
            return candidates.OrderBy(x => Math.Abs((x.t - target).TotalSeconds)).First().rec;
        }

        private void AddInfoRow(Control parent, string label, string value)
        {
            int currentY = 0;
            if (parent.Controls.Count > 0)
            {
                currentY = parent.Controls.Cast<Control>().Max(c => c.Bottom) + 4;
            }
            var l1 = new Label
            {
                Text = label + ":",
                Location = new Point(0, currentY),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 255, 255, 255),
                BackColor = Color.Transparent
            };
            var l2 = new Label
            {
                Text = value,
                Location = new Point(125, currentY),
                Size = new Size(Math.Max(10, parent.Width - 130), 20),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 180, 220, 255),
                BackColor = Color.Transparent
            };
            l2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(l1);
            parent.Controls.Add(l2);
        }

        private void AddSectionHeader(Control parent, string text)
        {
            int currentY = 0;
            if (parent.Controls.Count > 0)
            {
                currentY = parent.Controls.Cast<Control>().Max(c => c.Bottom) + 12;
            }
            var header = new Label
            {
                Text = text,
                AutoSize = false,
                Location = new Point(0, currentY),
                Size = new Size(parent.Width - 16, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255, 255),
                BackColor = Color.Transparent
            };
            header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            parent.Controls.Add(header);
        }

        private string FmtDouble(double? v, string format = "F2", string suffix = "")
        {
            if (!v.HasValue) return "N/A";
            return v.Value.ToString(format) + (string.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix);
        }

        private string FmtInt(int? v, string suffix = "")
        {
            if (!v.HasValue) return "N/A";
            return v.Value + (string.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix);
        }

        private string FmtBool(bool? v)
        {
            if (!v.HasValue) return "N/A";
            return v.Value ? "Sí" : "No";
        }

        private void BuildRadarInfoContent()
        {
            if (aircraftInfoContent == null) return;

            SuspendRedraw(aircraftInfoContent);
            aircraftInfoContent.SuspendLayout();
            aircraftInfoContent.Controls.Clear();

            aircraftInfoTitle.Text = "Estación Radar BCN";

            AddSectionHeader(aircraftInfoContent, "Información de la estación");
            AddInfoRow(aircraftInfoContent, "Nombre", "Radar BCN");
            AddInfoRow(aircraftInfoContent, "Lat", RADAR_LAT.ToString("F6"));
            AddInfoRow(aircraftInfoContent, "Lon", RADAR_LON.ToString("F6"));
            AddInfoRow(aircraftInfoContent, "Elevación", RADAR_ELEVATION.ToString("F2") + " m");

            AddSectionHeader(aircraftInfoContent, "Mapa");
            AddInfoRow(aircraftInfoContent, "Provider", gMapControl?.MapProvider?.Name ?? "N/A");
            AddInfoRow(aircraftInfoContent, "Zoom", gMapControl != null ? gMapControl.Zoom.ToString("F0") : "N/A");

            aircraftInfoContent.ResumeLayout();
            if (infoScrollBarMask != null) infoScrollBarMask.BringToFront();
            ResumeRedraw(aircraftInfoContent);
        }

        private void CreateLegendPanel()
        {
            legendPanel = new Panel
            {
                Location = new Point(10, 430), // posición inicial, se recalcula dinámicamente
                Size = new Size(150, 220),
                BackColor = Color.FromArgb(230, 255, 255, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Label legendTitle = new Label
            {
                Text = "LEYENDA",
                Location = new Point(5, 5),
                Size = new Size(140, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            legendPanel.Controls.Add(legendTitle);

            // CAT021 (ADS-B)
            PictureBox cat021Icon = CreateAircraftIcon(Color.FromArgb(255, 0, 120, 215), 10, 35);
            Label cat021Label = new Label
            {
                Text = "CAT021 (ADS-B)",
                Location = new Point(30, 33),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 8)
            };
            legendPanel.Controls.Add(cat021Icon);
            legendPanel.Controls.Add(cat021Label);

            // CAT048 (Radar)
            PictureBox cat048Icon = CreateAircraftIcon(Color.FromArgb(255, 0, 180, 0), 10, 60);
            Label cat048Label = new Label
            {
                Text = "CAT048 (Radar)",
                Location = new Point(30, 58),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 8)
            };
            legendPanel.Controls.Add(cat048Icon);
            legendPanel.Controls.Add(cat048Label);

            // Ambos sistemas (eliminado: ahora se tratan independientemente CAT021 y CAT048)

            // Trayectoria
            Panel trajectoryLine = new Panel
            {
                Location = new Point(10, 112),
                Size = new Size(15, 2),
                BackColor = Color.FromArgb(150, Color.Navy)
            };
            Label trajectoryLabel = new Label
            {
                Text = "Trayectoria",
                Location = new Point(30, 108),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 8)
            };
            legendPanel.Controls.Add(trajectoryLine);
            legendPanel.Controls.Add(trajectoryLabel);

            // Estación Radar
            PictureBox radarIcon = new PictureBox
            {
                Location = new Point(10, 135),
                Size = new Size(15, 15),
                Image = CreateRadarStationBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            Label radarLabel = new Label
            {
                Text = "Estación BCN",
                Location = new Point(30, 133),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 8)
            };
            legendPanel.Controls.Add(radarIcon);
            legendPanel.Controls.Add(radarLabel);

            this.Controls.Add(legendPanel);
            legendPanel.BringToFront();
        }

        private PictureBox CreateAircraftIcon(Color color, int x, int y)
        {
            // Mapear color a categoría esperada para iconos de leyenda
            string category = "CAT048"; // por defecto
            if (color == Color.FromArgb(255, 0, 120, 215)) category = "CAT021"; // azul

            Bitmap bmp = spriteManager != null
                ? spriteManager.GetAircraftSprite("LEGEND", category, 0)
                : new Bitmap(18, 18);

            PictureBox pb = new PictureBox
            {
                Location = new Point(x, y),
                Size = new Size(18, 18),
                Image = bmp,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            return pb;
        }

        private void ProcessTrajectories()
        {
            aircraftTrajectories = new Dictionary<string, List<AircraftPosition>>();
            timeStamps = new List<DateTime>();

            // Calcular límites geográficos a partir de los datos filtrados actuales
            ComputeGeoBoundsFromCurrentData();

            var allRecords = currentFilteredData.GetAll();

            foreach (var record in allRecords)
            {
                AircraftPosition position = null;
                string aircraftId = null;

                if (record.Category == AsterixCombinedList.AsterixCategory.CAT021 && record.Cat021 != null)
                {
                    var r = record.Cat021;
                    aircraftId = !string.IsNullOrWhiteSpace(r.TA) ? r.TA : r.TI;

                    if (string.IsNullOrWhiteSpace(aircraftId))
                        continue;

                    DateTime time = ParseTime(r.Time);

                    position = new AircraftPosition
                    {
                        Time = time,
                        Lat = r.LAT,
                        Lon = r.LON,
                        Altitude = r.ModeC_Corrected,
                        Callsign = r.TI,
                        Category = "CAT021",
                        Mode3A = r.Mode3A
                    };
                }
                else if (record.Category == AsterixCombinedList.AsterixCategory.CAT048 && record.Cat048 != null)
                {
                    var r = record.Cat048;

                    if (!r.LAT.HasValue || !r.LON.HasValue)
                        continue;

                    aircraftId = !string.IsNullOrWhiteSpace(r.TA) ? r.TA : r.TI;

                    if (string.IsNullOrWhiteSpace(aircraftId))
                        continue;

                    DateTime time = ParseTime(r.Time);

                    position = new AircraftPosition
                    {
                        Time = time,
                        Lat = r.LAT.Value,
                        Lon = r.LON.Value,
                        Altitude = r.H,
                        Callsign = r.TI,
                        Category = "CAT048",
                        Mode3A = r.Mode3A,
                        Speed = r.GS ?? r.GSSD,
                        Heading = r.HDG ?? r.HDG2
                    };
                }

                if (position != null && aircraftId != null)
                {
                    // Clave compuesta por ID base y categoría para mantener trayectorias separadas por sistema
                    string aircraftKey = aircraftId + "|" + position.Category;

                    if (!aircraftTrajectories.ContainsKey(aircraftKey))
                    {
                        aircraftTrajectories[aircraftKey] = new List<AircraftPosition>();
                        recentTrajectories[aircraftKey] = new Queue<TrajectoryPoint>();
                    }

                    aircraftTrajectories[aircraftKey].Add(position);

                    if (!timeStamps.Contains(position.Time))
                    {
                        timeStamps.Add(position.Time);
                    }
                }
            }

            // Limpiar tiempos inválidos y ordenar timestamps (de los datos originales)
            // Quitar marcas sin hora válida (DateTime.MinValue)
            timeStamps = timeStamps
                .Where(t => t != DateTime.MinValue)
                .OrderBy(t => t)
                .ToList();

            // Filtrar también posiciones inválidas por aeronave
            foreach (var key in aircraftTrajectories.Keys.ToList())
            {
                aircraftTrajectories[key] = aircraftTrajectories[key]
                    .Where(p => p.Time != DateTime.MinValue)
                    .OrderBy(p => p.Time)
                    .ToList();
            }

            // Ordenar posiciones de cada aeronave por tiempo
            foreach (var key in aircraftTrajectories.Keys.ToList())
            {
                aircraftTrajectories[key] = aircraftTrajectories[key].OrderBy(p => p.Time).ToList();
            }


            // Construir una línea temporal continua de 1 segundo entre el primer y el último tiempo
            if (timeStamps.Count > 0)
            {
                DateTime start = new DateTime(timeStamps.First().Year, timeStamps.First().Month, timeStamps.First().Day,
                                              timeStamps.First().Hour, timeStamps.First().Minute, timeStamps.First().Second);
                DateTime end = new DateTime(timeStamps.Last().Year, timeStamps.Last().Month, timeStamps.Last().Day,
                                            timeStamps.Last().Hour, timeStamps.Last().Minute, timeStamps.Last().Second);
                var perSecond = new List<DateTime>();
                for (var t = start; t <= end; t = t.AddSeconds(1))
                {
                    perSecond.Add(t);
                }
                timeStamps = perSecond;
            }
        }


        private DateTime ParseTime(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return DateTime.MinValue;

            try
            {
                // Formato esperado: "HH:mm:ss.fff" o "HH:mm:ss:fff"
                var parts = timeString.Split(new[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 3)
                {
                    int hours = int.Parse(parts[0]);
                    int minutes = int.Parse(parts[1]);
                    int seconds = int.Parse(parts[2]);
                    int milliseconds = parts.Length > 3 ? int.Parse(parts[3]) : 0;

                    return DateTime.Today.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds).AddMilliseconds(milliseconds);
                }
            }
            catch
            {
                // En caso de error, retornar fecha por defecto
            }

            return DateTime.MinValue;
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (timeStamps == null || timeStamps.Count == 0)
                    return;

                // Establecer pasos según el multiplicador de velocidad
                int steps = Math.Max(1, speedMultiplier);
                lastAdvanceSteps = steps;

                // Actualizar visualización en el índice actual
                UpdateSimulation();

                // Mover el índice según la dirección y la velocidad
                if (simulationDirection >= 0)
                {
                    currentTimeIndex = Math.Min(currentTimeIndex + steps, timeStamps.Count);

                    if (currentTimeIndex >= timeStamps.Count)
                    {
                        StopSimulation();
                        MessageBox.Show("Simulación completada", "Fin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    currentTimeIndex = Math.Max(0, currentTimeIndex - steps);

                    if (currentTimeIndex <= 0)
                    {
                        StopSimulation();
                        MessageBox.Show("Inicio de la simulación alcanzado", "Fin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                try { StopSimulation(); } catch { }
                MessageBox.Show($"Se produjo un error durante la simulación:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSimulation()
        {
            try
            {
                if (timeStamps == null || currentTimeIndex >= timeStamps.Count)
                    return;

                DateTime currentTime = timeStamps[currentTimeIndex];

                // Actualizar label de tiempo
                if (timeLabel != null)
                    timeLabel.Text = currentTime.ToString("HH:mm:ss");

                if (aircraftOverlay == null || trajectoryOverlay == null || gMapControl == null)
                    return;

                // Limpiar overlay de aeronaves
                aircraftOverlay.Markers.Clear();
                trajectoryOverlay.Routes.Clear();

                // Actualizar trayectorias recientes usando los pasos avanzados
                int steps = Math.Max(1, lastAdvanceSteps);
                UpdateRecentTrajectories(currentTime, steps);

                // Dibujar trayectorias con efecto fade
                DrawFadingTrajectories();

                // Dibujar aeronaves en posiciones actuales
                DrawAircraft(currentTime);

                // Ocultar panel hover si el marcador ya no existe (por ejemplo, aeronave salió del filtro)
                if (currentHoverMarker != null && !aircraftOverlay.Markers.Contains(currentHoverMarker))
                {
                    hoverPanel.Visible = false;
                    currentHoverMarker = null;
                }

                // Actualizar panel de información si está visible
                if (aircraftInfoPanel != null && aircraftInfoPanel.Visible && !string.IsNullOrEmpty(selectedAircraftId))
                {
                    BuildAircraftInfoContent(selectedAircraftId, currentTime);
                }

                // Refrescar mapa
                gMapControl.Refresh();
            }
            catch (Exception ex)
            {
                // Si algo falla en una iteración, detenemos para no saturar con mensajes
                try { StopSimulation(); } catch { }
                MessageBox.Show($"Se produjo un error al actualizar la simulación:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateRecentTrajectories(DateTime currentTime, int steps)
        {
            // Reconstruir la cola de cada aeronave con TODAS sus posiciones dentro de la ventana temporal
            TimeSpan window = TimeSpan.FromSeconds(TRAIL_DURATION_SECONDS);

            foreach (var aircraftId in aircraftTrajectories.Keys)
            {
                var positionsInWindow = aircraftTrajectories[aircraftId]
                    .Where(p => p.Time <= currentTime && p.Time >= currentTime - window)
                    .OrderBy(p => p.Time)
                    .ToList();

                if (!recentTrajectories.ContainsKey(aircraftId))
                    recentTrajectories[aircraftId] = new Queue<TrajectoryPoint>();

                var queue = new Queue<TrajectoryPoint>();

                foreach (var pos in positionsInWindow)
                {
                    int ageSec = (int)Math.Max(0, (currentTime - pos.Time).TotalSeconds);
                    var tp = new TrajectoryPoint
                    {
                        Position = new PointLatLng(pos.Lat, pos.Lon),
                        Timestamp = pos.Time,
                        Age = ageSec
                    };
                    queue.Enqueue(tp);

                    // Protección rendimiento
                    if (queue.Count >= MAX_TRAJECTORY_POINTS)
                    {
                        // Mantener solo los últimos MAX_TRAJECTORY_POINTS (más recientes)
                        while (queue.Count > MAX_TRAJECTORY_POINTS)
                            queue.Dequeue();
                        break;
                    }
                }

                // Si no había posiciones en ventana pero existía cola previa, la vaciamos
                recentTrajectories[aircraftId].Clear();
                recentTrajectories[aircraftId] = queue;
            }
        }

        private void DrawFadingTrajectories()
        {
            foreach (var aircraftId in recentTrajectories.Keys)
            {
                var points = recentTrajectories[aircraftId].ToList();

                if (points.Count < 2)
                    continue;

                // Filtrar según sistema
                var aircraftPositions = aircraftTrajectories[aircraftId];
                if (aircraftPositions.Count == 0)
                    continue;

                string category = aircraftPositions.First().Category;
                if (!ShouldShowAircraft(category))
                    continue;

                // Dibujar segmentos con alpha decreciente
                for (int i = 0; i < points.Count - 1; i++)
                {
                    float alpha = 1.0f - Math.Min(1.0f, (float)points[i].Age / Math.Max(1, TRAIL_DURATION_SECONDS));
                    alpha = Math.Max(0.15f, alpha); // Mínimo 15% de opacidad para ver traza lejana

                    Color trajectoryColor = Color.FromArgb((int)(alpha * 200), 0, 50, 100);

                    List<PointLatLng> segment = new List<PointLatLng>
                    {
                        points[i].Position,
                        points[i + 1].Position
                    };

                    GMapRoute route = new GMapRoute(segment, $"{aircraftId}_seg_{i}")
                    {
                        Stroke = new Pen(trajectoryColor, 2)
                    };

                    trajectoryOverlay.Routes.Add(route);
                }
            }
        }

        // Calcula los límites geográficos (min/max lat/lon) a partir de los datos filtrados actuales
        private void ComputeGeoBoundsFromCurrentData()
        {
            double? minLat = null, maxLat = null, minLon = null, maxLon = null;
            if (currentFilteredData == null)
            {
                geoMinLat = geoMaxLat = geoMinLon = geoMaxLon = null;
                return;
            }

            var all = currentFilteredData.GetAll();
            foreach (var r in all)
            {
                if (r.Category == AsterixCombinedList.AsterixCategory.CAT048 && r.Cat048 != null)
                {
                    var c = r.Cat048;
                    if (c.LAT.HasValue && c.LON.HasValue)
                    {
                        double lat = c.LAT.Value;
                        double lon = c.LON.Value;
                        minLat = !minLat.HasValue ? lat : Math.Min(minLat.Value, lat);
                        maxLat = !maxLat.HasValue ? lat : Math.Max(maxLat.Value, lat);
                        minLon = !minLon.HasValue ? lon : Math.Min(minLon.Value, lon);
                        maxLon = !maxLon.HasValue ? lon : Math.Max(maxLon.Value, lon);
                    }
                }
                else if (r.Category == AsterixCombinedList.AsterixCategory.CAT021 && r.Cat021 != null)
                {
                    var c = r.Cat021;
                    double lat = c.LAT;
                    double lon = c.LON;
                    minLat = !minLat.HasValue ? lat : Math.Min(minLat.Value, lat);
                    maxLat = !maxLat.HasValue ? lat : Math.Max(maxLat.Value, lat);
                    minLon = !minLon.HasValue ? lon : Math.Min(minLon.Value, lon);
                    maxLon = !maxLon.HasValue ? lon : Math.Max(maxLon.Value, lon);
                }
            }

            geoMinLat = minLat; geoMaxLat = maxLat; geoMinLon = minLon; geoMaxLon = maxLon;
        }

        private bool IsNearGeoBorder(double lat, double lon)
        {
            if (!geoMinLat.HasValue || !geoMaxLat.HasValue || !geoMinLon.HasValue || !geoMaxLon.HasValue)
                return false;

            double minLat = geoMinLat.Value, maxLat = geoMaxLat.Value;
            double minLon = geoMinLon.Value, maxLon = geoMaxLon.Value;
            double m = GEO_BORDER_MARGIN_DEG;

            bool insideExtended = lat >= (minLat - m) && lat <= (maxLat + m) && lon >= (minLon - m) && lon <= (maxLon + m);
            if (!insideExtended) return false;

            bool nearLatEdge = Math.Abs(lat - minLat) <= m || Math.Abs(lat - maxLat) <= m;
            bool nearLonEdge = Math.Abs(lon - minLon) <= m || Math.Abs(lon - maxLon) <= m;
            return nearLatEdge || nearLonEdge;
        }

        // Regla de negocio: una aeronave considerada "vieja" y "cerca del borde" del filtro no debe dibujarse
        private bool IsStaleNearGeoBorder(AircraftPosition pos, DateTime now)
        {
            int ageSec = (int)Math.Max(0, (now - pos.Time).TotalSeconds);
            if (ageSec >= INACTIVITY_THRESHOLD_SECONDS)
            {
                return IsNearGeoBorder(pos.Lat, pos.Lon);
            }
            return false;
        }

        private void DrawAircraft(DateTime atTime)
        {
            foreach (var aircraftId in aircraftTrajectories.Keys)
            {
                // Si ya no hay más actualizaciones disponibles para esta aeronave (hemos pasado su último timestamp), no dibujarla
                var allPositions = aircraftTrajectories[aircraftId];
                if (allPositions == null || allPositions.Count == 0)
                    continue;
                var lastTimeForAircraft = allPositions[allPositions.Count - 1].Time;
                if (atTime >= lastTimeForAircraft)
                    continue;

                var position = allPositions
                    .Where(p => p.Time <= atTime)
                    .OrderByDescending(p => p.Time)
                    .FirstOrDefault();

                if (position == null)
                    continue;

                // Filtrar según sistema
                if (!ShouldShowAircraft(position.Category))
                    continue;

                // Regla nueva: si esta posición está "vieja" y además está cerca del borde del filtro geográfico, no dibujar
                if (IsStaleNearGeoBorder(position, atTime))
                    continue;

                // Obtener rumbo
                double heading = position.Heading ?? 0;

                // CAMBIO PRINCIPAL: Usar el sprite manager
                // Usar ID base para el sprite (sin el sufijo de categoría)
                string baseId = aircraftId.Contains("|") ? aircraftId.Split('|')[0] : aircraftId;
                Bitmap aircraftBitmap = spriteManager.GetAircraftSprite(
                    position.Callsign ?? baseId,
                    position.Category,
                    heading
                );

                GMapMarker marker = new GMarkerGoogle(
                    new PointLatLng(position.Lat, position.Lon),
                    aircraftBitmap
                );
                // Centrar el icono en la posición exacta (ancla en el centro del bitmap)
                marker.Offset = new Point(-(aircraftBitmap.Width / 2), -(aircraftBitmap.Height / 2));

                // Asignar metadatos para hover y panel de información
                marker.Tag = new MarkerMeta
                {
                    AircraftId = aircraftId, // composite key (baseId|CATxxx)
                    Callsign = position.Callsign ?? baseId, // friendly
                    Category = position.Category
                };
                marker.ToolTipMode = MarkerTooltipMode.Never; // Deshabilitar tooltip nativo

                aircraftOverlay.Markers.Add(marker);
            }
        }

        private Color GetMarkerColor(string category)
        {
            return category switch
            {
                "CAT021" => Color.FromArgb(255, 0, 120, 215),  // Azul ADS-B
                "CAT048" => Color.FromArgb(255, 0, 180, 0),    // Verde Radar
                _ => Color.Gray
            };
        }

        private string GetSystemName(string category)
        {
            return category switch
            {
                "CAT021" => "ADS-B",
                "CAT048" => "Radar SMR",
                _ => "Desconocido"
            };
        }

        private void InitializeSpriteManager()
        {
            spriteManager = new AircraftSpriteManager();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string spritesPath = System.IO.Path.Combine(baseDir, SPRITES_FOLDER);

            // Cargar imágenes direccionales (0° y 180°) por categoría
            string blue0 = System.IO.Path.Combine(spritesPath, "aircraft_blue_0.png");
            string blue180 = System.IO.Path.Combine(spritesPath, "aircraft_blue_180.png");
            string red0 = System.IO.Path.Combine(spritesPath, "aircraft_red_0.png");
            string red180 = System.IO.Path.Combine(spritesPath, "aircraft_red_180.png");
            string yellow0 = System.IO.Path.Combine(spritesPath, "aircraft_yellow_0.png");
            string yellow180 = System.IO.Path.Combine(spritesPath, "aircraft_yellow_180.png");

            spriteManager.LoadDirectionalSprites(yellow0, yellow180, blue0, blue180, red0, red180);
        }

        private bool ShouldShowAircraft(string category)
        {
            if (category == "CAT021" && !showCAT021)
                return false;

            if (category == "CAT048" && !showCAT048)
                return false;


            return true;
        }

        private void StartSimulation()
        {
            if (timeStamps == null || timeStamps.Count == 0)
            {
                ProcessTrajectories();
            }
            isPlaying = true;
            simulationTimer.Start();
            auto_button.Text = "Pausar";
        }

        private void StopSimulation()
        {
            isPlaying = false;
            simulationTimer.Stop();
            auto_button.Text = "Auto";
        }

        private void avanzar_button_Click(object sender, EventArgs e)
        {
            simulationDirection = 1; // fijar dirección hacia adelante

            if (timeStamps == null || timeStamps.Count == 0)
            {
                ProcessTrajectories();
            }
            if (timeStamps == null || timeStamps.Count == 0) return;

            int steps = Math.Max(1, speedMultiplier);
            int newIndex = Math.Min(timeStamps.Count - 1, currentTimeIndex + steps);
            lastAdvanceSteps = steps;
            currentTimeIndex = newIndex;
            UpdateSimulation();
        }

        private void retroceder_button_Click(object sender, EventArgs e)
        {
            simulationDirection = -1; // fijar dirección hacia atrás

            if (timeStamps == null || timeStamps.Count == 0)
            {
                ProcessTrajectories();
            }
            if (timeStamps == null || timeStamps.Count == 0) return;

            int steps = Math.Max(1, speedMultiplier);
            int newIndex = Math.Max(0, currentTimeIndex - steps);
            lastAdvanceSteps = steps;
            currentTimeIndex = newIndex;
            UpdateSimulation();
        }


        private void auto_button_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                StopSimulation();
            }
            else
            {
                if (timeStamps == null || timeStamps.Count == 0)
                {
                    ProcessTrajectories();
                }
                StartSimulation();
            }
        }

        private void reset_button_Click(object sender, EventArgs e)
        {
            StopSimulation();
            currentTimeIndex = 0;

            // Limpiar trayectorias recientes
            if (recentTrajectories != null)
            {
                foreach (var key in recentTrajectories.Keys.ToList())
                {
                    recentTrajectories[key].Clear();
                }
            }

            UpdateSimulation();
        }

        private void Speed_trackBar_ValueChanged(object sender, EventArgs e)
        {
            speedMultiplier = speed_trackBar.Value;
            simulationTimer.Interval = 1000 / Math.Max(1, speedMultiplier);
        }

        private void Filter_button_Click(object sender, EventArgs e)
        {
            // Mostrar diálogo de filtros
            using (Form filterForm = new Form())
            {
                filterForm.Text = "Filtrar simulación";
                filterForm.Size = new Size(300, 200);
                filterForm.StartPosition = FormStartPosition.CenterParent;
                filterForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                filterForm.MaximizeBox = false;
                filterForm.MinimizeBox = false;

                CheckBox cat021Check = new CheckBox
                {
                    Text = "Mostrar CAT021 (ADS-B)",
                    Location = new Point(20, 20),
                    Size = new Size(250, 20),
                    Checked = showCAT021
                };

                CheckBox cat048Check = new CheckBox
                {
                    Text = "Mostrar CAT048 (Radar)",
                    Location = new Point(20, 50),
                    Size = new Size(250, 20),
                    Checked = showCAT048
                };

                Button applyButton = new Button
                {
                    Text = "Aplicar",
                    Location = new Point(100, 100),
                    Size = new Size(100, 30)
                };

                applyButton.Click += (s, args) =>
                {
                    showCAT021 = cat021Check.Checked;
                    showCAT048 = cat048Check.Checked;
                    UpdateSimulation();
                    filterForm.Close();
                };

                filterForm.Controls.Add(cat021Check);
                filterForm.Controls.Add(cat048Check);
                filterForm.Controls.Add(applyButton);

                filterForm.ShowDialog();
            }
        }

        private void csv_button_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    // Alinear el diálogo con el de la interfaz de tabla
                    saveDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveDialog.FilterIndex = 1;
                    saveDialog.RestoreDirectory = true;
                    saveDialog.FileName = $"asterix_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Usar la misma función de exportación combinada que el resto de interfaces
                        string outputPath = saveDialog.FileName;
                        currentFilteredData.ExportCombinedToCSV(outputPath);

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

        private void close_button_Click(object sender, EventArgs e)
        {
            StopSimulation();
            this.Close();
        }

        private void map_panel_Paint(object sender, PaintEventArgs e)
        {
            // Manejado por GMap.NET
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopSimulation();
            base.OnFormClosing(e);
        }

        private void speed_trackBar_Scroll(object sender, EventArgs e)
        {

        }

        private void AsterixDecoderSimulation_Interface_Load(object sender, EventArgs e)
        {

        }
    }
}
