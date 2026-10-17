using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace OmenHub
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            using (var mx = new Mutex(true, "OmenHub.SingleInstance", out created))
            {
                if (!created) return;
                Cfg.Load(); Th.Set(Cfg.Dark);
                AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs ev) { Log.W("UNHANDLED: " + ev.ExceptionObject); try { Bios.SetFanMax(false); } catch { } };
                Application.ThreadException += delegate (object s, ThreadExceptionEventArgs ev) { Log.W("UI EXCEPTION: " + ev.Exception); };
                Engine.Start();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                Engine.Stop(); Cfg.Save();
            }
        }
    }

    class MainForm : Form
    {
        Panel side, head; Label title, brand, status; Chip chipCpu, chipGpu;
        Panel[] pages = new Panel[5]; Nav[] navs = new Nav[5];
        string[] names = { "Обзор", "Вентиляторы", "Мощность", "Подсветка", "Настройки" };
        System.Windows.Forms.Timer tm = new System.Windows.Forms.Timer();
        bool dirty;

        // overview
        Label lFanCpu, lFanGpu, vSys, vBios, vGpuT, vGpuP, vGpuC, vGpuU, vCpuL, vCpuF;
        Bar bFanCpu, bFanGpu, bSys, bBios, bGpuT, bGpuP, bGpuC, bGpuU, bCpuL, bCpuF;
        // fans
        Seg segMode, segFan, segSrc; Slider slCpu, slGpu; Label lSlCpu, lSlGpu; Curve curve; Label modeInfo;
        // power / cpu
        Label lGpuFlags, lCpuW; Slider slCpuW; TextBox tbRyzen; Seg segPro;
        Seg segTheme;

        public MainForm()
        {
            Text = "OmenHub"; ClientSize = new Size(1060, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            side = new Panel { Dock = DockStyle.Left, Width = 210 }; Controls.Add(side);
            head = new Panel { Dock = DockStyle.Top, Height = 64 }; Controls.Add(head);
            brand = Lb(side, "OMEN HUB", 24, 22, 15, true, "text");
            Lb(side, "OMEN MAX 16", 26, 54, 8.5f, false, "sub");
            title = Lb(head, "", 28, 16, 17, true, "text");
            status = Lb(head, "", 28, 44, 8.5f, false, "sub");
            chipCpu = new Chip { Title = "КУЛЕР CPU", Left = head.Width - 370, Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right }; head.Controls.Add(chipCpu);
            chipGpu = new Chip { Title = "КУЛЕР GPU", Left = head.Width - 186, Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right }; head.Controls.Add(chipGpu);
            EventHandler place = delegate { chipCpu.Left = head.Width - 370; chipGpu.Left = head.Width - 186; };
            head.Resize += place; Load += place;
            // remove status from header height: keep tiny
            for (int i = 0; i < names.Length; i++)
            {
                int k = i;
                var n = new Nav { Text = names[i], Left = 0, Top = 100 + i * 46, Width = 210 };
                n.Click += delegate { Show(k); };
                side.Controls.Add(n); navs[i] = n;
                var pg = new Panel { Left = 210, Top = 64, Width = 850, Height = 636, Visible = false };
                Controls.Add(pg); pages[i] = pg;
            }
            // Controls added after docked ones need z-order fix
            side.BringToFront(); head.BringToFront(); side.SendToBack(); head.SendToBack();

            BuildOverview(pages[0]); BuildFans(pages[1]); BuildPower(pages[2]); BuildLight(pages[3]); BuildSettings(pages[4]);
            ApplyTheme(); Show(0);

            tm.Interval = 1000; tm.Tick += delegate { Tick(); }; tm.Start();
            FormClosing += delegate { Cfg.Save(); };
        }

        // ------------------------------------------------------------ helpers
        Label Lb(Control p, string text, int x, int y, float size, bool bold, string role)
        {
            var l = new Label { Text = text, Left = x, Top = y, AutoSize = true, Font = Th.F(size, bold), Tag = role, BackColor = Color.Transparent };
            p.Controls.Add(l); return l;
        }
        Card MkCard(Panel pg, int x, int y, int w, int h, string head)
        {
            var c = new Card { Left = x, Top = y, Width = w, Height = h }; pg.Controls.Add(c);
            if (head != null) Lb(c, head, 20, 14, 11, true, "text");
            return c;
        }
        Label Val(Control c, int y, int right) { var l = new Label { Left = c.Width - right - 130, Top = y, Width = 130, Height = 22, TextAlign = ContentAlignment.MiddleRight, Font = Th.F(10.5f, true), Tag = "text", BackColor = Color.Transparent, Text = "—" }; c.Controls.Add(l); return l; }
        Label Row(Card c, int y, string name, out Bar bar)
        {
            Lb(c, name, 20, y, 9.5f, false, "sub");
            var v = Val(c, y - 3, 20);
            bar = new Bar { Left = 20, Top = y + 24, Width = c.Width - 40 }; c.Controls.Add(bar);
            return v;
        }
        Btn Button(Control p, string text, int x, int y, int w, bool accent)
        {
            var b = new Btn { Text = text, Left = x, Top = y, Width = w, Accent = accent }; p.Controls.Add(b); return b;
        }

        void Show(int i)
        {
            for (int k = 0; k < pages.Length; k++) { pages[k].Visible = k == i; navs[k].Active = k == i; navs[k].Invalidate(); }
            title.Text = names[i];
            if (i == 2) lGpuFlags.Text = "Режим BIOS:  " + Bios.GetGpuPower();
        }

        void ApplyTheme() { Th.Set(Cfg.Dark); Recolor(this); Invalidate(true); }
        void Recolor(Control c)
        {
            if (c is Form) c.BackColor = Th.Bg;
            else if (c is Card) c.BackColor = Th.Card;
            else if (c == side) c.BackColor = Th.Side;
            else if (c == head) c.BackColor = Th.Bg;
            else if (c is Panel && c.Parent is Form) c.BackColor = Th.Bg;
            else if (c is TextBox) { c.BackColor = Th.Track; c.ForeColor = Th.Text; }
            var l = c as Label;
            if (l != null)
            {
                string r = l.Tag as string;
                l.ForeColor = r == "sub" ? Th.Sub : r == "accent" ? Th.Accent : Th.Text;
            }
            else if (c is Nav || c is Btn || c is Seg || c is Slider || c is Bar || c is Curve || c is Chip) { if (c.Parent != null) c.BackColor = c.Parent.BackColor; c.Invalidate(); }
            foreach (Control ch in c.Controls) Recolor(ch);
        }

        // ------------------------------------------------------------ pages
        void BuildOverview(Panel pg)
        {
            var a = MkCard(pg, 24, 16, 390, 118, "Кулер CPU");
            lFanCpu = new Label { Left = 20, Top = 44, Width = 350, Height = 44, Font = Th.F(26, true), Tag = "text", BackColor = Color.Transparent, Text = "—" }; a.Controls.Add(lFanCpu);
            bFanCpu = new Bar { Left = 20, Top = 94, Width = 350 }; a.Controls.Add(bFanCpu);
            var b = MkCard(pg, 436, 16, 390, 118, "Кулер GPU");
            lFanGpu = new Label { Left = 20, Top = 44, Width = 350, Height = 44, Font = Th.F(26, true), Tag = "text", BackColor = Color.Transparent, Text = "—" }; b.Controls.Add(lFanGpu);
            bFanGpu = new Bar { Left = 20, Top = 94, Width = 350 }; b.Controls.Add(bFanGpu);

            var t = MkCard(pg, 24, 150, 390, 230, "Температуры");
            vSys = Row(t, 52, "Система (макс. ACPI/BIOS)", out bSys);
            vBios = Row(t, 108, "BIOS-датчик", out bBios);
            vGpuT = Row(t, 164, "GPU", out bGpuT);
            var n = MkCard(pg, 436, 150, 390, 330, "Нагрузка");
            vGpuP = Row(n, 52, "GPU: мощность", out bGpuP);
            vGpuC = Row(n, 108, "GPU: частота ядра", out bGpuC);
            vGpuU = Row(n, 164, "GPU: загрузка", out bGpuU);
            vCpuL = Row(n, 220, "CPU: загрузка", out bCpuL);
            vCpuF = Row(n, 276, "CPU: частота", out bCpuF);
            Lb(pg, "Датчик CPU напрямую пока недоступен; «Система» — самый горячий из ACPI-зон и BIOS-датчика.", 24, 400, 8.5f, false, "sub");
        }

        void BuildFans(Panel pg)
        {
            var m = MkCard(pg, 24, 16, 802, 100, "Режим вентиляторов");
            segMode = new Seg { Left = 20, Top = 48, Width = 520, Items = new[] { "Авто (BIOS)", "Максимум", "Ручной", "По кривой" }, Sel = Cfg.FanMode };
            m.Controls.Add(segMode);
            modeInfo = Lb(m, "", 556, 56, 8.5f, false, "sub");
            segMode.Changed += delegate { Cfg.FanMode = segMode.Sel; dirty = true; ModeText(); };
            ModeText();

            var r = MkCard(pg, 24, 132, 802, 150, "Ручные обороты");
            Lb(r, "Кулер CPU", 20, 54, 9.5f, false, "sub"); Lb(r, "Кулер GPU", 20, 100, 9.5f, false, "sub");
            slCpu = new Slider { Left = 110, Top = 48, Width = 560, Min = 0, Max = 6000, Step = 100, Value = Cfg.ManCpu }; r.Controls.Add(slCpu);
            slGpu = new Slider { Left = 110, Top = 94, Width = 560, Min = 0, Max = 6000, Step = 100, Value = Cfg.ManGpu }; r.Controls.Add(slGpu);
            lSlCpu = new Label { Left = 690, Top = 50, Width = 100, Height = 24, Font = Th.F(10.5f, true), Tag = "text", BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleRight }; r.Controls.Add(lSlCpu);
            lSlGpu = new Label { Left = 690, Top = 96, Width = 100, Height = 24, Font = Th.F(10.5f, true), Tag = "text", BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleRight }; r.Controls.Add(lSlGpu);
            slCpu.Changed += delegate { Cfg.ManCpu = slCpu.Value; SlText(); dirty = true; };
            slGpu.Changed += delegate { Cfg.ManGpu = slGpu.Value; SlText(); dirty = true; };
            SlText();

            var c = MkCard(pg, 24, 298, 802, 322, "Кривая: температура → обороты");
            segFan = new Seg { Left = 330, Top = 8, Width = 200, Items = new[] { "Кулер CPU", "Кулер GPU" } }; c.Controls.Add(segFan);
            segSrc = new Seg { Left = 546, Top = 8, Width = 236, Items = new[] { "Система", "GPU", "Макс." } }; c.Controls.Add(segSrc);
            curve = new Curve { Left = 16, Top = 54, Width = 770, Height = 256 }; c.Controls.Add(curve);
            segFan.Changed += delegate { LoadCurve(); };
            segSrc.Changed += delegate { if (segFan.Sel == 0) Cfg.SrcCpu = segSrc.Sel; else Cfg.SrcGpu = segSrc.Sel; dirty = true; };
            curve.Edited += delegate
            {
                if (segFan.Sel == 0) { Cfg.TCpu = curve.T; Cfg.RCpu = curve.R; } else { Cfg.TGpu = curve.T; Cfg.RGpu = curve.R; }
                dirty = true;
            };
            LoadCurve();
        }
        void LoadCurve()
        {
            if (segFan.Sel == 0) { curve.T = Cfg.TCpu; curve.R = Cfg.RCpu; segSrc.Sel = Cfg.SrcCpu; }
            else { curve.T = Cfg.TGpu; curve.R = Cfg.RGpu; segSrc.Sel = Cfg.SrcGpu; }
            curve.Invalidate();
        }
        void SlText() { lSlCpu.Text = slCpu.Value + " об/мин"; lSlGpu.Text = slGpu.Value + " об/мин"; }
        void ModeText()
        {
            string[] t = { "BIOS сам управляет вентиляторами.", "Оба кулера на максимум (BIOS).", "Фиксированные обороты из ползунков.", "Обороты по кривым ниже." };
            modeInfo.Text = t[Cfg.FanMode] + "\nЗащита: ≥80 °C — оба кулера на максимум (нельзя отключить).";
        }

        void BuildPower(Panel pg)
        {
            var pr = MkCard(pg, 24, 16, 802, 100, "Профиль мощности");
            segPro = new Seg { Left = 20, Top = 50, Width = 340, Items = new[] { "Вручную", "Про авто (CPU 40 Вт)" }, Sel = Cfg.ProAuto ? 1 : 0 }; pr.Controls.Add(segPro);
            Lb(pr, "Про авто: процессор держится на 40 Вт — лучший баланс: ≈6100 очков против ≈6800 на 75 Вт.", 376, 58, 8.5f, false, "sub");
            segPro.Changed += delegate
            {
                Cfg.ProAuto = segPro.Sel == 1;
                if (Cfg.ProAuto) { Cfg.CpuW = Cfg.ProAutoW; slCpuW.Value = Cfg.ProAutoW; lCpuW.Text = Cfg.ProAutoW + " Вт"; }
                dirty = true;
            };

            var g = MkCard(pg, 24, 128, 802, 110, "GPU");
            lGpuFlags = Lb(g, "", 20, 46, 9.5f, false, "sub");
            Lb(g, "Лимит ватт видеокарты задаёт BIOS (режимы Custom TGP / Dynamic Boost). Управление ваттами GPU — следующий этап.", 20, 78, 8.5f, false, "sub");

            var c = MkCard(pg, 24, 254, 802, 370, "CPU: лимит мощности (RyzenAdj)");
            Lb(c, "Диапазон 20–75 Вт, шаг 5. Использует tools\\ryzenadj\\ryzenadj.exe (драйвер WinRing0) — запускается только по кнопкам ниже.", 20, 42, 8.5f, false, "sub");
            lCpuW = new Label { Left = 660, Top = 74, Width = 120, Height = 24, Font = Th.F(10.5f, true), Tag = "text", BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleRight }; c.Controls.Add(lCpuW);
            slCpuW = new Slider { Left = 20, Top = 74, Width = 630, Min = 20, Max = 75, Step = 5, Value = Cfg.ProAuto ? Cfg.ProAutoW : Cfg.CpuW }; c.Controls.Add(slCpuW);
            slCpuW.Changed += delegate { if (Cfg.ProAuto) { slCpuW.Value = Cfg.ProAutoW; return; } Cfg.CpuW = slCpuW.Value; lCpuW.Text = slCpuW.Value + " Вт"; dirty = true; };
            lCpuW.Text = slCpuW.Value + " Вт";
            var a1 = Button(c, "Применить", 20, 118, 140, true);
            var a2 = Button(c, "Прочитать (ryzenadj -i)", 172, 118, 220, false);
            tbRyzen = new TextBox { Left = 20, Top = 166, Width = 762, Height = 188, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None };
            c.Controls.Add(tbRyzen);
            a1.Click += delegate
            {
                int mw = slCpuW.Value * 1000;
                string set = Ryzen("--stapm-limit=" + mw + " --fast-limit=" + mw + " --slow-limit=" + mw);
                string info = Ryzen("-i");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Запрошено: " + slCpuW.Value + " Вт");
                sb.AppendLine("Ответ установки: " + set.Replace("\r\n", " | "));
                sb.AppendLine("--- реальные лимиты после установки ---");
                foreach (string line in info.Split('\n'))
                    if (line.IndexOf("LIMIT", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("VALUE", StringComparison.OrdinalIgnoreCase) >= 0
                        || line.IndexOf("TEMP", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                        || line.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("unsupport", StringComparison.OrdinalIgnoreCase) >= 0)
                        sb.AppendLine(line.TrimEnd('\r'));
                tbRyzen.Text = sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
            };
            a2.Click += delegate { tbRyzen.Text = Ryzen("-i"); };
            var a3 = Button(c, "Автотест 35 → 75 Вт", 404, 118, 230, false);
            a3.Click += delegate
            {
                a3.Enabled = false; tbRyzen.Text = "Тест идёт (~40 с): нагрузка на все ядра, замеры в logs\\omenhub.log ...";
                var t = new Thread(PowerTest); t.IsBackground = true; t.Start();
            };
        }

        static double Field(string info, string name)
        {
            foreach (string line in info.Split('\n'))
                if (line.IndexOf("| " + name, StringComparison.Ordinal) >= 0)
                {
                    string[] p = line.Split('|');
                    double v;
                    if (p.Length > 2 && double.TryParse(p[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return v;
                }
            return double.NaN;
        }

        void PowerTest()
        {
            var result = new System.Text.StringBuilder();
            try
            {
                foreach (int w in new[] { 35, 75 })
                {
                    int mw = w * 1000;
                    Log.W("TEST: set " + w + " W");
                    Ryzen("--stapm-limit=" + mw + " --fast-limit=" + mw + " --slow-limit=" + mw);
                    Thread.Sleep(1500);
                    volatile_stop = false;
                    var ths = new Thread[Environment.ProcessorCount];
                    for (int i = 0; i < ths.Length; i++) { ths[i] = new Thread(Burn); ths[i].IsBackground = true; ths[i].Start(); }
                    double maxFast = 0, maxSlow = 0, maxStapm = 0; int maxMhz = 0;
                    for (int s = 1; s <= 6; s++)
                    {
                        Thread.Sleep(2000);
                        string info = Ryzen("-i");
                        double fast = Field(info, "PPT VALUE FAST"), slow = Field(info, "PPT VALUE SLOW"), st = Field(info, "STAPM VALUE");
                        double lf = Field(info, "PPT LIMIT FAST"), ls = Field(info, "STAPM LIMIT");
                        var sn = Engine.S;
                        Log.W(string.Format("TEST limit={0}W t={1}s  stapmLimit={2} pptLimitFast={3}  VALUE fast={4:0.0} slow={5:0.0} stapm={6:0.0}  cpuMHz={7} load={8:0}% sysT={9} gpuT={10}",
                            w, s * 2, ls, lf, fast, slow, st, sn.CpuMhz, sn.CpuLoad, sn.SysTemp, sn.GpuTemp));
                        if (!double.IsNaN(fast)) maxFast = Math.Max(maxFast, fast);
                        if (!double.IsNaN(slow)) maxSlow = Math.Max(maxSlow, slow);
                        if (!double.IsNaN(st)) maxStapm = Math.Max(maxStapm, st);
                        maxMhz = Math.Max(maxMhz, sn.CpuMhz);
                        if (sn.SysTemp >= 90) { Log.W("TEST aborted: temperature " + sn.SysTemp); break; }
                    }
                    volatile_stop = true;
                    result.AppendLine(string.Format("Лимит {0} Вт: пик потребления fast={1:0.0} Вт, slow={2:0.0} Вт, stapm={3:0.0} Вт, частота до {4} МГц", w, maxFast, maxSlow, maxStapm, maxMhz));
                    Thread.Sleep(5000);
                }
            }
            catch (Exception e) { Log.W("TEST error: " + e); result.AppendLine("Ошибка: " + e.Message); }
            volatile_stop = true;
            Log.W("TEST done\r\n" + result);
            BeginInvoke(new Action(delegate { tbRyzen.Text = result.ToString().Replace("\n", "\r\n"); foreach (Control ch in tbRyzen.Parent.Controls) if (ch is Btn) ch.Enabled = true; }));
        }
        static volatile bool volatile_stop;
        static void Burn() { double x = 1.0001; while (!volatile_stop) { for (int i = 0; i < 100000; i++) x = Math.Sqrt(x * 1.0000001 + 0.5); if (x < 0) break; } }
        string Short(string s) { s = s.Replace("\r", " ").Replace("\n", " "); return s.Length > 70 ? s.Substring(0, 70) + "…" : s; }
        string Ryzen(string args)
        {
            int code;
            if (!RyzenAdj.Present) return "Не найден " + RyzenAdj.Exe;
            var r = RyzenAdj.Run(args, out code);
            if (code != 0) r = "[код выхода " + code + "] " + r;
            return r.Length == 0 ? "(готово, вывода нет)" : r.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        void BuildLight(Panel pg)
        {
            var c = MkCard(pg, 24, 16, 802, 220, "Подсветка клавиатуры");
            Lb(c, "Пока не реализовано.", 20, 52, 11, true, "text");
            Lb(c, "Клавиатура этой модели — HID-устройство Darfon (VID_0D62, PID_54BF). Поклавишная подсветка идёт не через BIOS,\nа через собственный HID-протокол. Следующий этап: снять и воспроизвести этот протокол\n(опираясь на открытые проекты вроде OmenMon), затем сделать выбор цвета и яркости для каждой клавиши.", 20, 88, 9.5f, false, "sub");
        }

        void BuildSettings(Panel pg)
        {
            var c = MkCard(pg, 24, 16, 802, 250, "Настройки");
            Lb(c, "Тема", 20, 58, 9.5f, false, "sub");
            segTheme = new Seg { Left = 120, Top = 52, Width = 260, Items = new[] { "Тёмная", "Светлая" }, Sel = Cfg.Dark ? 0 : 1 }; c.Controls.Add(segTheme);
            segTheme.Changed += delegate { Cfg.Dark = segTheme.Sel == 0; ApplyTheme(); dirty = true; };
            var b = Button(c, "Вернуть вентиляторы под контроль BIOS", 20, 112, 330, false);
            b.Click += delegate { Cfg.FanMode = 0; segMode.Sel = 0; ModeText(); Bios.SetFanMax(false); dirty = true; };
            Lb(c, "При закрытии программы вентиляторы автоматически возвращаются в режим BIOS.\nНастройки: %APPDATA%\\OmenHub\\settings.ini", 20, 170, 9, false, "sub");
        }

        // ------------------------------------------------------------ live update
        void Set(Label l, Bar b, string text, double frac, Color? col)
        {
            l.Text = text; b.V = frac; b.Col = col; b.Invalidate();
        }
        static Color? Heat(double t) { return t >= 85 ? Th.Bad : t >= 75 ? Th.Accent2 : (Color?)null; }

        void Tick()
        {
            if (dirty) { dirty = false; Cfg.Save(); }
            var s = Engine.S;
            status.Text = s.Status + "    |    режим: " + segMode.Items[Cfg.FanMode];
            status.ForeColor = s.Alert ? Th.Bad : Th.Sub; status.Font = Th.F(8.5f, s.Alert);
            chipCpu.Value = s.FanCpu < 0 ? "—" : s.FanCpu + " об/мин"; chipGpu.Value = s.FanGpu < 0 ? "—" : s.FanGpu + " об/мин";
            chipCpu.Alert = chipGpu.Alert = s.Alert; chipCpu.Invalidate(); chipGpu.Invalidate();
            if (!pages[0].Visible && !pages[1].Visible) return;

            lFanCpu.Text = s.FanCpu < 0 ? "—" : s.FanCpu + " об/мин"; bFanCpu.V = Math.Max(0, s.FanCpu) / 6000.0; bFanCpu.Invalidate();
            lFanGpu.Text = s.FanGpu < 0 ? "—" : s.FanGpu + " об/мин"; bFanGpu.V = Math.Max(0, s.FanGpu) / 6000.0; bFanGpu.Invalidate();
            Set(vSys, bSys, s.SysTemp < 0 ? "—" : s.SysTemp + " °C", s.SysTemp / 100.0, Heat(s.SysTemp));
            Set(vBios, bBios, s.BiosTemp < 0 ? "—" : s.BiosTemp + " °C", s.BiosTemp / 100.0, Heat(s.BiosTemp));
            Set(vGpuT, bGpuT, s.GpuTemp < 0 ? "—" : s.GpuTemp + " °C", s.GpuTemp / 100.0, Heat(s.GpuTemp));
            Set(vGpuP, bGpuP, s.GpuPower < 0 ? "—" : s.GpuPower.ToString("0.0") + " Вт", s.GpuPower / 140.0, null);
            Set(vGpuC, bGpuC, s.GpuClock < 0 ? "—" : s.GpuClock + " МГц", s.GpuClock / 3000.0, null);
            Set(vGpuU, bGpuU, s.GpuUtil < 0 ? "—" : s.GpuUtil + " %", s.GpuUtil / 100.0, null);
            Set(vCpuL, bCpuL, s.CpuLoad < 0 ? "—" : s.CpuLoad.ToString("0") + " %", s.CpuLoad / 100.0, null);
            Set(vCpuF, bCpuF, s.CpuMhz < 0 ? "—" : s.CpuMhz + " МГц", s.CpuMhz / 5100.0, null);
            if (curve != null)
            {
                double now = segFan.Sel == 0 ? (segSrc.Sel == 1 ? s.GpuTemp : segSrc.Sel == 0 ? s.SysTemp : Math.Max(s.SysTemp, s.GpuTemp)) : (segSrc.Sel == 1 ? s.GpuTemp : segSrc.Sel == 0 ? s.SysTemp : Math.Max(s.SysTemp, s.GpuTemp));
                curve.Now = now; if (pages[1].Visible) curve.Invalidate();
            }
        }
    }
}
