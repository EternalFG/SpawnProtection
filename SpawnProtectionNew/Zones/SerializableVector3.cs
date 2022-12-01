using UnityEngine;
using YamlDotNet.Serialization;

namespace SpawnProtectionNew.Zones
{
    public class SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;

        [YamlIgnore]
        public Vector3 Position => new Vector3(X, Y, Z);
        public SerializableVector3(Vector3 position)
        {
            X = position.x;
            Y = position.y;
            Z = position.z;
        }
        public SerializableVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public SerializableVector3() { }
    }
}
