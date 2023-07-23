using System;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    public class SnapshotSetting
    {
        public double bufferTimeMultiplier = 2;
        public float catchupNegativeThreshold = -1;
        public float catchupPositiveThreshold = 1;
        [Range(0, 1)] public double catchupSpeed = 0.02f;
        [Range(0, 1)] public double slowdownSpeed = 0.04f;
        public int driftEmaDuration = 1;
        public bool dynamicAdjustment = true;
        public float dynamicAdjustmentTolerance = 1;
        public int deliveryTimeEmaDuration = 2;
    }
}