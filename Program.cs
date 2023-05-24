using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrapeFarmer;

static class Program {
    static bool MatchFound = false;
    static ulong Counter = 0;

    class SelectorForm : Form {
        public SelectorForm() {
            ClientSize = new(320, 321);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ResizeRedraw = true;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "(0) Use this to select the correct area!";
            TopMost = true;
            TransparencyKey = Color.Blue;

            int attribute = 1;
            DwmSetWindowAttribute(Handle, 20, ref attribute, Marshal.SizeOf<int>());

            Shown += (object? sender, EventArgs e) => RunTasks();
        }

        protected virtual void RunTasks() {
            Task.Run(async () => {
                while (true) {
                    if (GetAsyncKeyState(0x45) == 0x8000) { // E
                        bool run = true;
                        _ = Task.Run(async () => {
                            await Task.Delay(5000);
                            run = false;
                        });
                        await Task.Run(() => {
                            Point[] whitePixels = { };
                            bool hasred = false;
                            while (run) {
                                using (Bitmap bitmap = Invoke<Bitmap>(() => {
                                    Text = $"({Counter++}) Use this to select the correct area!";
                                    return TakeScreenshot();
                                })) {

                                    if (!hasred && bitmap.ContainsColor(0x00ff0000)) {
                                        whitePixels = bitmap.CollectPixels(0x00ffffff).ToArray();
                                        hasred = true;
                                    }

                                    if (hasred) {

                                        MatchFound = CollectPixels(bitmap, 0x00ff0000).Intersect(whitePixels).Any();
                                        Invoke(() => Refresh());
                                    }
                                }
                            }
                        });
                    }
                    await Task.Delay(10);
                }
            });

            Task.Run(async () => {
                while (true) {
                    if (GetAsyncKeyState(0x78) == 0x8000) { // F9
                        Invoke(() => Visible = !Visible);
                        await Task.Delay(100);
                    }
                    await Task.Delay(10);
                }
            });
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            e.Graphics.FillRectangle(Brushes.Blue, ClientRectangle);
            e.Graphics.DrawRectangle(MatchFound ? Pens.Green : Pens.Red, 0, 1, ClientSize.Width - 1, ClientSize.Height - 2);
        }

        protected Bitmap TakeScreenshot() {
            Bitmap bm = new Bitmap(ClientSize.Width - 2, ClientSize.Height - 3);
            using (Graphics g = Graphics.FromImage(bm))
                g.CopyFromScreen(
                    PointToScreen(new(1, 2)),
                    Point.Empty,
                    bm.Size,
                    CopyPixelOperation.SourceCopy); // CopyPixelOperation.CaptureBlt
            return bm;
        }
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    extern static int GetAsyncKeyState(int keycode);

    static IEnumerable<Point> CollectPixels(this Bitmap bitmap, int rgb) {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if ((bitmap.GetPixel(x, y).ToArgb() & 0x00ffffff) == rgb)
                    yield return new(x, y);
    }

    static bool ContainsColor(this Bitmap bitmap, int rgb) {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if ((bitmap.GetPixel(x, y).ToArgb() & 0x00ffffff) == rgb)
                    return true;
        return false;
    }

    [STAThread]
    static void Main() {
        ApplicationConfiguration.Initialize();
        Application.Run(new SelectorForm());
    }
}