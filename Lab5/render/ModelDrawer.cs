using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab5.model;

namespace Lab5.render;

public unsafe class ModelDrawer(Camera camera, SceneSettings sceneSettings)
{
    private int _width = 0;
    private int _height = 0;
    private int* _buffer = null;
    private float[] _zBuffer = [];
    private int[] _triangleIdBuffer = [];
    private Model _model;

    public void DrawModel(WriteableBitmap wb, Model model)
    {
        _width = wb.PixelWidth;
        _height = wb.PixelHeight;
        _model = model;

        int pixelCount = _width * _height;

        if (_zBuffer.Length < _width * _height)
        {
            _zBuffer = new float[pixelCount];
            _triangleIdBuffer = new int[pixelCount];
        }
        Array.Fill(_zBuffer, float.MaxValue);
        Array.Fill(_triangleIdBuffer, -1);

        wb.Lock();

        _buffer = (int*)wb.BackBuffer;

        FirstLoop(model);
        SecondLoop(model);

        wb.Unlock();
    }

    private void FirstLoop(Model model)
    {
        for (int triangleId = 0; triangleId < model.FaceTrgs.Length; triangleId++)
        {
            var (v0, v1, v2) = model.FaceTrgs[triangleId];
            var worldV0 = model.WorldVtxs[v0.GeometrixIndex];
            var worldV1 = model.WorldVtxs[v1.GeometrixIndex];
            var worldV2 = model.WorldVtxs[v2.GeometrixIndex];

            Vector3 faceNormal = Vector3.Cross(worldV1 - worldV0, worldV2 - worldV0);
            Vector3 viewDir = camera.Eye - worldV0;

            if (Vector3.Dot(faceNormal, viewDir) <= 0) continue;

            RasterizeTriangle(
                model.TransformVtxs[v0.GeometrixIndex],
                model.TransformVtxs[v1.GeometrixIndex],
                model.TransformVtxs[v2.GeometrixIndex],
                triangleId
            );
        }
    }

    private void SecondLoop(Model model)
    {
        int bufferIndex = 0;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int triId = _triangleIdBuffer[bufferIndex];
                if (triId != -1)
                {
                    var (fv0, fv1, fv2) = model.FaceTrgs[triId];
                    var (tv0, tv1, tv2) = (
                        model.TransformVtxs[fv0.GeometrixIndex],
                        model.TransformVtxs[fv1.GeometrixIndex],
                        model.TransformVtxs[fv2.GeometrixIndex]
                    );

                    float area = FindArea(tv0, tv1, tv2);

                    if (Math.Abs(area) <= float.Epsilon) continue;

                    float w0 = EdgeFunction(tv1, tv2, x, y) / area;
                    float w1 = EdgeFunction(tv2, tv0, x, y) / area;
                    float w2 = 1.0f - w0 - w1;

                    float invW0 = 1.0f / tv0.W;
                    float invW1 = 1.0f / tv1.W;
                    float invW2 = 1.0f / tv2.W;

                    float currentInvW = w0 * invW0 + w1 * invW1 + w2 * invW2;

                    Vector3 currentWorld = (
                        w0 * model.WorldVtxs[fv0.GeometrixIndex] * invW0 +
                        w1 * model.WorldVtxs[fv1.GeometrixIndex] * invW1 +
                        w2 * model.WorldVtxs[fv2.GeometrixIndex] * invW2) / currentInvW;
                    Vector3 currentNormal = (
                        w0 * model.WorldNormalVtxs[fv0.NormalIndex ?? 0] * invW0 +
                        w1 * model.WorldNormalVtxs[fv1.NormalIndex ?? 0] * invW1 +
                        w2 * model.WorldNormalVtxs[fv2.NormalIndex ?? 0] * invW2) / currentInvW;
                    Vector3 currentTexel = (
                        w0 * model.TextureVtxs[fv0.TextureIndex] * invW0 +
                        w1 * model.TextureVtxs[fv1.TextureIndex] * invW1 +
                        w2 * model.TextureVtxs[fv2.TextureIndex] * invW2) / currentInvW;

                    _buffer[bufferIndex] = GetPhongColor(currentWorld, currentNormal, currentTexel);
                }
                bufferIndex++;
            }
        }
    }

    private float EdgeFunction(Vector4 a, Vector4 b, float px, float py)
    {
        return (b.X - a.X) * (py - a.Y) - (b.Y - a.Y) * (px - a.X);
    }

    private float FindArea(Vector4 a, Vector4 b, Vector4 c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    private void RasterizeTriangle(Vector4 v0, Vector4 v1, Vector4 v2, int triangleId)
    {
        if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
        if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
        if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

        if (Math.Abs(v2.Y - v0.Y) < float.Epsilon) return;

        if (Math.Abs(v1.Y - v0.Y) < float.Epsilon)
        {
            FillTriangleWithFlatTop(v0, v1, v2, triangleId);
            return;
        }

        if (Math.Abs(v2.Y - v1.Y) < float.Epsilon)
        {
            FillTriangleWithFlatBottom(v0, v1, v2, triangleId);
            return;
        }

        float t = (v1.Y - v0.Y) / (v2.Y - v0.Y);
        var v3 = Vector4.Lerp(v0, v2, t);

        FillTriangleWithFlatBottom(v0, v1, v3, triangleId);
        FillTriangleWithFlatTop(v1, v3, v2, triangleId);
    }

    private void FillTriangleWithFlatBottom(Vector4 v0, Vector4 v1, Vector4 v2, int triangleId)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(v1.Y));
        float totalHeight = v1.Y - v0.Y;
        float startY = v0.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            float t = (y - startY) / totalHeight;
            var left = Vector4.Lerp(v0, v1, t);
            var right = Vector4.Lerp(v0, v2, t);

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y, triangleId);
        }
    }

    private void FillTriangleWithFlatTop(Vector4 v0, Vector4 v1, Vector4 v2, int triangleId)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(v2.Y));
        float totalHeight = (v2.Y - v0.Y);
        float startY = v0.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            var t = (y - startY) / totalHeight;
            var left = Vector4.Lerp(v0, v2, t);
            var right = Vector4.Lerp(v1, v2, t);

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y, triangleId);
        }
    }

    private void DrawHorizontalLine(Vector4 vLeft, Vector4 vRight, int y, int triangleId)
    {
        int iLeftX = (int)Math.Ceiling(vLeft.X);
        int iRightX = (int)Math.Floor(vRight.X);

        if (iLeftX >= _width || iRightX < 0)
            return;

        int clampedXLeft = (int)Math.Max(iLeftX, 0.0f);
        int clampedXRight = (int)Math.Min(iRightX, _width - 1.0f);

        float totalWidth = vRight.X - vLeft.X;
        float leftX =  vLeft.X;
        float totalZWidth = vRight.Z - vLeft.Z;
        float leftZ = vLeft.Z;

        int bufferIndex = y * _width + clampedXLeft;

        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            float t = totalWidth > float.Epsilon ? (x - leftX) / totalWidth : 0.0f;

            float z = leftZ + t * totalZWidth;

            if (z < _zBuffer[bufferIndex])
            {
                _zBuffer[bufferIndex] = z;
                _triangleIdBuffer[bufferIndex] = triangleId;
            }

            bufferIndex++;
        }
    }

    private int GetPhongColor(Vector3 world, Vector3 normal, Vector3 texel)
    {
        var baseColor = new Vector4(
            sceneSettings.ModelColor.R,
            sceneSettings.ModelColor.G,
            sceneSettings.ModelColor.B,
            sceneSettings.ModelColor.A
        );

        if (_model.DiffuseMap != null)
        {
            baseColor = _model.DiffuseMap.Sample(texel.X, texel.Y);
        }

        baseColor /= 255.0f;

        var normalizeNormal = Vector3.Normalize(normal);
        if (_model.NormalMap != null)
        {
            var texelNormalColor = _model.NormalMap.Sample(texel.X, texel.Y);
            var localNormal = new Vector3(texelNormalColor.X, texelNormalColor.Y, texelNormalColor.Z) / 127.5f - Vector3.One;
            normalizeNormal = Vector3.Normalize(Vector3.Transform(localNormal, _model.RotationMatrix));
        }

        float specReflectionIntensity = sceneSettings.ReflectionIntensity;
        if (_model.SpecularMap != null)
        {
            Vector4 specColor = _model.SpecularMap.Sample(texel.X, texel.Y);
            specReflectionIntensity *= specColor.X / 255.0f;
        }

        Vector3 normalizeView = Vector3.Normalize(camera.Eye - world);
        Vector3 normalizeLight = Vector3.Normalize(sceneSettings.LightPosition - world);

        float ambientLight = sceneSettings.AmbientIntensity;
        float diff = Math.Max(0, Vector3.Dot(normalizeNormal, normalizeLight));
        float diffuseLight = sceneSettings.DiffuseIntensity * diff;

        float reflectionLight = 0.0f;

        if (diff > float.Epsilon)
        {
            Vector3 reflection = 2.0f * Math.Max(0f, Vector3.Dot(normalizeLight, normalizeNormal)) * normalizeNormal - normalizeLight;
            float specAngle = Math.Max(0, Vector3.Dot(Vector3.Normalize(reflection), normalizeView));
            reflectionLight = specReflectionIntensity * (float)Math.Pow(specAngle, sceneSettings.ReflectionAlpha);
        }

        float intensity = Math.Clamp(ambientLight + diffuseLight, 0f, 1f);

        int r = (int)(Math.Clamp(baseColor.X * intensity + reflectionLight, 0f, 1f) * 255f);
        int g = (int)(Math.Clamp(baseColor.Y * intensity + reflectionLight, 0f, 1f) * 255f);
        int b = (int)(Math.Clamp(baseColor.Z * intensity + reflectionLight, 0f, 1f) * 255f);

        return (255 << 24) | (r << 16) | (g << 8) | b;
    }

    public void FillBitmap(WriteableBitmap wb, Color color)
    {
        int colorInt = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
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
}