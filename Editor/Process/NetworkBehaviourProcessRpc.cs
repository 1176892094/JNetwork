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
            HashSet<string> names = new HashSet<string>();
            List<MethodDefinition> methods = new List<MethodDefinition>(generateCode.Methods);

            foreach (MethodDefinition md in methods)
            {
                foreach (CustomAttribute ca in md.CustomAttributes)
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
            MethodDefinition func =
                NetworkRpcProcess.ProcessClientRpcInvoke(models, writers, logger, generateCode, md, rpc, ref failed);
            if (func == null) return;
            MethodDefinition rpcFunc =
                NetworkRpcProcess.ProcessClientRpc(models, readers, logger, generateCode, md, func, ref failed);
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
            MethodDefinition func =
                NetworkRpcProcess.ProcessServerRpcInvoke(models, writers, logger, generateCode, md, rpc, ref failed);
            if (func == null) return;
            MethodDefinition rpcFunc =
                NetworkRpcProcess.ProcessServerRpc(models, readers, logger, generateCode, md, func, ref failed);
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
            MethodDefinition func = NetworkRpcProcess.ProcessTargetRpcInvoke(models, writers, logger, generateCode, md, rpc, ref failed);
            MethodDefinition rpcFunc = NetworkRpcProcess.ProcessTargetRpc(models, readers, logger, generateCode, md, func, ref failed);
            if (rpcFunc != null)
            {
                targetRpcFuncList.Add(rpcFunc);
            }
        }

        /// <summary>
        /// 判断是否为非静态方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidMethod(MethodDefinition method, RpcType rpcType, ref bool failed)
        {
            if (method.IsStatic)
            {
                logger.Error($"{method.Name} 方法不能是静态的。", method);
                failed = true;
                return false;
            }

            return IsValidFunc(method, ref failed) && IsValidParams(method, rpcType, ref failed);
        }

        /// <summary>
        /// 判断是否为有效Rpc
        /// </summary>
        /// <param name="md"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidFunc(MethodReference md, ref bool failed)
        {
            if (!md.ReturnType.Is(typeof(void)))
            {
                logger.Error($"{md.Name} 方法不能有返回值。", md);
                failed = true;
                return false;
            }

            if (md.HasGenericParameters)
            {
                logger.Error($"{md.Name} 方法不能有泛型参数。", md);
                failed = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 判断Rpc携带的参数
        /// </summary>
        /// <param name="method"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private bool IsValidParams(MethodReference method, RpcType rpcType, ref bool failed)
        {
            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                ParameterDefinition param = method.Parameters[i];
                if (!IsValidParam(method, param, rpcType, i == 0, ref failed))
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
        private bool IsValidParam(MethodReference method, ParameterDefinition param, RpcType rpcType, bool firstParam,
            ref bool failed)
        {
            if (param.ParameterType.IsGenericParameter)
            {
                logger.Error($"{method.Name} 方法不能有泛型参数。", method);
                failed = true;
                return false;
            }

            bool isNetworkConnection = param.ParameterType.Is<Connection>();
            bool isSenderConnection = NetworkRpcProcess.IsSenderConnection(param, rpcType);

            if (param.IsOut)
            {
                logger.Error($"{method.Name} 方法不能携带 out 关键字。", method);
                failed = true;
                return false;
            }

            if (!isSenderConnection && isNetworkConnection && !(rpcType == RpcType.TargetRpc && firstParam))
            {
                logger.Error($"{method.Name} 方法无效的参数 {param}，不能传递网络连接。", method);
                failed = true;
                return false;
            }

            if (param.IsOptional && !isSenderConnection)
            {
                logger.Error($"{method.Name} 方法不能有可选参数。", method);
                failed = true;
                return false;
            }

            return true;
        }
    }
}