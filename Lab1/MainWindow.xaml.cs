using System.Windows;
using System.Windows.Media.Imaging;
using Lab1.model;
using Lab1.parser;
using Microsoft.Win32;

namespace Lab1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObjModel? ObjModel { get; set; }
    private WriteableBitmap? Wb { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoadFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog()
        {
            Filter = "OBJ Files (*.obj)|*.obj"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                ObjModel = ObjModelFileReader.ReadModel(dlg.FileName);
                //
                // int width = (int)(ImagePanel.ActualWidth > 0 ? ImagePanel.ActualWidth : 800);
                // int height = (int)(ImagePanel.ActualHeight > 0 ? ImagePanel.ActualHeight : 600);
                // ObjModel.WindowSize = new(width, height);
                //
                // Wb = new WriteableBitmap(ObjModel.WindowSize.Width, ObjModel.WindowSize.Height, 96, 96, PixelFormats.Bgra32, null);
                // ImgDisplay.Source = Wb;
                //
                // ObjModel.TransformationChanged += ObjModel_TransformationChanged;
                //
                // ObjModel.Scale = ObjModel.Delta * 10.0f; // вызовет UpdateImage -> RedrawModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки файла: " + ex.Message);
            }
        }
    }

    private void FileClear_OnClick(object sender, RoutedEventArgs e)
    {
        if (Wb != null)
        {
            //  WireframeRenderer.ClearBitmap(Wb, BackgroundSelectedColor);
            ObjModel = null;
        }
    }
}