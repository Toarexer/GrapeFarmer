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

    class ImageForm : Form {
        public ImageForm(Bitmap bitmap) {
            ClientSize = bitmap.Size;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            BackgroundImage = bitmap;

            int attribute = 1;
            DwmSetWindowAttribute(Handle, 20, ref attribute, Marshal.SizeOf<int>());
        }

        public ImageForm(Bitmap bitmap, Point topLeft, Point bottomRight) : this(bitmap) {
            Text = $"({topLeft.X} : {topLeft.Y}) - ({bottomRight.X} : {bottomRight.Y})";
        }
    }

    class SelectorForm : Form {
        public SelectorForm() {
            ClientSize = new(320, 321);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            ResizeRedraw = true;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "(0) Use this to select the correct area!";
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
                            HashSet<Point> whitePixels = new();
                            while (run) {
                                using (Bitmap bitmap = Invoke<Bitmap>(() => {
                                    Text = $"({Counter++}) Use this to select the correct area!";
                                    return TakeScreenshot();
                                })) {
                                    FilterBitmap(bitmap);
                                    foreach (Point p in CollectPixels(bitmap, 0x00ffffff))
                                        whitePixels.Add(p);
                                    MatchFound = CollectPixels(bitmap, 0x00ff0000).Intersect(whitePixels).Any();
                                }
                            }
                        });
                    }
                    await Task.Delay(10);
                }
            });

            Task.Run(async () => {
                while (true) {
                    if (GetAsyncKeyState(0x77) == 0x8000) { // F8
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
            FilterBitmap(bm);
            return bm;
        }
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    extern static int GetAsyncKeyState(int keycode);

    static void FilterBitmap(Bitmap bitmap) {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++) {
                int rgb = bitmap.GetPixel(x, y).ToArgb() & 0x00ffffff;
                if (rgb != 0x00ff0000 && rgb != 0x00ffffff)
                    bitmap.SetPixel(x, y, Color.Black);
            }
    }

    static IEnumerable<Point> CollectPixels(Bitmap bitmap, int rgb) {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if ((bitmap.GetPixel(x, y).ToArgb() & 0x00ffffff) == rgb)
                    yield return new(x, y);
    }

    [STAThread]
    static void Main() {
        ApplicationConfiguration.Initialize();
        Application.Run(new SelectorForm());
    }
}