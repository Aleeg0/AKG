using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab2.model;

namespace Lab2.render;

public static unsafe class ObjModelDrawer
{
    private static int _width = 0;
    private static int _height = 0;
    private static int* _buffer = null;
    private static int _intColor = 0;
    private static float[] _zBuffer = [];

    public static void DrawModel(WriteableBitmap wb, ObjModel model, Camera camera, SceneSettings settings)
    {
        Vector4[] vtxs = model.TransformVtxs;

        _width = wb.PixelWidth;
        _height = wb.PixelHeight;

        if (_zBuffer.Length < _width * _height)
        {
            _zBuffer = new float[_width * _height];
        }
        Array.Fill(_zBuffer, float.MaxValue);

        wb.Lock();

        _buffer = (int*)wb.BackBuffer;

        foreach (var (v0, v1, v2) in model.FaceTrgs)
        {
            var worldV0 = model.WorldVtxs[v0.GeometrixIndex];
            var worldV1 = model.WorldVtxs[v1.GeometrixIndex];
            var worldV2 = model.WorldVtxs[v2.GeometrixIndex];

            Vector3 faceNormal = Vector3.Cross(
                worldV1 - worldV0,
                worldV2 - worldV0
            );
            Vector3 viewDir = camera.Eye - worldV0;

            if (Vector3.Dot(faceNormal, viewDir) <= 0) continue;

            Vector3 normal = Vector3.Normalize(faceNormal);

            float rowIntensity = Vector3.Dot(normal, settings.LightDirection);
            float intensity = settings.AmbientIntensity + settings.DiffuseIntensity * Math.Max(0, rowIntensity);

            int light = (int)(intensity * 255);
            _intColor = (255 << 24) | (light << 16) | (light << 8) | light;

            RasterizeTriangle(
                vtxs[v0.GeometrixIndex],
                vtxs[v1.GeometrixIndex],
                vtxs[v2.GeometrixIndex]
            );
        }

        wb.Unlock();
    }

    private static void RasterizeTriangle(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
        if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
        if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

        if (Math.Abs(v2.Y - v0.Y) < float.Epsilon) return;

        if (Math.Abs(v1.Y - v0.Y) < float.Epsilon)
        {
            FillTriangleWithFlatTop(v0, v1, v2);
            return;
        }

        if (Math.Abs(v2.Y - v1.Y) < float.Epsilon)
        {
            FillTriangleWithFlatBottom(v0, v1, v2);
            return;
        }

        float coef = (v1.Y - v0.Y) / (v2.Y - v0.Y);
        Vector4 v3 = new Vector4(
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
        int finishY = (int)Math.Min(_height - 1, Math.Floor(v1.Y));

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
        int iLeftX = (int)Math.Ceiling(vLeft.X);
        int iRightX = (int)Math.Floor(vRight.X);

        if (iLeftX >= _width || iRightX < 0)
            return;

        int clampedXLeft = (int)Math.Max(iLeftX, 0.0f);
        int clampedXRight = (int)Math.Min(iRightX, _width - 1.0f);

        float z = vLeft.Z;
        float stepZ = 0;

        if (vRight.X - vLeft.X > float.Epsilon)
        {
            stepZ = (vRight.Z - vLeft.Z) / (vRight.X - vLeft.X);
            z += stepZ * (clampedXLeft - vLeft.X);
        }
        int bufferIndex = (int)vLeft.Y * _width + clampedXLeft;
        int* row = _buffer + (int)vLeft.Y * _width;


        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            if (z < _zBuffer[bufferIndex])
            {
                _zBuffer[bufferIndex] = z;
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

    private static int ApplyLighting(Color baseColor, float intensity)
    {
        int r = (int)(baseColor.R * intensity);
        int g = (int)(baseColor.G * intensity);
        int b = (int)(baseColor.B * intensity);

        return (255 << 24) | (r << 16) | (g << 8) | b;
    }
}