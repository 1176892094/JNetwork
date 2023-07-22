using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static partial class NetworkRpcProcess
    {
        /// <summary>
        /// ClientRpc方法
        /// </summary>
        /// <param name="model"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessClientRpc(Model model, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func)
        {
            string rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.RPC_ATTRS, model.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkClientActive(worker, model, md.Name, label,"ClientRpc");
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);
        
            if (!ReadArguments(md, readers, logger, worker, RpcType.ClientRpc))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(model, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }
        
        /// <summary>
        /// ClientRpc方法体
        /// </summary>
        /// <param name="model"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessClientRpcInvoke(Model model, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute attribute)
        {
            MethodDefinition rpc = BaseRpcMethod(logger, td, md);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteSetupLocals(worker, model);
            NetworkBehaviourProcess.WriteGetWriter(worker, model);
            
            if (!WriteArguments(worker, writers, logger, md, RpcType.ClientRpc))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, attribute.GetField<sbyte>(1));
            worker.Emit(OpCodes.Callvirt, model.sendClientRpcInternal);
            NetworkBehaviourProcess.WriteReturnWriter(worker, model);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }
        
        /// <summary>
        /// ServerRpc方法
        /// </summary>
        /// <param name="model"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessServerRpc(Model model, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func)
        {
            string rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            MethodDefinition cmd = new MethodDefinition(rpcName, CONST.RPC_ATTRS, model.Import(typeof(void)));
            ILProcessor worker = cmd.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkServerActive(worker, model, md.Name, label, "ServerRpc");
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);

            if (!ReadArguments(md, readers, logger, worker, RpcType.ServerRpc))
            {
                return null;
            }

            AddSenderConnection(md, worker);
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(model, cmd.Parameters);
            td.Methods.Add(cmd);
            return cmd;
        }
        
        /// <summary>
        /// ServerRpc方法体
        /// </summary>
        /// <param name="model"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="commandAttr"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessServerRpcInvoke(Model model, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute commandAttr)
        {
            MethodDefinition rpc = BaseRpcMethod(logger, td, md);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteSetupLocals(worker, model);
            NetworkBehaviourProcess.WriteGetWriter(worker, model);

            if (!WriteArguments(worker, writers, logger, md, RpcType.ServerRpc))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, commandAttr.GetField<sbyte>( 1));
            worker.Emit(OpCodes.Call, model.sendServerRpcInternal);
            NetworkBehaviourProcess.WriteReturnWriter(worker, model);
            worker.Emit(OpCodes.Ret);
           
            return rpc;
        }
        
        /// <summary>
        /// 添加发送的连接
        /// </summary>
        /// <param name="method"></param>
        /// <param name="worker"></param>
        private static void AddSenderConnection(MethodDefinition method, ILProcessor worker)
        {
            foreach (var definition in method.Parameters)
            {
                if (IsSenderConnection(definition, RpcType.ServerRpc))
                {
                    worker.Emit(OpCodes.Ldarg_2);
                }
            }
        }

        /// <summary>
        /// TargetRpc方法
        /// </summary>
        /// <param name="model"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessTargetRpc(Model model, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func)
        {
            string rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.RPC_ATTRS, model.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkClientActive(worker, model, md.Name, label, "TargetRpc");
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);
            
            if (HasNetworkConnectionParameter(md))
            {
                worker.Emit(OpCodes.Ldnull);
            }
            
            if (!ReadArguments(md, readers, logger, worker, RpcType.TargetRpc))
            {
                return null;
            }
         
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(model, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }

        /// <summary>
        /// TargetRpc方法体
        /// </summary>
        /// <param name="model"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessTargetRpcInvoke(Model model, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute attr)
        {
            MethodDefinition rpc = BaseRpcMethod(logger, td, md);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteSetupLocals(worker, model);
            NetworkBehaviourProcess.WriteGetWriter(worker, model);
            
            if (!WriteArguments(worker, writers, logger, md, RpcType.TargetRpc))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(HasNetworkConnectionParameter(md) ? OpCodes.Ldarg_1 : OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, attr.GetField<sbyte>( 1));
            worker.Emit(OpCodes.Callvirt, model.sendTargetRpcInternal);
            NetworkBehaviourProcess.WriteReturnWriter(worker, model);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }

        /// <summary>
        /// 判断指定连接参数
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool HasNetworkConnectionParameter(MethodDefinition md)
        {
            if (md.Parameters.Count <= 0) return false;
            TypeReference td = md.Parameters[0].ParameterType;
            return td.Is<Connection>() || td.IsDerivedFrom<Connection>();
        }
        
        /// <summary>
        /// 写入参数
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="method"></param>
        /// <param name="rpcType"></param>
        /// <returns></returns>
        private static bool WriteArguments(ILProcessor worker, Writers writers, Logger logger, MethodDefinition method, RpcType rpcType)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && HasNetworkConnectionParameter(method);
            
            int argNum = 1;
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }
                if (IsSenderConnection(param, rpcType))
                {
                    argNum += 1;
                    continue;
                }

                MethodReference writeFunc = writers.GetWriteFunc(param.ParameterType);
                if (writeFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {param}。不支持类型 {param.ParameterType}。", method);
                    Process.failed = true;
                    return false;
                }
                
                worker.Emit(OpCodes.Ldloc_0);
                worker.Emit(OpCodes.Ldarg, argNum);
                worker.Emit(OpCodes.Call, writeFunc);
                argNum += 1;
            }
            return true;
        }
        
        /// <summary>
        /// 读取参数
        /// </summary>
        /// <param name="method"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="worker"></param>
        /// <param name="rpcType"></param>
        /// <returns></returns>
        private static bool ReadArguments(MethodDefinition method, Readers readers, Logger logger, ILProcessor worker, RpcType rpcType)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && HasNetworkConnectionParameter(method);
            int argNum = 1;
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }
                
                if (IsSenderConnection(param, rpcType))
                {
                    argNum += 1;
                    continue;
                }
                
                MethodReference readFunc = readers.GetReadFunc(param.ParameterType);

                if (readFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {param}。不支持类型 {param.ParameterType}。", method);
                    Process.failed = true;
                    return false;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
   
                if (param.ParameterType.Is<float>())
                {
                    worker.Emit(OpCodes.Conv_R4);
                }
                else if (param.ParameterType.Is<double>())
                {
                    worker.Emit(OpCodes.Conv_R8);
                }
            }

            return true;
        }
        
        /// <summary>
        /// 发送连接
        /// </summary>
        /// <param name="param"></param>
        /// <param name="rpcType"></param>
        /// <returns></returns>
        public static bool IsSenderConnection(ParameterDefinition param, RpcType rpcType)
        {
            if (rpcType != RpcType.ServerRpc)
            {
                return false;
            }

            TypeReference type = param.ParameterType;
            return type.Is<ClientEntity>() || type.Resolve().IsDerivedFrom<ClientEntity>();
        }

        /// <summary>
        /// 注入网络客户端是否活跃
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="model"></param>
        /// <param name="mdName"></param>
        /// <param name="label"></param>
        /// <param name="error"></param>
        private static void NetworkClientActive(ILProcessor worker, Model model, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, model.NetworkClientActiveRef);
            worker.Emit(OpCodes.Brtrue, label);
            worker.Emit(OpCodes.Ldstr, $"{error} 远程调用 {mdName} 方法，但是客户端不是活跃的。");
            worker.Emit(OpCodes.Call, model.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }

        /// <summary>
        /// 注入网络服务器是否活跃
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="model"></param>
        /// <param name="mdName"></param>
        /// <param name="label"></param>
        /// <param name="error"></param>
        private static void NetworkServerActive(ILProcessor worker, Model model, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, model.NetworkServerActiveRef);
            worker.Emit(OpCodes.Brtrue, label);

            worker.Emit(OpCodes.Ldstr, $"{error} 远程调用 {mdName} 方法，但是服务器不是活跃的。");
            worker.Emit(OpCodes.Call, model.logErrorReference);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }
    }
}