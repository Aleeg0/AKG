using System.Numerics;
using Lab5.model;
using Lab5.utils;

namespace Lab5.render;


public class ShadowRenderer
{
    public float[] ShadowBuffer;
    public int Width, Height;
    public Matrix4x4 LightSpaceMatrix;

    public ShadowRenderer(int width, int height)
    {
        Width = width;
        Height = height;
        ShadowBuffer = new float[width * height];
    }

    public void Render(Model model, Vector3 lightPos)
    {
        Array.Fill(ShadowBuffer, float.MaxValue);

        var view = Matrix4x4.CreateLookAt(lightPos, Vector3.Zero, Vector3.UnitY);

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var v in model.WorldVtxs)
        {
            Vector3 vLightSpace = Vector3.Transform(v, view);
            if (vLightSpace.X < minX) minX = vLightSpace.X;
            if (vLightSpace.X > maxX) maxX = vLightSpace.X;
            if (vLightSpace.Y < minY) minY = vLightSpace.Y;
            if (vLightSpace.Y > maxY) maxY = vLightSpace.Y;
            if (vLightSpace.Z < minZ) minZ = vLightSpace.Z;
            if (vLightSpace.Z > maxZ) maxZ = vLightSpace.Z;
        }

        float padding = 1.0f;

        var proj = Matrix4x4.CreateOrthographicOffCenter(
            minX - padding, maxX + padding,
            minY - padding, maxY + padding,
            minZ - padding, maxZ + padding
        );
        Matrix4x4 viewport = Matrix4x4Ex.CreateViewport(Width, Height);

        LightSpaceMatrix = view * proj * viewport;

        Parallel.For(0, model.FaceTrgs.Length, i =>
        {
            var (fv0, fv1, fv2) = model.FaceTrgs[i];

            Vector4 v0 = Vector4.Transform(new Vector4(model.WorldVtxs[fv0.GeometrixIndex], 1f), LightSpaceMatrix);
            Vector4 v1 = Vector4.Transform(new Vector4(model.WorldVtxs[fv1.GeometrixIndex], 1f), LightSpaceMatrix);
            Vector4 v2 = Vector4.Transform(new Vector4(model.WorldVtxs[fv2.GeometrixIndex], 1f), LightSpaceMatrix);

            v0 /= v0.W;
            v1 /= v1.W;
            v2 /= v2.W;

            RasterizeTriangle(v0, v1, v2);
        });
    }

    private void RasterizeTriangle(Vector4 v0, Vector4 v1, Vector4 v2)
    {
        if (v1.Y < v0.Y) (v0, v1) = (v1, v0);
        if (v2.Y < v0.Y) (v0, v2) = (v2, v0);
        if (v2.Y < v1.Y) (v1, v2) = (v2, v1);

        if (Math.Abs(v2.Y - v0.Y) < float.Epsilon) return;

        if (Math.Abs(v1.Y - v0.Y) < float.Epsilon) FillTriangleWithFlatTop(v0, v1, v2);
        else if (Math.Abs(v2.Y - v1.Y) < float.Epsilon) FillTriangleWithFlatBottom(v0, v1, v2);
        else {
            float t = (v1.Y - v0.Y) / (v2.Y - v0.Y);
            var v3 = Vector4.Lerp(v0, v2, t);
            FillTriangleWithFlatBottom(v0, v1, v3);
            FillTriangleWithFlatTop(v1, v3, v2);
        }
    }

    private void FillTriangleWithFlatBottom(Vector4 v0, Vector4 v1, Vector4 v2) {
        int iStartY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int iFinishY = (int)Math.Min(Height - 1, Math.Floor(v1.Y));
        float totalHeight = v1.Y - v0.Y;

        for (int y = iStartY; y <= iFinishY; y++) {
            float t = (y - v0.Y) / totalHeight;
            var left = Vector4.Lerp(v0, v1, t);
            var right = Vector4.Lerp(v0, v2, t);

            if (left.X > right.X) (left, right) = (right, left);

            DrawHorizontalLine(left, right, y);
        }
    }

    private void FillTriangleWithFlatTop(Vector4 v0, Vector4 v1, Vector4 v2) {
        int iStartY = (int)Math.Max(0, Math.Ceiling(v0.Y));
        int iFinishY = (int)Math.Min(Height - 1, Math.Floor(v2.Y));
        float totalHeight = v2.Y - v0.Y;

        for (int y = iStartY; y <= iFinishY; y++) {
            float t = (y - v0.Y) / totalHeight;
            var left = Vector4.Lerp(v0, v2, t);
            var right = Vector4.Lerp(v1, v2, t);

            if (left.X > right.X) (left, right) = (right, left);

            DrawHorizontalLine(left, right, y);
        }
    }

    private void DrawHorizontalLine(Vector4 vLeft, Vector4 vRight, int y)
    {
        int iLeftX = (int)Math.Ceiling(vLeft.X);
        int iRightX = (int)Math.Floor(vRight.X);

        if (iLeftX >= Width || iRightX < 0)
            return;


        int clampedXLeft = (int)Math.Max(iLeftX, 0.0f);
        int clampedXRight = (int)Math.Min(iRightX, Width - 1.0f);

        float totalWidth = vRight.X - vLeft.X;
        float totalZWidth = vRight.Z - vLeft.Z;

        int bufferIndex = y * Width + clampedXLeft;

        for (int x = clampedXLeft; x <= clampedXRight; x++) {
            float t = totalWidth > float.Epsilon ? (x -  vLeft.X) / totalWidth : 0.0f;
            float z = vLeft.Z + t * totalZWidth;

            if (z < ShadowBuffer[bufferIndex]) ShadowBuffer[bufferIndex] = z;

            ++bufferIndex;
        }
    }
}