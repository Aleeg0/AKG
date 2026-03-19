namespace Lab2.utils;

public class ZBuffer
{
    public float[] Value { get; private set; } = [];

    public void Resize(int width, int height)
    {
        Value = new float[width * height];
    }

    public void Clear()
    {
        Array.Fill(Value, float.MaxValue);
    }
}