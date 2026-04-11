using System.Numerics;

namespace Lab5.render;

public class DrawerVertex
{
    public Vector4 Transform;
    public Vector3 World;
    public Vector3 Normal;
    public Vector3 Texel;

    public static DrawerVertex Lerp(DrawerVertex v0, DrawerVertex v1, float t)
    {
        return new DrawerVertex()
        {
            Transform = Vector4.Lerp(v0.Transform, v1.Transform, t),
            World = Vector3.Lerp(v0.World, v1.World, t),
            Normal = Vector3.Lerp(v0.Normal, v1.Normal, t),
            Texel = Vector3.Lerp(v0.Texel, v1.Texel, t),
        };
    }

    public static DrawerVertex CreateInstance(Vector4 transform, Vector3 world, Vector3 normal, Vector3 texel)
    {
        float invW = 1 / transform.W;
        transform.W = invW;
        return new DrawerVertex()
        {
            Transform = transform,
            World = world * invW,
            Normal = normal * invW,
            Texel = texel * invW
        };
    }
}