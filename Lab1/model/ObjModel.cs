using System.Numerics;

namespace Lab1.model;

public class ObjModel
{
    public List<Vector4> VtxsGeometric { get; } = [];
    public List<Vector3> VtxsTexture { get; } = [];
    public List<Vector3> VtxsNormal { get; } = [];
    public List<Polygon> Polygons { get; } = [];
}