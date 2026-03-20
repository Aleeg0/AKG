using System.Numerics;
using System.Runtime.InteropServices;
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
    private static ZBuffer _zBuffer = new ();

    public static void DrawModel(WriteableBitmap wb, ObjModel model, Color color, Camera camera)
    {
        Vector4[] vtxs = model.VtxsTransform;


        _width = wb.PixelWidth;
        _height = wb.PixelHeight;

        if (_zBuffer.Value.Length < _width * _height)
        {
            _zBuffer.Resize(_width, _height);
        }
        _zBuffer.Clear();

        wb.Lock();

        _buffer = (int*)wb.BackBuffer;

        foreach (var poly in model.Polygons)
        {
            var records = CollectionsMarshal.AsSpan(poly.PolygonRecords);
            if (records.Length < 3) continue;

            int index0 = records[0].GeometrixIndex;
            if (index0 < 0 || index0 >= vtxs.Length) continue;

            Vector4 v0 = vtxs[index0];

            for (int i = 1; i < records.Length - 1; i++)
            {
                int index1 = records[i].GeometrixIndex;
                int index2 = records[i + 1].GeometrixIndex;

                if (index1 < 0 || index1 >= vtxs.Length || index2 < 0 || index2 >= vtxs.Length)
                    continue;

                Vector4 v1 = vtxs[index1];
                Vector4 v2 = vtxs[index2];

                float crossZ = (v1.X - v0.X) * (v2.Y - v0.Y) - (v1.Y - v0.Y) * (v2.X - v0.X);

                if (crossZ >= 0)
                    continue;

                var worldV0 = model.VtxsWorldTransform[index0];
                var worldV1 = model.VtxsWorldTransform[index1];
                var worldV2 = model.VtxsWorldTransform[index2];

                Vector3 edge1 = new Vector3(worldV1.X - worldV0.X, worldV1.Y - worldV0.Y, worldV1.Z - worldV0.Z);
                Vector3 edge2 = new Vector3(worldV2.X - worldV0.X, worldV2.Y - worldV0.Y, worldV2.Z - worldV0.Z);

                Vector3 test = Vector3.Cross(edge1, edge2);
                Vector3 normal = Vector3.Normalize(test);

                float rowIntensity = Vector3.Dot(normal, camera.Light);
                float intensity = 0.3f + 0.7f * Math.Max(0, rowIntensity);

                int light = (int)(intensity * 255);
                _intColor = (255 << 24) | (light << 16) | (light << 8) | light;

                RasterizeTriangle(v0, v1, v2);
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
        float invDy01 = 1.0f / (v1.Y - v0.Y);
        float invDy02 = 1.0f / (v2.Y - v0.Y);

        int startY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int finishY = (int)Math.Min(_height - 1, Math.Floor(v2.Y));

        float stepXLeft = (v1.X - v0.X) * invDy01;
        float stepXRight = (v2.X - v0.X) * invDy02;
        float stepZLeft = (v1.Z - v0.Z) * invDy01;
        float stepZRight = (v2.Z - v0.Z) * invDy02;

        float xLeft = v0.X, xRight = v0.X, zLeft = v0.Z, zRight = v0.Z;

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

    private static void FillTriangleWithFlatTop(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        float invDy01 = 1.0f / (v2.Y - v0.Y);
        float invDy02 = 1.0f / (v2.Y - v1.Y);

        int startY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int finishY = (int)Math.Min(_height - 1, Math.Floor(v2.Y));

        float stepXLeft = (v2.X - v0.X) * invDy01;
        float stepXRight = (v2.X - v1.X) * invDy02;
        float stepZLeft = (v2.Z - v0.Z) * invDy01;
        float stepZRight = (v2.Z - v1.Z) * invDy02;

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

        float z = vLeft.Z;
        float stepZ = 0;

        if (vRight.X != vLeft.X)
        {
            stepZ = (vRight.Z - vLeft.Z) / (vRight.X - vLeft.X);
            z += stepZ * (clampedXLeft - vLeft.X);
        }


        int bufferIndex = (int)vLeft.Y * _width + clampedXLeft;
        int* row = _buffer + (int)vLeft.Y * _width;


        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            if (z < _zBuffer.Value[bufferIndex])
            {
                _zBuffer.Value[bufferIndex] = z;
                row[x] = _intColor;
            }

            z += stepZ;
            bufferIndex++;
        }
    }

    public static void FillBitmap(WriteableBitmap wb, Color color)
    {
        int colorInt = color.ToInt();
        wb.Lock();
        try
        {
            int* pBackBuffer = (int*)wb.BackBuffer;

            for (int i = 0; i < wb.PixelHeight; i++)
            {
                for (int j = 0; j < wb.PixelWidth; j++)
                {
                    *pBackBuffer++ = colorInt;
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