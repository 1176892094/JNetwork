using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JFramework.Net
{
    internal class NetworkTransform : NetworkBehaviour
    {
        /// <summary>
        /// 客户端快照序列
        /// </summary>
        private readonly SortedList<double, SnapshotTransform> clientSnapshots = new SortedList<double, SnapshotTransform>();
        
        /// <summary>
        /// 服务器快照序列
        /// </summary>
        private readonly SortedList<double, SnapshotTransform> serverSnapshots = new SortedList<double, SnapshotTransform>();

        /// <summary>
        /// 是否拥有权限
        /// </summary>
        private bool isAuthority => isClient ? syncDirection == SyncMode.ClientToServer && isOwner : syncDirection == SyncMode.ServerToClient;
        
        /// <summary>
        /// 时间戳调整
        /// </summary>
        private double timeStampAdjustment => NetworkManager.Instance.sendRate * (sendIntervalMultiplier - 1);

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
        private bool scaleChanged;
        
        /// <summary>
        /// 发送间隔计数
        /// </summary>
        private uint sendIntervalCounter;
        
        /// <summary>
        /// 比较快照是否变化
        /// </summary>
        private bool cachedSnapshotComparison;
        
        /// <summary>
        /// 有发送一个没有改变的位置
        /// </summary>
        private bool hasSentUnchangedPosition;
        
        /// <summary>
        /// 上一次发送时间
        /// </summary>
        private double lastSendIntervalTime = double.MinValue;
        
        /// <summary>
        /// 上一个快照
        /// </summary>
        private SnapshotTransform lastSnapshot;
        
        /// <summary>
        /// 发送间隔乘数
        /// </summary>
        private readonly uint sendIntervalMultiplier = 1;
        
        /// <summary>
        /// 缓存重置次数
        /// </summary>
        private readonly float bufferResetMultiplier = 3;

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
        [TabGroup("Sync"), SerializeField] private bool scaleSync;
        
        /// <summary>
        /// 同步位置使用差值
        /// </summary>
        [TabGroup("Interpolate"), SerializeField] private bool positionInterpolate = true;
        
        /// <summary>
        /// 同步旋转使用差值
        /// </summary>
        [TabGroup("Interpolate"), SerializeField] private bool rotationInterpolate;
        
        /// <summary>
        /// 同步大小使用差值
        /// </summary>
        [TabGroup("Interpolate"), SerializeField] private bool scaleInterpolate;
        
        /// <summary>
        /// 同步位置感知差
        /// </summary>
        [TabGroup("Sensitivity"), SerializeField] private float positionSensitivity = 0.01f;
        
        /// <summary>
        /// 同步旋转感知差
        /// </summary>
        [TabGroup("Sensitivity"), SerializeField] private float rotationSensitivity = 0.01f;
        
        /// <summary>
        /// 同步大小感知差
        /// </summary>
        [TabGroup("Sensitivity"), SerializeField] private float scaleSensitivity = 0.01f;

        private void Update()
        {
            if (isServer)
            {
                UpdateServerInterpolation();
            }
            else if (isClient && !isAuthority)
            {
                UpdateClientInterpolation();
            }
        }
        
        private void LateUpdate()
        {
            if (isServer)
            {
                UpdateServerBroadcast();
            }
            else if (isClient && isAuthority)
            {
                UpdateClientBroadcast();
            }
        }

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
            if (scaleSync) writer.WriteVector3(target.localScale);
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
            if (scaleSync) target.localScale = reader.ReadVector3();
        }
        
        /// <summary>
        /// 更新服务器差值
        /// </summary>
        private void UpdateServerInterpolation()
        {
            if (serverSnapshots.Count == 0 || syncDirection != SyncMode.ClientToServer || connection == null || isOwner) return;
            SnapshotUtils.StepInterpolation(serverSnapshots, connection.remoteTimeline, out SnapshotTransform start, out SnapshotTransform end, out double t);
            Apply(SnapshotTransform.Interpolate(start, end, t), end);
        }
        
        
        /// <summary>
        /// 更新客户端差值
        /// </summary>
        private void UpdateClientInterpolation()
        {
            if (clientSnapshots.Count == 0) return;
            SnapshotUtils.StepInterpolation(clientSnapshots, NetworkTime.fixedTime, out SnapshotTransform start, out SnapshotTransform end, out double t);
            Apply(SnapshotTransform.Interpolate(start, end, t), end);
        }
        
        /// <summary>
        /// 应用同步位置
        /// </summary>
        /// <param name="interpolate"></param>
        /// <param name="end"></param>
        private void Apply(SnapshotTransform interpolate, SnapshotTransform end)
        {
            if (positionSync)
            {
                target.localPosition = positionInterpolate ? interpolate.position : end.position;
            }

            if (rotationSync)
            {
                target.localRotation = rotationInterpolate ? interpolate.rotation : end.rotation;
            }

            if (scaleSync)
            {
                target.localScale = scaleInterpolate ? interpolate.localScale : end.localScale;
            }
        }
        
        /// <summary>
        /// 更新服务器广播
        /// </summary>
        private void UpdateServerBroadcast()
        {
            CheckSendTime();

            if (sendIntervalCounter == sendIntervalMultiplier && (syncDirection == SyncMode.ServerToClient || isAuthority))
            {
                var snapshot = Construct();
                cachedSnapshotComparison = CompareSnapshots(snapshot);
                if (cachedSnapshotComparison && hasSentUnchangedPosition) return;
                var position = positionSync && positionChanged ? snapshot.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? snapshot.rotation : default(Quaternion?);
                var localScale = scaleSync && scaleChanged ? snapshot.localScale : default(Vector3?);
                RpcServerToClientSync(position, rotation, localScale);

                if (cachedSnapshotComparison)
                {
                    hasSentUnchangedPosition = true;
                }
                else
                {
                    hasSentUnchangedPosition = false;
                    lastSnapshot = snapshot;
                }
            }
        }
        
        /// <summary>
        /// 更新客户端广播
        /// </summary>
        private void UpdateClientBroadcast()
        {
            if (!NetworkManager.Client.isReady) return;
            CheckSendTime();
            if (sendIntervalCounter == sendIntervalMultiplier)
            {
                var snapshot = Construct();
                cachedSnapshotComparison = CompareSnapshots(snapshot);
                if (cachedSnapshotComparison && hasSentUnchangedPosition) return;
                var position = positionSync && positionChanged ? snapshot.position : default(Vector3?);
                var rotation = rotationSync && rotationChanged ? snapshot.rotation : default(Quaternion?);
                var localScale = scaleSync && scaleChanged ? snapshot.localScale : default(Vector3?);
                ClientToServerSync(position, rotation, localScale);

                if (cachedSnapshotComparison)
                {
                    hasSentUnchangedPosition = true;
                }
                else
                {
                    hasSentUnchangedPosition = false;
                    lastSnapshot = snapshot;
                }
            }
        }


        /// <summary>
        /// 检测上一次发送时间
        /// </summary>
        private void CheckSendTime()
        {
            if (sendIntervalCounter == sendIntervalMultiplier)
            {
                sendIntervalCounter = 0;
            }

            if (NetworkUtils.HeartBeat(NetworkTime.localTime, NetworkManager.Instance.sendRate, ref lastSendIntervalTime))
            {
                sendIntervalCounter++;
            }
        }
        
        /// <summary>
        /// 构建 Transform 的快照
        /// </summary>
        /// <returns></returns>
        private SnapshotTransform Construct()
        {
            return new SnapshotTransform(NetworkTime.localTime, 0, target.localPosition, target.localRotation, target.localScale);
        }
        
        /// <summary>
        /// 快照值对比
        /// </summary>
        /// <param name="currentSnapshot"></param>
        /// <returns></returns>
        private bool CompareSnapshots(SnapshotTransform currentSnapshot)
        {
            positionChanged = Vector3.SqrMagnitude(lastSnapshot.position - currentSnapshot.position) > positionSensitivity * positionSensitivity;
            rotationChanged = Quaternion.Angle(lastSnapshot.rotation, currentSnapshot.rotation) > rotationSensitivity;
            scaleChanged = Vector3.SqrMagnitude(lastSnapshot.localScale - currentSnapshot.localScale) > scaleSensitivity * scaleSensitivity;
            return !positionChanged && !rotationChanged && !scaleChanged;
        }
        
        /// <summary>
        /// 客户端到服务器的同步
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        [ServerRpc(Channel.Unreliable)]
        private void ClientToServerSync(Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            OnClientToServerSync(position, rotation, scale);
            if (syncDirection == SyncMode.ClientToServer)
            {
                RpcServerToClientSync(position, rotation, scale);
            }
        }
        
        /// <summary>
        /// 当客户端同步到服务器
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void OnClientToServerSync(Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            if (syncDirection != SyncMode.ClientToServer) return;
            if (serverSnapshots.Count >= connection.snapshotBufferSizeLimit) return;
            double remoteTime = connection.remoteTime;
            double timeIntervalCheck = bufferResetMultiplier * sendIntervalMultiplier * NetworkManager.Instance.sendRate;

            if (serverSnapshots.Count > 0 && serverSnapshots.Values[serverSnapshots.Count - 1].remoteTime + timeIntervalCheck < remoteTime)
            {
                Reset();
            }
            AddSnapshot(serverSnapshots, connection.remoteTime + timeStampAdjustment, position, rotation, scale);
        }
        
        /// <summary>
        /// 服务器到客户端的同步
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        [ClientRpc(Channel.Unreliable)]
        private void RpcServerToClientSync(Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            OnServerToClientSync(position, rotation, scale);
        }

        /// <summary>
        /// 当服务器同步到客户端
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void OnServerToClientSync(Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            if (isServer || isAuthority) return;
            double remote = NetworkManager.Client.connection.remoteTime;
            double timeIntervalCheck = bufferResetMultiplier * sendIntervalMultiplier * NetworkManager.Instance.sendRate;
            if (clientSnapshots.Count > 0 && clientSnapshots.Values[clientSnapshots.Count - 1].remoteTime + timeIntervalCheck < remote)
            {
                Reset();
            }
            
            AddSnapshot(clientSnapshots, NetworkManager.Client.connection.remoteTime + timeStampAdjustment, position, rotation, scale);
        }
        
        /// <summary>
        /// 添加快照
        /// </summary>
        /// <param name="snapshots"></param>
        /// <param name="timeStamp"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void AddSnapshot(SortedList<double, SnapshotTransform> snapshots, double timeStamp, Vector3? position, Quaternion? rotation, Vector3? scale)
        {
            position ??= snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].position : target.localPosition;
            rotation ??= snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].rotation : target.localRotation;
            scale ??= snapshots.Count > 0 ? snapshots.Values[snapshots.Count - 1].localScale : target.localScale;
            SnapshotUtils.InsertIfNotExists(snapshots, new SnapshotTransform(timeStamp, NetworkTime.localTime, position.Value, rotation.Value, scale.Value));
        }
        
        /// <summary>
        /// 重置快照列表
        /// </summary>
        public void Reset()
        {
            serverSnapshots.Clear();
            clientSnapshots.Clear();
        }
    }
}