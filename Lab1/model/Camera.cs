using System.Numerics;

namespace Lab1.model;

public class Camera
{
    public Vector3 Eye { get; set; } = new(1.0f, 1.0f, MathF.PI);
    public Vector3 Target { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;

    public float Fov { get; set; } = MathF.PI / 4;
    public float ZNear { get; set; } = 1f;
    public float ZFar { get; set; } = 100f;
}