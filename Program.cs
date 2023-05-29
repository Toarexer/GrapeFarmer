using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinUser;

namespace GrapeFarmer;

static class Program {
    static Input.VirtualKey EnableDisableKey = Input.VirtualKey.VK_F9;
    static Input.VirtualKey ShowHideKey = Input.VirtualKey.VK_F12;
    static bool RunCheck = false;
    static int Counter = 0;

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

            NotifyIcon notifyIcon = new() { ContextMenuStrip = new(), Icon = Icon, Visible = true };
            notifyIcon.ContextMenuStrip.Items.Add(
                "Set Keybinds",
                GetIcon(Environment.SystemDirectory + "\\wmploc.dll", 17).ToBitmapOrEmpty(),
                (object? sender, EventArgs e) => SwapToSelectKeysForm());
            notifyIcon.ContextMenuStrip.Items.Add(
                "Exit",
                GetIcon(Environment.SystemDirectory + "\\shell32.dll", 131).ToBitmapOrEmpty(),
                (object? sender, EventArgs e) => Application.Exit());

            int attribute = 1;
            DwmSetWindowAttribute(Handle, 20, ref attribute, sizeof(int));

            Shown += (object? sender, EventArgs e) => RunTasks();
        }

        protected virtual void RunTasks() {
            Task.Run(async () => {
                while (true) {
                    if (RunCheck) {
                        InvokeSetText(0);
                        bool run = true;

                        Input.SetKeyDown(Input.VirtualKey.VK_E);
                        await Task.Delay(10);
                        Input.SetKeyUp(Input.VirtualKey.VK_E);

                        Task timeout = Task.Run(async () => {
                            await Task.Delay(5000);
                            run = false;
                        });

                        await Task.Run(() => {
                            Point[] whitePixels = { };
                            bool hasred = false;

                            while (run && RunCheck) {
                                using (Bitmap bitmap = Invoke<Bitmap>(() => {
                                    InvokeSetText(++Counter);
                                    return TakeScreenshot();
                                })) {

                                    if (!hasred && bitmap.ContainsColor(0x00ff0000)) {
                                        whitePixels = bitmap.CollectPixels(0x00ffffff).ToArray();
                                        hasred = true;
                                    }

                                    if (hasred && bitmap.CollectPixels(0x00ff0000).Intersect(whitePixels).Any()) {
                                        run = false;
                                        Input.PressKey(Input.VirtualKey.VK_SPACE);
                                    }
                                }
                            }
                        });
                        await timeout;
                    }
                    await Task.Delay(10);
                }
            });

            Task.Run(async () => {
                while (true) {
                    if (Input.IsKeyPressed(EnableDisableKey)) {
                        RunCheck = !RunCheck;
                        Invoke(() => Refresh());
                        await Task.Delay(100);
                    } else if (Input.IsKeyPressed(ShowHideKey)) {
                        Invoke(() => Visible = !Visible);
                        await Task.Delay(100);
                    }
                    await Task.Delay(10);
                }
            });
        }

        void InvokeSetText(int counter) => Invoke(() => Text = $"({Counter = counter}) Use this to select the correct area!");

        protected override void OnPaintBackground(PaintEventArgs e) {
            e.Graphics.FillRectangle(Brushes.Blue, ClientRectangle);
            e.Graphics.DrawRectangle(RunCheck ? Pens.LimeGreen : Pens.Red, 0, 1, ClientSize.Width - 1, ClientSize.Height - 2);
        }

        protected Bitmap TakeScreenshot() {
            Bitmap bm = new Bitmap(ClientSize.Width - 2, ClientSize.Height - 3);
            using (Graphics g = Graphics.FromImage(bm))
                g.CopyFromScreen(
                    PointToScreen(new(1, 2)),
                    Point.Empty,
                    bm.Size,
                    CopyPixelOperation.SourceCopy);
            return bm;
        }

        protected void SwapToSelectKeysForm() {
            bool run = RunCheck;
            bool visible = Visible;
            RunCheck = false;
            Visible = false;
            Refresh();
            new SelectKeysForm() { Icon = Icon }.ShowDialog();
            RunCheck = run;
            if (!IsDisposed)
                Visible = visible;
        }
    }

    class SelectKeysForm : Form {
        public SelectKeysForm() {
            ClientSize = new(408, 120);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Keybind Selector";

            AutoCompleteStringCollection ac = new();

            ComboBox cb1 = new() { Location = new(140, 8), Width = 256, AutoCompleteSource = AutoCompleteSource.CustomSource, AutoCompleteCustomSource = ac, DropDownHeight = 300 };
            ComboBox cb2 = new() { Location = new(140, 40), Width = 256, AutoCompleteSource = AutoCompleteSource.CustomSource, AutoCompleteCustomSource = ac, DropDownHeight = 300 };
            cb1.SelectedValueChanged += (object? sender, EventArgs e) => SetIfKeyIsCorrect(cb1);
            cb2.SelectedValueChanged += (object? sender, EventArgs e) => SetIfKeyIsCorrect(cb2);

            foreach (string name in Enum.GetNames<Input.VirtualKey>().Select(x => x.Remove(0, 3))) {
                ac.Add(name);
                cb1.Items.Add(name);
                cb2.Items.Add(name);
            }
            SetSelectedItem(cb1, EnableDisableKey);
            SetSelectedItem(cb2, ShowHideKey);

            Button btn = new() { Location = new(10, 80), Size = new(387, 28), Text = "Set Keys" };
            btn.Click += (object? sender, EventArgs e) => {
                Input.VirtualKey k;
                if (Enum.TryParse<Input.VirtualKey>("VK_" + cb1.Text, true, out k))
                    EnableDisableKey = k;
                if (Enum.TryParse<Input.VirtualKey>("VK_" + cb2.Text, true, out k))
                    ShowHideKey = k;
                Close();
            };

            Controls.Add(new Label() { Location = new(8, 12), Width = 128, Text = "Enable/Disable" });
            Controls.Add(new Label() { Location = new(8, 44), Width = 128, Text = "Show/Hide" });
            Controls.Add(cb2);
            Controls.Add(cb1);
            Controls.Add(cb2);
            Controls.Add(btn);

            int attribute = 1;
            DwmSetWindowAttribute(Handle, 20, ref attribute, sizeof(int));
        }

        void SetSelectedItem(ComboBox cb, Input.VirtualKey key) {
            foreach (string item in cb.Items) {
                if (item == Enum.GetName(key)?.Remove(0, 3)) {
                    cb.SelectedItem = item;
                    return;
                }
            }
        }

        void SetIfKeyIsCorrect(ComboBox cb) {
            Input.VirtualKey k;
            if (!Enum.TryParse<Input.VirtualKey>("VK_" + cb.SelectedItem, true, out k))
                cb.SelectedItem = null;
        }
    }

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("shell32.dll")]
    public static extern int ExtractIconEx(string file, int iconIndex, out IntPtr large, out IntPtr small, int iconsCount);

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

    static Icon? GetIcon(string path, int index) {
        IntPtr small, large;
        ExtractIconEx(path, index, out large, out small, 1);
        return Icon.FromHandle(large) ?? Icon.FromHandle(small);
    }

    static Bitmap ToBitmapOrEmpty(this Icon? icon) => icon is null ? new(0, 0) : icon.ToBitmap();

    [STAThread]
    static void Main() {
        ApplicationConfiguration.Initialize();
        Application.Run(new SelectorForm());
    }
}
