using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.model;
using Lab1.utils;
using Microsoft.Win32;

namespace Lab1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObjModel? _model;
    private Camera _camera;
    private RenderProcessor _processor;
    private WriteableBitmap? _wb;

    private Point _lastMousePos;
    private bool _isMouseDown;

    // Цвета
    private readonly Color _bgColor = Colors.Black;
    private readonly Color _modelColor = Colors.White;

     public MainWindow()
    {
        InitializeComponent();

        _processor = new RenderProcessor();
        _camera = new Camera()
        {
            Eye = new Vector3(0, 0, 5f),
        };

        KeyDown += OnKeyDown;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        Loaded += (s, e) => Focus();

        InitWriteableBitmap();
    }

    private void InitWriteableBitmap()
    {
        int width = (int)(ImagePanel.ActualWidth > 0 ? ImagePanel.ActualWidth : 800);
        int height = (int)(ImagePanel.ActualHeight > 0 ? ImagePanel.ActualHeight : 600);

        _wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        ImgDisplay.Source = _wb;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_model == null) return;

        float transSpeed = 0.2f;
        float scaleSpeed = 0.1f;
        float rotSpeed = 0.1f;

        switch (e.Key)
        {
            case Key.Left:  _model.Translation -= new Vector3(transSpeed, 0, 0); break;
            case Key.Right: _model.Translation += new Vector3(transSpeed, 0, 0); break;
            case Key.Up:    _model.Translation += new Vector3(0, transSpeed, 0); break;
            case Key.Down:  _model.Translation -= new Vector3(0, transSpeed, 0); break;

            case Key.OemPlus:
            case Key.Add: _model.Scale += scaleSpeed; break;
            case Key.OemMinus:
            case Key.Subtract: _model.Scale = Math.Max(0.1f, _model.Scale - scaleSpeed); break;

            case Key.W: _model.Rotation += new Vector3(rotSpeed, 0, 0); break;
            case Key.S: _model.Rotation -= new Vector3(rotSpeed, 0, 0); break;
            case Key.A: _model.Rotation -= new Vector3(0, rotSpeed, 0); break;
            case Key.D: _model.Rotation += new Vector3(0, rotSpeed, 0); break;
            case Key.Q: _model.Rotation += new Vector3(0, 0, rotSpeed); break;
            case Key.E: _model.Rotation -= new Vector3(0, 0, rotSpeed); break;
        }

        DrawScene();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isMouseDown = true;
            _lastMousePos = e.GetPosition(ImagePanel);
            Mouse.Capture(ImagePanel);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isMouseDown = false;
            Mouse.Capture(null);
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_model == null || !_isMouseDown) return;

        Point currentPos = e.GetPosition(ImagePanel);
        float deltaX = (float)(currentPos.X - _lastMousePos.X);
        float deltaY = (float)(currentPos.Y - _lastMousePos.Y);

        float sensitivity = 0.005f;

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            _model.Rotation += new Vector3(0, 0, deltaX * sensitivity);
        }
        else
        {
            _model.Rotation += new Vector3(deltaY * sensitivity, deltaX * sensitivity, 0);
        }

        _lastMousePos = currentPos;
        DrawScene();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_model == null) return;

        float zoomSpeed = 0.1f;

        if (e.Delta > 0)
            _model.Scale += zoomSpeed;
        else
            _model.Scale = Math.Max(0.05f, _model.Scale - zoomSpeed); // Не даем уйти в минус

        DrawScene();
    }

    // --- Отрисовка ---
    private void DrawScene()
    {
        if (_wb == null || _model == null) return;

        ObjModelDrawer.FillBitmap(_wb, _bgColor);

        _processor.TransformModel(_model, _camera, (float)_wb.Width, (float)_wb.Height);

        ObjModelDrawer.DrawModel(_wb, _model, _modelColor);
    }

    private void LoadFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog() { Filter = "OBJ Files (*.obj)|*.obj" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var loadedModel = ObjModelFileReader.ReadModel(dlg.FileName);
                loadedModel.Initialize();
                loadedModel.Normalize();
                _model = loadedModel;

                _model.Scale = 1f;
                _model.Rotation = Vector3.Zero;
                _model.Translation = Vector3.Zero;

                DrawScene();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }

    private void FileClear_OnClick(object sender, RoutedEventArgs e)
    {
        if (_wb != null)
        {
            ObjModelDrawer.FillBitmap(_wb, _bgColor);
            _model = null;
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitWriteableBitmap();
        DrawScene();
    }
}