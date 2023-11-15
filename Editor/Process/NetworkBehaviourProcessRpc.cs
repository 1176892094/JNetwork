using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 处理Rpc方法
        /// </summary>
        private void ProcessRpcMethods(ref bool failed)
        {
            var names = new HashSet<string>();
            var methods = new List<MethodDefinition>(generate.Methods);

            foreach (var md in methods)
            {
                foreach (var ca in md.CustomAttributes)
                {
                    if (ca.AttributeType.Is<ServerRpcAttribute>())
                    {
                        ProcessServerRpc(names, md, ca, ref failed);
                        break;
                    }

                    if (ca.AttributeType.Is<TargetRpcAttribute>())
                    {
                        ProcessTargetRpc(names, md, ca, ref failed);
                        break;
                    }

                    if (ca.AttributeType.Is<ClientRpcAttribute>())
                    {
                        ProcessClientRpc(names, md, ca, ref failed);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 处理ClientRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessClientRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("ClientRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.ClientRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            clientRpcList.Add(md);
            var func = NetworkRpcProcess.ProcessClientRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            if (func == null) return;
            var rpcFunc = NetworkRpcProcess.ProcessClientRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                clientRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 处理ServerRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessServerRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("ServerRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.ServerRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            serverRpcList.Add(md);
            var func = NetworkRpcProcess.ProcessServerRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            if (func == null) return;
            var rpcFunc = NetworkRpcProcess.ProcessServerRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                serverRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 处理TargetRpc
        /// </summary>
        /// <param name="names"></param>
        /// <param name="md"></param>
        /// <param name="rpc"></param>
        /// <param name="failed"></param>
        private void ProcessTargetRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool failed)
        {
            if (md.IsAbstract)
            {
                logger.Error("TargetRpc不能作用在抽象方法中。", md);
                failed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.TargetRpc, ref failed))
            {
                return;
            }

            names.Add(md.Name);
            targetRpcList.Add(md);
            var func = NetworkRpcProcess.ProcessTargetRpcInvoke(models, writers, logger, generate, md, rpc, ref failed);
            var rpcFunc = NetworkRpcProcess.ProcessTargetRpc(models, readers, logger, generate, md, func, ref failed);
            if (rpcFunc != null)
            {
                targetRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 判断是否为非静态方法
        /// </summary>
        /// <param name="md"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidMethod(MethodDefinition md, RpcType rpcType, ref bool failed)
        {
            if (md.IsStatic)
            {
                logger.Error($"{md.Name} 方法不能是静态的。", md);
                failed = true;
                return false;
            }

            return IsValidFunc(md, ref failed) && IsValidParams(md, rpcType, ref failed);
        }

        /// <summary>
        /// 判断是否为有效Rpc
        /// </summary>
        /// <param name="mr"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidFunc(MethodReference mr, ref bool failed)
        {
            if (!mr.ReturnType.Is(typeof(void)))
            {
                logger.Error($"{mr.Name} 方法不能有返回值。", mr);
                failed = true;
                return false;
            }

            if (mr.HasGenericParameters)
            {
                logger.Error($"{mr.Name} 方法不能有泛型参数。", mr);
                failed = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断Rpc携带的参数
        /// </summary>
        /// <param name="mr"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidParams(MethodReference mr, RpcType rpcType, ref bool failed)
        {
            for (int i = 0; i < mr.Parameters.Count; ++i)
            {
                ParameterDefinition param = mr.Parameters[i];
                if (!IsValidParam(mr, param, rpcType, i == 0, ref failed))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断Rpc是否为有效参数
        /// </summary>
        /// <param name="method"></param>
        /// <param name="param"></param>
        /// <param name="rpcType"></param>
        /// <param name="firstParam"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidParam(MethodReference method, ParameterDefinition param, RpcType rpcType, bool firstParam, ref bool failed)
        {
            if (param.ParameterType.IsGenericParameter)
            {
                logger.Error($"{method.Name} 方法不能有泛型参数。", method);
                failed = true;
                return false;
            }

            bool connection = param.ParameterType.Is<UnityPeer>();
            bool sendTarget = NetworkRpcProcess.IsSendTarget(param, rpcType);

            if (param.IsOut)
            {
                logger.Error($"{method.Name} 方法不能携带 out 关键字。", method);
                failed = true;
                return false;
            }

            if (!sendTarget && connection && !(rpcType == RpcType.TargetRpc && firstParam))
            {
                logger.Error($"{method.Name} 方法无效的参数 {param}，不能传递网络连接。", method);
                failed = true;
                return false;
            }

            if (param.IsOptional && !sendTarget)
            {
                logger.Error($"{method.Name} 方法不能有可选参数。", method);
                failed = true;
                return false;
            }

            return true;
        }
    }
}