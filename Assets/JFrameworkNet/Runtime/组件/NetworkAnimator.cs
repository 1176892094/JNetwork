using System.Linq;
using UnityEngine;
// ReSharper disable All

namespace JFramework.Net
{
     public class NetworkAnimator : NetworkBehaviour
    {
        /// <summary>
        /// 动画控制器
        /// </summary>
        public Animator animator;
        
        /// <summary>
        /// 同步动画速度
        /// </summary>
        [SyncVar(nameof(OnSpeedChanged))] private float animaSpeed;
        
        /// <summary>
        /// 上个动画速度
        /// </summary>
        private float lastSpeed;
        
        /// <summary>
        /// 控制器中 int 类型的动画参数
        /// </summary>
        private int[] lastIntParams;
        
        /// <summary>
        /// 控制器中 bool 类型的动画参数
        /// </summary>
        private bool[] lastBoolParams;
        
        /// <summary>
        /// 控制器中 类型的动画参数
        /// </summary>
        private float[] lastFloatParams;
        
        /// <summary>
        /// 动画控制器的所有参数
        /// </summary>
        private AnimatorControllerParameter[] animatorParams;
        
        /// <summary>
        /// 每个动画的Hash
        /// </summary>
        private int[] animationHash;
        
        /// <summary>
        /// 每个过渡的Hash
        /// </summary>
        private int[] transitionHash;
        
        /// <summary>
        /// 动画控制器每个层级的权重
        /// </summary>
        private float[] layerWeight;
        
        /// <summary>
        /// 下一次发送时间
        /// </summary>
        private double nextSendTime;

        /// <summary>
        /// 是否为客户端权限
        /// </summary>
        private bool authority => syncDirection == SyncMode.ClientToServer;

        /// <summary>
        /// 能否发送
        /// </summary>
        private bool CanSend
        {
            get
            {
                if (isServer)
                {
                    if (!authority || (@object != null && @object.connection == null))
                    {
                        return true;
                    }
                }

                return isOwner && authority;
            }
        }

        /// <summary>
        /// 初始化动画控制器
        /// </summary>
        private void Awake()
        {
            animatorParams = animator.parameters.Where(parameter => !animator.IsParameterControlledByCurve(parameter.nameHash)).ToArray();
            lastIntParams = new int[animatorParams.Length];
            lastFloatParams = new float[animatorParams.Length];
            lastBoolParams = new bool[animatorParams.Length];
            var layerCount = animator.layerCount;
            animationHash = new int[layerCount];
            transitionHash = new int[layerCount];
            layerWeight = new float[layerCount];
        }

        private void FixedUpdate()
        {
            if (!CanSend) return;
            if (!animator.enabled) return;

            CheckSendRate();

            for (int i = 0; i < animator.layerCount; i++)
            {
                if (!CheckAnimStateChanged(out var stateHash, out var normalizedTime, i))
                {
                    continue;
                }

                using var writer = NetworkWriter.Pop();
                WriteParameters(writer);
                SendAnimationMessage(stateHash, normalizedTime, i, layerWeight[i], writer.ToArray());
            }

            CheckSpeed();
        }
        
        /// <summary>
        /// 同步网络变量 动画速度
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnSpeedChanged(float oldValue, float newValue)
        {
            if (isServer || (isOwner && authority)) return;
            animator.speed = newValue;
        }

        /// <summary>
        /// 检测速度是否变化
        /// </summary>
        private void CheckSpeed()
        {
            float newSpeed = animator.speed;
            if (Mathf.Abs(lastSpeed - newSpeed) > 0.001f)
            {
                lastSpeed = newSpeed;
                if (isServer)
                {
                    animaSpeed = newSpeed;
                }
                else if (isClient)
                {
                    SetAnimatorSpeed(newSpeed);
                }
            }
        }

        /// <summary>
        /// 检测动画状态是否改变
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="layerId"></param>
        /// <returns></returns>
        private bool CheckAnimStateChanged(out int stateHash, out float normalizedTime, int layerId)
        {
            bool isChanged = false;
            stateHash = 0;
            normalizedTime = 0;

            float lw = animator.GetLayerWeight(layerId);
            if (Mathf.Abs(lw - layerWeight[layerId]) > 0.001f)
            {
                layerWeight[layerId] = lw;
                isChanged = true;
            }

            if (animator.IsInTransition(layerId))
            {
                var transitionInfo = animator.GetAnimatorTransitionInfo(layerId);
                if (transitionInfo.fullPathHash != transitionHash[layerId])
                {
                    transitionHash[layerId] = transitionInfo.fullPathHash;
                    animationHash[layerId] = 0;
                    return true;
                }

                return isChanged;
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(layerId);
            if (stateInfo.fullPathHash != animationHash[layerId])
            {
                if (animationHash[layerId] != 0)
                {
                    stateHash = stateInfo.fullPathHash;
                    normalizedTime = stateInfo.normalizedTime;
                }

                transitionHash[layerId] = 0;
                animationHash[layerId] = stateInfo.fullPathHash;
                return true;
            }

            return isChanged;
        }

        /// <summary>
        /// 检测能否发送
        /// </summary>
        private void CheckSendRate()
        {
            double now = NetworkTime.localTime;
            if (CanSend && syncInterval >= 0 && now > nextSendTime)
            {
                nextSendTime = now + syncInterval;
                using var writer = NetworkWriter.Pop();
                if (WriteParameters(writer))
                {
                    SendAnimationParamsMessage(writer.ToArray());
                }
            }
        }

        /// <summary>
        /// 发送动画消息
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="layerId"></param>
        /// <param name="weight"></param>
        /// <param name="parameters"></param>
        private void SendAnimationMessage(int stateHash, float normalizedTime, int layerId, float weight, byte[] parameters)
        {
            if (isServer)
            {
                SetAnimForClient(stateHash, normalizedTime, layerId, weight, parameters);
            }
            else if (isClient)
            {
                SetAnimForServer(stateHash, normalizedTime, layerId, weight, parameters);
            }
        }

        /// <summary>
        /// 发送动画参数信息
        /// </summary>
        /// <param name="parameters"></param>
        private void SendAnimationParamsMessage(byte[] parameters)
        {
            if (isServer)
            {
                SetAnimParamsForClient(parameters);
            }
            else if (isClient)
            {
                SetAnimaParamsForServer(parameters);
            }
        }

        /// <summary>
        /// 处理设置动画控制器
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="layerId"></param>
        /// <param name="weight"></param>
        /// <param name="reader"></param>
        private void HandleAnimator(int stateHash, float normalizedTime, int layerId, float weight, NetworkReader reader)
        {
            if (isOwner && authority) return;

            if (stateHash != 0 && animator.enabled)
            {
                animator.Play(stateHash, layerId, normalizedTime);
            }

            animator.SetLayerWeight(layerId, weight);

            ReadParameters(reader);
        }

        /// <summary>
        /// 处理设置参数
        /// </summary>
        /// <param name="reader"></param>
        private void HandleSetParams(NetworkReader reader)
        {
            if (isOwner && authority) return;
            ReadParameters(reader);
        }

        /// <summary>
        /// 处理设置触发器
        /// </summary>
        /// <param name="hash"></param>
        private void HandleSetTrigger(int hash)
        {
            if (animator.enabled)
            {
                animator.SetTrigger(hash);
            }
        }

        /// <summary>
        /// 处理重置触发器
        /// </summary>
        /// <param name="hash"></param>
        private void HandleResetTrigger(int hash)
        {
            if (animator.enabled)
            {
                animator.ResetTrigger(hash);
            }
        }

        /// <summary>
        /// 写入已经改变的动画参数
        /// </summary>
        /// <returns></returns>
        private ulong NextDirty()
        {
            ulong dirty = 0;
            for (int i = 0; i < animatorParams.Length; i++)
            {
                var parameter = animatorParams[i];
                bool changed = false;
                if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    int newIntValue = animator.GetInteger(parameter.nameHash);
                    changed = newIntValue != lastIntParams[i];
                    if (changed)
                    {
                        lastIntParams[i] = newIntValue;
                    }
                }
                else if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = animator.GetFloat(parameter.nameHash);
                    changed = Mathf.Abs(newFloatValue - lastFloatParams[i]) > 0.001f;
                    if (changed)
                    {
                        lastFloatParams[i] = newFloatValue;
                    }
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = animator.GetBool(parameter.nameHash);
                    changed = newBoolValue != lastBoolParams[i];
                    if (changed)
                    {
                        lastBoolParams[i] = newBoolValue;
                    }
                }

                if (changed)
                {
                    dirty |= 1ul << i;
                }
            }

            return dirty;
        }

        /// <summary>
        /// 写入动画控制器参数
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="forceAll"></param>
        /// <returns></returns>
        private bool WriteParameters(NetworkWriter writer, bool forceAll = false)
        {
            ulong dirtyBits = forceAll ? ~0UL : NextDirty();
            writer.WriteULong(dirtyBits);
            for (int i = 0; i < animatorParams.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0) continue;

                var parameter = animatorParams[i];
                if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    int newIntValue = animator.GetInteger(parameter.nameHash);
                    writer.WriteInt(newIntValue);
                }
                else if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = animator.GetFloat(parameter.nameHash);
                    writer.WriteFloat(newFloatValue);
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = animator.GetBool(parameter.nameHash);
                    writer.WriteBool(newBoolValue);
                }
            }
            return dirtyBits != 0;
        }

        /// <summary>
        /// 读取动画控制器参数
        /// </summary>
        /// <param name="reader"></param>
        private void ReadParameters(NetworkReader reader)
        {
            bool animatorEnabled = animator.enabled;
            
            ulong dirtyBits = reader.ReadULong();
            for (int i = 0; i < animatorParams.Length; i++)
            {
                if ((dirtyBits & (1ul << i)) == 0) continue;

                var parameter = animatorParams[i];
                if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    int newIntValue = reader.ReadInt();
                    if (animatorEnabled)
                    {
                        animator.SetInteger(parameter.nameHash, newIntValue);
                    }
                }
                else if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    float newFloatValue = reader.ReadFloat();
                    if (animatorEnabled)
                    {
                        animator.SetFloat(parameter.nameHash, newFloatValue);
                    }
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    bool newBoolValue = reader.ReadBool();
                    if (animatorEnabled)
                    {
                        animator.SetBool(parameter.nameHash, newBoolValue);
                    }
                }
            }
        }

        /// <summary>
        /// 序列化 Animator
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="start"></param>
        protected override void OnSerialize(NetworkWriter writer, bool start)
        {
            base.OnSerialize(writer,start);
            if (!start) return;
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.IsInTransition(i))
                {
                    AnimatorStateInfo stateInfo = animator.GetNextAnimatorStateInfo(i);
                    writer.WriteInt(stateInfo.fullPathHash);
                    writer.WriteFloat(stateInfo.normalizedTime);
                }
                else
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                    writer.WriteInt(stateInfo.fullPathHash);
                    writer.WriteFloat(stateInfo.normalizedTime);
                }

                writer.WriteFloat(animator.GetLayerWeight(i));
            }

            WriteParameters(writer, true);
        }

        /// <summary>
        /// 反序列化 Animator
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="start"></param>
        protected override void OnDeserialize(NetworkReader reader, bool start)
        {
            base.OnDeserialize(reader,start);
            if (!start) return;
            for (int i = 0; i < animator.layerCount; i++)
            {
                int stateHash = reader.ReadInt();
                float normalizedTime = reader.ReadFloat();
                animator.SetLayerWeight(i, reader.ReadFloat());
                animator.Play(stateHash, i, normalizedTime);
            }

            ReadParameters(reader);
        }
        
        /// <summary>
        /// 根据 string 设置触发器
        /// </summary>
        /// <param name="triggerName"></param>
        public void SetTrigger(string triggerName)
        {
            SetTrigger(Animator.StringToHash(triggerName));
        }
        
        /// <summary>
        /// 根据 hash 设置触发器
        /// </summary>
        /// <param name="hash"></param>
        public void SetTrigger(int hash)
        {
            if (authority)
            {
                if (!isClient)
                {
                    Debug.LogWarning("试图在服务器上为客户端控制的动画器设置动画。");
                    return;
                }

                if (!isOwner)
                {
                    Debug.LogWarning("只有拥有权限的客户端才能设置动画。");
                    return;
                }

                if (isClient)
                {
                    SetAnimTriggerForServer(hash);
                }
                
                HandleSetTrigger(hash);
            }
            else
            {
                if (!isServer)
                {
                    Debug.LogWarning("试图在客户端为服务器控制的动画器设置动画。");
                    return;
                }

                HandleSetTrigger(hash);
                SetAnimTriggerForClient(hash);
            }
        }
        
        /// <summary>
        /// 根据 string 重置触发器
        /// </summary>
        /// <param name="triggerName"></param>
        public void ResetTrigger(string triggerName)
        {
            ResetTrigger(Animator.StringToHash(triggerName));
        }

        /// <summary>
        /// 根据 hash 重置触发器
        /// </summary>
        /// <param name="hash"></param>
        public void ResetTrigger(int hash)
        {
            if (authority)
            {
                if (!isClient)
                {
                    Debug.LogWarning("试图在服务器上为客户端控制的动画器重置动画。");
                    return;
                }

                if (!isOwner)
                {
                    Debug.LogWarning("只有拥有权限的客户端才能设置动画。");
                    return;
                }

                if (isClient)
                {
                    ResetAnimTriggerForServer(hash);
                }

                HandleResetTrigger(hash);
            }
            else
            {
                if (!isServer)
                {
                    Debug.LogWarning("试图在客户端重置服务器控制的动画。");
                    return;
                }

                HandleResetTrigger(hash);
                ResetAnimTriggerForClient(hash);
            }
        }
        
        /// <summary>
        /// 设置动画速度到服务器
        /// </summary>
        /// <param name="newSpeed"></param>
        [ServerRpc]
        private void SetAnimatorSpeed(float newSpeed)
        {
            animator.speed = newSpeed;
            animaSpeed = newSpeed;
        }
        
        /// <summary>
        /// 设置动画控制器到服务器
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="layerId"></param>
        /// <param name="weight"></param>
        /// <param name="parameters"></param>
        [ServerRpc]
        private void SetAnimForServer(int stateHash, float normalizedTime, int layerId, float weight, byte[] parameters)
        {
            if (!authority) return;
            using var networkReader = NetworkReader.Pop(parameters);
            HandleAnimator(stateHash, normalizedTime, layerId, weight, networkReader);
            SetAnimForClient(stateHash, normalizedTime, layerId, weight, parameters);
        }

        /// <summary>
        /// 为所有客户端设置动画控制器
        /// </summary>
        /// <param name="stateHash"></param>
        /// <param name="normalizedTime"></param>
        /// <param name="layerId"></param>
        /// <param name="weight"></param>
        /// <param name="param"></param>
        [ClientRpc]
        private void SetAnimForClient(int stateHash, float normalizedTime, int layerId, float weight, byte[] param)
        {
            using var networkReader = NetworkReader.Pop(param);
            HandleAnimator(stateHash, normalizedTime, layerId, weight, networkReader);
        }

        /// <summary>
        /// 设置动画参数到服务器
        /// </summary>
        /// <param name="param"></param>
        [ServerRpc]
        private void SetAnimaParamsForServer(byte[] param)
        {
            if (!authority) return;
            using var networkReader = NetworkReader.Pop(param);
            HandleSetParams(networkReader);
            SetAnimParamsForClient(param);
        }
        
        /// <summary>
        /// 为所有客户端设置动画参数
        /// </summary>
        /// <param name="param"></param>
        [ClientRpc]
        private void SetAnimParamsForClient(byte[] param)
        {
            using var networkReader = NetworkReader.Pop(param);
            HandleSetParams(networkReader);
        }

        /// <summary>
        /// 设置动画触发器到服务器
        /// </summary>
        /// <param name="hash"></param>
        [ServerRpc]
        private void SetAnimTriggerForServer(int hash)
        {
            if (!authority) return;
            if (!isClient && isOwner)
            {
                HandleSetTrigger(hash);
            }

            SetAnimTriggerForClient(hash);
        }
        
        /// <summary>
        /// 为所有客户端设置动画触发器
        /// </summary>
        /// <param name="hash"></param>
        [ClientRpc]
        private void SetAnimTriggerForClient(int hash)
        {
            if (isServer || (authority && isOwner)) return;
            HandleSetTrigger(hash);
        }

        /// <summary>
        /// 重置动画触发器到服务器
        /// </summary>
        /// <param name="hash"></param>
        [ServerRpc]
        private void ResetAnimTriggerForServer(int hash)
        {
            if (!authority) return;
            if (!isClient && isOwner)
            {
                HandleResetTrigger(hash);
            }

            ResetAnimTriggerForClient(hash);
        }

        /// <summary>
        /// 为所有客户端重置动画触发器
        /// </summary>
        /// <param name="hash"></param>
        [ClientRpc]
        private void ResetAnimTriggerForClient(int hash)
        {
            if (isServer || (authority && isOwner)) return;
            HandleResetTrigger(hash);
        }
    }
}