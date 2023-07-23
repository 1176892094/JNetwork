using UnityEngine;

namespace JFramework.Net
{
    internal struct SnapshotTransform : Snapshot
    {
        public double remoteTime { get; }
        public double localTime { get; }

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;

        public SnapshotTransform(double remoteTime, double localTime, Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            this.remoteTime = remoteTime;
            this.localTime = localTime;
            this.position = position;
            this.rotation = rotation;
            this.localScale = localScale;
        }

        public static SnapshotTransform Interpolate(SnapshotTransform origin, SnapshotTransform target, double time)
        {
            var position = Vector3.LerpUnclamped(origin.position, target.position, (float)time);
            var rotation = Quaternion.SlerpUnclamped(origin.rotation, target.rotation, (float)time);
            var localScale = Vector3.LerpUnclamped(origin.localScale, target.localScale, (float)time);
            return new SnapshotTransform(0, 0, position, rotation, localScale);
        }
    }
}