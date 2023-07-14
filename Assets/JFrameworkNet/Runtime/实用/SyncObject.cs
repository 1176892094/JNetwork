using System;

namespace JFramework.Net
{
    /// <summary>
    /// 网络同步对戏那个
    /// </summary>
    public abstract class SyncObject
    {
        public Action OnDirty;
        public Func<bool> IsRecording = () => true;
        public Func<bool> IsWritable = () => true;
        public abstract void ClearChanges();
        public abstract void OnSerializeAll(NetworkWriter writer);
        public abstract void OnSerializeDelta(NetworkWriter writer);
        public abstract void OnDeserializeAll(NetworkReader reader);
        public abstract void OnDeserializeDelta(NetworkReader reader);
        public abstract void Reset();
    }
}