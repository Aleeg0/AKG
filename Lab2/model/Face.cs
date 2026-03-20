using System.Numerics;

namespace Lab2.model;

public struct Face
{
    public Face()
    {
    }

    public List<FaceVtx> FaceVtxs { get; } = [];
}