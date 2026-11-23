using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NavidadCompanero
{
    // Configuración persistente con nombres personalizables por mascota
    public class AppConfig
    {
        public string SelectedPet = "Reno"; // Duende, Reno, Pinguino, Gato, Perro
        public bool ShowIntroOnStart = true;
        public bool GhostMode = false;
        public bool StayInPlace = false;
        public bool DisableAutoMessages = false;
        public int ScalePercent = 100; // 50% a 180%
        public Dictionary<string, string> PetNames = new Dictionary<string, string>();

        public AppConfig()
        {
            PetNames["Duende"] = "Duende Navideño";
            PetNames["Reno"] = "Rudolph";
            PetNames["Pinguino"] = "Pingüino Invernal";
            PetNames["Gato"] = "Gatito con Gorro";
            PetNames["Perro"] = "Perrito Navideño";
        }

        public string GetPetName(string key)
        {
            if (PetNames.ContainsKey(key) && !string.IsNullOrWhiteSpace(PetNames[key]))
                return PetNames[key];
            return key;
        }

        public void SetPetName(string key, string customName)
        {
            if (!string.IsNullOrWhiteSpace(customName))
                PetNames[key] = customName.Trim();
        }

        private static string ConfigPath
        {
            get
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos");
                if (!Directory.Exists(dir))
                {
                    try { Directory.CreateDirectory(dir); } catch { }
                }
                return Path.Combine(dir, "config.ini");
            }
        }

        public static AppConfig Load()
        {
            AppConfig cfg = new AppConfig();
            try
            {
                if (File.Exists(ConfigPath))
                {
                    foreach (string line in File.ReadAllLines(ConfigPath))
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            string k = line.Substring(0, eq).Trim();
                            string v = line.Substring(eq + 1).Trim();
                            if (k == "SelectedPet") cfg.SelectedPet = v;
                            else if (k == "ShowIntroOnStart") bool.TryParse(v, out cfg.ShowIntroOnStart);
                            else if (k == "GhostMode") bool.TryParse(v, out cfg.GhostMode);
                            else if (k == "StayInPlace") bool.TryParse(v, out cfg.StayInPlace);
                            else if (k == "DisableAutoMessages") bool.TryParse(v, out cfg.DisableAutoMessages);
                            else if (k == "ScalePercent")
                            {
                                int sp;
                                if (int.TryParse(v, out sp))
                                    cfg.ScalePercent = Math.Max(50, Math.Min(180, sp));
                            }
                            else if (k.StartsWith("Name_"))
                            {
                                string petKey = k.Substring(5);
                                if (!string.IsNullOrWhiteSpace(v))
                                    cfg.PetNames[petKey] = v;
                            }
                        }
                    }
                }
            }
            catch { }
            return cfg;
        }

        public void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("SelectedPet=" + SelectedPet);
                lines.Add("ShowIntroOnStart=" + ShowIntroOnStart);
                lines.Add("GhostMode=" + GhostMode);
                lines.Add("StayInPlace=" + StayInPlace);
                lines.Add("DisableAutoMessages=" + DisableAutoMessages);
                lines.Add("ScalePercent=" + ScalePercent);
                foreach (var kv in PetNames)
                {
                    lines.Add("Name_" + kv.Key + "=" + kv.Value);
                }
                File.WriteAllLines(ConfigPath, lines.ToArray());
            }
            catch { }
        }
    }

    // Helpers Win32 para ventanas layered transparentes (Per-pixel alpha) y Click-Through
    public static class Win32
    {
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int GWL_EXSTYLE = -20;
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; public POINT(int x, int y) { this.x = x; this.y = y; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx, cy; public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; } }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint uPeriod);

        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        public static void CompactMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle);
            }
            catch { }
        }

        public static void SetBitmap(Form form, Bitmap bitmap, byte opacity)
        {
            if (form.IsDisposed || !form.IsHandleCreated) return;

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                POINT pointSource = new POINT(0, 0);
                POINT topPos = new POINT(form.Left, form.Top);
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = opacity;
                blend.AlphaFormat = AC_SRC_ALPHA;

                UpdateLayeredWindow(form.Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }
    }

    // Gestor de Auto-Inicio en Windows (Triple respaldo: Registro Run + Carpeta shell:startup + StartupApproved)
    public static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string AppName = "NavidadCompaneroVirtual";

        private static string StartupShortcutPath
        {
            get
            {
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                return Path.Combine(startupDir, "CompaneroNavidad.cmd");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                if (File.Exists(StartupShortcutPath)) return true;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppName);
                    return val != null;
                }
            }
            catch { return false; }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = "\"" + Application.ExecutablePath + "\"";
                            key.SetValue(AppName, exePath);
                        }
                        else
                        {
                            if (key.GetValue(AppName) != null)
                                key.DeleteValue(AppName, false);
                        }
                    }
                }

                // Asegurar que Windows 11 StartupApproved lo marque explícitamente como habilitado (02 00 00 00...)
                using (RegistryKey appKey = Registry.CurrentUser.CreateSubKey(ApprovedKey))
                {
                    if (appKey != null)
                    {
                        if (enable)
                        {
                            byte[] enabledBytes = new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                            appKey.SetValue(AppName, enabledBytes, RegistryValueKind.Binary);
                        }
                        else
                        {
                            if (appKey.GetValue(AppName) != null)
                                appKey.DeleteValue(AppName, false);
                        }
                    }
                }

                // Respaldo directo en shell:startup para arranque inmediato al iniciar sesión
                if (enable)
                {
                    string cmdContent = "@echo off\r\nstart \"\" \"" + Application.ExecutablePath + "\"\r\n";
                    File.WriteAllText(StartupShortcutPath, cmdContent);
                }
                else
                {
                    if (File.Exists(StartupShortcutPath))
                        File.Delete(StartupShortcutPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar el inicio automático: " + ex.Message, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // =========================================================================
    // FASE 1: OVERLAY DE SALUDO NAVIDEÑO (8 SEGUNDOS, CLICK-THROUGH, SIN CUADRADOS)
    // =========================================================================
    public class ChristmasIntroOverlay : Form
    {
        private class Snowflake
        {
            public float X, Y, SpeedY, SpeedX, Radius, Phase, Alpha, Angle, Spin;
            public int Kind; // 0 = soft distant bokeh, 1 = medium flake, 2 = detailed crystal snowflake
        }

        private Timer animTimer;
        private List<Snowflake> flakes = new List<Snowflake>();
        private Random rnd = new Random();
        private float tickCount = 0f;
        private float totalTicks = 270f; // ~9 segundos en base 30Hz
        private float screenScale = 1.0f;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private long lastMs = 0;
        private Bitmap canvasBitmap;
        private Action onFinished;

        public ChristmasIntroOverlay(Action onFinishedCallback)
        {
            this.onFinished = onFinishedCallback;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            this.Bounds = bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            // Factor de escala según resolución (1.0 en 1080p, ~0.75 en laptop 768p, ~1.8 en 4K)
            screenScale = Math.Max(0.72f, Math.Min(2.2f, bounds.Height / 1080f));

            canvasBitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            for (int i = 0; i < 115; i++)
            {
                int k = (i % 5 == 0) ? 2 : ((i % 2 == 0) ? 1 : 0);
                float r = ((k == 2) ? (float)(6.0 + rnd.NextDouble() * 5.0)
                        : (k == 1) ? (float)(2.8 + rnd.NextDouble() * 2.5)
                        : (float)(1.8 + rnd.NextDouble() * 2.0)) * screenScale;
                flakes.Add(new Snowflake
                {
                    X = (float)(rnd.NextDouble() * bounds.Width),
                    Y = (float)(rnd.NextDouble() * bounds.Height * 0.85 - bounds.Height * 0.25),
                    SpeedY = (float)(1.8 + rnd.NextDouble() * 3.8) * (k == 0 ? 0.7f : 1.0f) * screenScale,
                    SpeedX = (float)((rnd.NextDouble() - 0.5) * 1.4) * screenScale,
                    Radius = r,
                    Phase = (float)(rnd.NextDouble() * Math.PI * 2),
                    Alpha = (k == 2) ? (float)(0.65 + rnd.NextDouble() * 0.35) : (float)(0.30 + rnd.NextDouble() * 0.45),
                    Angle = (float)(rnd.NextDouble() * 360),
                    Spin = (float)((rnd.NextDouble() - 0.5) * 2.2),
                    Kind = k
                });
            }

            animTimer = new Timer();
            animTimer.Interval = 15; // ~60-66 FPS reales con timeBeginPeriod(1)
            animTimer.Tick += OnTick;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            sw.Start();
            lastMs = sw.ElapsedMilliseconds;
            animTimer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            long now = sw.ElapsedMilliseconds;
            float dt = (now - lastMs) / 33.333f; // 1.0 = un paso de 30Hz, ~0.48 a 60FPS
            if (dt <= 0f || dt > 3f) dt = 0.5f;
            lastMs = now;

            tickCount += dt;
            if (tickCount >= totalTicks)
            {
                animTimer.Stop();
                this.Close();
                if (onFinished != null) onFinished();
                Win32.CompactMemory();
                return;
            }

            float globalAlpha = 1.0f;
            if (tickCount < 25f) globalAlpha = tickCount / 25.0f;
            else if (tickCount > totalTicks - 45f) globalAlpha = (totalTicks - tickCount) / 45.0f;
            globalAlpha = Math.Max(0f, Math.Min(1f, globalAlpha));

            RenderFrame(globalAlpha, dt);
        }

        private void RenderFrame(float globalAlpha, float dt)
        {
            int w = canvasBitmap.Width;
            int h = canvasBitmap.Height;

            using (Graphics g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                // 1. Guirnalda de luces superior
                DrawTopGarland(g, w, tickCount);

                // 2. Copos de nieve cristalinos y bokeh suave interpolados a 60 FPS
                foreach (var f in flakes)
                {
                    f.Y += f.SpeedY * dt;
                    f.Phase += 0.05f * dt;
                    f.X += (f.SpeedX + (float)Math.Sin(f.Phase) * 0.85f) * dt;
                    f.Angle += f.Spin * dt;

                    if (f.Y > h + 15)
                    {
                        f.Y = -15;
                        f.X = (float)(rnd.NextDouble() * w);
                    }

                    int a = (int)(f.Alpha * 255);
                    if (f.Kind == 2)
                    {
                        DrawCrystalSnowflake(g, f.X, f.Y, f.Radius, f.Angle, a);
                    }
                    else if (f.Kind == 1)
                    {
                        using (SolidBrush glow = new SolidBrush(Color.FromArgb(a / 3, 210, 235, 255)))
                        using (SolidBrush core = new SolidBrush(Color.FromArgb(a, 255, 255, 255)))
                        {
                            g.FillEllipse(glow, f.X - f.Radius * 1.6f, f.Y - f.Radius * 1.6f, f.Radius * 3.2f, f.Radius * 3.2f);
                            g.FillEllipse(core, f.X - f.Radius * 0.75f, f.Y - f.Radius * 0.75f, f.Radius * 1.5f, f.Radius * 1.5f);
                        }
                    }
                    else
                    {
                        using (SolidBrush sb = new SolidBrush(Color.FromArgb(a, 240, 248, 255)))
                        {
                            g.FillEllipse(sb, f.X - f.Radius, f.Y - f.Radius, f.Radius * 2, f.Radius * 2);
                        }
                    }
                }

                // 3. Banner editorial de bienvenida navideña
                DrawCenterGreeting(g, w, h, tickCount);
            }

            byte op = (byte)(globalAlpha * 255);
            Win32.SetBitmap(this, canvasBitmap, op);
        }

        private void DrawTopGarland(Graphics g, int width, float tick)
        {
            int bulbs = Math.Max(12, width / 75);
            float spacing = (float)width / bulbs;

            Color[] bulbColors = new Color[]
            {
                Color.FromArgb(255, 50, 50),
                Color.FromArgb(40, 220, 80),
                Color.FromArgb(255, 215, 40),
                Color.FromArgb(60, 170, 255),
                Color.FromArgb(255, 100, 220)
            };

            using (Pen wirePen = new Pen(Color.FromArgb(190, 20, 75, 35), 4f))
            {
                for (int i = 0; i < bulbs; i++)
                {
                    float x1 = i * spacing;
                    float x2 = (i + 1) * spacing;
                    g.DrawBezier(wirePen, x1, 6, x1 + spacing * 0.3f, 38, x1 + spacing * 0.7f, 38, x2, 6);
                }
            }

            for (int i = 0; i <= bulbs; i++)
            {
                float bx = i * spacing;
                float by = 14 + (i % 2 == 0 ? 4 : 18);
                Color baseCol = bulbColors[i % bulbColors.Length];
                float pulse = 0.6f + 0.4f * (float)Math.Sin(tick * 0.18f + i * 1.1f);

                int glowAlpha = (int)(95 * pulse);
                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, baseCol)))
                {
                    g.FillEllipse(glowBrush, bx - 18, by - 14, 36, 36);
                }

                using (SolidBrush cap = new SolidBrush(Color.FromArgb(220, 60, 60, 60)))
                {
                    g.FillRectangle(cap, bx - 4, by - 5, 8, 6);
                }

                using (SolidBrush bulbBrush = new SolidBrush(Color.FromArgb(245, baseCol)))
                {
                    g.FillEllipse(bulbBrush, bx - 7, by, 14, 18);
                }
                using (SolidBrush shine = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    g.FillEllipse(shine, bx - 4, by + 3, 4, 6);
                }
            }
        }

        private void DrawCrystalSnowflake(Graphics g, float x, float y, float radius, float angleDeg, int alpha)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angleDeg);

            using (Pen mainPen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 1.35f))
            using (Pen branchPen = new Pen(Color.FromArgb((int)(alpha * 0.8f), 225, 242, 255), 1.0f))
            {
                mainPen.StartCap = LineCap.Round;
                mainPen.EndCap = LineCap.Round;
                branchPen.StartCap = LineCap.Round;
                branchPen.EndCap = LineCap.Round;

                for (int i = 0; i < 6; i++)
                {
                    g.DrawLine(mainPen, 0, 0, 0, -radius);
                    float b1 = radius * 0.58f;
                    float bLen = radius * 0.32f;
                    g.DrawLine(branchPen, 0, -b1, -bLen * 0.75f, -b1 - bLen * 0.65f);
                    g.DrawLine(branchPen, 0, -b1, bLen * 0.75f, -b1 - bLen * 0.65f);
                    g.RotateTransform(60f);
                }
            }

            g.Restore(st);
        }

        private void DrawCenterGreeting(Graphics g, int w, int h, float tick)
        {
            float floatY = (float)Math.Sin(tick * 0.08f) * 6f * screenScale;
            float cx = w / 2f;
            float cy = h * 0.44f + floatY;

            GraphicsState st = g.Save();
            g.TranslateTransform(cx, cy);
            g.ScaleTransform(screenScale, screenScale);
            g.TranslateTransform(-cx, -cy);

            // Resplandor ambiental suave detrás del texto (sin caja rectangular pesada)
            int glowW = 680;
            int glowH = 230;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - glowW / 2f, cy - glowH / 2f, glowW, glowH);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterPoint = new PointF(cx, cy);
                    pgb.CenterColor = Color.FromArgb(155, 12, 20, 28);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 12, 20, 28) };
                    g.FillPath(pgb, gp);
                }
            }

            // Segundo halo cálido dorado en el núcleo
            using (GraphicsPath gpGold = new GraphicsPath())
            {
                gpGold.AddEllipse(cx - 260, cy - 85, 520, 170);
                using (PathGradientBrush pgbGold = new PathGradientBrush(gpGold))
                {
                    pgbGold.CenterPoint = new PointF(cx, cy - 10);
                    pgbGold.CenterColor = Color.FromArgb(55, 255, 200, 95);
                    pgbGold.SurroundColors = new Color[] { Color.FromArgb(0, 255, 200, 95) };
                    g.FillPath(pgbGold, gpGold);
                }
            }

            // Ornamento superior: Ramitas de pino y bayas de acebo (Holly)
            DrawHollyOrnament(g, cx, cy - 64);

            // Líneas finas doradas que se desvanecen a los lados del acebo
            using (LinearGradientBrush lineL = new LinearGradientBrush(new PointF(cx - 190, cy - 64), new PointF(cx - 32, cy - 64), Color.FromArgb(0, 255, 215, 120), Color.FromArgb(200, 255, 215, 120)))
            using (LinearGradientBrush lineR = new LinearGradientBrush(new PointF(cx + 32, cy - 64), new PointF(cx + 190, cy - 64), Color.FromArgb(200, 255, 215, 120), Color.FromArgb(0, 255, 215, 120)))
            using (Pen pL = new Pen(lineL, 1.5f))
            using (Pen pR = new Pen(lineR, 1.5f))
            {
                g.DrawLine(pL, cx - 185, cy - 64, cx - 35, cy - 64);
                g.DrawLine(pR, cx + 35, cy - 64, cx + 185, cy - 64);
            }

            // Tipografía Serif Clásica Editorial ("Georgia") con relleno metálico dorado y sombra multicapa
            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using (FontFamily ff = new FontFamily("Georgia"))
            using (GraphicsPath textPath = new GraphicsPath())
            {
                textPath.AddString("¡Feliz Navidad!", ff, (int)(FontStyle.Bold | FontStyle.Italic), 62f, new PointF(cx, cy - 10), sf);

                // Sombra profunda suave (3 capas para efecto drop-shadow real)
                for (int s = 3; s >= 1; s--)
                {
                    using (Matrix m = new Matrix())
                    {
                        m.Translate(0, s * 2.2f);
                        using (GraphicsPath shadowPath = (GraphicsPath)textPath.Clone())
                        {
                            shadowPath.Transform(m);
                            using (SolidBrush sb = new SolidBrush(Color.FromArgb(65 * (4 - s), 5, 10, 15)))
                            using (Pen sp = new Pen(Color.FromArgb(45 * (4 - s), 5, 10, 15), s * 2.5f) { LineJoin = LineJoin.Round })
                            {
                                g.DrawPath(sp, shadowPath);
                                g.FillPath(sb, shadowPath);
                            }
                        }
                    }
                }

                // Degradado dorado champán a oro cálido para las letras
                RectangleF textBounds = new RectangleF(cx - 260, cy - 50, 520, 80);
                using (LinearGradientBrush goldFill = new LinearGradientBrush(
                    textBounds,
                    Color.FromArgb(255, 255, 250, 225),
                    Color.FromArgb(255, 235, 185, 85),
                    LinearGradientMode.Vertical))
                using (Pen outlinePen = new Pen(Color.FromArgb(180, 95, 55, 15), 1.4f) { LineJoin = LineJoin.Round })
                {
                    g.FillPath(goldFill, textPath);
                    g.DrawPath(outlinePen, textPath);
                }
            }

            // Destellos finos de 4 puntas alrededor del título
            float pulse1 = 0.7f + 0.3f * (float)Math.Sin(tick * 0.18f);
            float pulse2 = 0.7f + 0.3f * (float)Math.Cos(tick * 0.18f);
            DrawFourPointStar(g, cx - 235, cy - 18, 9f * pulse1, Color.FromArgb(235, 255, 240, 175));
            DrawFourPointStar(g, cx + 235, cy - 18, 9f * pulse2, Color.FromArgb(235, 255, 240, 175));
            DrawFourPointStar(g, cx - 195, cy + 16, 5.5f * pulse2, Color.FromArgb(190, 255, 255, 255));
            DrawFourPointStar(g, cx + 195, cy + 16, 5.5f * pulse1, Color.FromArgb(190, 255, 255, 255));

            // Divisor inferior elegante con diamante central
            float divY = cy + 34f;
            using (LinearGradientBrush lineL = new LinearGradientBrush(new PointF(cx - 140, divY), new PointF(cx, divY), Color.FromArgb(0, 255, 220, 140), Color.FromArgb(190, 255, 220, 140)))
            using (LinearGradientBrush lineR = new LinearGradientBrush(new PointF(cx, divY), new PointF(cx + 140, divY), Color.FromArgb(190, 255, 220, 140), Color.FromArgb(0, 255, 220, 140)))
            using (Pen pL = new Pen(lineL, 1.3f))
            using (Pen pR = new Pen(lineR, 1.3f))
            {
                g.DrawLine(pL, cx - 135, divY, cx - 10, divY);
                g.DrawLine(pR, cx + 10, divY, cx + 135, divY);
            }
            PointF[] diamond = {
                new PointF(cx, divY - 4.5f),
                new PointF(cx + 4.5f, divY),
                new PointF(cx, divY + 4.5f),
                new PointF(cx - 4.5f, divY)
            };
            using (SolidBrush db = new SolidBrush(Color.FromArgb(230, 255, 220, 130)))
            {
                g.FillPolygon(db, diamond);
            }

            // Subtítulo limpio con sombra de lectura sobre cualquier fondo
            using (Font subFont = new Font("Georgia", 17.5f, FontStyle.Italic, GraphicsUnit.Pixel))
            using (SolidBrush subShadow = new SolidBrush(Color.FromArgb(210, 5, 10, 15)))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(245, 245, 248, 252)))
            {
                string subText = "Que tengas un día lleno de alegría, paz y magia";
                g.DrawString(subText, subFont, subShadow, cx + 1.5f, cy + 56f + 1.5f, sf);
                g.DrawString(subText, subFont, subBrush, cx, cy + 56f, sf);
            }

            // Conteo dinámico de días para Navidad debajo del mensaje
            string countdownText = GetChristmasCountdownText();
            using (Font countFont = new Font("Georgia", 14.5f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF cSize = g.MeasureString(countdownText, countFont);
                float pillW = cSize.Width + 34f;
                float pillH = 28f;
                float pillY = cy + 92f;
                RectangleF pillRect = new RectangleF(cx - pillW / 2f, pillY - pillH / 2f, pillW, pillH);

                using (GraphicsPath pillPath = CreateRoundedRect(pillRect, 14f))
                using (SolidBrush pillBg = new SolidBrush(Color.FromArgb(145, 16, 28, 36)))
                using (Pen pillBorder = new Pen(Color.FromArgb(175, 245, 205, 115), 1.2f))
                {
                    g.FillPath(pillBg, pillPath);
                    g.DrawPath(pillBorder, pillPath);
                }

                using (SolidBrush countShadow = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                using (SolidBrush countBrush = new SolidBrush(Color.FromArgb(250, 255, 230, 155)))
                {
                    g.DrawString(countdownText, countFont, countShadow, cx + 1f, pillY + 1f, sf);
                    g.DrawString(countdownText, countFont, countBrush, cx, pillY, sf);
                }
            }

            g.Restore(st);
        }

        private string GetChristmasCountdownText()
        {
            DateTime today = DateTime.Today;
            DateTime christmas = new DateTime(today.Year, 12, 25);
            if (today > christmas)
            {
                christmas = new DateTime(today.Year + 1, 12, 25);
            }
            int daysLeft = (christmas - today).Days;
            if (daysLeft == 0) return "✦  ¡Hoy es Navidad!  ✦";
            if (daysLeft == 1) return "✦  Falta 1 día para Navidad  ✦";
            return "✦  Faltan " + daysLeft + " días para Navidad  ✦";
        }

        private void DrawHollyOrnament(Graphics g, float x, float y)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);

            // Dos hojas de acebo estilizadas a izquierda y derecha
            using (SolidBrush leafDark = new SolidBrush(Color.FromArgb(240, 24, 115, 52)))
            using (SolidBrush leafLight = new SolidBrush(Color.FromArgb(240, 42, 150, 72)))
            using (Pen leafVein = new Pen(Color.FromArgb(180, 180, 235, 160), 1.2f))
            {
                PointF[] leftLeaf = {
                    new PointF(-4, 0),
                    new PointF(-14, -9),
                    new PointF(-26, -4),
                    new PointF(-15, 7)
                };
                PointF[] rightLeaf = {
                    new PointF(4, 0),
                    new PointF(14, -9),
                    new PointF(26, -4),
                    new PointF(15, 7)
                };
                g.FillClosedCurve(leafDark, leftLeaf, FillMode.Winding, 0.35f);
                g.FillClosedCurve(leafLight, rightLeaf, FillMode.Winding, 0.35f);
                g.DrawLine(leafVein, -4, 0, -22, -3);
                g.DrawLine(leafVein, 4, 0, 22, -3);
            }

            // Tres bayas rojas brillantes en el centro
            using (SolidBrush berryRed = new SolidBrush(Color.FromArgb(250, 215, 30, 42)))
            using (SolidBrush berryDark = new SolidBrush(Color.FromArgb(250, 155, 15, 25)))
            using (SolidBrush shine = new SolidBrush(Color.FromArgb(210, 255, 230, 230)))
            {
                g.FillEllipse(berryDark, -8, -3, 8, 8);
                g.FillEllipse(berryDark, 1, -3, 8, 8);
                g.FillEllipse(berryRed, -4.5f, -7, 9, 9);

                g.FillEllipse(shine, -2.5f, -5.5f, 3, 2.5f);
                g.FillEllipse(shine, -6.5f, -1.5f, 2.2f, 2f);
                g.FillEllipse(shine, 3.0f, -1.5f, 2.2f, 2f);
            }

            g.Restore(st);
        }

        private void DrawFourPointStar(Graphics g, float x, float y, float r, Color col)
        {
            PointF[] pts = {
                new PointF(x, y - r),
                new PointF(x + r * 0.24f, y - r * 0.24f),
                new PointF(x + r, y),
                new PointF(x + r * 0.24f, y + r * 0.24f),
                new PointF(x, y + r),
                new PointF(x - r * 0.24f, y + r * 0.24f),
                new PointF(x - r, y),
                new PointF(x - r * 0.24f, y - r * 0.24f)
            };
            using (SolidBrush sb = new SolidBrush(col))
            {
                g.FillPolygon(sb, pts);
            }
        }

        public static void DrawVectorChristmasTree(Graphics g, float x, float y, float scale, float tick)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale, scale);

            // Tronco
            using (SolidBrush trunk = new SolidBrush(Color.FromArgb(120, 70, 35)))
            {
                g.FillRectangle(trunk, -5, 14, 10, 10);
            }

            // 3 capas de pino verde
            using (SolidBrush greenDark = new SolidBrush(Color.FromArgb(25, 135, 55)))
            using (SolidBrush greenMid = new SolidBrush(Color.FromArgb(35, 165, 68)))
            using (SolidBrush greenLight = new SolidBrush(Color.FromArgb(50, 190, 82)))
            using (Pen goldOutline = new Pen(Color.FromArgb(140, 255, 225, 110), 1.2f))
            {
                PointF[] layer3 = { new PointF(0, -10), new PointF(-22, 15), new PointF(22, 15) };
                PointF[] layer2 = { new PointF(0, -20), new PointF(-18, 4), new PointF(18, 4) };
                PointF[] layer1 = { new PointF(0, -30), new PointF(-13, -7), new PointF(13, -7) };

                g.FillPolygon(greenDark, layer3);
                g.DrawPolygon(goldOutline, layer3);

                g.FillPolygon(greenMid, layer2);
                g.DrawPolygon(goldOutline, layer2);

                g.FillPolygon(greenLight, layer1);
                g.DrawPolygon(goldOutline, layer1);
            }

            // Estrella dorada en la punta
            float pulse = 1f + 0.15f * (float)Math.Sin(tick * 0.2f);
            using (SolidBrush gold = new SolidBrush(Color.Gold))
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(110, 255, 235, 100)))
            {
                g.FillEllipse(glow, -8 * pulse, -38 * pulse, 16 * pulse, 16 * pulse);
                g.FillEllipse(gold, -4.5f, -34.5f, 9, 9);
            }

            // Esferas navideñas
            using (SolidBrush redOrn = new SolidBrush(Color.FromArgb(245, 45, 45)))
            using (SolidBrush goldOrn = new SolidBrush(Color.Gold))
            using (SolidBrush cyanOrn = new SolidBrush(Color.FromArgb(80, 210, 255)))
            {
                g.FillEllipse(redOrn, -7, -14, 5, 5);
                g.FillEllipse(goldOrn, 4, -5, 5, 5);
                g.FillEllipse(cyanOrn, -10, 4, 5, 5);
                g.FillEllipse(redOrn, 6, 7, 5, 5);
                g.FillEllipse(goldOrn, -1, -2, 4.5f, 4.5f);
            }

            g.Restore(st);
        }

        private void DrawSparkle(Graphics g, float x, float y, float size, Color col)
        {
            using (Pen p = new Pen(Color.FromArgb(220, col), 2.5f))
            {
                g.DrawLine(p, x - size, y, x + size, y);
                g.DrawLine(p, x, y - size, x, y + size);
                g.DrawLine(p, x - size * 0.6f, y - size * 0.6f, x + size * 0.6f, y + size * 0.6f);
                g.DrawLine(p, x - size * 0.6f, y + size * 0.6f, x + size * 0.6f, y - size * 0.6f);
            }
        }

        public static GraphicsPath CreateRoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (animTimer != null) animTimer.Dispose();
                if (canvasBitmap != null) canvasBitmap.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // =========================================================================
    // FASE 2: COMPAÑERO VIRTUAL DE ESCRITORIO (MÁS GRANDE + ETIQUETA DE NOMBRE)
    // =========================================================================
    public enum PetState
    {
        Walking,
        Idle,
        Sitting,
        Sleeping,
        Jumping,
        Dragging,
        Falling,
        Celebrating
    }

    public enum ParticleKind
    {
        Snowflake,
        Star,
        Heart,
        Zzz
    }

    // Ventana espejo ligera para cuando la mascota cruza de un borde de la pantalla al otro (Screen Wrap-Around)
    public class WrapMirrorForm : Form
    {
        public WrapMirrorForm(int w, int h)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(w, h);
            this.TopMost = true;
            this.ShowInTaskbar = false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST;
                return cp;
            }
        }
    }

    public class DesktopPetForm : Form
    {
        private class MiniParticle
        {
            public float X, Y, VX, VY, Life, Size;
            public ParticleKind Kind;
            public Color Color;
        }

        private AppConfig config;
        private Timer loopTimer;
        private Bitmap frameBuffer;
        private WrapMirrorForm wrapMirror;
        private Random rnd = new Random();

        // Física y posición (con delta-time a 60 FPS reales)
        private float posX, posY;
        private float customGroundY = -1f; // Permite dejar a la mascota fija en cualquier lugar de la pantalla si StayInPlace está activo
        private float velX = 2.0f, velY = 0f;
        private bool facingRight = true;
        private PetState state = PetState.Falling;
        private float stateTimer = 0f;
        private float animTick = 0f;
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private long lastMs = 0;

        // Interacción de arrastre vs clic simple
        private bool isMouseDown = false;
        private bool isDragging = false;
        private Point dragOffset;
        private Point mouseDownScreenPos;
        private Point lastMousePos;

        // Globito de diálogo ocasional
        private string speechText = "";
        private float speechTimer = 0f;

        // Partículas vectoriales alrededor de la mascota (sin cuadrados de emojis)
        private List<MiniParticle> particles = new List<MiniParticle>();

        // Lienzo amplio para soportar cualquier porcentaje del slider (50% a 180%) sin recortar
        private const int CanvasW = 240;
        private const int CanvasH = 265;
        private const float BasePetScale = 0.76f;

        private float CurrentPetScale
        {
            get
            {
                int pct = (config != null) ? Math.Max(50, Math.Min(180, config.ScalePercent)) : 100;
                return BasePetScale * (pct / 100.0f);
            }
        }

        public void SetScalePercent(int pct)
        {
            config.ScalePercent = Math.Max(50, Math.Min(180, pct));
            config.Save();
        }

        // Helpers Multi-Monitor: une el ancho horizontal de todos los monitores y detecta el suelo del monitor donde está la mascota
        private void GetMultiMonitorSpan(out int minLeft, out int maxRight, out int totalWidth)
        {
            Screen[] screens = Screen.AllScreens;
            if (screens == null || screens.Length == 0)
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                minLeft = wa.Left;
                maxRight = wa.Right;
                totalWidth = wa.Width;
                return;
            }
            minLeft = int.MaxValue;
            maxRight = int.MinValue;
            foreach (Screen s in screens)
            {
                if (s.WorkingArea.Left < minLeft) minLeft = s.WorkingArea.Left;
                if (s.WorkingArea.Right > maxRight) maxRight = s.WorkingArea.Right;
            }
            totalWidth = Math.Max(800, maxRight - minLeft);
        }

        private float GetGroundYAt(float x)
        {
            int centerX = (int)Math.Round(x + CanvasW / 2f);
            Screen[] screens = Screen.AllScreens;
            if (screens != null)
            {
                foreach (Screen s in screens)
                {
                    if (centerX >= s.Bounds.Left && centerX <= s.Bounds.Right)
                    {
                        return s.WorkingArea.Bottom - CanvasH + 6;
                    }
                }
            }
            Screen nearest = Screen.FromPoint(new Point(centerX, (int)Math.Round(posY + CanvasH / 2f)));
            return nearest.WorkingArea.Bottom - CanvasH + 6;
        }

        public event Action OnOpenControlPanelRequested;

        public DesktopPetForm(AppConfig cfg)
        {
            this.config = cfg;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(CanvasW, CanvasH);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            frameBuffer = new Bitmap(CanvasW, CanvasH, PixelFormat.Format32bppArgb);
            wrapMirror = new WrapMirrorForm(CanvasW, CanvasH);

            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            posX = workArea.Right - CanvasW - 100;
            posY = workArea.Bottom - CanvasH - 160;
            this.Location = new Point((int)posX, (int)posY);

            SayGreeting("¡Feliz Navidad!");

            this.MouseDown += OnPetMouseDown;
            this.MouseMove += OnPetMouseMove;
            this.MouseUp += OnPetMouseUp;
            this.MouseDoubleClick += OnPetDoubleClick;

            BuildContextMenu();

            sw.Start();
            lastMs = sw.ElapsedMilliseconds;

            loopTimer = new Timer();
            loopTimer.Interval = 15; // ~60-66 FPS suaves con timeBeginPeriod(1)
            loopTimer.Tick += OnLoopTick;
            loopTimer.Start();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Win32.WS_EX_LAYERED | Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_TOPMOST;
                if (config != null && config.GhostMode)
                    cp.ExStyle |= Win32.WS_EX_TRANSPARENT;
                return cp;
            }
        }

        public void ApplyGhostMode(bool ghost)
        {
            config.GhostMode = ghost;
            config.Save();
            int exStyle = Win32.GetWindowLong(this.Handle, Win32.GWL_EXSTYLE);
            if (ghost)
                exStyle |= Win32.WS_EX_TRANSPARENT;
            else
                exStyle &= ~Win32.WS_EX_TRANSPARENT;
            Win32.SetWindowLong(this.Handle, Win32.GWL_EXSTYLE, exStyle);
        }

        public void ChangePet(string petName)
        {
            config.SelectedPet = petName;
            config.Save();
            state = PetState.Celebrating;
            stateTimer = 75;
            velY = -7f;
            SpawnParticles(ParticleKind.Star, Color.Gold, 7);
            SayGreeting(GetRandomGreetingFor(petName));
        }

        public void TriggerCelebration()
        {
            state = PetState.Celebrating;
            stateTimer = 85;
            SpawnParticles(ParticleKind.Snowflake, Color.LightCyan, 7);
            SayGreeting(GetRandomGreetingFor(config.SelectedPet));
        }

        private void BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem titleItem = new ToolStripMenuItem("Compañero Navideño");
            titleItem.Enabled = false;
            menu.Items.Add(titleItem);
            menu.Items.Add(new ToolStripSeparator());

            string[] pets = { "Duende", "Reno", "Pinguino", "Gato", "Perro" };
            foreach (string p in pets)
            {
                string petCopy = p;
                ToolStripMenuItem item = new ToolStripMenuItem("Cambiar a: " + GetSpeciesTitle(p));
                item.Click += (s, e) => ChangePet(petCopy);
                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem renameItem = new ToolStripMenuItem("Cambiar nombre de la mascota...");
            renameItem.Click += (s, e) => { if (OnOpenControlPanelRequested != null) OnOpenControlPanelRequested(); };
            menu.Items.Add(renameItem);

            ToolStripMenuItem celebrateItem = new ToolStripMenuItem("Hacer truco / Saludar");
            celebrateItem.Click += (s, e) => TriggerCelebration();
            menu.Items.Add(celebrateItem);

            ToolStripMenuItem panelItem = new ToolStripMenuItem("Configuración...");
            panelItem.Click += (s, e) => { if (OnOpenControlPanelRequested != null) OnOpenControlPanelRequested(); };
            menu.Items.Add(panelItem);

            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Cerrar compañero");
            exitItem.Click += (s, e) => Application.Exit();
            menu.Items.Add(exitItem);

            this.ContextMenuStrip = menu;
        }

        public static string GetSpeciesTitle(string key)
        {
            switch (key)
            {
                case "Duende": return "Duende Navideño";
                case "Reno": return "Reno";
                case "Pinguino": return "Pingüino Invernal";
                case "Gato": return "Gatito con Gorro";
                case "Perro": return "Perrito Navideño";
                default: return key;
            }
        }

        private string GetRandomGreetingFor(string pet)
        {
            string[] common = { "¡Feliz Navidad!", "¡Ho, ho, ho!" };
            if (rnd.NextDouble() < 0.30) return common[rnd.Next(common.Length)];

            string[] petLines;
            switch (pet)
            {
                case "Duende":
                    petLines = new string[] {
                        "¡Listo para envolver regalos!",
                        "¡El taller de Santa está a tope!",
                        "¿Te portaste bien este año?",
                        "¡Ajustando los últimos juguetes!",
                        "¡Huele a bastones de caramelo!"
                    };
                    break;
                case "Reno":
                    petLines = new string[] {
                        "¡Mi nariz brilla hoy!",
                        "¡Listo para guiar el trineo!",
                        "¿Me guardaste una zanahoria?",
                        "¡Esta noche volaremos muy alto!",
                        "¡Mis cascos están listos para la nieve!"
                    };
                    break;
                case "Pinguino":
                    petLines = new string[] {
                        "¡Qué rico friecito!",
                        "¡Deslizarse en el hielo es lo mejor!",
                        "¡Esta bufanda me queda genial!",
                        "¿Hacemos un muñeco de nieve?",
                        "¡El Polo Norte está hermoso hoy!"
                    };
                    break;
                case "Gato":
                    petLines = new string[] {
                        "¡Miau navideño!",
                        "¡Esas esferas del árbol brillan mucho!",
                        "¡Ronroneando junto a la chimenea!",
                        "¿Esa caja de regalo es para dormir?",
                        "¡No fui yo quien tiró el adorno!"
                    };
                    break;
                case "Perro":
                    petLines = new string[] {
                        "¡Guau! ¿Hay galletas?",
                        "¡Llegó mi época favorita del año!",
                        "¡Vamos a jugar en la nieve!",
                        "¡Soy el guardián del árbol de Navidad!",
                        "¿Escuchaste los cascabeles de Santa?"
                    };
                    break;
                default:
                    return "¡Feliz Navidad!";
            }
            return petLines[rnd.Next(petLines.Length)];
        }

        public void SayGreeting(string text)
        {
            speechText = text;
            speechTimer = 110;
        }

        private void SpawnParticles(ParticleKind kind, Color col, int count)
        {
            for (int i = 0; i < count; i++)
            {
                particles.Add(new MiniParticle
                {
                    X = CanvasW / 2f + rnd.Next(-40, 41),
                    Y = CanvasH - 120 + rnd.Next(-25, 25),
                    VX = (float)(rnd.NextDouble() - 0.5) * 3.4f,
                    VY = (float)(-1.6 - rnd.NextDouble() * 2.6),
                    Life = 1.0f,
                    Size = (float)(6 + rnd.NextDouble() * 4),
                    Kind = kind,
                    Color = col
                });
            }
        }

        private void OnPetMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                isDragging = false;
                dragOffset = e.Location;
                mouseDownScreenPos = Cursor.Position;
                lastMousePos = Cursor.Position;
            }
        }

        private void OnPetMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                Point cur = Cursor.Position;
                if (!isDragging)
                {
                    int dx = Math.Abs(cur.X - mouseDownScreenPos.X);
                    int dy = Math.Abs(cur.Y - mouseDownScreenPos.Y);
                    if (dx > 6 || dy > 6)
                    {
                        isDragging = true;
                        state = PetState.Dragging;
                        velX = 0;
                        velY = 0;
                        SayGreeting(rnd.Next(2) == 0 ? "¡Vuelo! D:" : "¡Vuelo! :D");
                    }
                }

                if (isDragging)
                {
                    velX = (cur.X - lastMousePos.X) * 0.45f;
                    velY = (cur.Y - lastMousePos.Y) * 0.45f;
                    if (Math.Abs(velX) > 0.5f) facingRight = velX > 0;
                    lastMousePos = cur;

                    posX = cur.X - dragOffset.X;
                    posY = cur.Y - dragOffset.Y;
                    this.Location = new Point((int)posX, (int)posY);
                }
            }
        }

        private void OnPetMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isMouseDown)
            {
                isMouseDown = false;
                if (isDragging)
                {
                    isDragging = false;
                    float groundY = GetGroundYAt(posX);

                    if (config.StayInPlace)
                    {
                        // Si está activado "Quedarse en su sitio", se queda exactamente donde el usuario la soltó (en cualquier monitor)
                        customGroundY = posY;
                        velX = 0;
                        velY = 0;
                        state = PetState.Idle;
                        stateTimer = 60;
                    }
                    else
                    {
                        customGroundY = -1f;
                        if (posY < groundY - 5)
                        {
                            state = PetState.Falling;
                        }
                        else
                        {
                            posY = groundY;
                            state = PetState.Idle;
                            stateTimer = 55;
                        }
                    }
                }
                else
                {
                    // Fue un clic simple sin arrastrar: un solo saludo y celebración
                    state = PetState.Celebrating;
                    stateTimer = 60;
                    SpawnParticles(ParticleKind.Heart, Color.FromArgb(255, 95, 140), 5);
                    SayGreeting(GetRandomGreetingFor(config.SelectedPet));
                }
            }
        }

        private void OnPetDoubleClick(object sender, EventArgs e)
        {
            // El clic simple ya lanza el saludo; en doble clic hacemos un saltito alegre sin duplicar mensaje
            state = PetState.Jumping;
            velY = -9.5f;
            SpawnParticles(ParticleKind.Star, Color.Gold, 6);
        }

        private void OnLoopTick(object sender, EventArgs e)
        {
            long now = sw.ElapsedMilliseconds;
            float dt = (now - lastMs) / 33.333f; // 1.0 = un paso de 30Hz, ~0.48 a 60FPS
            if (dt <= 0f || dt > 3f) dt = 0.5f;
            lastMs = now;

            float prevTick = animTick;
            animTick += dt;
            if (speechTimer > 0) speechTimer -= dt;

            int minLeft, maxRight, totalSpanW;
            GetMultiMonitorSpan(out minLeft, out maxRight, out totalSpanW);
            float taskbarGroundY = GetGroundYAt(posX);
            float groundY = (config.StayInPlace && customGroundY >= 0f) ? customGroundY : taskbarGroundY;
            float primaryW = Screen.PrimaryScreen.WorkingArea.Width;
            // Escala de velocidad física proporcional al ancho de un monitor estándar (1.0 en 1920x1080)
            float resFactor = Math.Max(0.70f, Math.Min(2.2f, primaryW / 1920f));

            if (!isDragging)
            {
                if (config.StayInPlace && state == PetState.Walking)
                {
                    state = PetState.Idle;
                    stateTimer = 80;
                }

                if (state == PetState.Falling)
                {
                    velY += 0.85f * resFactor * dt;
                    posY += velY * dt;
                    if (!config.StayInPlace) posX += velX * dt;
                    velX *= (float)Math.Pow(0.96, dt);

                    if (posY >= groundY)
                    {
                        posY = groundY;
                        velY = 0;
                        velX = 0;
                        state = PetState.Idle;
                        stateTimer = 50;
                        SpawnParticles(ParticleKind.Snowflake, Color.White, 5);
                    }
                }
                else if (state == PetState.Jumping)
                {
                    velY += 0.65f * resFactor * dt;
                    posY += velY * dt;
                    if (!config.StayInPlace)
                        posX += (facingRight ? 2.4f : -2.4f) * resFactor * dt;

                    if (posY >= groundY)
                    {
                        posY = groundY;
                        velY = 0;
                        state = PetState.Idle;
                        stateTimer = 60;
                    }
                }
                else
                {
                    posY = groundY;
                    stateTimer -= dt;

                    if (state == PetState.Walking && !config.StayInPlace)
                    {
                        float speed = 1.95f * resFactor * dt;
                        posX += facingRight ? speed : -speed;

                        if ((int)(animTick / 45f) > (int)(prevTick / 45f) && rnd.NextDouble() < 0.4)
                        {
                            SpawnParticles(ParticleKind.Snowflake, Color.LightCyan, 1);
                        }
                    }

                    if (stateTimer <= 0)
                    {
                        ChooseNextBehavior();
                    }
                }

                // Efecto Pac-Man / Teletransporte continuo a través de todos los monitores conectados
                if (posX < minLeft - CanvasW / 2f)
                {
                    posX += totalSpanW;
                    if (!config.StayInPlace) posY = GetGroundYAt(posX);
                }
                else if (posX > maxRight - CanvasW / 2f)
                {
                    posX -= totalSpanW;
                    if (!config.StayInPlace) posY = GetGroundYAt(posX);
                }

                this.Location = new Point((int)Math.Round(posX), (int)Math.Round(posY));
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.X += p.VX * dt;
                p.Y += p.VY * dt;
                p.Life -= 0.028f * dt;
                if (p.Life <= 0) particles.RemoveAt(i);
            }

            RenderPet(prevTick);
        }

        public void SetStayInPlace(bool stay)
        {
            config.StayInPlace = stay;
            config.Save();
            if (stay)
            {
                customGroundY = posY;
                if (state == PetState.Walking) state = PetState.Idle;
            }
            else
            {
                customGroundY = -1f;
                float taskbarGroundY = GetGroundYAt(posX);
                if (posY < taskbarGroundY - 5) state = PetState.Falling;
            }
        }

        private void ChooseNextBehavior()
        {
            double roll = rnd.NextDouble();
            if (!config.StayInPlace && roll < 0.44)
            {
                state = PetState.Walking;
                stateTimer = rnd.Next(90, 240);
                if (rnd.NextDouble() < 0.5) facingRight = !facingRight;
            }
            else if (roll < 0.62)
            {
                state = PetState.Idle;
                stateTimer = rnd.Next(60, 140);
                if (!config.DisableAutoMessages && rnd.NextDouble() < 0.22)
                    SayGreeting(GetRandomGreetingFor(config.SelectedPet));
            }
            else if (roll < 0.80)
            {
                state = PetState.Sitting;
                stateTimer = rnd.Next(110, 220);
                if (!config.DisableAutoMessages && rnd.NextDouble() < 0.30)
                    SayGreeting(GetRandomGreetingFor(config.SelectedPet));
            }
            else if (roll < 0.92)
            {
                state = PetState.Sleeping;
                stateTimer = rnd.Next(150, 280);
            }
            else
            {
                state = PetState.Jumping;
                velY = -9.5f;
                SpawnParticles(ParticleKind.Star, Color.Gold, 4);
                if (!config.DisableAutoMessages && rnd.NextDouble() < 0.25)
                    SayGreeting(GetRandomGreetingFor(config.SelectedPet));
            }
        }

        private void RenderPet(float prevTick)
        {
            using (Graphics g = Graphics.FromImage(frameBuffer))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                float cx = CanvasW / 2f;
                float groundBase = CanvasH - 36f;

                float bounceY = 0f;
                float walkCycle = 0f;
                float tiltAngle = 0f;
                float stretchX = 1.0f;
                float stretchY = 1.0f;

                if (state == PetState.Walking)
                {
                    walkCycle = (float)Math.Sin(animTick * 0.32f);
                    bounceY = -Math.Abs(walkCycle) * 5.0f;
                    tiltAngle = walkCycle * 3.8f;
                }
                else if (state == PetState.Idle || state == PetState.Sitting)
                {
                    float breath = (float)Math.Sin(animTick * 0.11f);
                    bounceY = breath * 1.5f;
                    stretchY = 1.0f + breath * 0.02f;
                    stretchX = 1.0f - breath * 0.015f;
                }
                else if (state == PetState.Sleeping)
                {
                    float sleepBreath = (float)Math.Sin(animTick * 0.07f);
                    stretchY = 0.88f + sleepBreath * 0.03f;
                    stretchX = 1.08f - sleepBreath * 0.02f;
                }
                else if (state == PetState.Celebrating)
                {
                    // Truco único por mascota
                    switch (config.SelectedPet)
                    {
                        case "Duende":
                            // Salto acrobático sacando un regalo mágico
                            bounceY = -(float)Math.Abs(Math.Sin(animTick * 0.38f)) * 14f;
                            tiltAngle = (float)Math.Sin(animTick * 0.38f) * 14f;
                            break;
                        case "Reno":
                            // Levanta las patitas delanteras (prance en el aire) y súper destello de nariz
                            bounceY = -(float)Math.Abs(Math.Sin(animTick * 0.32f)) * 16f;
                            tiltAngle = -14f + (float)Math.Sin(animTick * 0.32f) * 6f;
                            break;
                        case "Pinguino":
                            // Se desliza de pancita sobre el hielo (belly slide)
                            bounceY = -2f;
                            tiltAngle = 22f + (float)Math.Sin(animTick * 0.45f) * 5f;
                            walkCycle = (float)Math.Sin(animTick * 0.6f);
                            break;
                        case "Gato":
                            // Se para en dos patitas saludando con la patita levantada ("Maneki-neko") y ronroneando
                            bounceY = -(float)Math.Abs(Math.Sin(animTick * 0.36f)) * 10f;
                            tiltAngle = (float)Math.Sin(animTick * 0.36f) * 6f;
                            break;
                        case "Perro":
                            //Da brincos rápidos batiendo la cola a toda velocidad con un huesito navideño
                            bounceY = -(float)Math.Abs(Math.Sin(animTick * 0.55f)) * 11f;
                            tiltAngle = (float)Math.Sin(animTick * 0.55f) * 7f;
                            break;
                    }
                }
                else if (state == PetState.Dragging)
                {
                    tiltAngle = (float)Math.Sin(animTick * 0.45f) * 12f;
                }

                float curScale = CurrentPetScale;
                float scaleRatio = curScale / BasePetScale;

                // Sombra suave dinámica (se achica si la mascota salta alto, se ensancha si duerme y escala con el slider)
                float shadowScale = Math.Max(0.55f, 1.0f + (bounceY / 45f)) * (state == PetState.Sleeping ? 1.25f : 1.0f) * scaleRatio;
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(85, 0, 0, 0)))
                {
                    float sw = 54f * shadowScale;
                    float sh = 11f * shadowScale;
                    g.FillEllipse(shadow, cx - sw / 2f, groundBase - sh / 2f, sw, sh);
                }

                GraphicsState gState = g.Save();
                g.TranslateTransform(cx, groundBase + bounceY);
                if (!facingRight) g.ScaleTransform(-1, 1);
                g.ScaleTransform(curScale * stretchX, curScale * stretchY);
                g.RotateTransform(tiltAngle);

                switch (config.SelectedPet)
                {
                    case "Duende":
                        DrawElf(g, walkCycle);
                        break;
                    case "Reno":
                        DrawReindeer(g, walkCycle);
                        break;
                    case "Pinguino":
                        DrawPenguin(g, walkCycle);
                        break;
                    case "Gato":
                        DrawCat(g, walkCycle);
                        break;
                    case "Perro":
                        DrawDog(g, walkCycle);
                        break;
                    default:
                        DrawReindeer(g, walkCycle);
                        break;
                }

                g.Restore(gState);

                // Etiqueta pequeña con el nombre de la mascota bajo sus pies (escala con el tamaño de la mascota)
                DrawPetNameTag(g, cx, CanvasH - 17f, config.GetPetName(config.SelectedPet), scaleRatio);

                if (state == PetState.Sleeping)
                {
                    if ((int)(animTick / 35f) > (int)(prevTick / 35f))
                        SpawnParticles(ParticleKind.Zzz, Color.LightSkyBlue, 1);
                }

                // Dibujar partículas 100% vectoriales
                foreach (var p in particles)
                {
                    DrawVectorParticle(g, p);
                }

                // Globo de diálogo si está activo (pegadito sobre la cabeza de la mascota)
                if (speechTimer > 0 && !string.IsNullOrEmpty(speechText))
                {
                    float sleepOffset = (state == PetState.Sleeping) ? 18f : 0f;
                    float headTopY = (groundBase + bounceY) - ((106f - sleepOffset) * curScale);
                    DrawSpeechBubble(g, speechText, headTopY);
                }
            }

            byte alpha = (byte)(config.GhostMode ? 175 : 255);
            Win32.SetBitmap(this, frameBuffer, alpha);

            // Actualizar ventana espejo cuando la mascota está atravesando los extremos del escritorio multi-monitor
            if (wrapMirror != null && !wrapMirror.IsDisposed)
            {
                int minLeft, maxRight, totalSpanW;
                GetMultiMonitorSpan(out minLeft, out maxRight, out totalSpanW);

                if (posX < minLeft)
                {
                    int mirrorX = (int)Math.Round(posX + totalSpanW);
                    int mirrorY = config.StayInPlace ? this.Top : (int)Math.Round(GetGroundYAt(mirrorX));
                    wrapMirror.Location = new Point(mirrorX, mirrorY);
                    if (!wrapMirror.Visible) wrapMirror.Show();
                    Win32.SetBitmap(wrapMirror, frameBuffer, alpha);
                }
                else if (posX + CanvasW > maxRight)
                {
                    int mirrorX = (int)Math.Round(posX - totalSpanW);
                    int mirrorY = config.StayInPlace ? this.Top : (int)Math.Round(GetGroundYAt(mirrorX));
                    wrapMirror.Location = new Point(mirrorX, mirrorY);
                    if (!wrapMirror.Visible) wrapMirror.Show();
                    Win32.SetBitmap(wrapMirror, frameBuffer, alpha);
                }
                else
                {
                    if (wrapMirror.Visible) wrapMirror.Hide();
                }
            }
        }

        private void DrawPetNameTag(Graphics g, float cx, float cy, string petName, float scaleRatio)
        {
            if (string.IsNullOrWhiteSpace(petName)) return;

            // Escalar suavemente la fuente, el alto, el padding y el borde del cuadro de nombre
            float tagScale = Math.Max(0.65f, Math.Min(1.45f, 0.85f + (scaleRatio - 1.0f) * 0.65f));
            float fontSize = Math.Max(8.5f, 11f * tagScale);

            using (Font nameFont = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(petName, nameFont);
                float padX = 14f * tagScale;
                float bw = Math.Min(CanvasW - 6, sz.Width + padX);
                float bh = 20f * tagScale;
                RectangleF badgeRect = new RectangleF(cx - bw / 2f, cy - bh / 2f, bw, bh);

                using (GraphicsPath path = ChristmasIntroOverlay.CreateRoundedRect(badgeRect, 9f * tagScale))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(195, 22, 32, 28)))
                using (Pen border = new Pen(Color.FromArgb(210, 255, 215, 90), Math.Max(1.0f, 1.4f * tagScale)))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(border, path);
                }

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(250, 255, 245, 200)))
                {
                    g.DrawString(petName, nameFont, tb, badgeRect, sf);
                }
            }
        }

        private void DrawVectorParticle(Graphics g, MiniParticle p)
        {
            int a = (int)(Math.Max(0f, Math.Min(1f, p.Life)) * 255);
            Color c = Color.FromArgb(a, p.Color);
            float s = p.Size;

            if (p.Kind == ParticleKind.Snowflake)
            {
                using (Pen pen = new Pen(c, 1.8f))
                {
                    g.DrawLine(pen, p.X - s, p.Y, p.X + s, p.Y);
                    g.DrawLine(pen, p.X, p.Y - s, p.X, p.Y + s);
                    g.DrawLine(pen, p.X - s * 0.7f, p.Y - s * 0.7f, p.X + s * 0.7f, p.Y + s * 0.7f);
                    g.DrawLine(pen, p.X - s * 0.7f, p.Y + s * 0.7f, p.X + s * 0.7f, p.Y - s * 0.7f);
                }
            }
            else if (p.Kind == ParticleKind.Star)
            {
                using (Pen pen = new Pen(c, 2.2f))
                {
                    g.DrawLine(pen, p.X - s, p.Y, p.X + s, p.Y);
                    g.DrawLine(pen, p.X, p.Y - s, p.X, p.Y + s);
                }
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(a, Color.White)))
                {
                    g.FillEllipse(sb, p.X - 2, p.Y - 2, 4, 4);
                }
            }
            else if (p.Kind == ParticleKind.Heart)
            {
                using (SolidBrush sb = new SolidBrush(c))
                {
                    g.FillEllipse(sb, p.X - s * 0.7f, p.Y - s * 0.5f, s * 0.8f, s * 0.8f);
                    g.FillEllipse(sb, p.X - s * 0.1f, p.Y - s * 0.5f, s * 0.8f, s * 0.8f);
                    PointF[] tri = {
                        new PointF(p.X - s * 0.72f, p.Y - s * 0.05f),
                        new PointF(p.X + s * 0.72f, p.Y - s * 0.05f),
                        new PointF(p.X, p.Y + s * 0.75f)
                    };
                    g.FillPolygon(sb, tri);
                }
            }
            else if (p.Kind == ParticleKind.Zzz)
            {
                using (Font zFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (SolidBrush sb = new SolidBrush(c))
                {
                    g.DrawString("Zzz", zFont, sb, p.X - 8, p.Y - 8);
                }
            }
        }

        private void DrawSpeechBubble(Graphics g, string text, float headTopY)
        {
            using (Font f = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(text, f);
                float bw = Math.Min(CanvasW - 8, sz.Width + 20);
                float bh = sz.Height + 10;
                float bx = (CanvasW - bw) / 2f;
                // Ubicamos el globo justo encima del gorrito de la mascota (con un pequeño piquito de cómic)
                float by = Math.Max(4f, headTopY - bh - 9f);

                RectangleF rect = new RectangleF(bx, by, bw, bh);
                using (GraphicsPath path = ChristmasIntroOverlay.CreateRoundedRect(rect, 10))
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(245, 255, 252, 240)))
                using (Pen border = new Pen(Color.FromArgb(235, 195, 40, 40), 2.0f))
                {
                    g.FillPath(bg, path);
                    g.DrawPath(border, path);

                    // Piquito inferior apuntando a la mascota
                    float midX = CanvasW / 2f;
                    PointF[] tail = {
                        new PointF(midX - 5, rect.Bottom - 1),
                        new PointF(midX + 5, rect.Bottom - 1),
                        new PointF(midX, rect.Bottom + 6)
                    };
                    g.FillPolygon(bg, tail);
                    g.DrawLine(border, tail[0], tail[2]);
                    g.DrawLine(border, tail[1], tail[2]);
                }

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(245, 140, 20, 20)))
                {
                    g.DrawString(text, f, tb, rect, sf);
                }
            }
        }

        // ---------------------------------------------------------------------
        // DIBUJO VECTORIAL DETALLADO DE LOS 5 COMPAÑEROS NAVIDEÑOS
        // (Con sombreado degradado, postura única al dormir y truco específico)
        // ---------------------------------------------------------------------
        private void DrawSantaHat(Graphics g, float x, float y, float scale)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(scale, scale);

            float droop = (state == PetState.Sleeping) ? 6f : (float)Math.Sin(animTick * 0.18f) * 2.5f;

            PointF[] cone = new PointF[]
            {
                new PointF(-24, 0),
                new PointF(24, 0),
                new PointF(9, -23 + droop * 0.3f),
                new PointF(-28, -11 + droop)
            };
            using (LinearGradientBrush redGrad = new LinearGradientBrush(new PointF(0, -28), new PointF(0, 5),
                Color.FromArgb(245, 50, 55), Color.FromArgb(175, 18, 24)))
            using (Pen darkRed = new Pen(Color.FromArgb(135, 15, 20), 1.8f))
            {
                g.FillClosedCurve(redGrad, cone, FillMode.Winding, 0.32f);
                g.DrawClosedCurve(darkRed, cone, 0.32f, FillMode.Winding);
            }

            // Pompón esponjoso con sombra
            using (SolidBrush pomShadow = new SolidBrush(Color.FromArgb(220, 215, 225)))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(252, 252, 255)))
            using (Pen gray = new Pen(Color.FromArgb(185, 190, 205), 1.5f))
            {
                g.FillEllipse(pomShadow, -35, -16 + droop, 14, 14);
                g.FillEllipse(white, -35, -17 + droop, 13, 12);
                g.DrawEllipse(gray, -35, -16 + droop, 14, 14);

                RectangleF rim = new RectangleF(-26, -6, 52, 12.5f);
                using (GraphicsPath rimPath = ChristmasIntroOverlay.CreateRoundedRect(rim, 6))
                using (LinearGradientBrush rimGrad = new LinearGradientBrush(rim, Color.White, Color.FromArgb(230, 235, 245), LinearGradientMode.Vertical))
                {
                    g.FillPath(rimGrad, rimPath);
                    g.DrawPath(gray, rimPath);
                }
            }

            g.Restore(st);
        }

        private void DrawCuteEyesAndBlush(Graphics g, float centerX, float eyeY, float spacing)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool celebrating = (state == PetState.Celebrating);
            bool blink = sleeping || (((int)animTick % 110) < 5);

            // Mejillas sonrojadas suaves
            using (SolidBrush blush = new SolidBrush(Color.FromArgb(145, 255, 115, 135)))
            {
                g.FillEllipse(blush, centerX - spacing - 9, eyeY + 5, 11.5f, 7);
                g.FillEllipse(blush, centerX + spacing - 2.5f, eyeY + 5, 11.5f, 7);
            }

            if (celebrating)
            {
                // Ojitos felices "^ ^" durante su truco especial
                using (Pen happyPen = new Pen(Color.FromArgb(40, 25, 25), 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(happyPen, centerX - spacing - 5, eyeY - 3, 10, 9, 180, 180);
                    g.DrawArc(happyPen, centerX + spacing - 5, eyeY - 3, 10, 9, 180, 180);
                }
            }
            else if (blink)
            {
                // Ojitos cerrados tranquilos "u u" al dormir o parpadear
                using (Pen eyePen = new Pen(Color.FromArgb(45, 30, 30), 2.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(eyePen, centerX - spacing - 5, eyeY - 1, 10, 7, 0, 180);
                    g.DrawArc(eyePen, centerX + spacing - 5, eyeY - 1, 10, 7, 0, 180);
                }

                // Burbujita de sueño kawaii cuando está dormido
                if (sleeping)
                {
                    float bubbleSize = 7f + (float)Math.Sin(animTick * 0.12f) * 2.5f;
                    using (SolidBrush bFill = new SolidBrush(Color.FromArgb(130, 195, 235, 255)))
                    using (Pen bPen = new Pen(Color.FromArgb(200, 240, 250, 255), 1.2f))
                    {
                        g.FillEllipse(bFill, centerX + 7, eyeY + 2 - bubbleSize * 0.5f, bubbleSize, bubbleSize);
                        g.DrawEllipse(bPen, centerX + 7, eyeY + 2 - bubbleSize * 0.5f, bubbleSize, bubbleSize);
                    }
                }
            }
            else
            {
                // Ojos detallados con iris brillante y doble destello
                using (SolidBrush dark = new SolidBrush(Color.FromArgb(32, 22, 24)))
                using (SolidBrush iris = new SolidBrush(Color.FromArgb(75, 50, 42)))
                using (SolidBrush shineBig = new SolidBrush(Color.White))
                using (SolidBrush shineSmall = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    g.FillEllipse(dark, centerX - spacing - 5.5f, eyeY - 5.5f, 11, 12);
                    g.FillEllipse(dark, centerX + spacing - 5.5f, eyeY - 5.5f, 11, 12);

                    g.FillEllipse(iris, centerX - spacing - 3.5f, eyeY - 1f, 7, 6.5f);
                    g.FillEllipse(iris, centerX + spacing - 3.5f, eyeY - 1f, 7, 6.5f);

                    g.FillEllipse(shineBig, centerX - spacing - 3f, eyeY - 4f, 4.2f, 4.2f);
                    g.FillEllipse(shineBig, centerX + spacing - 3f, eyeY - 4f, 4.2f, 4.2f);

                    g.FillEllipse(shineSmall, centerX - spacing + 1.2f, eyeY + 1.5f, 2.2f, 2.2f);
                    g.FillEllipse(shineSmall, centerX + spacing + 1.2f, eyeY + 1.5f, 2.2f, 2.2f);
                }
            }
        }

        private void DrawGiftBoxProp(Graphics g, float x, float y, float size)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(x, y);
            using (SolidBrush boxRed = new SolidBrush(Color.FromArgb(230, 40, 48)))
            using (Pen boxBorder = new Pen(Color.FromArgb(145, 20, 25), 1.5f))
            using (SolidBrush goldRibbon = new SolidBrush(Color.FromArgb(255, 220, 65)))
            {
                g.FillRectangle(boxRed, -size / 2, -size / 2, size, size);
                g.DrawRectangle(boxBorder, -size / 2, -size / 2, size, size);
                g.FillRectangle(goldRibbon, -2.5f, -size / 2, 5f, size);
                g.FillRectangle(goldRibbon, -size / 2, -2.5f, size, 5f);
                g.FillEllipse(goldRibbon, -6, -size / 2 - 5, 6, 6);
                g.FillEllipse(goldRibbon, 0, -size / 2 - 5, 6, 6);
            }
            g.Restore(st);
        }

        // 1. DUENDE NAVIDEÑO (Duerme sentado apoyando la cabecita; Truco: malabares con caja de regalo mágica)
        private void DrawElf(Graphics g, float walk)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool trick = (state == PetState.Celebrating);
            float legL = sleeping ? -4f : walk * 6f;
            float legR = sleeping ? 4f : -walk * 6f;
            float sleepDrop = sleeping ? 10f : 0f;

            // Botitas curvas de duende con cascabel dorado en la punta
            using (SolidBrush boot = new SolidBrush(Color.FromArgb(58, 38, 28)))
            using (SolidBrush bell = new SolidBrush(Color.Gold))
            {
                g.FillEllipse(boot, -17 + legL, -10, 16, 10);
                g.FillEllipse(boot, 3 + legR, -10, 16, 10);
                g.FillEllipse(bell, -19 + legL, -8, 4.5f, 4.5f);
                g.FillEllipse(bell, 16 + legR, -8, 4.5f, 4.5f);
            }

            // Trajecito verde esmeralda con degradado y faldón festivo
            RectangleF bodyRect = new RectangleF(-21, -48 + sleepDrop * 0.5f, 42, 40 - sleepDrop * 0.3f);
            using (LinearGradientBrush suitGrad = new LinearGradientBrush(bodyRect,
                Color.FromArgb(48, 178, 82), Color.FromArgb(24, 118, 48), LinearGradientMode.Vertical))
            using (Pen border = new Pen(Color.FromArgb(18, 85, 34), 2f))
            {
                g.FillEllipse(suitGrad, bodyRect);
                g.DrawEllipse(border, bodyRect);
            }

            // Bracitos y mangas
            float armY = -38 + sleepDrop * 0.6f;
            float armLift = trick ? -14f : (walk * 4f);
            using (SolidBrush sleeve = new SolidBrush(Color.FromArgb(38, 155, 68)))
            using (SolidBrush mitten = new SolidBrush(Color.FromArgb(220, 42, 48)))
            {
                g.FillEllipse(sleeve, -26, armY + armLift * 0.5f, 9, 16);
                g.FillEllipse(sleeve, 17, armY - armLift * 0.5f, 9, 16);
                g.FillEllipse(mitten, -26, armY + 11 + armLift * 0.5f, 8, 8);
                g.FillEllipse(mitten, 18, armY + 11 - armLift * 0.5f, 8, 8);
            }

            // Cinturón de cuero con hebilla dorada brillante
            using (SolidBrush belt = new SolidBrush(Color.FromArgb(38, 34, 32)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 215, 60)))
            {
                g.FillRectangle(belt, -19, -27 + sleepDrop * 0.4f, 38, 6);
                g.FillRectangle(gold, -5, -28.5f + sleepDrop * 0.4f, 10, 9);
                g.FillRectangle(belt, -2.5f, -26.5f + sleepDrop * 0.4f, 5, 5);
            }

            // Cuello rojo en picos de duende
            using (SolidBrush red = new SolidBrush(Color.FromArgb(225, 42, 48)))
            {
                g.FillEllipse(red, -18, -50 + sleepDrop * 0.6f, 36, 10);
            }

            // Cabeza (inclinada si está durmiendo)
            GraphicsState headState = g.Save();
            g.TranslateTransform(0, -68 + sleepDrop);
            if (sleeping) g.RotateTransform(14f);

            // Cabello castaño/dorado asomando y orejas puntiagudas
            using (SolidBrush skin = new SolidBrush(Color.FromArgb(255, 226, 198)))
            using (Pen skinBorder = new Pen(Color.FromArgb(195, 148, 118), 1.8f))
            using (SolidBrush hair = new SolidBrush(Color.FromArgb(215, 140, 55)))
            {
                PointF[] leftEar = { new PointF(-21, -4), new PointF(-37, -11), new PointF(-19, 6) };
                PointF[] rightEar = { new PointF(21, -4), new PointF(37, -11), new PointF(19, 6) };
                g.FillPolygon(skin, leftEar);
                g.DrawPolygon(skinBorder, leftEar);
                g.FillPolygon(skin, rightEar);
                g.DrawPolygon(skinBorder, rightEar);

                g.FillEllipse(skin, -24, -24, 48, 44);
                g.DrawEllipse(skinBorder, -24, -24, 48, 44);

                // Flequillo de duende
                g.FillPie(hair, -22, -24, 44, 24, 180, 180);
            }

            DrawCuteEyesAndBlush(g, 2, -2, 11);

            using (Pen smile = new Pen(Color.FromArgb(145, 70, 60), 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (trick)
                    g.FillPie(Brushes.IndianRed, -5, 2, 12, 9, 0, 180);
                else
                    g.DrawArc(smile, -4, 3, 10, 6, 10, 160);
            }

            // Gorro de duende con borde rojo y cascabel dorado
            PointF[] cone = { new PointF(-25, -20), new PointF(25, -20), new PointF(7, -44), new PointF(-28, -32) };
            using (LinearGradientBrush greenHat = new LinearGradientBrush(new PointF(0, -46), new PointF(0, -15),
                Color.FromArgb(52, 185, 85), Color.FromArgb(25, 125, 52)))
            using (Pen hatOutline = new Pen(Color.FromArgb(18, 90, 36), 1.8f))
            using (SolidBrush redTrim = new SolidBrush(Color.FromArgb(228, 45, 50)))
            using (SolidBrush goldBell = new SolidBrush(Color.Gold))
            {
                g.FillClosedCurve(greenHat, cone);
                g.DrawClosedCurve(hatOutline, cone);
                RectangleF brim = new RectangleF(-25, -25, 50, 9.5f);
                using (GraphicsPath bp = ChristmasIntroOverlay.CreateRoundedRect(brim, 4.5f))
                {
                    g.FillPath(redTrim, bp);
                }
                g.FillEllipse(goldBell, -34, -38, 11, 11);
                g.FillEllipse(Brushes.White, -32, -36, 3.5f, 3.5f);
            }

            g.Restore(headState);

            // Truco específico del Duende: Lanza un regalo navideño brillante al aire
            if (trick)
            {
                float giftY = -98f - (float)Math.Abs(Math.Sin(animTick * 0.35f)) * 18f;
                DrawGiftBoxProp(g, 26f, giftY, 18f);
            }
        }

        // 2. RENO (Duerme echadito con las patitas plegadas; Truco: Salto de trineo con súper destello de nariz roja)
        private void DrawReindeer(Graphics g, float walk)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool trick = (state == PetState.Celebrating);
            float legL = walk * 6f;
            float legR = -walk * 6f;
            float sleepDrop = sleeping ? 12f : 0f;

            RectangleF bodyRect = new RectangleF(-23, -52 + sleepDrop, 46, 40 - sleepDrop * 0.25f);
            using (LinearGradientBrush furGrad = new LinearGradientBrush(bodyRect,
                Color.FromArgb(182, 118, 68), Color.FromArgb(138, 82, 42), LinearGradientMode.Vertical))
            using (SolidBrush belly = new SolidBrush(Color.FromArgb(242, 212, 175)))
            using (SolidBrush hoof = new SolidBrush(Color.FromArgb(65, 42, 28)))
            using (Pen dark = new Pen(Color.FromArgb(98, 58, 30), 2f))
            {
                // Colita de reno
                g.FillEllipse(furGrad, -28, -42 + sleepDrop, 10, 12);

                if (sleeping)
                {
                    // Patitas plegadas bajo el cuerpo al dormir echado
                    g.FillEllipse(furGrad, -22, -12, 16, 9);
                    g.FillEllipse(furGrad, 6, -12, 16, 9);
                }
                else
                {
                    float pranceLift = trick ? -8f : 0f;
                    g.FillRectangle(furGrad, -15 + legL, -16, 10, 14);
                    g.FillRectangle(furGrad, 5 + legR, -16 + pranceLift, 10, 14);
                    g.FillRectangle(hoof, -15 + legL, -6, 10, 6);
                    g.FillRectangle(hoof, 5 + legR, -6 + pranceLift, 10, 6);
                }

                // Cuerpo con volumen
                g.FillEllipse(furGrad, bodyRect);
                g.DrawEllipse(dark, bodyRect);
                g.FillEllipse(belly, -13, -45 + sleepDrop, 28, 26 - sleepDrop * 0.2f);

                // Collar de cuero rojo con cascabeles dorados
                using (SolidBrush red = new SolidBrush(Color.FromArgb(220, 35, 38)))
                using (SolidBrush gold = new SolidBrush(Color.Gold))
                using (Pen bellLine = new Pen(Color.FromArgb(140, 100, 20), 1.5f))
                {
                    g.FillRectangle(red, -18, -51 + sleepDrop, 36, 7);
                    g.FillEllipse(gold, -6, -48 + sleepDrop, 13, 13);
                    g.DrawEllipse(bellLine, -6, -48 + sleepDrop, 13, 13);
                    g.FillEllipse(Brushes.White, -3, -46 + sleepDrop, 3.5f, 3.5f);
                }

                // Astas (Cuernos ramificados elegantes)
                using (Pen antlerPen = new Pen(Color.FromArgb(110, 70, 42), 4.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(antlerPen, -12, -88 + sleepDrop, -25, -113 + sleepDrop);
                    g.DrawLine(antlerPen, -19, -102 + sleepDrop, -30, -101 + sleepDrop);
                    g.DrawLine(antlerPen, -16, -96 + sleepDrop, -10, -109 + sleepDrop);

                    g.DrawLine(antlerPen, 12, -88 + sleepDrop, 25, -113 + sleepDrop);
                    g.DrawLine(antlerPen, 19, -102 + sleepDrop, 30, -101 + sleepDrop);
                    g.DrawLine(antlerPen, 16, -96 + sleepDrop, 10, -109 + sleepDrop);
                }

                // Orejitas de reno a los lados
                PointF[] earL = { new PointF(-22, -80 + sleepDrop), new PointF(-36, -86 + sleepDrop), new PointF(-24, -70 + sleepDrop) };
                PointF[] earR = { new PointF(22, -80 + sleepDrop), new PointF(36, -86 + sleepDrop), new PointF(24, -70 + sleepDrop) };
                g.FillPolygon(furGrad, earL);
                g.DrawPolygon(dark, earL);
                g.FillPolygon(furGrad, earR);
                g.DrawPolygon(dark, earR);

                // Cabeza y hocico crema
                RectangleF headRect = new RectangleF(-24, -92 + sleepDrop, 48, 44);
                g.FillEllipse(furGrad, headRect);
                g.DrawEllipse(dark, headRect);
                g.FillEllipse(belly, -16, -71 + sleepDrop, 34, 21);
            }

            DrawCuteEyesAndBlush(g, 2, -73 + sleepDrop, 12);

            // Nariz roja mágica de Rudolph (halo sutil sin tapar la carita; en su truco saca una zanahoria y sus astas brillan con lucecitas navideñas)
            float glow = 0.55f + 0.45f * (float)Math.Sin(animTick * 0.22f);
            using (SolidBrush redGlow = new SolidBrush(Color.FromArgb((int)(70 * glow), 255, 50, 50)))
            using (SolidBrush redNose = new SolidBrush(Color.FromArgb(245, 25, 30)))
            using (SolidBrush shine = new SolidBrush(Color.White))
            {
                g.FillEllipse(redGlow, -1.5f, -70.5f + sleepDrop, 16f, 15f);
                g.FillEllipse(redNose, 1.5f, -67.5f + sleepDrop, 10f, 9f);
                g.FillEllipse(shine, 3.5f, -66f + sleepDrop, 3f, 2.5f);
            }

            // Truco específico del Reno: Lucecitas de colores encendidas en sus astas + una zanahoria flotando
            if (trick)
            {
                using (SolidBrush bulbGold = new SolidBrush(Color.Gold))
                using (SolidBrush bulbGreen = new SolidBrush(Color.FromArgb(60, 235, 100)))
                using (SolidBrush bulbCyan = new SolidBrush(Color.FromArgb(80, 210, 255)))
                using (SolidBrush bulbRed = new SolidBrush(Color.FromArgb(255, 65, 65)))
                {
                    g.FillEllipse(bulbGold, -28, -115 + sleepDrop, 6, 6);
                    g.FillEllipse(bulbGreen, -32, -103 + sleepDrop, 6, 6);
                    g.FillEllipse(bulbCyan, -13, -111 + sleepDrop, 6, 6);

                    g.FillEllipse(bulbRed, 22, -115 + sleepDrop, 6, 6);
                    g.FillEllipse(bulbGold, 27, -103 + sleepDrop, 6, 6);
                    g.FillEllipse(bulbGreen, 7, -111 + sleepDrop, 6, 6);
                }

                // Zanahoria navideña
                float cY = -62f + (float)Math.Sin(animTick * 0.45f) * 4f;
                float cX = 24f;
                using (SolidBrush carrotOrange = new SolidBrush(Color.FromArgb(255, 135, 25)))
                using (Pen carrotBorder = new Pen(Color.FromArgb(185, 85, 10), 1.3f))
                using (SolidBrush leafGreen = new SolidBrush(Color.FromArgb(45, 180, 65)))
                {
                    PointF[] carrotBody = { new PointF(cX, cY), new PointF(cX + 14, cY - 5), new PointF(cX + 12, cY + 2) };
                    g.FillPolygon(carrotOrange, carrotBody);
                    g.DrawPolygon(carrotBorder, carrotBody);
                    g.FillEllipse(leafGreen, cX + 12, cY - 7, 6, 4);
                    g.FillEllipse(leafGreen, cX + 11, cY - 3, 6, 4);
                }
            }
        }

        // 3. PINGÜINO INVERNAL (Duerme acurrucado como bolita esponjosa; Truco: se desliza de pancita agitando las alitas)
        private void DrawPenguin(Graphics g, float walk)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool trick = (state == PetState.Celebrating);
            float footL = walk * 5f;
            float footR = -walk * 5f;
            float sleepDrop = sleeping ? 10f : 0f;

            // Patitas naranjas
            using (SolidBrush orange = new SolidBrush(Color.FromArgb(255, 160, 25)))
            using (Pen orangeBorder = new Pen(Color.FromArgb(195, 110, 10), 1.5f))
            {
                g.FillEllipse(orange, -18 + footL, -9, 16, 9);
                g.DrawEllipse(orangeBorder, -18 + footL, -9, 16, 9);
                g.FillEllipse(orange, 4 + footR, -9, 16, 9);
                g.DrawEllipse(orangeBorder, 4 + footR, -9, 16, 9);
            }

            RectangleF bodyRect = new RectangleF(-26, -78 + sleepDrop, 52, 72 - sleepDrop);
            using (LinearGradientBrush bodyGrad = new LinearGradientBrush(bodyRect,
                Color.FromArgb(52, 66, 90), Color.FromArgb(28, 36, 52), LinearGradientMode.Vertical))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(248, 250, 255)))
            using (Pen outline = new Pen(Color.FromArgb(20, 26, 38), 2f))
            {
                g.FillEllipse(bodyGrad, bodyRect);
                g.DrawEllipse(outline, bodyRect);

                // Pancita y máscara facial blanca suave
                g.FillEllipse(white, -18, -68 + sleepDrop, 38, 58 - sleepDrop);

                // Alitas laterales (en su truco las abre feliz mientras se desliza)
                float wingFlap = trick ? (float)Math.Sin(animTick * 0.7f) * 14f - 12f : (sleeping ? 4f : walk * 8f);
                g.FillEllipse(bodyGrad, -30, -54 + sleepDrop + wingFlap * 0.3f, 11, 28);
                g.DrawEllipse(outline, -30, -54 + sleepDrop + wingFlap * 0.3f, 11, 28);
                g.FillEllipse(bodyGrad, 19, -54 + sleepDrop - wingFlap * 0.3f, 11, 28);
                g.DrawEllipse(outline, 19, -54 + sleepDrop - wingFlap * 0.3f, 11, 28);
            }

            DrawCuteEyesAndBlush(g, 2, -56 + sleepDrop, 11);

            // Piquito naranja redondeado
            using (SolidBrush beak = new SolidBrush(Color.FromArgb(255, 175, 25)))
            using (Pen beakPen = new Pen(Color.FromArgb(195, 120, 15), 1.4f))
            {
                PointF[] beakPts = { new PointF(-3.5f, -53 + sleepDrop), new PointF(9.5f, -53 + sleepDrop), new PointF(3f, -45.5f + sleepDrop) };
                g.FillPolygon(beak, beakPts);
                g.DrawPolygon(beakPen, beakPts);
            }

            // Bufanda tejida roja con franjas blancas
            using (SolidBrush scarf = new SolidBrush(Color.FromArgb(222, 38, 45)))
            using (Pen stripe = new Pen(Color.FromArgb(240, 255, 245, 230), 2f))
            using (Pen scarfOutline = new Pen(Color.FromArgb(145, 20, 25), 1.5f))
            {
                RectangleF neckScarf = new RectangleF(-21, -44 + sleepDrop, 42, 8.5f);
                g.FillRectangle(scarf, neckScarf);
                g.DrawRectangle(scarfOutline, neckScarf.X, neckScarf.Y, neckScarf.Width, neckScarf.Height);
                g.FillRectangle(scarf, 8, -41 + sleepDrop, 9, 17);
                g.DrawLine(stripe, 8, -28 + sleepDrop, 17, -28 + sleepDrop);
            }

            // En su truco especial salen chispas de hielo bajo su pancita al deslizarse
            if (trick)
            {
                using (Pen icePen = new Pen(Color.FromArgb(210, 180, 240, 255), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(icePen, -28, -3, -10, -3);
                    g.DrawLine(icePen, -22, -7, -14, -7);
                }
            }

            DrawSantaHat(g, 0, -76 + sleepDrop, 0.95f);
        }

        // 4. GATITO SIAMÉS ATIGRADO / LYNX POINT (Blanco/crema claro, rayitas gris-topo en frente y mejillas, orejitas grisáceas y ojos azul cielo)
        private void DrawCat(Graphics g, float walk)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool trick = (state == PetState.Celebrating);
            float legL = walk * 5f;
            float legR = -walk * 5f;
            float sleepDrop = sleeping ? 14f : 0f;

            // Colita gris-topo claro con punta más oscura (Lynx point)
            float tailWave = sleeping ? 0f : (float)Math.Sin(animTick * 0.22f) * 9f;
            using (Pen tailBorder = new Pen(Color.FromArgb(115, 108, 102), 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (Pen tailPen = new Pen(Color.FromArgb(182, 174, 166), 6.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                if (sleeping)
                {
                    g.DrawArc(tailBorder, -26, -24, 52, 20, 0, 180);
                    g.DrawArc(tailPen, -26, -24, 52, 20, 0, 180);
                }
                else
                {
                    g.DrawBezier(tailBorder, -16, -20, -36, -25, -38, -45 + tailWave, -26, -52 + tailWave);
                    g.DrawBezier(tailPen, -16, -20, -36, -25, -38, -45 + tailWave, -26, -52 + tailWave);
                }
            }

            RectangleF bodyRect = new RectangleF(-22, -48 + sleepDrop, 44, 42 - sleepDrop * 0.35f);
            using (LinearGradientBrush furGrad = new LinearGradientBrush(bodyRect,
                Color.FromArgb(250, 248, 244), Color.FromArgb(234, 228, 220), LinearGradientMode.Vertical))
            using (SolidBrush pureWhite = new SolidBrush(Color.FromArgb(255, 254, 252)))
            using (SolidBrush earTaupe = new SolidBrush(Color.FromArgb(172, 162, 152)))
            using (SolidBrush innerEar = new SolidBrush(Color.FromArgb(228, 185, 182)))
            using (Pen border = new Pen(Color.FromArgb(148, 138, 128), 1.8f))
            using (Pen tabbyStripe = new Pen(Color.FromArgb(185, 118, 110, 104), 2.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                // Patitas blancas con suave sombra grisácea
                if (!sleeping)
                {
                    g.FillEllipse(pureWhite, -16 + legL, -10, 13, 10);
                    g.DrawEllipse(border, -16 + legL, -10, 13, 10);
                    g.FillEllipse(pureWhite, 4 + legR, -10, 13, 10);
                    g.DrawEllipse(border, 4 + legR, -10, 13, 10);
                }

                // Cuerpo blanco marfil con pechito blanco puro
                g.FillEllipse(furGrad, bodyRect);
                g.DrawEllipse(border, bodyRect);
                g.FillEllipse(pureWhite, -13, -43 + sleepDrop, 28, 29 - sleepDrop * 0.3f);

                // Rayitas atigradas muy sutiles en los costados del cuerpo
                g.DrawLine(tabbyStripe, -20, -32 + sleepDrop, -15, -30 + sleepDrop);
                g.DrawLine(tabbyStripe, 15, -30 + sleepDrop, 20, -32 + sleepDrop);

                // Orejitas gris-topo suave con interior rosado pálido (como en la foto)
                PointF[] earL = { new PointF(-22, -74 + sleepDrop), new PointF(-19, -99 + sleepDrop), new PointF(-4, -84 + sleepDrop) };
                PointF[] earR = { new PointF(22, -74 + sleepDrop), new PointF(19, -99 + sleepDrop), new PointF(4, -84 + sleepDrop) };
                PointF[] inEarL = { new PointF(-19, -76 + sleepDrop), new PointF(-17, -93 + sleepDrop), new PointF(-8, -83 + sleepDrop) };
                PointF[] inEarR = { new PointF(19, -76 + sleepDrop), new PointF(17, -93 + sleepDrop), new PointF(8, -83 + sleepDrop) };

                g.FillPolygon(earTaupe, earL);
                g.FillPolygon(innerEar, inEarL);
                g.DrawPolygon(border, earL);

                g.FillPolygon(earTaupe, earR);
                g.FillPolygon(innerEar, inEarR);
                g.DrawPolygon(border, earR);

                // Cabeza blanca/crema sin mancha negra pesada
                RectangleF headRect = new RectangleF(-24, -88 + sleepDrop, 48, 44);
                g.FillEllipse(furGrad, headRect);
                g.DrawEllipse(border, headRect);

                // Hocico blanco luminoso y puente de la nariz ligeramente arena/beige (exacto a la foto)
                using (SolidBrush noseBridge = new SolidBrush(Color.FromArgb(115, 195, 170, 148)))
                {
                    g.FillEllipse(noseBridge, -3, -72 + sleepDrop, 10, 11);
                }
                g.FillEllipse(pureWhite, -11, -63 + sleepDrop, 13, 10);
                g.FillEllipse(pureWhite, 0, -63 + sleepDrop, 13, 10);

                // Rayitas atigradas (Lynx Tabby) en la frente ("M") y en las mejillas junto a los ojos
                g.DrawLine(tabbyStripe, -5, -83 + sleepDrop, -3, -75 + sleepDrop);
                g.DrawLine(tabbyStripe, 2, -84 + sleepDrop, 2, -75 + sleepDrop);
                g.DrawLine(tabbyStripe, 9, -83 + sleepDrop, 7, -75 + sleepDrop);

                // Líneas finas desde el borde exterior de los ojos hacia las sienes/mejillas
                g.DrawLine(tabbyStripe, -22, -70 + sleepDrop, -15, -69 + sleepDrop);
                g.DrawLine(tabbyStripe, -21, -65 + sleepDrop, -15, -65 + sleepDrop);
                g.DrawLine(tabbyStripe, 17, -69 + sleepDrop, 23, -70 + sleepDrop);
                g.DrawLine(tabbyStripe, 17, -65 + sleepDrop, 22, -65 + sleepDrop);
            }

            // Ojos azul zafiro redonditos como en el diseño anterior
            DrawCuteEyesAndBlush(g, 2, -67 + sleepDrop, 11);
            if (!sleeping && state != PetState.Celebrating && (((int)animTick % 110) >= 5))
            {
                using (SolidBrush blueIris = new SolidBrush(Color.FromArgb(65, 165, 245)))
                using (SolidBrush shineBig = new SolidBrush(Color.White))
                {
                    g.FillEllipse(blueIris, 2 - 11 - 3.5f, -67 + sleepDrop - 1f, 7f, 6.5f);
                    g.FillEllipse(blueIris, 2 + 11 - 3.5f, -67 + sleepDrop - 1f, 7f, 6.5f);
                    g.FillEllipse(shineBig, 2 - 11 - 3f, -67 + sleepDrop - 4f, 4.2f, 4.2f);
                    g.FillEllipse(shineBig, 2 + 11 - 3f, -67 + sleepDrop - 4f, 4.2f, 4.2f);
                }
            }

            // Naricita oscura/terracota con borde negro, boquita ":3", puntitos de bigotes y bigotes blancos
            using (SolidBrush noseCol = new SolidBrush(Color.FromArgb(115, 78, 72)))
            using (Pen darkNosePen = new Pen(Color.FromArgb(58, 42, 38), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(140, 95, 82, 74)))
            using (Pen whisker = new Pen(Color.FromArgb(210, 245, 245, 245), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.FillEllipse(noseCol, -0.5f, -63.5f + sleepDrop, 7f, 4.5f);
                g.DrawEllipse(darkNosePen, -0.5f, -63.5f + sleepDrop, 7f, 4.5f);
                g.DrawArc(darkNosePen, -4, -60 + sleepDrop, 6, 4.5f, 0, 180);
                g.DrawArc(darkNosePen, 2, -60 + sleepDrop, 6, 4.5f, 0, 180);

                // Puntitos de los bigotes en el hocico blanco (como en la foto)
                g.FillEllipse(dotBrush, -6.5f, -59.5f + sleepDrop, 2f, 2f);
                g.FillEllipse(dotBrush, -9.0f, -60.5f + sleepDrop, 2f, 2f);
                g.FillEllipse(dotBrush, 8.5f, -59.5f + sleepDrop, 2f, 2f);
                g.FillEllipse(dotBrush, 11.0f, -60.5f + sleepDrop, 2f, 2f);

                g.DrawLine(whisker, -15, -63 + sleepDrop, -30, -65 + sleepDrop);
                g.DrawLine(whisker, -15, -59 + sleepDrop, -30, -58 + sleepDrop);
                g.DrawLine(whisker, 19, -63 + sleepDrop, 32, -65 + sleepDrop);
                g.DrawLine(whisker, 19, -59 + sleepDrop, 32, -58 + sleepDrop);
            }

            // Moño verde y rojo navideño
            using (SolidBrush green = new SolidBrush(Color.FromArgb(35, 165, 70)))
            using (SolidBrush red = new SolidBrush(Color.FromArgb(228, 42, 48)))
            {
                PointF[] bowL = { new PointF(0, -45 + sleepDrop), new PointF(-12, -51 + sleepDrop), new PointF(-12, -39 + sleepDrop) };
                PointF[] bowR = { new PointF(0, -45 + sleepDrop), new PointF(12, -51 + sleepDrop), new PointF(12, -39 + sleepDrop) };
                g.FillPolygon(green, bowL);
                g.FillPolygon(green, bowR);
                g.FillEllipse(red, -4, -49 + sleepDrop, 8, 8);
            }

            DrawSantaHat(g, 0, -86 + sleepDrop, 0.9f);

            // Truco específico del Gatito: Levanta su patita blanca con almohadillas rosadas saludando ("Maneki-neko")
            if (trick)
            {
                float waveY = (float)Math.Sin(animTick * 0.55f) * 5f;
                using (SolidBrush pawWhite = new SolidBrush(Color.FromArgb(252, 250, 246)))
                using (Pen pawBorder = new Pen(Color.FromArgb(148, 138, 128), 1.8f))
                using (SolidBrush padPink = new SolidBrush(Color.FromArgb(235, 165, 172)))
                {
                    RectangleF pawRect = new RectangleF(14, -52 + waveY, 13, 15);
                    g.FillEllipse(pawWhite, pawRect);
                    g.DrawEllipse(pawBorder, pawRect);
                    // Almohadilla rosada kawaii
                    g.FillEllipse(padPink, 18, -44 + waveY, 5.5f, 4.5f);
                    g.FillEllipse(padPink, 16.5f, -48 + waveY, 2.5f, 2.5f);
                    g.FillEllipse(padPink, 19.5f, -49 + waveY, 2.5f, 2.5f);
                    g.FillEllipse(padPink, 22.5f, -48 + waveY, 2.5f, 2.5f);
                }
            }
        }

        // 5. PERRITO NAVIDEÑO (Duerme echadito sobre sus patitas delanteras; Truco: bate la cola rapidísimo con un huesito navideño)
        private void DrawDog(Graphics g, float walk)
        {
            bool sleeping = (state == PetState.Sleeping);
            bool trick = (state == PetState.Celebrating);
            float legL = walk * 5f;
            float legR = -walk * 5f;
            float sleepDrop = sleeping ? 13f : 0f;

            // Colita moviéndose feliz (a toda velocidad durante su truco)
            float wagSpeed = trick ? 0.85f : 0.42f;
            float tailWag = sleeping ? 6f : (float)Math.Sin(animTick * wagSpeed) * (trick ? 15f : 10f);
            using (Pen tailBorder = new Pen(Color.FromArgb(140, 92, 48), 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (Pen tailPen = new Pen(Color.FromArgb(222, 165, 102), 6.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(tailBorder, -16, -24 + sleepDrop * 0.5f, -31, -38 + sleepDrop + tailWag);
                g.DrawLine(tailPen, -16, -24 + sleepDrop * 0.5f, -31, -38 + sleepDrop + tailWag);
            }

            RectangleF bodyRect = new RectangleF(-22, -48 + sleepDrop, 44, 42 - sleepDrop * 0.3f);
            using (LinearGradientBrush furGrad = new LinearGradientBrush(bodyRect,
                Color.FromArgb(235, 180, 118), Color.FromArgb(198, 138, 78), LinearGradientMode.Vertical))
            using (SolidBrush darkEar = new SolidBrush(Color.FromArgb(132, 82, 42)))
            using (SolidBrush cream = new SolidBrush(Color.FromArgb(252, 242, 226)))
            using (Pen border = new Pen(Color.FromArgb(138, 90, 46), 2f))
            {
                // Patitas (extendidas al frente si duerme echadito)
                if (sleeping)
                {
                    g.FillEllipse(cream, -20, -10, 17, 9);
                    g.DrawEllipse(border, -20, -10, 17, 9);
                    g.FillEllipse(cream, 3, -10, 17, 9);
                    g.DrawEllipse(border, 3, -10, 17, 9);
                }
                else
                {
                    g.FillEllipse(cream, -16 + legL, -10, 14, 10);
                    g.DrawEllipse(border, -16 + legL, -10, 14, 10);
                    g.FillEllipse(cream, 4 + legR, -10, 14, 10);
                    g.DrawEllipse(border, 4 + legR, -10, 14, 10);
                }

                // Cuerpo
                g.FillEllipse(furGrad, bodyRect);
                g.DrawEllipse(border, bodyRect);
                g.FillEllipse(cream, -13, -42 + sleepDrop, 26, 28 - sleepDrop * 0.25f);

                // Cabeza
                RectangleF headRect = new RectangleF(-24, -88 + sleepDrop, 48, 44);
                g.FillEllipse(furGrad, headRect);
                g.DrawEllipse(border, headRect);

                // Manchita tierna alrededor del ojo izquierdo y cejitas crema
                g.FillEllipse(darkEar, -17, -77 + sleepDrop, 14, 13);
                g.FillEllipse(cream, -12, -79 + sleepDrop, 5, 3);
                g.FillEllipse(cream, 10, -79 + sleepDrop, 5, 3);

                // Orejitas caídas suaves (se levantan un poquito en su truco)
                float earLift = trick ? -5f : 0f;
                g.FillEllipse(darkEar, -30, -82 + sleepDrop + earLift, 12, 27);
                g.DrawEllipse(border, -30, -82 + sleepDrop + earLift, 12, 27);
                g.FillEllipse(darkEar, 18, -82 + sleepDrop + earLift, 12, 27);
                g.DrawEllipse(border, 18, -82 + sleepDrop + earLift, 12, 27);

                // Hocico crema
                g.FillEllipse(cream, -10, -66 + sleepDrop, 24, 16);
            }

            DrawCuteEyesAndBlush(g, 2, -70 + sleepDrop, 11);

            // Nariz negra brillante y lengüita rosada feliz
            using (SolidBrush nose = new SolidBrush(Color.FromArgb(38, 32, 32)))
            using (SolidBrush tongue = new SolidBrush(Color.FromArgb(255, 110, 132)))
            {
                if (!sleeping)
                    g.FillEllipse(tongue, 1, -56 + sleepDrop, 7, 9);
                g.FillEllipse(nose, -1.5f, -65.5f + sleepDrop, 9, 6.5f);
                g.FillEllipse(Brushes.White, 0, -64.5f + sleepDrop, 2.5f, 2f);
            }

            // Bufanda navideña roja con medalla dorada
            using (SolidBrush red = new SolidBrush(Color.FromArgb(222, 38, 42)))
            {
                g.FillRectangle(red, -18, -48 + sleepDrop, 36, 7);
                g.FillEllipse(Brushes.Gold, -3.5f, -45 + sleepDrop, 7, 7);
            }

            DrawSantaHat(g, 0, -86 + sleepDrop, 0.92f);

            // Truco específico del Perrito: Sostiene/atrapa un huesito navideño con moño rojo
            if (trick)
            {
                float boneY = -55f + (float)Math.Sin(animTick * 0.55f) * 3f;
                float boneX = 18f;
                using (SolidBrush boneWhite = new SolidBrush(Color.FromArgb(252, 248, 238)))
                using (Pen boneOutline = new Pen(Color.FromArgb(160, 145, 125), 1.4f))
                using (SolidBrush ribbon = new SolidBrush(Color.FromArgb(230, 35, 45)))
                {
                    g.FillEllipse(boneWhite, boneX - 9, boneY - 4, 6, 6);
                    g.FillEllipse(boneWhite, boneX - 9, boneY, 6, 6);
                    g.FillEllipse(boneWhite, boneX + 7, boneY - 4, 6, 6);
                    g.FillEllipse(boneWhite, boneX + 7, boneY, 6, 6);
                    g.FillRectangle(boneWhite, boneX - 6, boneY - 2, 16, 6);
                    g.DrawRectangle(boneOutline, boneX - 6, boneY - 2, 16, 6);
                    g.FillEllipse(ribbon, boneX - 1, boneY - 3, 6, 7);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (loopTimer != null) loopTimer.Dispose();
                if (frameBuffer != null) frameBuffer.Dispose();
                if (wrapMirror != null && !wrapMirror.IsDisposed) wrapMirror.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // =========================================================================
    // FASE 3: PANEL DE CONTROL VISUAL (CON CAMBIO DE NOMBRE POR MASCOTA)
    // =========================================================================
    public class ControlPanelForm : Form
    {
        private AppConfig config;
        private DesktopPetForm petForm;
        private CheckBox chkStartup;
        private CheckBox chkIntro;
        private CheckBox chkGhost;
        private CheckBox chkStayInPlace;
        private CheckBox chkDisableAutoMessages;
        private ComboBox cmbPet;
        private TextBox txtPetName;
        private TrackBar trkScale;
        private Label lblScaleVal;
        private Label lblStatus;
        private ToolTip toolTip;
        private string[] keys = { "Duende", "Reno", "Pinguino", "Gato", "Perro" };

        public ControlPanelForm(AppConfig cfg, DesktopPetForm pet)
        {
            this.config = cfg;
            this.petForm = pet;

            this.Text = "Compañero Navideño";
            this.ClientSize = new Size(520, 705);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 35, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            toolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 200,
                ReshowDelay = 100,
                ShowAlways = true
            };

            BuildUI();
        }

        private void BuildUI()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.FromArgb(145, 25, 32)
            };

            Label title = new Label
            {
                Text = "Compañero Navideño",
                Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 240, 185),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            header.Controls.Add(title);
            this.Controls.Add(header);

            int y = 90;

            Label lblChoose = new Label
            {
                Text = "1. Elige tu compañero virtual de escritorio:",
                Location = new Point(30, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 225, 130)
            };
            this.Controls.Add(lblChoose);

            y += 28;
            cmbPet = new ComboBox
            {
                Location = new Point(30, y),
                Width = 460,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11f)
            };
            foreach (string k in keys)
            {
                cmbPet.Items.Add(DesktopPetForm.GetSpeciesTitle(k));
            }
            int idx = Array.IndexOf(keys, config.SelectedPet);
            cmbPet.SelectedIndex = idx >= 0 ? idx : 1;
            cmbPet.SelectedIndexChanged += (s, e) =>
            {
                string chosen = keys[cmbPet.SelectedIndex];
                petForm.ChangePet(chosen);
                if (txtPetName != null)
                    txtPetName.Text = config.GetPetName(chosen);
                UpdateStatus("Compañero cambiado a: " + DesktopPetForm.GetSpeciesTitle(chosen));
            };
            this.Controls.Add(cmbPet);

            // Campo para cambiar el nombre personalizado de la mascota actual
            y += 46;
            Label lblName = new Label
            {
                Text = "2. Nombre de tu mascota:",
                Location = new Point(30, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 225, 130)
            };
            this.Controls.Add(lblName);

            y += 28;
            txtPetName = new TextBox
            {
                Location = new Point(30, y),
                Width = 315,
                Font = new Font("Segoe UI", 11f),
                Text = config.GetPetName(config.SelectedPet)
            };
            this.Controls.Add(txtPetName);

            Button btnSaveName = new Button
            {
                Text = "Guardar",
                Location = new Point(355, y - 1),
                Size = new Size(135, 31),
                BackColor = Color.FromArgb(45, 125, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSaveName.Click += (s, e) =>
            {
                string currentKey = keys[cmbPet.SelectedIndex];
                config.SetPetName(currentKey, txtPetName.Text);
                config.Save();
                petForm.SayGreeting("¡Me llamo " + config.GetPetName(currentKey) + "!");
                UpdateStatus("Nombre actualizado a: " + config.GetPetName(currentKey));
            };
            this.Controls.Add(btnSaveName);

            // 3. Slider para ajustar el tamaño de la mascota en tiempo real
            y += 44;
            lblScaleVal = new Label
            {
                Text = "3. Tamaño de la mascota: " + config.ScalePercent + "%",
                Location = new Point(30, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 225, 130)
            };
            this.Controls.Add(lblScaleVal);

            y += 24;
            trkScale = new TrackBar
            {
                Location = new Point(25, y),
                AutoSize = false,
                Size = new Size(470, 42),
                Minimum = 50,
                Maximum = 180,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 15,
                Value = Math.Max(50, Math.Min(180, config.ScalePercent)),
                BackColor = Color.FromArgb(28, 35, 32)
            };
            trkScale.Scroll += (s, e) =>
            {
                int val = trkScale.Value;
                lblScaleVal.Text = "3. Tamaño de la mascota: " + val + "%";
                petForm.SetScalePercent(val);
            };
            AttachHelp(trkScale, "Desliza hacia la izquierda o derecha para hacer a tu compañero más pequeño o más grande en tiempo real.");
            this.Controls.Add(trkScale);

            y += 58;
            Button btnPreviewIntro = new Button
            {
                Text = "Probar Animación de '¡Feliz Navidad!'",
                Location = new Point(30, y),
                Size = new Size(460, 40),
                BackColor = Color.FromArgb(35, 115, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnPreviewIntro.Click += (s, e) =>
            {
                ChristmasIntroOverlay intro = new ChristmasIntroOverlay(null);
                intro.Show();
                petForm.TriggerCelebration();
            };
            this.Controls.Add(btnPreviewIntro);

            y += 54;
            chkStartup = new CheckBox
            {
                Text = "Iniciar automáticamente con Windows",
                Location = new Point(30, y),
                AutoSize = true,
                Checked = StartupManager.IsStartupEnabled()
            };
            chkStartup.CheckedChanged += (s, e) =>
            {
                StartupManager.SetStartup(chkStartup.Checked);
                UpdateStatus(chkStartup.Checked
                    ? "Inicio automático con Windows ACTIVADO."
                    : "Inicio automático con Windows DESACTIVADO.");
            };
            AttachHelp(chkStartup, "Abre tu compañero y el saludo automáticamente cada vez que inicias sesión en tu PC.");
            this.Controls.Add(chkStartup);

            y += 32;
            chkIntro = new CheckBox
            {
                Text = "Mostrar cartel '¡Feliz Navidad!' y nieve al arrancar",
                Location = new Point(30, y),
                AutoSize = true,
                Checked = config.ShowIntroOnStart
            };
            chkIntro.CheckedChanged += (s, e) =>
            {
                config.ShowIntroOnStart = chkIntro.Checked;
                config.Save();
                UpdateStatus(chkIntro.Checked
                    ? "Se mostrará la animación de bienvenida al iniciar."
                    : "Solo aparecerá la mascota al iniciar, sin el cartel grande.");
            };
            AttachHelp(chkIntro, "Muestra durante 8 segundos la animación de nieve, luces y el conteo de Navidad al abrir el programa.");
            this.Controls.Add(chkIntro);

            y += 32;
            chkGhost = new CheckBox
            {
                Text = "Modo Fantasma",
                Location = new Point(30, y),
                AutoSize = true,
                Checked = config.GhostMode
            };
            chkGhost.CheckedChanged += (s, e) =>
            {
                petForm.ApplyGhostMode(chkGhost.Checked);
                UpdateStatus(chkGhost.Checked
                    ? "Modo Fantasma activado: los clics atraviesan a la mascota."
                    : "Modo Fantasma desactivado: puedes arrastrar y hacer clic a la mascota.");
            };
            AttachHelp(chkGhost, "Hace a la mascota semitransparente e intangible para que tus clics la atraviesen y no te estorbe al trabajar.");
            this.Controls.Add(chkGhost);

            y += 32;
            chkStayInPlace = new CheckBox
            {
                Text = "Quedarse fijo en su sitio",
                Location = new Point(30, y),
                AutoSize = true,
                Checked = config.StayInPlace
            };
            chkStayInPlace.CheckedChanged += (s, e) =>
            {
                petForm.SetStayInPlace(chkStayInPlace.Checked);
                UpdateStatus(chkStayInPlace.Checked
                    ? "Posición fija activada: se quedará donde lo coloques."
                    : "Posición fija desactivada: volverá a caminar libremente.");
            };
            AttachHelp(chkStayInPlace, "La mascota no caminará por su cuenta; se quedará en el lugar donde la dejes haciendo sus animaciones, saltos y trucos.");
            this.Controls.Add(chkStayInPlace);

            y += 32;
            chkDisableAutoMessages = new CheckBox
            {
                Text = "Desactivar mensajes automáticos",
                Location = new Point(30, y),
                AutoSize = true,
                Checked = config.DisableAutoMessages
            };
            chkDisableAutoMessages.CheckedChanged += (s, e) =>
            {
                config.DisableAutoMessages = chkDisableAutoMessages.Checked;
                config.Save();
                UpdateStatus(chkDisableAutoMessages.Checked
                    ? "Mensajes automáticos desactivados (solo hablará al darle clic)."
                    : "Mensajes automáticos activados.");
            };
            AttachHelp(chkDisableAutoMessages, "Evita que la mascota saque globos de texto por su cuenta mientras descansa (solo hablará cuando le hagas clic o la arrastres).");
            this.Controls.Add(chkDisableAutoMessages);

            y += 44;
            Button btnDisable = new Button
            {
                Text = "Desactivar y Cerrar",
                Location = new Point(30, y),
                Size = new Size(222, 42),
                BackColor = Color.FromArgb(165, 85, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnDisable.Click += (s, e) =>
            {
                StartupManager.SetStartup(false);
                MessageBox.Show("Se ha desactivado el inicio automático con Windows y se cerrará el compañero.\n\nPuedes volver a abrirlo cuando quieras desde su carpeta en el Escritorio.", "Desactivado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            };
            AttachHelp(btnDisable, "Quita el inicio automático con Windows y cierra la mascota inmediatamente.");
            this.Controls.Add(btnDisable);

            Button btnClosePanel = new Button
            {
                Text = "Ocultar ventana",
                Location = new Point(268, y),
                Size = new Size(222, 42),
                BackColor = Color.FromArgb(65, 75, 72),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnClosePanel.Click += (s, e) => this.Hide();
            AttachHelp(btnClosePanel, "Cierra esta ventana de configuración dejando a tu mascota activa en el escritorio.");
            this.Controls.Add(btnClosePanel);

            y += 54;
            lblStatus = new Label
            {
                Text = "Pasa el ratón sobre cualquier opción para ver qué hace.",
                Location = new Point(30, y),
                Size = new Size(460, 52),
                ForeColor = Color.FromArgb(185, 235, 200),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic)
            };
            this.Controls.Add(lblStatus);
        }

        private void AttachHelp(Control ctrl, string description)
        {
            if (toolTip != null)
            {
                toolTip.SetToolTip(ctrl, description);
            }
            ctrl.MouseEnter += (s, e) => UpdateStatus(description);
        }

        private void UpdateStatus(string msg)
        {
            if (lblStatus != null) lblStatus.Text = msg;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }

    // Contexto principal de la aplicación
    public class ChristmasAppContext : ApplicationContext
    {
        private AppConfig config;
        private NotifyIcon trayIcon;
        private DesktopPetForm petForm;
        private ControlPanelForm controlPanel;
        private System.Threading.EventWaitHandle showPanelEvent;
        private Timer signalPollTimer;

        public ChristmasAppContext(bool openPanelDirectly)
        {
            config = AppConfig.Load();

            // Siempre sincronizar el inicio automático si está activo (o si es el primer arranque)
            string cfgFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "config.ini");
            if (!File.Exists(cfgFile))
            {
                StartupManager.SetStartup(true);
                config.Save();
            }
            else if (StartupManager.IsStartupEnabled())
            {
                StartupManager.SetStartup(true);
            }

            petForm = new DesktopPetForm(config);
            petForm.OnOpenControlPanelRequested += ShowControlPanel;

            try
            {
                showPanelEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "NavidadCompaneroShowPanelEvent");
                signalPollTimer = new Timer { Interval = 400 };
                signalPollTimer.Tick += (s, e) =>
                {
                    if (showPanelEvent != null && showPanelEvent.WaitOne(0))
                    {
                        ShowControlPanel();
                    }
                };
                signalPollTimer.Start();
            }
            catch { }

            BuildTrayIcon();

            if (config.ShowIntroOnStart && !openPanelDirectly)
            {
                ChristmasIntroOverlay intro = new ChristmasIntroOverlay(() =>
                {
                    if (!petForm.IsDisposed) petForm.Show();
                });
                intro.Show();
                petForm.Show();
            }
            else
            {
                petForm.Show();
            }

            if (openPanelDirectly)
            {
                ShowControlPanel();
            }

            // Compactar memoria periódicamente cada 45 segundos para mantener un consumo mínimo en el Administrador de tareas
            Timer memTimer = new Timer { Interval = 45000 };
            memTimer.Tick += (s, e) => Win32.CompactMemory();
            memTimer.Start();
            Win32.CompactMemory();
        }

        private void BuildTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Compañero Navideño (Clic para configurar)";
            trayIcon.Icon = CreateChristmasTreeIcon();
            trayIcon.Visible = true;

            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem openPanelItem = new ToolStripMenuItem("Configuración / Cambiar Nombre");
            openPanelItem.Font = new Font(menu.Font, FontStyle.Bold);
            openPanelItem.Click += (s, e) => ShowControlPanel();
            menu.Items.Add(openPanelItem);

            ToolStripMenuItem introItem = new ToolStripMenuItem("Mostrar Saludo '¡Feliz Navidad!'");
            introItem.Click += (s, e) =>
            {
                ChristmasIntroOverlay intro = new ChristmasIntroOverlay(null);
                intro.Show();
            };
            menu.Items.Add(introItem);

            menu.Items.Add(new ToolStripSeparator());

            string[] pets = { "Duende", "Reno", "Pinguino", "Gato", "Perro" };
            foreach (string p in pets)
            {
                string petCopy = p;
                ToolStripMenuItem item = new ToolStripMenuItem("Mascota: " + DesktopPetForm.GetSpeciesTitle(p));
                item.Click += (s, e) => petForm.ChangePet(petCopy);
                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem ghostItem = new ToolStripMenuItem("Alternar Modo Fantasma");
            ghostItem.Click += (s, e) => petForm.ApplyGhostMode(!config.GhostMode);
            menu.Items.Add(ghostItem);

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Salir");
            exitItem.Click += (s, e) => ExitApp();
            menu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => ShowControlPanel();
        }

        private void ShowControlPanel()
        {
            if (controlPanel == null || controlPanel.IsDisposed)
            {
                controlPanel = new ControlPanelForm(config, petForm);
            }
            controlPanel.Show();
            controlPanel.BringToFront();
            controlPanel.Activate();
        }

        private Icon CreateChristmasTreeIcon()
        {
            try
            {
                string icoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "assets", "icon.ico");
                if (File.Exists(icoFile)) return new Icon(icoFile);
                Icon assoc = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (assoc != null) return assoc;
            }
            catch { }

            using (Bitmap bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (SolidBrush green = new SolidBrush(Color.FromArgb(30, 170, 65)))
                using (SolidBrush trunk = new SolidBrush(Color.FromArgb(120, 75, 35)))
                using (SolidBrush gold = new SolidBrush(Color.Gold))
                using (SolidBrush red = new SolidBrush(Color.Red))
                {
                    g.FillRectangle(trunk, 13, 24, 6, 7);
                    Point[] p1 = { new Point(16, 4), new Point(4, 24), new Point(28, 24) };
                    g.FillPolygon(green, p1);
                    g.FillEllipse(gold, 13, 1, 6, 6);
                    g.FillEllipse(red, 11, 13, 4, 4);
                    g.FillEllipse(gold, 17, 17, 4, 4);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void ExitApp()
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try { Win32.SetProcessDPIAware(); } catch { }
            try { Win32.timeBeginPeriod(1); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool openConfig = false;
            bool uninstall = false;

            if (args != null)
            {
                foreach (string a in args)
                {
                    if (a.Equals("--config", StringComparison.OrdinalIgnoreCase)) openConfig = true;
                    if (a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)) uninstall = true;
                }
            }

            if (uninstall)
            {
                StartupManager.SetStartup(false);
                MessageBox.Show(
                    "Se ha desactivado y eliminado el inicio automático con Windows.\n\nSi deseas borrar los archivos por completo, ya puedes eliminar la carpeta 'CompaneroNavidad' de tu Escritorio.",
                    "Desinstalación / Desactivación Completa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            bool createdNew;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "NavidadCompaneroVirtualMutex", out createdNew))
            {
                if (!createdNew)
                {
                    try
                    {
                        using (System.Threading.EventWaitHandle evt = System.Threading.EventWaitHandle.OpenExisting("NavidadCompaneroShowPanelEvent"))
                        {
                            evt.Set();
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Tu compañero navideño ya está activo en el escritorio.\n\nPuedes darle clic derecho directamente a la mascota o usar el icono del arbolito junto al reloj de Windows.", "Compañero Navideño", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                Application.Run(new ChristmasAppContext(openConfig));
            }
        }
    }
}
