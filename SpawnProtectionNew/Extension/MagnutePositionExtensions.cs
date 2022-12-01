using UnityEngine;

namespace SpawnProtectionNew.Extension
{
    public static class MagnutePositionExtensions
    {
        public static bool IsInRange(this Vector3 source, Vector3 target, float sqrValue)
        {
            return (source - target).sqrMagnitude <= sqrValue;
        }
    }
}
