using System.Numerics;

namespace Lab2.model;

public class ObjModel
{
    private Vector3 _translation = Vector3.Zero;
    private Vector3 _rotation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;

    private Matrix4x4 _scaleMatrix = Matrix4x4.Identity;
    private Matrix4x4 _translationMatrix = Matrix4x4.Identity;
    private Matrix4x4 _rotationMatrix = Matrix4x4.Identity;

    public List<Vector4> VtxsGeometric { get; } = [];
    public Vector4[] VtxsTransform { get; set; } = [];
    public List<Vector3> VtxsTexture { get; } = [];
    public List<Vector3> VtxsNormal { get; } = [];
    public List<Polygon> Polygons { get; } = [];
    public Vector4[] VtxsWorldTransform { get; private set; } = [];
    public Matrix4x4 ModelMatrix { get; private set; } = Matrix4x4.Identity;

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            _scaleMatrix = Matrix4x4.CreateScale(value);
            UpdateModelMatrix();
            UpdateVtxsWorld();
        }
    }

    public Vector3 Translation
    {
        get => _translation;
        set
        {
            _translation = value;
            _translationMatrix = Matrix4x4.CreateTranslation(value);
            UpdateModelMatrix();
            UpdateVtxsWorld();
        }
    }

    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _rotationMatrix = Matrix4x4.CreateRotationX(value.X) *
                              Matrix4x4.CreateRotationY(value.Y) *
                              Matrix4x4.CreateRotationZ(value.Z);
            UpdateModelMatrix();
            UpdateVtxsWorld();
        }
    }

    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }

    public void Initialize()
    {
        VtxsTransform = new Vector4[VtxsGeometric.Count];
        VtxsWorldTransform = new Vector4[VtxsGeometric.Count];
    }

    public void SetTransform(Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        _translation = translation;
        _translationMatrix = Matrix4x4.CreateTranslation(translation);
        _scale = scale;
        _scaleMatrix = Matrix4x4.CreateScale(scale);
        _rotation = rotation;
        _rotationMatrix = Matrix4x4.CreateRotationX(rotation.X) *
                          Matrix4x4.CreateRotationY(rotation.Y) *
                          Matrix4x4.CreateRotationZ(rotation.Z);
        UpdateModelMatrix();
        UpdateVtxsWorld();
    }

    public void Reset()
    {
        SetTransform(Vector3.Zero, Vector3.Zero, Vector3.One);
    }

    private void UpdateModelMatrix()
    {
        ModelMatrix = _scaleMatrix * _rotationMatrix * _translationMatrix;
    }

    private void UpdateVtxsWorld()
    {
        Parallel.For(0, VtxsGeometric.Count, i =>
        {
            VtxsWorldTransform[i] = Vector4.Transform(VtxsGeometric[i], ModelMatrix);
        });
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