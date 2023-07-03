using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private double lastSyncTime;
        private ulong syncVarDirtyBits;
        private ulong syncObjectDirtyBits;
        private readonly List<SyncObject> syncObjects = new List<SyncObject>();
        public void ClearAllDirtyBits()
        {
            lastSyncTime = NetworkTime.localTime;
            syncVarDirtyBits = 0L;
            syncObjectDirtyBits = 0L;
            
            foreach (var obj in syncObjects)
            {
                obj.ClearChanges();
            }
        }
    }
}