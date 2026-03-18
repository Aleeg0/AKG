using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab2.model;

namespace Lab2.utils;

public static unsafe class ObjModelDrawer
{
    private static int _width = 0;
    private static int _height = 0;
    private static int* _buffer = null;
    private static int _intColor = 0;

    public static void DrawModel(WriteableBitmap wb, ObjModel model, Color color)
    {
        _intColor = color.ToInt();
        Vector4[] vtxs = model.VtxsTransform;
        int vtxsCount = vtxs.Length;
        List<Polygon> polygons = model.Polygons;

        _width = wb.PixelWidth;
        _height = wb.PixelHeight;

        wb.Lock();

        _buffer = (int*)wb.BackBuffer;

        foreach (var poly in polygons)
        {
            int count = poly.PolygonRecords.Count;
            if (count < 3)
                continue;

            for (int i = 1; i < count - 1; i++)
            {
                int index1 = poly.PolygonRecords[0].GeometrixIndex;
                int index2 = poly.PolygonRecords[i].GeometrixIndex;
                int index3 = poly.PolygonRecords[i + 1].GeometrixIndex;

                if (index1 < 0 || index1 >= vtxsCount || index2 < 0 || index2 >= vtxsCount || index3 < 0 || index3  >= vtxsCount)
                    continue;

                RasterizeTriangle(vtxs[index1], vtxs[index2], vtxs[index3]);
            }
        }

        wb.Unlock();
    }

    private static void RasterizeTriangle(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
        if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
        if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

        if (v0.Y == v2.Y) return;

        if (v0.Y == v1.Y)
        {
            FillTriangleWithFlatTop(v0, v1, v2);
            return;
        }

        if (v1.Y == v2.Y)
        {
            FillTriangleWithFlatBottom(v0, v1, v2);
            return;
        }

        float coef = (v1.Y - v0.Y) / (v2.Y - v0.Y);
        Vector4 v3 = new  Vector4(
            v0.X + coef * (v2.X - v0.X),
            v1.Y,
            v0.Z + coef * (v2.Z - v0.Z),
            0
        );

        FillTriangleWithFlatBottom(v0, v1, v3);
        FillTriangleWithFlatTop(v1, v3, v2);
    }

    private static void FillTriangleWithFlatBottom(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        float dY01 = v1.Y - v0.Y;
        float dY02 = v2.Y - v0.Y;

        int startY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int finishY = (int)Math.Min(_height - 1, Math.Floor(v2.Y));

        float stepXLeft = (v1.X - v0.X) / dY01;
        float stepXRight = (v2.X - v0.X) / dY02;
        float stepZLeft = (v1.Z - v0.Z) / dY01;
        float stepZRight = (v2.Z - v0.Z) / dY02;

        float xLeft = v0.X, xRight = v0.X, zLeft = v0.Z, zRight = v0.Z;

        for (int y = startY; y <= finishY; y++)
        {
            float currXLeft = xLeft, currXRight = xRight;
            float currZLeft = zLeft, currZRight = zRight;

            if (currXLeft > currXRight)
            {
                (currXLeft, currXRight) = (currXRight, currXLeft);
                (currZLeft, currZRight) = (currZRight, currZLeft);
            }

            DrawHorizontalLine(
                new Vector3(currXLeft, y, currZLeft),
                new Vector3(currXRight, y, currZRight)
            );

            xLeft += stepXLeft;
            xRight += stepXRight;
            zLeft += stepZLeft;
            zRight += stepZRight;
        }
    }

    private static void FillTriangleWithFlatTop(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        float dY01 = v2.Y - v0.Y;
        float dY02 = v2.Y - v1.Y;

        int startY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int finishY = (int)Math.Min(_height - 1, Math.Floor(v2.Y));

        float stepXLeft = (v2.X - v0.X) / dY01;
        float stepXRight = (v2.X - v1.X) / dY02;
        float stepZLeft = (v2.Z - v0.Z) / dY01;
        float stepZRight = (v2.Z - v1.Z) / dY02;

        float xLeft = v0.X, xRight = v1.X, zLeft = v0.Z, zRight = v1.Z;

        float preStep = startY - v0.Y;
        xLeft += stepXLeft * preStep;
        xRight += stepXRight * preStep;
        zLeft += stepZLeft * preStep;
        zRight += stepZRight * preStep;

        for (int y = startY; y <= finishY; y++)
        {

            float currXLeft = xLeft, currXRight = xRight;
            float currZLeft = zLeft, currZRight = zRight;

            if (currXLeft > currXRight)
            {
                (currXLeft, currXRight) = (currXRight, currXLeft);
                (currZLeft, currZRight) = (currZRight, currZLeft);
            }

            DrawHorizontalLine(
                new Vector3(currXLeft, y, currZLeft),
                new Vector3(currXRight, y, currZRight)
            );

            xLeft += stepXLeft;
            xRight += stepXRight;
            zLeft += stepZLeft;
            zRight += stepZRight;
        }
    }

    private static void DrawHorizontalLine(Vector3 vLeft, Vector3 vRight)
    {
        if (vLeft.X >= _width || vRight.X < 0)
            return;

        int clampedXLeft = (int)Math.Max(vLeft.X, 0.0f);
        int clampedXRight = (int)Math.Min(vRight.X, _width - 1.0f);

        int* row = _buffer + ((int)vLeft.Y * _width);

        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            row[x] = _intColor;
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