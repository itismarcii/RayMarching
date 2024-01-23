namespace Script.RayMarching
{
    public enum ShapeType : byte
    {
        Sphere = 0,
        Cube = 1,
        Cone = 2,       // Not implemented
        Pyramid = 3,    // [EXPERIMENTAL: Broken base]
        Capsule = 4,
        Torus = 5,
        // Prism = 6    // Not implemented
    }
}
