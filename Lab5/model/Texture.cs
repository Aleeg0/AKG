using System.Numerics;
using System.Windows.Media.Imaging;

namespace Lab5.model;

public class Texture
{
    private readonly int _width;
    private readonly int _height;
    private readonly byte[] _pixels;

    public Texture(BitmapImage bmp)
    {
        _width = bmp.PixelWidth;
        _height = bmp.PixelHeight;
        int stride = _width * 4;
        _pixels = new byte[stride * _height];
        bmp.CopyPixels(_pixels, stride, 0);
    }

    public Vector4 Sample(float u, float v)
    {
        u = u - MathF.Floor(u);
        v = v - MathF.Floor(v);

        v = 1.0f - v;

        int iU = (int)(u * (_width - 1));
        int iV = (int)(v * (_height - 1));

        int index = (iV * _width + iU) * 4;
        byte b = _pixels[index];
        byte g = _pixels[index + 1];
        byte r = _pixels[index + 2];
        byte a = _pixels[index + 3];

        return new Vector4(r,g,b,a);
    }
}