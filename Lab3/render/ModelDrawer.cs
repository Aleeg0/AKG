using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lab3.model;
using Vector = System.Windows.Vector;

namespace Lab3.render;

public unsafe class ModelDrawer(Camera camera, SceneSettings sceneSettings)
{
    private int _width = 0;
    private int _height = 0;
    private int* _buffer = null;
    private int _intColor = 0;
    private float[] _zBuffer = [];

    private struct DrawerVertex
    {
        public Vector4 Transform;
        public Vector3 World;
        public Vector3 Normal;

        public static DrawerVertex Lerp(DrawerVertex v0, DrawerVertex v1, float t)
        {
            return new DrawerVertex()
            {
                Transform = Vector4.Lerp(v0.Transform, v1.Transform, t),
                World = Vector3.Lerp(v0.World, v1.World, t),
                Normal = Vector3.Lerp(v0.Normal, v1.Normal, t),
            };
        }
    }

    public void DrawModel(WriteableBitmap wb, ObjModel model)
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

            Vector3 faceNormal = Vector3.Cross(worldV1 - worldV0, worldV2 - worldV0);
            Vector3 viewDir = camera.Eye - worldV0;

            if (Vector3.Dot(faceNormal, viewDir) <= 0) continue;

            DrawerVertex dv0 = new DrawerVertex()
            {
                Transform = vtxs[v0.GeometrixIndex],
                World = worldV0,
                Normal = model.WorldNormalVtxs[v0.NormalIndex ?? 0],
            };
            DrawerVertex dv1 = new DrawerVertex()
            {
                Transform = vtxs[v1.GeometrixIndex],
                World = worldV1,
                Normal = model.WorldNormalVtxs[v1.NormalIndex ?? 0],
            };
            DrawerVertex dv2 = new DrawerVertex()
            {
                Transform = vtxs[v2.GeometrixIndex],
                World = worldV2,
                Normal = model.WorldNormalVtxs[v2.NormalIndex ?? 0],
            };

            RasterizeTriangle(dv0,dv1,dv2);
        }

        wb.Unlock();
    }

    private void RasterizeTriangle(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        if (dv1.Transform.Y < dv0.Transform.Y) (dv0, dv1) = (dv1, dv0);
        if (dv2.Transform.Y < dv0.Transform.Y) (dv0, dv2) = (dv2, dv0);
        if (dv2.Transform.Y < dv1.Transform.Y) (dv1, dv2) = (dv2, dv1);

        if (Math.Abs(dv2.Transform.Y - dv0.Transform.Y) < float.Epsilon) return;

        if (Math.Abs(dv1.Transform.Y - dv0.Transform.Y) < float.Epsilon)
        {
            FillTriangleWithFlatTop(dv0, dv1, dv2);
            return;
        }

        if (Math.Abs(dv2.Transform.Y - dv1.Transform.Y) < float.Epsilon)
        {
            FillTriangleWithFlatBottom(dv0, dv1, dv2);
            return;
        }

        float t = (dv1.Transform.Y - dv0.Transform.Y) / (dv2.Transform.Y - dv0.Transform.Y);
        DrawerVertex dv3 = DrawerVertex.Lerp(dv0, dv2, t);
        // TODO убедись что Y у dv3 будет dv1.Transform.Y

        FillTriangleWithFlatBottom(dv0, dv1, dv3);
        FillTriangleWithFlatTop(dv1, dv3, dv2);
    }

    private void FillTriangleWithFlatBottom(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(dv0.Transform.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(dv1.Transform.Y));
        float totalHeight = (dv1.Transform.Y - dv0.Transform.Y);
        float startY = dv0.Transform.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            float t = (y - startY) / totalHeight;
            DrawerVertex left = DrawerVertex.Lerp(dv0, dv1, t);
            DrawerVertex right = DrawerVertex.Lerp(dv0, dv2, t);

            if (left.Transform.X > right.Transform.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y);
        }
    }

    private void FillTriangleWithFlatTop(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(dv0.Transform.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(dv2.Transform.Y));
        float totalHeight = (dv2.Transform.Y - dv0.Transform.Y);
        float startY = dv0.Transform.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            float t = (y - startY) / totalHeight;
            DrawerVertex left = DrawerVertex.Lerp(dv0, dv2, t);
            DrawerVertex right = DrawerVertex.Lerp(dv1, dv2, t);

            if (left.Transform.X > right.Transform.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y);
        }
    }

    private void DrawHorizontalLine(DrawerVertex dvLeft, DrawerVertex dvRight, int y)
    {
        int iLeftX = (int)Math.Ceiling(dvLeft.Transform.X);
        int iRightX = (int)Math.Floor(dvRight.Transform.X);

        if (iLeftX >= _width || iRightX < 0)
            return;

        int clampedXLeft = (int)Math.Max(iLeftX, 0.0f);
        int clampedXRight = (int)Math.Min(iRightX, _width - 1.0f);

        float totalWidth = dvRight.Transform.X - dvLeft.Transform.X;
        float leftX =  dvLeft.Transform.X;
        float totalZWidth = dvRight.Transform.Z - dvLeft.Transform.Z;
        float leftZ = dvLeft.Transform.Z;

        int bufferIndex = y * _width + clampedXLeft;
        int* row = _buffer + y * _width;

        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            float t = totalWidth > float.Epsilon ? (x - leftX) / totalWidth : 0.0f;

            float z = leftZ + t *(totalZWidth);

            if (z < _zBuffer[bufferIndex])
            {
                _zBuffer[bufferIndex] = z;

                Vector3 currentWorld = Vector3.Lerp(dvLeft.World, dvRight.World, t);
                Vector3 currentNormal = Vector3.Lerp(dvLeft.Normal, dvRight.Normal, t);

                row[x] = GetPhongColor(currentWorld, currentNormal);
            }

            bufferIndex++;
        }
    }

    private int GetPhongColor(Vector3 world, Vector3 normal)
    {
        Vector3 normalizeNormal = Vector3.Normalize(normal);
        Vector3 normalizeView = Vector3.Normalize(camera.Eye - world);
        Vector3 normalizeLight = sceneSettings.LightDirection;

        float ambientLight = sceneSettings.AmbientIntensity;
        float diff = Math.Max(0, Vector3.Dot(normalizeNormal, normalizeLight));
        float diffuseLight = sceneSettings.DiffuseIntensity * diff;

        float reflectionLight = 0.0f;

        if (diff > float.Epsilon)
        {
            Vector3 reflection = normalizeLight - 2.0f * Math.Max(0, Vector3.Dot(normalizeLight, normalizeNormal)) * normalizeNormal;
            float specAngle = Math.Max(0, Vector3.Dot(Vector3.Normalize(reflection), normalizeView));
            reflectionLight = sceneSettings.ReflectionIntensity * (float)Math.Pow(specAngle, sceneSettings.ReflectionAlpha);
        }

        float intensity = Math.Clamp(ambientLight + diffuseLight + reflectionLight, 0.0f, 1.0f);

        int r = (int)(sceneSettings.ModelColor.R * intensity);
        int g = (int)(sceneSettings.ModelColor.G * intensity);
        int b = (int)(sceneSettings.ModelColor.B * intensity);

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