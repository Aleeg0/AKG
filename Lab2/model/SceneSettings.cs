using System.Numerics;
using System.Windows.Media;

namespace Lab2.model;

public struct SceneSettings()
{
    public Vector3 LightDirection = Vector3.Normalize(new (1,1,1));
    public float AmbientIntensity = 0.3f;
    public float DiffuseIntensity = 0.7f;
    public Color ModelColor =  Colors.White;
}