using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MouseHzTest
{
    public class MouseHzForm : Form
    {
        private Label lblInstant, lblAvg, lblPeak, lblSamples;
        private Button btnClear, btnLogToggle;
        private bool logging = false;
        private StringBuilder csv = new StringBuilder("timestamp,dt_seconds,hz\n");

        private long lastTick = 0;
        private readonly double tickToSeconds = 1.0 / Stopwatch.Frequency;
        private readonly Queue<double> hzWindow = new Queue<double>();
        private const int WINDOW_SIZE = 500;
        private double peakHz = 0;
        private long totalSamples = 0;

        public MouseHzForm()
        {
            Text = "Mouse Poll Rate Tester (Raw Input)";
            Width = 560; Height = 240;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch {}

            lblInstant = new Label { Left = 20, Top = 20, Width = 520, Text = "Instant Hz: -" };
            lblAvg     = new Label { Left = 20, Top = 50, Width = 520, Text = "Average Hz (last 500): -" };
            lblPeak    = new Label { Left = 20, Top = 80, Width = 520, Text = "Peak Hz (session): -" };
            lblSamples = new Label { Left = 20, Top = 110, Width = 520, Text = "Samples: 0" };
            btnClear   = new Button{ Left = 20, Top = 150, Width = 140, Text = "Clear Stats" };
            btnLogToggle = new Button{ Left = 180, Top = 150, Width = 180, Text = "Start CSV Logging" };

            Controls.AddRange(new Control[]{ lblInstant, lblAvg, lblPeak, lblSamples, btnClear, btnLogToggle });

            btnClear.Click += (s,e)=> {
                hzWindow.Clear(); peakHz = 0; totalSamples = 0; lastTick = 0;
                lblInstant.Text="Instant Hz: -";
                lblAvg.Text="Average Hz (last 500): -";
                lblPeak.Text="Peak Hz (session): -";
                lblSamples.Text="Samples: 0";
            };

            btnLogToggle.Click += (s,e)=> {
                logging = !logging;
                btnLogToggle.Text = logging ? "Stop & Save CSV" : "Start CSV Logging";
                if(!logging)
                {
                    var path = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "mouse_poll_log.csv");
                    System.IO.File.WriteAllText(path, csv.ToString());
                    MessageBox.Show($"Kaydedildi:\n{path}");
                    csv.Clear(); csv.AppendLine("timestamp,dt_seconds,hz");
                }
            };
        }

        private const int RIM_TYPEMOUSE = 0;
        private const int WM_INPUT = 0x00FF;
        private const int RID_INPUT = 0x10000003;
        private const int RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public int dwFlags; public IntPtr hwndTarget; }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER { public int dwType; public int dwSize; public IntPtr hDevice; public IntPtr wParam; }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWMOUSE
        {
            public ushort usFlags; public uint ulButtons; public ushort usButtonFlags;
            public ushort usButtonData; public uint ulRawButtons;
            public int lLastX; public int lLastY; public uint ulExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUT { public RAWINPUTHEADER header; public RAWMOUSE mouse; }

        [DllImport("User32.dll", SetLastError = true)]
        static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData,
            ref uint pcbSize, uint cbSizeHeader);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var rid = new RAWINPUTDEVICE[]
            {
                new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = this.Handle }
            };
            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
                MessageBox.Show("RegisterRawInputDevices failed. Hata: " + Marshal.GetLastWin32Error());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                uint dwSize = 0;
                GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>());
                if (dwSize > 0)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                    try
                    {
                        if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) == dwSize)
                        {
                            var raw = Marshal.PtrToStructure<RAWINPUT>(buffer);
                            if (raw.header.dwType == RIM_TYPEMOUSE)
                                OnMouseRawEvent();
                        }
                    }
                    finally { Marshal.FreeHGlobal(buffer); }
                }
            }
            base.WndProc(ref m);
        }

        private void OnMouseRawEvent()
        {
            long now = Stopwatch.GetTimestamp();
            if (lastTick != 0)
            {
                double dt = (now - lastTick) * tickToSeconds;
                if (dt > 0)
                {
                    double hz = 1.0 / dt;
                    hzWindow.Enqueue(hz);
                    if (hzWindow.Count > WINDOW_SIZE) hzWindow.Dequeue();

                    double avg = 0;
                    foreach (var v in hzWindow) avg += v;
                    if (hzWindow.Count > 0) avg /= hzWindow.Count;

                    if (hz > peakHz) peakHz = hz;

                    lblInstant.Text = $"Instant Hz: {hz:F0}";
                    lblAvg.Text     = $"Average Hz (last {hzWindow.Count}): {avg:F0}";
                    lblPeak.Text    = $"Peak Hz (session): {peakHz:F0}";
                    lblSamples.Text = $"Samples: {++totalSamples}";

                    if (logging)
                        csv.AppendLine($"{DateTime.Now:O},{dt:F9},{hz:F2}");
                }
            }
            lastTick = now;
        }
    }

    public static class MouseHz
    {
        public static void Run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MouseHzForm());
        }
    }
}
