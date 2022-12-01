using YamlDotNet.Serialization;

namespace SpawnProtectionNew.Zones
{
    public class ZoneData
    {
        public SerializableVector3 Point;

        public string Name;

        public float Range;

        [YamlIgnore]
        public float SqrRange => Range * Range;

        public ZoneData(SerializableVector3 point, string name, float range)
        {
            Point = point;
            Name = name;
            Range = range;
        }

        public ZoneData() { }

    }
}
