using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ZXing;

namespace qr
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Update("11ed5b705e72e9fa2e57");
        }

        private async void Update(string key)
        {
            var list = new List<Grid>();
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_R.png")));
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_L.png")));
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_U.png")));
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_D.png")));
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_F.png")));
            list.AddRange(await Slice(new Uri($"http://qubicrube.pwn.seccon.jp:33654/images/{key}_B.png")));
            xxx.ItemsSource = list.GroupBy(grid => DetectColor((CroppedBitmap)((Image)grid.Children[0]).Source));
        }

        private string DetectColor(CroppedBitmap bitmap)
        {
            var bytes = new Byte[82 * 82 * 4];
            bitmap.CopyPixels(new Int32Rect(0, 0, 82, 82), bytes, 328, 0);
            for (var i = 0; i < 82 * 82; ++i)
            {
                if (bytes[i * 4] != 0
                    || bytes[i * 4 + 1] != 0
                    || bytes[i * 4 + 2] != 0)
                {
                    return $"{bytes[i * 4]},{bytes[i * 4 + 1]},{bytes[i * 4 + 2]}";
                }
            }
            return null;
        }

        private async Task<List<Grid>> Slice(Uri uri)
        {
            var bitmap = new BitmapImage();
            var client = new HttpClient();
            bitmap.BeginInit();
            bitmap.StreamSource = await client.GetStreamAsync(uri);
            bitmap.EndInit();
            await bitmap.WaitForDownloadAsync();
            var list = new List<Grid>();
            for (var i = 0; i < 3; ++i)
            {
                for (var ii = 0; ii < 3; ++ii)
                {
                    list.Add(MapGrid(new CroppedBitmap(bitmap, new Int32Rect(i * 82, ii * 82, 82, 82)), i, ii));
                }
            }
            return list;
        }

        private Grid MapGrid(CroppedBitmap bitmap, int x, int y)
        {
            var grid = new Grid { RenderTransform = new RotateTransform(0, 123, 123) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82) });
            var img = new Image { Source = bitmap };
            grid.Children.Add(img);
            Grid.SetColumn(img, x);
            Grid.SetRow(img, y);
            grid.Width = 82 * 3;
            grid.Height = 82 * 3;
            return grid;
        }
        
        private string ReadCode(UIElement element)
        {
            var target = new RenderTargetBitmap(246, 246, 96, 96, PixelFormats.Pbgra32);
            target.Render(element);
            // --
            var reader = new BarcodeReader();
            var result = reader.Decode(target.AsBitmap());
            return result?.Text;
        }


        private async void RotateClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var index = int.Parse((string)button.Tag);
            var ie = ((IEnumerable<Grid>)button.DataContext).ToArray();
            ((RotateTransform)(ie[index].RenderTransform)).Angle += 90;
            for (var i = 0; i < 9; ++i)
            {
                ie[i].Opacity = 1;
            }
            await Task.Delay(16);
            var a = ReadCode((UIElement)VisualTreeHelper.GetParent(ie[index]));
            for (var i = 0; i < 9; ++i)
            {
                if (i == index) continue;
                ie[i].Opacity = 0.2;
            }
            if (a != null)
            {
                var grid = (Grid)VisualTreeHelper.GetParent(VisualTreeHelper.GetParent((UIElement)sender));
                var ellipse = (Ellipse)grid.Children[2];
                ellipse.Visibility = Visibility.Visible;
                Debug.WriteLine(a);
                if (a.StartsWith("http://"))
                {
                    Update(key.Text = a.Substring(a.LastIndexOf("/") + 1));
                }
                else
                {
                    ellipse.Fill = Brushes.IndianRed;
                }
            }
        }

        private void RotateMouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var index = int.Parse((string)button.Tag);
                var ie = ((IEnumerable<Grid>)button.DataContext).ToArray();
                for (var i = 0; i < 9; ++i)
                {
                    if (i == index) continue;
                    ie[i].Opacity = 0.2;
                }
                Panel.SetZIndex(ie[index], 9);
            }
            catch
            {
            }
        }

        private void RotateMouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var index = int.Parse((string)button.Tag);
                var ie = ((IEnumerable<Grid>)button.DataContext).ToArray();
                for (var i = 0; i < 9; ++i)
                {
                    ie[i].Opacity = 1;
                }
                Panel.SetZIndex(ie[index], 0);
            }
            catch 
            {
            }
        }

        private void Key_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Update(key.Text);
            }
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var index = int.Parse((string)button.Tag);
            var ie = ((IEnumerable<Grid>)button.DataContext).ToArray();
            var x = Grid.GetColumn(ie[index].Children[0]);
            var y = Grid.GetRow(ie[index].Children[0]);
            if (x == 0 || x == 2 || y == 0 || y == 2)
            {
                button.BorderBrush = Brushes.MediumVioletRed;
            }
            if (x == 1 || y == 1)
            {
                button.BorderBrush = Brushes.DodgerBlue;
            }
            if (x == 1 && y == 1)
            {
                button.BorderBrush = Brushes.ForestGreen;
            }
            button.BorderThickness = new Thickness(3);
        }
    }

    public static class Extensions
    {
        public static async Task WaitForDownloadAsync(this BitmapImage bitmap)
        {
            while (true)
            {
                await Task.Delay(100);
                if (!bitmap.IsDownloading) return;
            }
        }

        public static System.Drawing.Bitmap AsBitmap(this BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
