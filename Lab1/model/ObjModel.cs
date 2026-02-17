using System.Numerics;

namespace Lab1.model;

public class ObjModel
{
    public List<Vector4> VtxsGeometric { get; } = [];
    public Vector4[] VtxsTransform { get; set; } = [];
    public List<Vector3> VtxsTexture { get; } = [];
    public List<Vector3> VtxsNormal { get; } = [];
    public List<Polygon> Polygons { get; } = [];

    public float Scale { get; set; } = 1;

    public Vector3 Translation { get; set; } = Vector3.Zero;

    public Vector3 Rotation { get; set; } = Vector3.Zero;

    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }

    public void Initialize()
    {
        VtxsTransform = new Vector4[VtxsGeometric.Count];
    }

    public void Normalize()
    {
        if (VtxsGeometric.Count == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var v in VtxsGeometric)
        {
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Z < minZ) minZ = v.Z;

            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
            if (v.Z > maxZ) maxZ = v.Z;
        }

        Min = new Vector3(minX, minY, minZ);
        Max = new Vector3(maxX, maxY, maxZ);

        float centerX = (minX + maxX) / 2.0f;
        float centerY = (minY + maxY) / 2.0f;
        float centerZ = (minZ + maxZ) / 2.0f;

        float sizeX = maxX - minX;
        float sizeY = maxY - minY;
        float sizeZ = maxZ - minZ;
        float maxDim = Math.Max(sizeX, Math.Max(sizeY, sizeZ));

        float scaleFactor = maxDim > 0 ? 2.0f / maxDim : 1.0f;

        for (int i = 0; i < VtxsGeometric.Count; i++)
        {
            var v = VtxsGeometric[i];

            float x = (v.X - centerX) * scaleFactor;
            float y = (v.Y - centerY) * scaleFactor;
            float z = (v.Z - centerZ) * scaleFactor;

            VtxsGeometric[i] = new Vector4(x, y, z, v.W);
        }
    }
}