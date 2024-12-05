// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-10  04:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using UnityEngine;

namespace JFramework.Net
{
    public class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// 同步坐标
        /// </summary>
        private struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;

            public TransformData(Vector3 position, Quaternion rotation, Vector3 localScale)
            {
                this.position = position;
                this.rotation = rotation;
                this.localScale = localScale;
            }
        }

        /// <summary>
        /// 比较快照是否变化
        /// </summary>
        private bool cachedComparison;

        /// <summary>
        /// 有发送一个没有改变的位置
        /// </summary>
        private bool hasSentUnchanged;

        /// <summary>
        /// 上一次发送时间
        /// </summary>
        private double sendTime = double.MinValue;

        /// <summary>
        /// 下一个位置
        /// </summary>
        private TransformData mainData;

        /// <summary>
        /// 缓存的位置
        /// </summary>
        private TransformData nextData;

        /// <summary>
        /// 位置是否变化
        /// </summary>
        private bool positionChanged;

        /// <summary>
        /// 旋转是否变化
        /// </summary>
        private bool rotationChanged;

        /// <summary>
        /// 大小是否变化
        /// </summary>
        private bool localScaleChanged;

        /// <summary>
        /// 同步目标
        /// </summary>
        [SerializeField] private Transform target;

        /// <summary>
        /// 是否同步位置
        /// </summary>
        [SerializeField] private bool positionSync = true;

        /// <summary>
        /// 是否同步旋转
        /// </summary>
        [SerializeField] private bool rotationSync;

        /// <summary>
        /// 是否同步大小
        /// </summary>
        [SerializeField] private bool localScaleSync;

        /// <summary>
        /// 同步位置感知差
        /// </summary>
        [SerializeField] private float positionPerceive = 0.01f;

        /// <summary>
        /// 同步旋转感知差
        /// </summary>
        [SerializeField] private float rotationPerceive = 0.01f;

        /// <summary>
        /// 同步大小感知差
        /// </summary>
        [SerializeField] private float localScalePerceive = 0.01f;

        /// <summary>
        /// 设置初始值
        /// </summary>
        private void Awake()
        {
            mainData = new TransformData(target.position, target.rotation, target.localScale);
        }

        /// <summary>
        /// 设置目标
        /// </summary>
        private void OnValidate()
        {
            if (target == null)
            {
                target = transform;
            }
        }

        /// <summary>
        /// 位置更新
        /// </summary>
        private void Update()
        {
            if (isServer)
            {
                if (syncDirection == SyncMode.Server || isOwner || connection == null) return;
                if (positionSync) target.position = mainData.position;
                if (rotationSync) target.rotation = mainData.rotation;
                if (localScaleSync) target.localScale = mainData.localScale;
            }
            else if (isClient)
            {
                if (syncDirection == SyncMode.Client && isOwner) return;
                if (positionSync) target.position = mainData.position;
                if (rotationSync) target.rotation = mainData.rotation;
                if (localScaleSync) target.localScale = mainData.localScale;
            }
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        private void LateUpdate()
        {
            if (isServer && syncDirection == SyncMode.Server)
            {
                if (!NetworkManager.Instance.Tick(ref sendTime)) return;
                nextData = new TransformData(target.position, target.rotation, target.localScale);
                positionChanged = Vector3.SqrMagnitude(mainData.position - target.position) > positionPerceive * positionPerceive;
                rotationChanged = Quaternion.Angle(mainData.rotation, target.rotation) > rotationPerceive;
                localScaleChanged = Vector3.SqrMagnitude(mainData.localScale - target.localScale) > localScalePerceive * localScalePerceive;
                cachedComparison = !positionChanged && !rotationChanged && !localScaleChanged;
                if (cachedComparison && hasSentUnchanged) return;
                hasSentUnchanged = cachedComparison;
                if (!hasSentUnchanged) mainData = nextData;
                var position = positionSync && positionChanged ? nextData.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? nextData.rotation : default(Quaternion?);
                var localScale = localScaleSync && localScaleChanged ? nextData.localScale : default(Vector3?);
                SendToClientRpc(position, rotation, localScale);
            }
            else if (isClient && NetworkManager.Client.isReady && isOwner && syncDirection == SyncMode.Client)
            {
                if (!NetworkManager.Instance.Tick(ref sendTime)) return;
                nextData = new TransformData(target.position, target.rotation, target.localScale);
                positionChanged = Vector3.SqrMagnitude(mainData.position - target.position) > positionPerceive * positionPerceive;
                rotationChanged = Quaternion.Angle(mainData.rotation, target.rotation) > rotationPerceive;
                localScaleChanged = Vector3.SqrMagnitude(mainData.localScale - target.localScale) > localScalePerceive * localScalePerceive;
                cachedComparison = !positionChanged && !rotationChanged && !localScaleChanged;
                if (cachedComparison && hasSentUnchanged) return;
                hasSentUnchanged = cachedComparison;
                if (!hasSentUnchanged) mainData = nextData;
                var position = positionSync && positionChanged ? nextData.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? nextData.rotation : default(Quaternion?);
                var localScale = localScaleSync && localScaleChanged ? nextData.localScale : default(Vector3?);
                SendToServerRpc(position, rotation, localScale);
            }
        }

        /// <summary>
        /// 序列化 Transform
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="status"></param>
        protected override void OnSerialize(NetworkWriter writer, bool status)
        {
            if (!status) return;
            if (positionSync) writer.WriteVector3(target.localPosition);
            if (rotationSync) writer.WriteQuaternion(target.localRotation);
            if (localScaleSync) writer.WriteVector3(target.localScale);
        }

        /// <summary>
        /// 反序列化 Transform
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="status"></param>
        protected override void OnDeserialize(NetworkReader reader, bool status)
        {
            if (!status) return;
            if (positionSync) mainData.position = reader.ReadVector3();
            if (rotationSync) mainData.rotation = reader.ReadQuaternion();
            if (localScaleSync) mainData.localScale = reader.ReadVector3();
        }

        /// <summary>
        /// 客户端到服务器的同步
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localScale"></param>
        [ServerRpc(Channel.Unreliable)]
        private void SendToServerRpc(Vector3? position, Quaternion? rotation, Vector3? localScale)
        {
            mainData.position = position ?? target.position;
            mainData.rotation = rotation ?? target.rotation;
            mainData.localScale = localScale ?? target.localScale;
            if (syncDirection == SyncMode.Server) return;
            SendToClientRpc(position, rotation, localScale);
        }

        /// <summary>
        /// 服务器到客户端的同步
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localScale"></param>
        [ClientRpc(Channel.Unreliable)]
        private void SendToClientRpc(Vector3? position, Quaternion? rotation, Vector3? localScale)
        {
            mainData.position = position ?? target.position;
            mainData.rotation = rotation ?? target.rotation;
            mainData.localScale = localScale ?? target.localScale;
        }

        /// <summary>
        /// 由外部调用并同步 Transform
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="localScale"></param>
        public void SyncTransform(Vector3? position, Quaternion? rotation = null, Vector3? localScale = null)
        {
            mainData.position = position ?? target.position;
            mainData.rotation = rotation ?? target.rotation;
            mainData.localScale = localScale ?? target.localScale;
            target.position = mainData.position;
            target.rotation = mainData.rotation;
            target.localScale = mainData.localScale;
        }
    }
}