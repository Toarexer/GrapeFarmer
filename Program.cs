using System;
using System.Drawing;
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
            TransparencyKey = Color.Blue;

            int attribute = 1;
            DwmSetWindowAttribute(Handle, 20, ref attribute, Marshal.SizeOf<int>());

            Shown += (object? sender, EventArgs e) => RunTasks();
        }

        protected virtual void RunTasks() {
            Task.Run(async () => {
                while (true) {
                    if (GetAsyncKeyState(0x20) == 0x8000) {
                        Invoke(() => {
                            Bitmap bitmap = TakeScreenshot();
                            Point topLeft = PointToScreen(new(1, 2));
                            new ImageForm(bitmap, topLeft, new Point(topLeft.X + bitmap.Width, topLeft.Y + bitmap.Height)).ShowDialog();
                        });
                        await Task.Delay(200);
                    }
                    if (GetAsyncKeyState(0x77) == 0x8000) {
                        Invoke(() => Visible = !Visible);
                        await Task.Delay(100);
                    }
                    await Task.Delay(10);
                }
            });

            Task.Run(async () => {
                while (true) {
                    Invoke(() => Text = $"({Counter++}) Use this to select the correct area!");
                    await Task.Delay(100);
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

    class FilterForm : SelectorForm {
        public class ImageBoxForm : Form {
            Bitmap FilteredBitmap;

            public ImageBoxForm() {
                DoubleBuffered = true;
                FilteredBitmap = new(ClientSize.Width, ClientSize.Height);
                FormBorderStyle = FormBorderStyle.None;
                Icon = SystemIcons.Information;
                TopMost = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }

            public void Update(Bitmap bitmap) {
                ClientSize = bitmap.Size;
                FilteredBitmap = bitmap;
                Refresh();
            }

            protected override void OnPaintBackground(PaintEventArgs e) {
                e.Graphics.FillRectangle(Brushes.Blue, ClientRectangle);
                e.Graphics.DrawImage(FilteredBitmap, Point.Empty);
            }
        }

        ImageBoxForm ImageBox = new();

        public FilterForm() {
            TopMost = true;
            Move += (object? sender, EventArgs e) => {
                Text = $"({DesktopLocation.X} : {DesktopLocation.Y}) - ({DesktopLocation.X + Width} : {DesktopLocation.Y + Height})";
                if (!ImageBox.IsDisposed)
                    ImageBox.SetDesktopLocation(DesktopLocation.X + Width + 8, DesktopLocation.Y);
            };
            Shown += (object? sender, EventArgs e) => OnMove(new());
            ImageBox.Show();
        }

        protected override void RunTasks() {
            Task.Run(async () => {
                while (true) {
                    Invoke(() => {
                        if (!ImageBox.IsDisposed) {
                            Bitmap bitmap = TakeScreenshot();
                            Point topLeft = PointToScreen(new(1, 2));
                            ImageBox.Update(bitmap);
                        }
                    });
                    await Task.Delay(1);
                }
            });
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

    static bool IsRedOnWhite(Bitmap bitmap) {
        return false;
    }

    static bool FindInBitmap(Bitmap bitmap) {
        return false;
    }

    [STAThread]
    static void Main() {
        ApplicationConfiguration.Initialize();
        Application.Run(new FilterForm());
    }
}