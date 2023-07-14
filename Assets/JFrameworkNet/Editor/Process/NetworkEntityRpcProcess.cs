using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal partial class NetworkEntityProcess
    {
        private void ProcessMethods(ref bool WeavingFailed)
        {
            HashSet<string> names = new HashSet<string>();
            List<MethodDefinition> methods = new List<MethodDefinition>(generateCode.Methods);
            foreach (MethodDefinition md in methods)
            {
                foreach (CustomAttribute ca in md.CustomAttributes)
                {
                    if (ca.AttributeType.Is<ServerRpcAttribute>())
                    {
                        ProcessServerRpc(names, md, ca, ref WeavingFailed);
                        break;
                    }

                    if (ca.AttributeType.Is<TargetRpcAttribute>())
                    {
                        ProcessTargetRpc(names, md, ca, ref WeavingFailed);
                        break;
                    }

                    if (ca.AttributeType.Is<ClientRpcAttribute>())
                    {
                        ProcessClientRpc(names, md, ca, ref WeavingFailed);
                        break;
                    }
                }
            }
        }
        
        private void ProcessClientRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool isFailed)
        {
            if (md.IsAbstract)
            {
                logger.Error("ClientRpc不能作用在抽象方法中。", md);
                isFailed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.ClientRpc, ref isFailed))
            {
                return;
            }
            

            names.Add(md.Name);
            clientRpcList.Add(new ClientRpcResult(md));
            MethodDefinition func = ClientRpcProcess.ProcessClientRpcInvoke(processor, writers, logger, generateCode, md, rpc, ref isFailed);
            if (func == null) return;
            MethodDefinition rpcFunc = ClientRpcProcess.ProcessClientRpc(processor, writers, readers, logger, generateCode, md, func, ref isFailed);
            if (rpcFunc != null)
            {
                clientRpcFuncList.Add(rpcFunc);
            }
        }
        
        private void ProcessServerRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool isFailed)
        {
            if (md.IsAbstract)
            { 
                logger.Error("ServerRpc不能作用在抽象方法中。", md);
                isFailed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.ServerRpc, ref isFailed))
            {
                return;
            }
            
            names.Add(md.Name);
            serverRpcList.Add(new ServerRpcResult(md));
            MethodDefinition func = ServerRpcProcess.ProcessServerRpcInvoke(processor, writers, logger, generateCode, md, rpc, ref isFailed);
            if (func == null) return;
            MethodDefinition rpcFunc = ServerRpcProcess.ProcessServerRpc(processor, readers, logger, generateCode, md, func, ref isFailed);
            if (rpcFunc != null)
            {
                serverRpcFuncList.Add(rpcFunc);
            }
        }
        
        private void ProcessTargetRpc(HashSet<string> names, MethodDefinition md, CustomAttribute rpc, ref bool isFailed)
        {
            if (md.IsAbstract)
            {
                logger.Error("TargetRpc不能作用在抽象方法中。", md);
                isFailed = true;
                return;
            }

            if (!IsValidMethod(md, RpcType.TargetRpc, ref isFailed))
            {
                return;
            }

            names.Add(md.Name);
            targetRpcList.Add(md);
            MethodDefinition func = TargetRpcProcess.ProcessTargetRpcInvoke(processor, writers, logger, generateCode, md, rpc, ref isFailed);
            MethodDefinition rpcFunc = TargetRpcProcess.ProcessTargetRpc(processor, readers, logger, generateCode, md, func, ref isFailed);
            if (rpcFunc != null)
            {
                targetRpcFuncList.Add(rpcFunc);
            }
        }

        private bool IsValidMethod(MethodDefinition method, RpcType rpcType, ref bool isFailed)
        {
            if (method.IsStatic)
            {
                logger.Error($"{method.Name} 方法不能是静态的。", method);
                isFailed = true;
                return false;
            }

            return IsValidFunc(method, ref isFailed) && IsValidParams(method, rpcType, ref isFailed);
        }

        private bool IsValidFunc(MethodReference md, ref bool isFailed)
        {
            if (!md.ReturnType.Is(typeof(void)))
            {
                logger.Error($"{md.Name} 方法不能有返回值。", md);
                isFailed = true;
                return false;
            }
            if (md.HasGenericParameters)
            {
                logger.Error($"{md.Name} 方法不能有泛型参数。", md);
                isFailed = true;
                return false;
            }
            return true;
        }

        private bool IsValidParams(MethodReference method, RpcType rpcType, ref bool isFailed)
        {
            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                ParameterDefinition param = method.Parameters[i];
                if (!IsValidParam(method, param, rpcType, i == 0, ref isFailed))
                {
                    return false;
                }
            }
            return true;
        }
        
        private bool IsValidParam(MethodReference method, ParameterDefinition param, RpcType rpcType, bool firstParam, ref bool isFailed)
        {
            if (param.ParameterType.IsGenericParameter)
            {
                logger.Error($"{method.Name} 方法不能有泛型参数。", method);
                isFailed = true;
                return false;
            }

            bool isNetworkConnection = param.ParameterType.Is<NetworkConnection>();
            bool isSenderConnection = IsSenderConnection(param, rpcType);

            if (param.IsOut)
            {
                logger.Error($"{method.Name} 方法不能携带 out 关键字。", method);
                isFailed = true;
                return false;
            }
            
            if (!isSenderConnection && isNetworkConnection && !(rpcType == RpcType.TargetRpc && firstParam))
            {
                logger.Error($"{method.Name} 方法无效的参数 {param}，不能传递网络连接。", method);
                isFailed = true;
                return false;
            }
            
            if (param.IsOptional && !isSenderConnection)
            {
                logger.Error($"{method.Name} 方法不能有可选参数。", method);
                isFailed = true;
                return false;
            }

            return true;
        }
    }
}