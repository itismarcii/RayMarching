using UnityEngine;

namespace Script.RayMarching
{
    public struct ShapeData {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 size;
        public Vector3 colour;
        public int shapeType;
        public int operation;
        public float blendStrength;
        public int numChildren;

        public static int GetSize () {
            return sizeof(float) * 13 + sizeof(int) * 3;
        }
    }
}