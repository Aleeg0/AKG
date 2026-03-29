using System.Numerics;
using Lab4.model;

namespace Lab4.utils;

public class RenderProcessor
{
    public void TransformModel(ObjModel model, Camera cam, float width, float height)
    {
        var viewMatrix = CreateViewMatrix(cam.Eye, cam.Target, cam.Up);
        var projMatrix = CreateProjectionMatrix(cam.Fov, cam.AspectRatio, cam.ZNear, cam.ZFar);
        var viewportMatrix = CreateViewportMatrix(width, height, 0, 0);

        var finalMatrix = model.ModelMatrix * viewMatrix * projMatrix * viewportMatrix;

        int count = model.GeometricVtxs.Length;
        Parallel.For(0, count, i =>
        {
            var v = Vector4.Transform(model.GeometricVtxs[i], finalMatrix);
            if (v.W > cam.ZNear)
            {
                v.X /= v.W;
                v.Y /= v.W;
                v.Z /= v.W;
            }

            model.TransformVtxs[i] = v;
        });
    }

    private static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        var yAxis = up;

        return Matrix4x4.Transpose(new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, -Vector3.Dot(xAxis, eye),
            yAxis.X, yAxis.Y, yAxis.Z, -Vector3.Dot(yAxis, eye),
            zAxis.X, zAxis.Y, zAxis.Z, -Vector3.Dot(zAxis, eye),
            0f, 0f, 0f, 1f
        ));
    }

    private static Matrix4x4 CreateProjectionMatrix(float fov, float aspect, float zNear, float zFar)
    {
        float tanHalfFov = MathF.Tan(fov / 2);
        float m11 = 1 / (aspect * tanHalfFov);
        float m22 = 1 / (tanHalfFov);
        float m33 = zFar / (zNear - zFar);
        float m34 = zNear * zFar / (zNear - zFar);

        return Matrix4x4.Transpose(new Matrix4x4(
            m11, 0, 0, 0,
            0, m22, 0, 0,
            0, 0, m33, m34,
            0, 0, -1, 0
        ));
    }

    private static Matrix4x4 CreateViewportMatrix(float width, float height, float xMin, float yMin)
    {
        float m11 = width / 2;
        float m22 = -height / 2;
        float m14 = xMin + width / 2;
        float m24 = yMin + height / 2;

        return Matrix4x4.Transpose(new Matrix4x4(
            m11, 0, 0, m14,
            0, m22, 0, m24,
            0, 0, 1, 0,
            0, 0, 0, 1
        ));
    }
}