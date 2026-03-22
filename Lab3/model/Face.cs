using System.Numerics;

namespace Lab3.model;

public struct Face
{
    public Face()
    {
    }

    public List<FaceVtx> FaceVtxs { get; } = [];
}