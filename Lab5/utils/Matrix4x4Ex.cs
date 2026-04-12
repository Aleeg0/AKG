using System.Numerics;

namespace Lab5.utils;

public static class Matrix4x4Ex
{
    public static Matrix4x4 CreateViewport(float width, float height) {
        return new Matrix4x4(
            width / 2f, 0, 0, 0,
            0, -height / 2f, 0, 0,
            0, 0, 1, 0,
            width / 2f, height / 2f, 0, 1
        );
    }
}