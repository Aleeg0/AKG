using System.Numerics;
using System.Windows.Media;

namespace Lab3.model;

public struct SceneSettings()
{
    public Vector3 LightPosition = new (-4, 10, 4);
    public float AmbientIntensity = 0.2f;
    public float DiffuseIntensity = 1.0f;
    public float ReflectionIntensity = 100.0f;
    public float ReflectionAlpha = 64;
    public Color ModelColor =  Colors.Pink;
}