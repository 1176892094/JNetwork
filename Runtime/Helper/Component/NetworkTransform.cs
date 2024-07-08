// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-10  04:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    internal class NetworkTransform : NetworkBehaviour
    {
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
        /// 上一个位置
        /// </summary>
        private Synchronize origin;

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
        /// 修改的玩家位置
        /// </summary>
        private Vector3 fixedPosition;

        /// <summary>
        /// 修改的玩家旋转
        /// </summary>
        private Quaternion fixedRotation;

        /// <summary>
        /// 修改的玩家大小
        /// </summary>
        private Vector3 fixedLocalScale;

        /// <summary>
        /// 同步目标
        /// </summary>
        [SerializeField] private Transform target;

        /// <summary>
        /// 是否同步位置
        /// </summary>
        [TabGroup("Sync"), SerializeField] private bool positionSync = true;

        /// <summary>
        /// 是否同步旋转
        /// </summary>
        [TabGroup("Sync"), SerializeField] private bool rotationSync;

        /// <summary>
        /// 是否同步大小
        /// </summary>
        [TabGroup("Sync"), SerializeField] private bool localScaleSync;

        /// <summary>
        /// 位置平滑度
        /// </summary>
        [TabGroup("Interpolation"), SerializeField, Range(0, 1)]
        private float positionSmooth = 0.5f;

        /// <summary>
        /// 位置平滑度
        /// </summary>
        [TabGroup("Interpolation"), SerializeField, Range(0, 1)]
        private float rotationSmooth = 0.5f;

        /// <summary>
        /// 位置平滑度
        /// </summary>
        [TabGroup("Interpolation"), SerializeField, Range(0, 1)]
        private float localScaleSmooth = 0.5f;

        /// <summary>
        /// 同步位置感知差
        /// </summary>
        [TabGroup("Perceive"), SerializeField, Range(0, 1)]
        private float positionPerceive = 0.01f;

        /// <summary>
        /// 同步旋转感知差
        /// </summary>
        [TabGroup("Perceive"), SerializeField, Range(0, 1)]
        private float rotationPerceive = 0.01f;

        /// <summary>
        /// 同步大小感知差
        /// </summary>
        [TabGroup("Perceive"), SerializeField, Range(0, 1)]
        private float localScalePerceive = 0.01f;

        /// <summary>
        /// 位置更新
        /// </summary>
        private void Update()
        {
            if (isServer)
            {
                if (syncDirection == SyncMode.Server || isOwner || connection == null) return;
                if (positionSync) target.position = Vector3.Lerp(target.position, fixedPosition, 1 - positionSmooth);
                if (rotationSync) target.rotation = Quaternion.Lerp(target.rotation, fixedRotation, 1 - rotationSmooth);
                if (localScaleSync) target.localScale = Vector3.Lerp(target.localScale, fixedLocalScale, 1 - localScaleSmooth);
            }
            else if (isClient)
            {
                if (syncDirection == SyncMode.Client && isOwner) return;
                if (positionSync) target.position = Vector3.Lerp(target.position, fixedPosition, 1 - positionSmooth);
                if (rotationSync) target.rotation = Quaternion.Lerp(target.rotation, fixedRotation, 1 - rotationSmooth);
                if (localScaleSync) target.localScale = Vector3.Lerp(target.localScale, fixedLocalScale, 1 - localScaleSmooth);
            }
        }

        /// <summary>
        /// 延迟更新
        /// </summary>
        private void LateUpdate()
        {
            if (isServer && syncDirection == SyncMode.Server)
            {
                if (!TimeManager.Ticks(NetworkManager.SendRate, ref sendTime)) return;
                var current = new Synchronize(target.position, target.rotation, target.localScale);
                positionChanged = Vector3.SqrMagnitude(origin.position - target.position) > positionPerceive * positionPerceive;
                rotationChanged = Quaternion.Angle(origin.rotation, target.rotation) > rotationPerceive;
                localScaleChanged = Vector3.SqrMagnitude(origin.localScale - target.localScale) > localScalePerceive * localScalePerceive;
                cachedComparison = !positionChanged && !rotationChanged && !localScaleChanged;
                if (cachedComparison && hasSentUnchanged) return;
                var position = positionSync && positionChanged ? current.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? current.rotation : default(Quaternion?);
                var localScale = localScaleSync && localScaleChanged ? current.localScale : default(Vector3?);
                SendToClientRpc(position, rotation, localScale);
                if (cachedComparison)
                {
                    hasSentUnchanged = true;
                }
                else
                {
                    hasSentUnchanged = false;
                    origin = current;
                }
            }
            else if (isClient && NetworkManager.Client.isReady && isOwner && syncDirection == SyncMode.Client)
            {
                if (!TimeManager.Ticks(NetworkManager.SendRate, ref sendTime)) return;
                var current = new Synchronize(target.position, target.rotation, target.localScale);
                positionChanged = Vector3.SqrMagnitude(origin.position - target.position) > positionPerceive * positionPerceive;
                rotationChanged = Quaternion.Angle(origin.rotation, target.rotation) > rotationPerceive;
                localScaleChanged = Vector3.SqrMagnitude(origin.localScale - target.localScale) > localScalePerceive * localScalePerceive;
                cachedComparison = !positionChanged && !rotationChanged && !localScaleChanged;
                if (cachedComparison && hasSentUnchanged) return;
                var position = positionSync && positionChanged ? current.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? current.rotation : default(Quaternion?);
                var localScale = localScaleSync && localScaleChanged ? current.localScale : default(Vector3?);
                SendToServerRpc(position, rotation, localScale);
                if (cachedComparison)
                {
                    hasSentUnchanged = true;
                }
                else
                {
                    hasSentUnchanged = false;
                    origin = current;
                }
            }
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
        /// 序列化 Transform
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="start"></param>
        protected override void OnSerialize(NetworkWriter writer, bool start)
        {
            if (!start) return;
            if (positionSync) writer.WriteVector3(target.localPosition);
            if (rotationSync) writer.WriteQuaternion(target.localRotation);
            if (localScaleSync) writer.WriteVector3(target.localScale);
        }

        /// <summary>
        /// 反序列化 Transform
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        protected override void OnDeserialize(NetworkReader reader, bool start)
        {
            if (!start) return;
            if (positionSync) target.localPosition = reader.ReadVector3();
            if (rotationSync) target.localRotation = reader.ReadQuaternion();
            if (localScaleSync) target.localScale = reader.ReadVector3();
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
            fixedPosition = position ?? target.position;
            fixedRotation = rotation ?? target.rotation;
            fixedLocalScale = localScale ?? target.localScale;
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
            fixedPosition = position ?? target.position;
            fixedRotation = rotation ?? target.rotation;
            fixedLocalScale = localScale ?? target.localScale;
        }

        /// <summary>
        /// 同步坐标
        /// </summary>
        private struct Synchronize
        {
            public readonly Vector3 position;
            public readonly Quaternion rotation;
            public readonly Vector3 localScale;

            public Synchronize(Vector3 position, Quaternion rotation, Vector3 localScale)
            {
                this.position = position;
                this.rotation = rotation;
                this.localScale = localScale;
            }
        }
    }
}