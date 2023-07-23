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

        public static SnapshotTransform Interpolate(SnapshotTransform start, SnapshotTransform end, double time)
        {
            var position = Vector3.LerpUnclamped(start.position, end.position, (float)time);
            var rotation = Quaternion.SlerpUnclamped(start.rotation, end.rotation, (float)time);
            var localScale = Vector3.LerpUnclamped(start.localScale, end.localScale, (float)time);
            return new SnapshotTransform(0, 0, position, rotation, localScale);
        }
    }
}