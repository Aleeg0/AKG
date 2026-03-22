using System.Numerics;
using System.Windows.Media;

namespace Lab3.model;

public struct SceneSettings()
{
    public Vector3 LightDirection = Vector3.Normalize(new (1,1,1));
    public float AmbientIntensity = 0.2f;
    public float DiffuseIntensity = 1.0f;
    public float ReflectionIntensity = 0.9f;
    public float ReflectionAlpha = 8;
    public Color ModelColor =  Colors.Pink;
}