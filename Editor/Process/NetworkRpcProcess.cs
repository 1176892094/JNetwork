using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal enum RpcType : byte
    {
        ServerRpc,
        ClientRpc,
        TargetRpc,
    }

    internal static partial class NetworkRpcProcess
    {
        /// <summary>
        /// ClientRpc方法
        /// </summary>
        /// <param name="models"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessClientRpc(Models models, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func, ref bool failed)
        {
            var rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            var rpc = new MethodDefinition(rpcName, CONST.RPC_ATTRS, models.Import(typeof(void)));
            var worker = rpc.Body.GetILProcessor();
            var label = worker.Create(OpCodes.Nop);
            NetworkClientActive(worker, models, md.Name, label, "ClientRpc");

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);

            if (!ReadArguments(md, readers, logger, worker, RpcType.ClientRpc, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(models, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }

        /// <summary>
        /// ClientRpc方法体
        /// </summary>
        /// <param name="models"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="ca"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessClientRpcInvoke(Models models, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute ca, ref bool failed)
        {
            var rpc = BaseRpcMethod(logger, td, md, ref failed);
            var worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteInitLocals(worker, models);
            NetworkBehaviourProcess.WritePopWriter(worker, models);

            if (!WriteArguments(worker, writers, logger, md, RpcType.ClientRpc, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkMessage.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, ca.GetField<sbyte>(1));
            worker.Emit(OpCodes.Callvirt, models.sendClientRpcInternal);
            NetworkBehaviourProcess.WritePushWriter(worker, models);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }

        /// <summary>
        /// ServerRpc方法
        /// </summary>
        /// <param name="models"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessServerRpc(Models models, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func, ref bool failed)
        {
            var rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            var rpc = new MethodDefinition(rpcName, CONST.RPC_ATTRS, models.Import(typeof(void)));
            var worker = rpc.Body.GetILProcessor();
            var label = worker.Create(OpCodes.Nop);
            NetworkServerActive(worker, models, md.Name, label, "ServerRpc");

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);

            if (!ReadArguments(md, readers, logger, worker, RpcType.ServerRpc, ref failed))
            {
                return null;
            }

            AddSenderConnection(md, worker);
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(models, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }

        /// <summary>
        /// ServerRpc方法体
        /// </summary>
        /// <param name="models"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="commandAttr"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessServerRpcInvoke(Models models, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute commandAttr, ref bool failed)
        {
            var rpc = BaseRpcMethod(logger, td, md, ref failed);
            var worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteInitLocals(worker, models);
            NetworkBehaviourProcess.WritePopWriter(worker, models);

            if (!WriteArguments(worker, writers, logger, md, RpcType.ServerRpc, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkMessage.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, commandAttr.GetField<sbyte>(1));
            worker.Emit(OpCodes.Call, models.sendServerRpcInternal);
            NetworkBehaviourProcess.WritePushWriter(worker, models);
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
                if (IsSendTarget(definition, RpcType.ServerRpc))
                {
                    worker.Emit(OpCodes.Ldarg_2);
                }
            }
        }

        /// <summary>
        /// TargetRpc方法
        /// </summary>
        /// <param name="models"></param>
        /// <param name="readers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessTargetRpc(Models models, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func, ref bool failed)
        {
            var rpcName = Process.GenerateMethodName(CONST.INV_METHOD, md);
            var rpc = new MethodDefinition(rpcName, CONST.RPC_ATTRS, models.Import(typeof(void)));
            var worker = rpc.Body.GetILProcessor();
            var label = worker.Create(OpCodes.Nop);
            NetworkClientActive(worker, models, md.Name, label, "TargetRpc");

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);

            if (HasConnectionParameter(md))
            {
                worker.Emit(OpCodes.Ldnull);
            }

            if (!ReadArguments(md, readers, logger, worker, RpcType.TargetRpc, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(models, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }

        /// <summary>
        /// TargetRpc方法体
        /// </summary>
        /// <param name="models"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="attr"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public static MethodDefinition ProcessTargetRpcInvoke(Models models, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute attr, ref bool failed)
        {
            var rpc = BaseRpcMethod(logger, td, md, ref failed);
            var worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteInitLocals(worker, models);
            NetworkBehaviourProcess.WritePopWriter(worker, models);

            if (!WriteArguments(worker, writers, logger, md, RpcType.TargetRpc, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(HasConnectionParameter(md) ? OpCodes.Ldarg_1 : OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkMessage.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4_S, attr.GetField<sbyte>(1));
            worker.Emit(OpCodes.Callvirt, models.sendTargetRpcInternal);
            NetworkBehaviourProcess.WritePushWriter(worker, models);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }

        /// <summary>
        /// 判断指定连接参数
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool HasConnectionParameter(MethodDefinition md)
        {
            if (md.Parameters.Count <= 0) return false;
            TypeReference td = md.Parameters[0].ParameterType;
            return td.Is<NetworkPeer>() || td.IsDerivedFrom<NetworkPeer>();
        }

        /// <summary>
        /// 写入参数
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="writers"></param>
        /// <param name="logger"></param>
        /// <param name="method"></param>
        /// <param name="rpcType"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private static bool WriteArguments(ILProcessor worker, Writers writers, Logger logger, MethodDefinition method, RpcType rpcType, ref bool failed)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && HasConnectionParameter(method);

            int argNum = 1;
            foreach (var pd in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }

                if (IsSendTarget(pd, rpcType))
                {
                    argNum += 1;
                    continue;
                }

                MethodReference writeFunc = writers.GetWriteFunc(pd.ParameterType, ref failed);
                if (writeFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {pd}。不支持类型 {pd.ParameterType}。", method);
                    failed = true;
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
        /// <param name="failed"></param>
        /// <returns></returns>
        private static bool ReadArguments(MethodDefinition method, Readers readers, Logger logger, ILProcessor worker, RpcType rpcType, ref bool failed)
        {
            bool skipFirst = rpcType == RpcType.TargetRpc && HasConnectionParameter(method);
            int argNum = 1;
            foreach (var pd in method.Parameters)
            {
                if (argNum == 1 && skipFirst)
                {
                    argNum += 1;
                    continue;
                }

                if (IsSendTarget(pd, rpcType))
                {
                    argNum += 1;
                    continue;
                }

                MethodReference readFunc = readers.GetReadFunc(pd.ParameterType, ref failed);

                if (readFunc == null)
                {
                    logger.Error($"{method.Name} 有无效的参数 {pd}。不支持类型 {pd.ParameterType}。", method);
                    failed = true;
                    return false;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);

                if (pd.ParameterType.Is<float>())
                {
                    worker.Emit(OpCodes.Conv_R4);
                }
                else if (pd.ParameterType.Is<double>())
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
        public static bool IsSendTarget(ParameterDefinition param, RpcType rpcType)
        {
            if (rpcType != RpcType.ServerRpc)
            {
                return false;
            }

            TypeReference type = param.ParameterType;
            return type.Is<NetworkClient>() || type.Resolve().IsDerivedFrom<NetworkClient>();
        }

        /// <summary>
        /// 注入网络客户端是否活跃
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="models"></param>
        /// <param name="mdName"></param>
        /// <param name="label"></param>
        /// <param name="error"></param>
        private static void NetworkClientActive(ILProcessor worker, Models models, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, models.NetworkClientActiveRef);
            worker.Emit(OpCodes.Brtrue, label);
            worker.Emit(OpCodes.Ldstr, $"{error} 远程调用 {mdName} 方法，但是客户端不是活跃的。");
            worker.Emit(OpCodes.Call, models.logErrorRef);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }

        /// <summary>
        /// 注入网络服务器是否活跃
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="models"></param>
        /// <param name="mdName"></param>
        /// <param name="label"></param>
        /// <param name="error"></param>
        private static void NetworkServerActive(ILProcessor worker, Models models, string mdName, Instruction label, string error)
        {
            worker.Emit(OpCodes.Call, models.NetworkServerActiveRef);
            worker.Emit(OpCodes.Brtrue, label);

            worker.Emit(OpCodes.Ldstr, $"{error} 远程调用 {mdName} 方法，但是服务器不是活跃的。");
            worker.Emit(OpCodes.Call, models.logErrorRef);
            worker.Emit(OpCodes.Ret);
            worker.Append(label);
        }
    }
}