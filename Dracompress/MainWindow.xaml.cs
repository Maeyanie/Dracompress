using Openize.Drako;
using System.IO;
using System.Numerics;
using System.Printing.IndexedProperties;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Dracompress
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // try 20 and 19 for compatibility
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += Window_SourceInitialized;
            Closing += Window_Closing;

            DrcBits.Text = Properties.Settings.Default.DrcBits.ToString();

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                string file = args[i];
                if (!File.Exists(file)) continue;

                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext != ".stl" && ext != ".drc") continue;

                FileList.Items.Add(file);
            }
        }

        private void Window_SourceInitialized(object? sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int useDark = 1; // 1 = dark, 0 = light
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (int.TryParse(DrcBits.Text, out int value))
            {
                Properties.Settings.Default["DrcBits"] = value;
                Properties.Settings.Default.Save();
            }
        }

        private async void ToSTL_Click(object sender, RoutedEventArgs e)
        {
            WindowGrid.IsEnabled = false;
            var files = FileList.Items.Cast<string>().ToList();
            var progress = new Progress<double>(value => ProgressBar.Value = value);

            await Task.Run(() => ToSTLTask(files, progress));

            WindowGrid.IsEnabled = true;
            FileList.Items.Clear();
        }

        private static async Task ToSTLTask(List<string> fileList, IProgress<double> progress)
        {
            for (int f = 0; f < fileList.Count; f++)
            {
                progress.Report((double)(f + 1) / fileList.Count * 100.0);
                string file = fileList[f];
                if (!File.Exists(file)) continue;
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext != ".drc") continue;

                byte[] bytes = File.ReadAllBytes(file);
                DracoMesh drc = (DracoMesh)Draco.Decode(bytes);
                if (drc == null) continue;

                PointAttribute posAttr = drc.GetNamedAttribute(AttributeType.Position);
                byte[] pointBytes = posAttr.Buffer.ToArray();
                float[] points = new float[pointBytes.Length / sizeof(float)];
                Buffer.BlockCopy(pointBytes, 0, points, 0, pointBytes.Length);

                STL stl = new STL();
                int faceCount = drc.NumFaces;
                int[] fvi = new int[3];
                for (int i = 0; i < faceCount; i++)
                {
                    // I'm going to assume this will always give triangles. I'm not sure that's a safe assumption.
                    drc.ReadFace(i, fvi);

                    int ai = posAttr.MappedIndex(fvi[0]);
                    float[] a = { points[ai * 3 + 0], points[ai * 3 + 1], points[ai * 3 + 2] };

                    int bi = posAttr.MappedIndex(fvi[1]);
                    float[] b = { points[bi * 3 + 0], points[bi * 3 + 1], points[bi * 3 + 2] };

                    int ci = posAttr.MappedIndex(fvi[2]);
                    float[] c = { points[ci * 3 + 0], points[ci * 3 + 1], points[ci * 3 + 2] };

                    stl.triangles.Add(new STL.Triangle(a, b, c));
                }

                string newFile = System.IO.Path.ChangeExtension(file, ".stl");
                stl.Write(newFile);
            }
        }

        private async void ToDRC_Click(object sender, RoutedEventArgs e)
        {
            WindowGrid.IsEnabled = false;
            var files = FileList.Items.Cast<string>().ToList();
            var progress = new Progress<double>(value => ProgressBar.Value = value);

            await Task.Run(() => ToDRCTask(files, progress));

            WindowGrid.IsEnabled = true;
            FileList.Items.Clear();
        }

        private static async Task ToDRCTask(List<string> fileList, IProgress<double> progress) {
            for (int f = 0; f < fileList.Count; f++)
            {
                progress.Report((double)(f+1) / fileList.Count * 100.0);
                string file = fileList[f];
                if (!File.Exists(file)) continue;
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext != ".stl") continue;

                STL stl = new();
                stl.Read(file);

                Vector3[] points = new Vector3[stl.triangles.Count * 3];
                int[] indices = new int[stl.triangles.Count * 3];
                int i = 0;
                foreach (var t in stl.triangles)
                {
                    points[i] = new Vector3(t.A[0], t.A[1], t.A[2]);
                    indices[i] = i++;
                    points[i] = new Vector3(t.B[0], t.B[1], t.B[2]);
                    indices[i] = i++;
                    points[i] = new Vector3(t.C[0], t.C[1], t.C[2]);
                    indices[i] = i++;
                }

                DracoMesh drc = new();
                drc.AddAttribute(PointAttribute.Wrap(AttributeType.Position, points));
                drc.NumPoints = points.Length;
                drc.Indices.AddRange(indices);
                drc.NumFaces = indices.Length / 3;

                drc.DeduplicateAttributeValues();
                drc.DeduplicatePointIds();

                DracoEncodeOptions opt = new()
                {
                    CompressionLevel = DracoCompressionLevel.Optimal,
                    PositionBits = 16
                };
                byte[] buffer = Draco.Encode(drc, opt);
                File.WriteAllBytes(System.IO.Path.ChangeExtension(file, ".drc"), buffer);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            FileList.Items.Clear();
        }

        private void FileList_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Any(file => { string ext = System.IO.Path.GetExtension(file).ToLower(); return ext == ".stl" || ext == ".drc"; })) {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void FileList_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                if (!File.Exists(file)) continue;

                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext != ".stl" && ext != ".drc") continue;

                FileList.Items.Add(file);
            }
        }

        private void DrcBits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void DrcBits_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DrcBits.Text, out int value))
            {
                if (value < 0) DrcBits.Text = "0";
                else if (value > 30) DrcBits.Text = "30";
            }
            else
            {
                DrcBits.Text = "0";
            }
        }
    }
}


