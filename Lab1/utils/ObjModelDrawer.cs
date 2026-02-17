using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab1.model;

namespace Lab1.utils;

public static class ObjModelDrawer
{
    public static void DrawModel(WriteableBitmap wb, ObjModel model, Color color)
    {
        int intColor = color.ToInt();
        Vector4[] vtxs = model.VtxsTransform;
        int vtxsCount = vtxs.Length;
        List<Polygon> polygons = model.Polygons;

        int width = wb.PixelWidth;
        int height = wb.PixelHeight;

        wb.Lock();

        unsafe
        {
            int* buffer = (int*)wb.BackBuffer;

            foreach (var poly in polygons)
            {
                int count = poly.PolygonRecords.Count;
                if (count < 2)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    int index1 = poly.PolygonRecords[i].GeometrixIndex;
                    int index2 = poly.PolygonRecords[(i + 1) % count].GeometrixIndex;

                    if (index1 < 0 || index1 >= vtxsCount || index2 < 0 || index2 >= vtxsCount)
                        continue;



                    DrawLineBresenham(buffer, width, height, vtxs[index1], vtxs[index2], intColor);
                }
            }
        }

        wb.Unlock();
    }

    private static unsafe void DrawLineBresenham(int* buffer, int width, int height, Vector4 v1, Vector4 v2, int color)
    {
        int x0 = (int)Math.Round(v1.X);
        int y0 = (int)Math.Round(v1.Y);
        int x1 = (int)Math.Round(v2.X);
        int y1 = (int)Math.Round(v2.Y);

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                buffer[y0 * width + x0] = color;
            }

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public static void FillBitmap(WriteableBitmap wb, Color color)
    {
        int colorInt = color.ToInt();
        wb.Lock();
        try
        {
            unsafe
            {
                int* pBackBuffer = (int*)wb.BackBuffer;

                for (int i = 0; i < wb.PixelHeight; i++)
                {
                    for (int j = 0; j < wb.PixelWidth; j++)
                    {
                        *pBackBuffer++ = colorInt;
                    }
                }
            }

            wb.AddDirtyRect(new Int32Rect(0, 0, wb.PixelWidth, wb.PixelHeight));
        }
        finally
        {
            wb.Unlock();
        }
    }

    private static int ToInt(this Color color)
    {
        return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }
}