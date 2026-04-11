namespace Lab5.model;

public struct FaceTrg(FaceVtx v0, FaceVtx v1, FaceVtx v2)
{
    public FaceVtx V0 { get; set; } = v0;
    public FaceVtx V1 { get; set; } = v1;
    public FaceVtx V2 { get; set; } = v2;

    public void Deconstruct(out FaceVtx v0, out FaceVtx v1, out FaceVtx v2)
    {
        v0 = V0;
        v1 = V1;
        v2 = V2;
    }
}