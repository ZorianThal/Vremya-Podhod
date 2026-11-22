using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VremyaPodhod
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI Controls
        private Panel pnlTopControls;
        private Panel pnlCanvas;
        private Panel pnlAdvisor;
        private SplitContainer splitMain;

        // Input Controls
        private NumericUpDown nudDistanceKm;
        private NumericUpDown nudWaveSeconds;
        private NumericUpDown nudWindSpeedKmH;
        private ComboBox cbEventType;
        private ComboBox cbTimeScale;
        private Button btnStart;
        private Button btnReset;

        // Advisor Display Controls
        private Label lblDistanceResult;
        private Label lblWaveTimeResult;
        private Label lblCloudTimeResult;
        private Label lblZoneStatus;
        private Label lblTransportStatus;
        private TextBox txtActionPlan;

        // Animation Timer & State
        private Timer animTimer;
        private bool isRunning;
        private double simElapsedSec;
        private double timeScale = 1.0;

        // Physical Constants & Parameters
        private const double SoundSpeedKmh = 1224.0; // 340 m/s = 1224 km/h (2.94 sec per km)
        private const double SoundSpeedKms = 0.340;  // 0.34 km/s

        private double distanceKm = 10.0;
        private double waveTimeSec = 29.4;
        private double windSpeedKmH = 15.0; // ~4.16 m/s
        private double cloudArrivalSec = 2400.0; // x / U in seconds

        private bool waveReached = false;
        private bool cloudReached = false;

        // Palette (Khaki / Military Civil Defense)
        private readonly Color colBgDark = Color.FromArgb(43, 49, 41);
        private readonly Color colCanvasBg = Color.FromArgb(53, 61, 51);
        private readonly Color colPanelBg = Color.FromArgb(64, 73, 61);
        private readonly Color colTextKhaki = Color.FromArgb(230, 235, 220);
        private readonly Color colAccentKhaki = Color.FromArgb(178, 194, 158);
        private readonly Color colDangerRed = Color.FromArgb(215, 65, 55);
        private readonly Color colWarningYellow = Color.FromArgb(235, 185, 55);
        private readonly Color colSafeGreen = Color.FromArgb(85, 175, 85);

        public MainForm()
        {
            this.Text = "ВРЕМЯ-ПОДХОД: Расчет долета ударной волны и действий населения (ГОСТ / РД)";
            this.Size = new Size(1024, 740);
            this.MinimumSize = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = colBgDark;
            this.ForeColor = colTextKhaki;

            InitializeComponents();
            RecalculateAll(true);
        }

        private void InitializeComponents()
        {
            // Top Controls Panel
            pnlTopControls = new Panel();
            pnlTopControls.Dock = DockStyle.Top;
            pnlTopControls.Height = 85;
            pnlTopControls.BackColor = colPanelBg;
            pnlTopControls.Padding = new Padding(10);
            this.Controls.Add(pnlTopControls);

            int curX = 10;

            // Event Type
            Label lblEvent = new Label();
            lblEvent.Text = "Тип ЧС / Аварии:";
            lblEvent.Location = new Point(curX, 10);
            lblEvent.AutoSize = true;
            lblEvent.ForeColor = colTextKhaki;
            pnlTopControls.Controls.Add(lblEvent);

            cbEventType = new ComboBox();
            cbEventType.Location = new Point(curX, 32);
            cbEventType.Width = 175;
            cbEventType.DropDownStyle = ComboBoxStyle.DropDownList;
            cbEventType.Items.Add("Ядерный взрыв / Реактор");
            cbEventType.Items.Add("Авария с Хлором (АХОВ)");
            cbEventType.Items.Add("Авария с Аммиаком (АХОВ)");
            cbEventType.Items.Add("Взрыв на ПС / Трансформатор");
            cbEventType.SelectedIndex = 0;
            cbEventType.SelectedIndexChanged += new EventHandler(OnParamChanged);
            pnlTopControls.Controls.Add(cbEventType);

            curX += 185;

            // Distance
            Label lblDist = new Label();
            lblDist.Text = "Расстояние (км):";
            lblDist.Location = new Point(curX, 10);
            lblDist.AutoSize = true;
            lblDist.ForeColor = colTextKhaki;
            pnlTopControls.Controls.Add(lblDist);

            nudDistanceKm = new NumericUpDown();
            nudDistanceKm.Location = new Point(curX, 32);
            nudDistanceKm.Width = 100;
            nudDistanceKm.Minimum = 1;
            nudDistanceKm.Maximum = 500;
            nudDistanceKm.DecimalPlaces = 1;
            nudDistanceKm.Value = 10;
            nudDistanceKm.ValueChanged += new EventHandler(OnDistanceChanged);
            pnlTopControls.Controls.Add(nudDistanceKm);

            curX += 110;

            // Wave Travel Time (seconds)
            Label lblWave = new Label();
            lblWave.Text = "Задержка волны (с):";
            lblWave.Location = new Point(curX, 10);
            lblWave.AutoSize = true;
            lblWave.ForeColor = colTextKhaki;
            pnlTopControls.Controls.Add(lblWave);

            nudWaveSeconds = new NumericUpDown();
            nudWaveSeconds.Location = new Point(curX, 32);
            nudWaveSeconds.Width = 110;
            nudWaveSeconds.Minimum = 1;
            nudWaveSeconds.Maximum = 3600;
            nudWaveSeconds.DecimalPlaces = 1;
            nudWaveSeconds.Value = 29;
            nudWaveSeconds.ValueChanged += new EventHandler(OnWaveSecChanged);
            pnlTopControls.Controls.Add(nudWaveSeconds);

            curX += 120;

            // Wind Speed
            Label lblWind = new Label();
            lblWind.Text = "Ветер U (км/ч):";
            lblWind.Location = new Point(curX, 10);
            lblWind.AutoSize = true;
            lblWind.ForeColor = colTextKhaki;
            pnlTopControls.Controls.Add(lblWind);

            nudWindSpeedKmH = new NumericUpDown();
            nudWindSpeedKmH.Location = new Point(curX, 32);
            nudWindSpeedKmH.Width = 95;
            nudWindSpeedKmH.Minimum = 1;
            nudWindSpeedKmH.Maximum = 150;
            nudWindSpeedKmH.DecimalPlaces = 1;
            nudWindSpeedKmH.Value = 15;
            nudWindSpeedKmH.ValueChanged += new EventHandler(OnParamChanged);
            pnlTopControls.Controls.Add(nudWindSpeedKmH);

            curX += 105;

            // Time Scale (Speedup)
            Label lblScale = new Label();
            lblScale.Text = "Скорость мод.:";
            lblScale.Location = new Point(curX, 10);
            lblScale.AutoSize = true;
            lblScale.ForeColor = colTextKhaki;
            pnlTopControls.Controls.Add(lblScale);

            cbTimeScale = new ComboBox();
            cbTimeScale.Location = new Point(curX, 32);
            cbTimeScale.Width = 85;
            cbTimeScale.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTimeScale.Items.Add("x1 (Реал)");
            cbTimeScale.Items.Add("x5");
            cbTimeScale.Items.Add("x15");
            cbTimeScale.Items.Add("x60");
            cbTimeScale.SelectedIndex = 1; // x5
            cbTimeScale.SelectedIndexChanged += new EventHandler(OnTimeScaleChanged);
            pnlTopControls.Controls.Add(cbTimeScale);

            curX += 95;

            // Start / Pause Button
            btnStart = new Button();
            btnStart.Text = "▶ ПУСК";
            btnStart.Location = new Point(curX, 22);
            btnStart.Size = new Size(100, 36);
            btnStart.BackColor = Color.FromArgb(80, 115, 65);
            btnStart.ForeColor = Color.White;
            btnStart.Font = new Font("Arial", 9.5f, FontStyle.Bold);
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Click += new EventHandler(OnStartClick);
            pnlTopControls.Controls.Add(btnStart);

            curX += 105;

            // Reset Button
            btnReset = new Button();
            btnReset.Text = "СБРОС";
            btnReset.Location = new Point(curX, 22);
            btnReset.Size = new Size(85, 36);
            btnReset.BackColor = Color.FromArgb(75, 75, 75);
            btnReset.ForeColor = Color.White;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.Click += new EventHandler(OnResetClick);
            pnlTopControls.Controls.Add(btnReset);

            // Split Container: Top is Graphic Canvas, Bottom is Advisor & Results
            splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.SplitterDistance = 330;
            splitMain.BackColor = colBgDark;
            this.Controls.Add(splitMain);
            splitMain.BringToFront();

            // Canvas Panel (Double Buffered)
            pnlCanvas = new DoubleBufferedPanel();
            pnlCanvas.Dock = DockStyle.Fill;
            pnlCanvas.BackColor = colCanvasBg;
            pnlCanvas.Paint += new PaintEventHandler(OnCanvasPaint);
            splitMain.Panel1.Controls.Add(pnlCanvas);

            // Advisor Panel
            pnlAdvisor = new Panel();
            pnlAdvisor.Dock = DockStyle.Fill;
            pnlAdvisor.BackColor = colPanelBg;
            pnlAdvisor.Padding = new Padding(12);
            splitMain.Panel2.Controls.Add(pnlAdvisor);

            BuildAdvisorControls();

            // Animation Timer (30 FPS)
            animTimer = new Timer();
            animTimer.Interval = 33;
            animTimer.Tick += new EventHandler(OnAnimTick);
        }

        private void BuildAdvisorControls()
        {
            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Fill;
            tlp.ColumnCount = 2;
            tlp.RowCount = 1;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            pnlAdvisor.Controls.Add(tlp);

            // Left Box: Numeric Summary
            GroupBox gbResults = new GroupBox();
            gbResults.Text = " РАСЧЕТНЫЕ ПАРАМЕТРЫ УГРОЗЫ (ГОСТ 22.1.01-2023) ";
            gbResults.Dock = DockStyle.Fill;
            gbResults.ForeColor = colAccentKhaki;
            gbResults.Font = new Font("Arial", 9.0f, FontStyle.Bold);
            tlp.Controls.Add(gbResults, 0, 0);

            TableLayoutPanel tlpRes = new TableLayoutPanel();
            tlpRes.Dock = DockStyle.Fill;
            tlpRes.ColumnCount = 1;
            tlpRes.RowCount = 5;
            tlpRes.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            tlpRes.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            tlpRes.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            tlpRes.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            tlpRes.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
            gbResults.Controls.Add(tlpRes);

            lblDistanceResult = CreateMetricLabel("Дистанция до взрыва: 10.0 км");
            lblWaveTimeResult = CreateMetricLabel("Время долета ударной волны: 29.4 сек");
            lblCloudTimeResult = CreateMetricLabel("Время подхода облака: 40 мин (2400 с)");
            lblZoneStatus = CreateMetricLabel("Зона опасности: БЛИЖНЯЯ ЗОНА");
            lblTransportStatus = CreateMetricLabel("СТАТУС АВТОТРАНСПОРТА: ЗАПРЕЩЕНО");

            tlpRes.Controls.Add(lblDistanceResult, 0, 0);
            tlpRes.Controls.Add(lblWaveTimeResult, 0, 1);
            tlpRes.Controls.Add(lblCloudTimeResult, 0, 2);
            tlpRes.Controls.Add(lblZoneStatus, 0, 3);
            tlpRes.Controls.Add(lblTransportStatus, 0, 4);

            // Right Box: Action Advisor
            GroupBox gbAdvisor = new GroupBox();
            gbAdvisor.Text = " ПОРЯДОК ДЕЙСТВИЙ НАСЕЛЕНИЯ И ПЕРСОНАЛА ";
            gbAdvisor.Dock = DockStyle.Fill;
            gbAdvisor.ForeColor = colAccentKhaki;
            gbAdvisor.Font = new Font("Arial", 9.0f, FontStyle.Bold);
            tlp.Controls.Add(gbAdvisor, 1, 0);

            txtActionPlan = new TextBox();
            txtActionPlan.Dock = DockStyle.Fill;
            txtActionPlan.Multiline = true;
            txtActionPlan.ReadOnly = true;
            txtActionPlan.ScrollBars = ScrollBars.Vertical;
            txtActionPlan.BackColor = Color.FromArgb(38, 43, 36);
            txtActionPlan.ForeColor = Color.FromArgb(240, 245, 235);
            txtActionPlan.Font = new Font("Consolas", 9.5f, FontStyle.Regular);
            gbAdvisor.Controls.Add(txtActionPlan);
        }

        private Label CreateMetricLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Font = new Font("Arial", 9.0f, FontStyle.Bold);
            lbl.ForeColor = colTextKhaki;
            return lbl;
        }

        private bool updatingSync = false;

        private void OnDistanceChanged(object sender, EventArgs e)
        {
            if (updatingSync) return;
            updatingSync = true;
            try
            {
                distanceKm = (double)nudDistanceKm.Value;
                waveTimeSec = distanceKm / SoundSpeedKms;
                if (waveTimeSec > (double)nudWaveSeconds.Maximum) waveTimeSec = (double)nudWaveSeconds.Maximum;
                nudWaveSeconds.Value = (decimal)Math.Round(waveTimeSec, 1);
            }
            finally
            {
                updatingSync = false;
            }
            RecalculateAll(false);
        }

        private void OnWaveSecChanged(object sender, EventArgs e)
        {
            if (updatingSync) return;
            updatingSync = true;
            try
            {
                waveTimeSec = (double)nudWaveSeconds.Value;
                distanceKm = waveTimeSec * SoundSpeedKms;
                if (distanceKm > (double)nudDistanceKm.Maximum) distanceKm = (double)nudDistanceKm.Maximum;
                nudDistanceKm.Value = (decimal)Math.Round(distanceKm, 1);
            }
            finally
            {
                updatingSync = false;
            }
            RecalculateAll(false);
        }

        private void OnParamChanged(object sender, EventArgs e)
        {
            RecalculateAll(false);
        }

        private void OnTimeScaleChanged(object sender, EventArgs e)
        {
            switch (cbTimeScale.SelectedIndex)
            {
                case 0: timeScale = 1.0; break;
                case 1: timeScale = 5.0; break;
                case 2: timeScale = 15.0; break;
                case 3: timeScale = 60.0; break;
                default: timeScale = 5.0; break;
            }
        }

        private void RecalculateAll(bool resetTime)
        {
            distanceKm = (double)nudDistanceKm.Value;
            waveTimeSec = distanceKm / SoundSpeedKms;
            windSpeedKmH = (double)nudWindSpeedKmH.Value;

            if (windSpeedKmH < 0.1) windSpeedKmH = 0.1;
            // Approach time: t = x / U (hours) -> * 3600 (seconds)
            cloudArrivalSec = (distanceKm / windSpeedKmH) * 3600.0;

            if (resetTime)
            {
                simElapsedSec = 0;
                waveReached = false;
                cloudReached = false;
            }

            // Update Labels
            lblDistanceResult.Text = string.Format("Дистанция до эпицентра: {0:F1} км ({1:F0} м)", distanceKm, distanceKm * 1000);
            lblWaveTimeResult.Text = string.Format("Время долета волны: {0:F1} с (~{1:F1} мин)", waveTimeSec, waveTimeSec / 60.0);

            long hours = (long)(cloudArrivalSec / 3600);
            long mins = (long)((cloudArrivalSec % 3600) / 60);
            long secs = (long)(cloudArrivalSec % 60);
            lblCloudTimeResult.Text = string.Format("Время подхода облака: {0:D2}ч {1:D2}м {2:D2}с ({3:F0} с)", hours, mins, secs, cloudArrivalSec);

            // Determine Zone and Civil Defense Rules
            string eventName = cbEventType.SelectedItem != null ? cbEventType.SelectedItem.ToString() : "";
            
            if (distanceKm <= 15.0)
            {
                // CLOSE ZONE (0 - 15 km)
                lblZoneStatus.Text = "ЗОНА: БЛИЖНЯЯ ЗОНА КАТАСТРОФИЧЕСКОЙ ОПАСНОСТИ";
                lblZoneStatus.ForeColor = colDangerRed;

                lblTransportStatus.Text = "ТРАНСПОРТ: ⛔ КАТЕГОРИЧЕСКИ ЗАПРЕЩЕНО!";
                lblTransportStatus.ForeColor = colDangerRed;

                txtActionPlan.Text = 
                    "■ РЕКОМЕНДАЦИИ ГОЧС ДЛЯ БЛИЖНЕЙ ЗОНЫ (" + distanceKm.ToString("F1") + " км, удар за " + waveTimeSec.ToString("F1") + " сек):\r\n\r\n" +
                    "1. АВТОТРАНСПОРТ: КАТЕГОРИЧЕСКИ ЗАПРЕЩЕН!\r\n" +
                    "   * Дороги подвержены завалам и разрушениям.\r\n" +
                    "   * Автомобиль не защищает от фугасного удара и быстро накрывается зараженным воздухом.\r\n" +
                    "   * Выезд на авто гарантированно создает панические пробки и гибель в салоне.\r\n\r\n" +
                    "2. ПОРЯДОК НЕМЕДЛЕННЫХ ДЕЙСТВИЙ:\r\n" +
                    "   * Немедленно укрыться в подвале, бомбоубежище или защитном заглубленном сооружении.\r\n" +
                    "   * Если вы в здании: отойти от окон, лечь на пол у несущей стены или в дверном проеме.\r\n" +
                    "   * Отключить вентиляцию, газ, электроприборы. Закрыть все форточки и вытяжки.\r\n" +
                    "   * Защитить органы дыхания: противогаз, респиратор или ткань, смоченная 2% раствором соды/воды.\r\n" +
                    "   * Ожидать команды органов ГОЧС по радиовещанию. Не выходить наружу до отбоя тревоги!";
            }
            else if (distanceKm <= 50.0)
            {
                // BUFFER ZONE (15 - 50 km)
                lblZoneStatus.Text = "ЗОНА: БУФЕРНАЯ ЗОНА ВЫСОКОЙ ОПАСНОСТИ";
                lblZoneStatus.ForeColor = colWarningYellow;

                lblTransportStatus.Text = "ТРАНСПОРТ: ⚠️ ОГРАНИЧЕНО (Только экстренный выезд)";
                lblTransportStatus.ForeColor = colWarningYellow;

                txtActionPlan.Text = 
                    "■ РЕКОМЕНДАЦИИ ГОЧС ДЛЯ СРЕДНЕЙ ЗОНЫ (" + distanceKm.ToString("F1") + " км, удар за " + waveTimeSec.ToString("F1") + " сек):\r\n\r\n" +
                    "1. АВТОТРАНСПОРТ: ОГРАНИЧЕННО РАЗРЕШЕН.\r\n" +
                    "   * Выезд на авто разрешен ТОЛЬКО если автомобиль уже готов к движению и эвакуация начинается НЕМЕДЛЕННО.\r\n" +
                    "   * Маршрут движения СТРОГО ПЕРПЕНДИКУЛЯРНО направлению ветра (вбок от оси сноса облака).\r\n" +
                    "   * Окна машины наглухо закрыть, вентиляцию/кондиционер перевести во внутреннюю рециркуляцию.\r\n\r\n" +
                    "2. ПРИ ОТСУТСТВИИ ГОТОВОГО АВТО:\r\n" +
                    "   * Укрыться в капитальном жилом или производственном здании (средние этажи).\r\n" +
                    "   * Провести полную герметизацию помещения (скотч, влажные ткани на щели окон и дверей).\r\n" +
                    "   * Подготовить запас питьевой воды в закрытой таре и документы.";
            }
            else
            {
                // DISTANT ZONE (> 50 km)
                lblZoneStatus.Text = "ЗОНА: ДАЛЬНЯЯ ЗОНА ОПЕРАТИВНОЙ ЭВАКУАЦИИ";
                lblZoneStatus.ForeColor = colSafeGreen;

                lblTransportStatus.Text = "ТРАНСПОРТ: ✔️ РАЗРЕШЕНО (Рекомендована эвакуация)";
                lblTransportStatus.ForeColor = colSafeGreen;

                txtActionPlan.Text = 
                    "■ РЕКОМЕНДАЦИИ ГОЧС ДЛЯ ДАЛЬНЕЙ ЗОНЫ (" + distanceKm.ToString("F1") + " км, удар за " + (waveTimeSec/60.0).ToString("F1") + " мин, облако за " + hours + "ч " + mins + "м):\r\n\r\n" +
                    "1. АВТОТРАНСПОРТ: РАЗРЕШЕН И РЕКОМЕНДОВАН.\r\n" +
                    "   * Значительный запас времени до подхода фронта зараженного воздуха (" + hours + " ч " + mins + " мин).\r\n" +
                    "   * Рекомендована плановая эвакуация на автотранспорте за пределы расчетного сектора распространения.\r\n" +
                    "   * Направление движения: строго ПЕРПЕНДИКУЛЯРНО оси сноса облака ветром.\r\n\r\n" +
                    "2. ДЕЙСТВИЯ ПЕРЕД ВЫЕЗДОМ:\r\n" +
                    "   * Взять документы, средства индивидуальной защиты (СИЗ), аптечку, запас воды на 3 суток.\r\n" +
                    "   * Обесточить жилье, перекрыть газ и воду.\r\n" +
                    "   * В пути следования включить автомагнитолу на информационный канал ГОЧС.";
            }

            pnlCanvas.Invalidate();
        }

        private void OnStartClick(object sender, EventArgs e)
        {
            if (isRunning)
            {
                animTimer.Stop();
                isRunning = false;
                btnStart.Text = "▶ ПУСК";
                btnStart.BackColor = Color.FromArgb(80, 115, 65);
            }
            else
            {
                animTimer.Start();
                isRunning = true;
                btnStart.Text = "❚❚ ПАУЗА";
                btnStart.BackColor = Color.FromArgb(170, 110, 40);
            }
        }

        private void OnResetClick(object sender, EventArgs e)
        {
            animTimer.Stop();
            isRunning = false;
            btnStart.Text = "▶ ПУСК";
            btnStart.BackColor = Color.FromArgb(80, 115, 65);
            RecalculateAll(true);
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            // dt in seconds
            double dt = (animTimer.Interval / 1000.0) * timeScale;
            simElapsedSec += dt;

            if (simElapsedSec >= waveTimeSec)
                waveReached = true;
            if (simElapsedSec >= cloudArrivalSec)
                cloudReached = true;

            pnlCanvas.Invalidate();
        }

        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = pnlCanvas.ClientSize.Width;
            int h = pnlCanvas.ClientSize.Height;

            // Draw Background Grid (Tactical Map)
            using (Pen gridPen = new Pen(Color.FromArgb(62, 72, 60), 1))
            {
                for (int x = 0; x < w; x += 40)
                    g.DrawLine(gridPen, x, 0, x, h);
                for (int y = 0; y < h; y += 40)
                    g.DrawLine(gridPen, 0, y, w, y);
            }

            int srcX = 90;
            int srcY = h / 2 + 30;
            int tgtX = w - 120;
            int tgtY = srcY;

            // 1. Draw Distance / Baseline Axis
            using (Pen axisPen = new Pen(Color.FromArgb(120, 140, 110), 2))
            {
                axisPen.DashStyle = DashStyle.Dash;
                g.DrawLine(axisPen, srcX, srcY, tgtX, tgtY);
            }

            // Distance Label on line
            string distText = string.Format("— Расстояние L = {0:F1} км —", distanceKm);
            using (Font fDist = new Font("Arial", 10.0f, FontStyle.Bold))
            using (Brush bDist = new SolidBrush(colAccentKhaki))
            {
                SizeF sz = g.MeasureString(distText, fDist);
                g.DrawString(distText, fDist, bDist, (srcX + tgtX - sz.Width) / 2, srcY - 24);
            }

            // 2. Wind Vector Arrow (Top Center)
            int arrowStartX = w / 2 - 80;
            int arrowEndX = w / 2 + 80;
            int arrowY = 35;
            using (Pen windPen = new Pen(Color.FromArgb(90, 175, 225), 3))
            {
                windPen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(windPen, arrowStartX, arrowY, arrowEndX, arrowY);
            }
            string windStr = string.Format("Ветер U = {0:F1} км/ч ({1:F1} м/с) на объект", windSpeedKmH, windSpeedKmH / 3.6);
            using (Font fWind = new Font("Arial", 9.5f, FontStyle.Bold))
            using (Brush bWind = new SolidBrush(Color.FromArgb(130, 205, 245)))
            {
                SizeF sz = g.MeasureString(windStr, fWind);
                g.DrawString(windStr, fWind, bWind, (w - sz.Width) / 2, arrowY - 22);
            }

            // 3. Shockwave Animation (Fast Expansion)
            double waveProgress = 0.0;
            if (waveTimeSec > 0)
                waveProgress = simElapsedSec / waveTimeSec;
            if (waveProgress > 1.2) waveProgress = 1.2;

            if (simElapsedSec > 0)
            {
                float currentWaveX = (float)(srcX + (tgtX - srcX) * Math.Min(waveProgress, 1.0));
                float waveRadius = Math.Max(10f, currentWaveX - srcX);

                using (Pen shockPen = new Pen(Color.FromArgb(240, 90, 40), 3))
                {
                    // Draw Shockwave Arc
                    g.DrawArc(shockPen, srcX - waveRadius, srcY - waveRadius, waveRadius * 2, waveRadius * 2, -60, 120);
                }

                // Shockwave Label
                using (Font fShock = new Font("Arial", 8.5f, FontStyle.Bold))
                using (Brush bShock = new SolidBrush(Color.FromArgb(255, 120, 60)))
                {
                    string waveInfo = waveReached ? "УДАРНАЯ ВОЛНА ДОСТИГЛА РУБЕЖА!" : "Фронт ударной волны";
                    g.DrawString(waveInfo, fShock, bShock, currentWaveX - 40, srcY - waveRadius * 0.5f - 18);
                }
            }

            // 4. Contaminated Air Cloud Animation (Wind Advection)
            double cloudProgress = 0.0;
            if (cloudArrivalSec > 0)
                cloudProgress = simElapsedSec / cloudArrivalSec;
            if (cloudProgress > 1.1) cloudProgress = 1.1;

            if (simElapsedSec > 0)
            {
                float currentCloudFrontX = (float)(srcX + (tgtX - srcX) * Math.Min(cloudProgress, 1.0));
                float cloudLength = Math.Max(25f, currentCloudFrontX - srcX);
                float cloudHeight = Math.Min(130f, 30f + (cloudLength * 0.35f));

                Color cloudColor = Color.FromArgb(140, 200, 195, 60); // Toxic Plume / Radiation Khaki
                if (cbEventType.SelectedIndex == 1) // Chlorine
                    cloudColor = Color.FromArgb(150, 185, 215, 55);
                else if (cbEventType.SelectedIndex == 2) // Ammonia
                    cloudColor = Color.FromArgb(140, 215, 215, 230);

                using (GraphicsPath cloudPath = new GraphicsPath())
                {
                    cloudPath.AddEllipse(srcX, srcY - cloudHeight / 2, cloudLength, cloudHeight);
                    using (PathGradientBrush pgb = new PathGradientBrush(cloudPath))
                    {
                        pgb.CenterColor = cloudColor;
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, cloudColor) };
                        g.FillPath(pgb, cloudPath);
                    }
                }

                if (cloudLength > 40)
                {
                    using (Font fCloud = new Font("Arial", 8.5f, FontStyle.Italic))
                    using (Brush bCloud = new SolidBrush(Color.FromArgb(235, 245, 180)))
                    {
                        string cText = cloudReached ? "РУБЕЖ НАКРЫТ ОБЛАКОМ!" : "Облако зараженного воздуха";
                        g.DrawString(cText, fCloud, bCloud, srcX + (cloudLength / 2) - 50, srcY - 10);
                    }
                }
            }

            // 5. Draw Source Icon (Explosion / Enterprise)
            DrawSourceIcon(g, srcX, srcY);

            // 6. Draw Target Icon (House / Population Shelter)
            DrawTargetHouse(g, tgtX, tgtY);

            // 7. Timer & Status Overlay
            DrawOverlayTimer(g, 15, h - 55);
        }

        private void DrawSourceIcon(Graphics g, int x, int y)
        {
            // Building with stack / explosion mark
            using (Brush bBuild = new SolidBrush(Color.FromArgb(95, 105, 90)))
            {
                g.FillRectangle(bBuild, x - 25, y - 20, 45, 40);
                g.FillRectangle(bBuild, x - 15, y - 35, 12, 15); // Stack
            }
            using (Pen pBuild = new Pen(Color.FromArgb(160, 180, 150), 2))
            {
                g.DrawRectangle(pBuild, x - 25, y - 20, 45, 40);
                g.DrawLine(pBuild, x - 30, y - 20, x - 2, y - 40); // Roof
                g.DrawLine(pBuild, x - 2, y - 40, x + 25, y - 20);
            }

            // Explosion flash symbol
            using (Brush bFlash = new SolidBrush(Color.FromArgb(255, 90, 40)))
            {
                Point[] star = new Point[] {
                    new Point(x - 2, y - 55), new Point(x + 5, y - 43),
                    new Point(x + 18, y - 45), new Point(x + 9, y - 33),
                    new Point(x + 16, y - 20), new Point(x + 2, y - 25),
                    new Point(x - 10, y - 18), new Point(x - 6, y - 30),
                    new Point(x - 20, y - 38), new Point(x - 5, y - 41)
                };
                g.FillPolygon(bFlash, star);
            }

            using (Font f = new Font("Arial", 9.0f, FontStyle.Bold))
            using (Brush b = new SolidBrush(colTextKhaki))
            {
                string txt = "ИСТОЧНИК\n(Эпицентр)";
                SizeF sz = g.MeasureString(txt, f);
                g.DrawString(txt, f, b, x - (sz.Width / 2), y + 25);
            }
        }

        private void DrawTargetHouse(Graphics g, int x, int y)
        {
            // House body
            using (Brush bHouse = new SolidBrush(Color.FromArgb(120, 135, 110)))
            {
                g.FillRectangle(bHouse, x - 20, y - 15, 40, 35);
            }
            // Roof
            using (Brush bRoof = new SolidBrush(Color.FromArgb(180, 80, 60)))
            {
                Point[] roof = new Point[] {
                    new Point(x - 26, y - 15),
                    new Point(x, y - 38),
                    new Point(x + 26, y - 15)
                };
                g.FillPolygon(bRoof, roof);
            }
            using (Pen pHouse = new Pen(Color.FromArgb(200, 220, 180), 2))
            {
                g.DrawRectangle(pHouse, x - 20, y - 15, 40, 35);
                g.DrawLine(pHouse, x - 26, y - 15, x, y - 38);
                g.DrawLine(pHouse, x, y - 38, x + 26, y - 15);
                // Windows
                g.DrawRectangle(pHouse, x - 12, y - 5, 10, 12);
                g.DrawRectangle(pHouse, x + 3, y - 5, 10, 12);
            }

            // Target Label
            using (Font f = new Font("Arial", 9.0f, FontStyle.Bold))
            using (Brush b = new SolidBrush(colTextKhaki))
            {
                string txt = "РУБЕЖ / ОБЪЕКТ\n(Население/Дом)";
                SizeF sz = g.MeasureString(txt, f);
                g.DrawString(txt, f, b, x - (sz.Width / 2), y + 25);
            }

            // Hit Markers
            if (waveReached)
            {
                using (Font fAlert = new Font("Arial", 8.0f, FontStyle.Bold))
                using (Brush bAlert = new SolidBrush(Color.FromArgb(255, 90, 70)))
                {
                    g.DrawString("⚡ УДАРНАЯ ВОЛНА", fAlert, bAlert, x - 45, y - 55);
                }
            }
            if (cloudReached)
            {
                using (Font fAlert = new Font("Arial", 8.0f, FontStyle.Bold))
                using (Brush bAlert = new SolidBrush(Color.FromArgb(255, 215, 0)))
                {
                    g.DrawString("☣️ ЗАРАЖЕНИЕ!", fAlert, bAlert, x - 35, y - 70);
                }
            }
        }

        private void DrawOverlayTimer(Graphics g, int x, int y)
        {
            using (Brush bBg = new SolidBrush(Color.FromArgb(200, 30, 35, 28)))
            {
                g.FillRectangle(bBg, x, y, 380, 42);
            }
            using (Pen pBorder = new Pen(colAccentKhaki, 1))
            {
                g.DrawRectangle(pBorder, x, y, 380, 42);
            }

            long h = (long)(simElapsedSec / 3600);
            long m = (long)((simElapsedSec % 3600) / 60);
            double s = simElapsedSec % 60;

            string timerStr = string.Format("Модельное время: {0:D2}:{1:D2}:{2:00.0} (Скорость: {3}x)", h, m, s, timeScale);
            using (Font fTimer = new Font("Consolas", 10.0f, FontStyle.Bold))
            using (Brush bTimer = new SolidBrush(Color.FromArgb(170, 240, 140)))
            {
                g.DrawString(timerStr, fTimer, bTimer, x + 8, y + 6);
            }

            string statusStr = isRunning ? "● РАСЧЕТ И АНИМАЦИЯ АКТИВНЫ" : "○ ОЖИДАНИЕ ПУСКА";
            Color statColor = isRunning ? colSafeGreen : Color.LightGray;
            using (Font fStat = new Font("Arial", 8.5f, FontStyle.Bold))
            using (Brush bStat = new SolidBrush(statColor))
            {
                g.DrawString(statusStr, fStat, bStat, x + 8, y + 24);
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                           ControlStyles.UserPaint |
                           ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}
