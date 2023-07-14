using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static class TargetRpcProcess
    {
        public static MethodDefinition ProcessTargetRpc(Processor processor, Readers readers, Logger logger, TypeDefinition type, MethodDefinition md, MethodDefinition func, ref bool isFailed)
        {
            string rpcName = Process.GenerateMethodName(CONST.INVOKE_RPC, md);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkEntityProcess.WriteClientActiveCheck(worker, processor, md.Name, label, "TargetRpc");
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, type);
            
            if (HasNetworkConnectionParameter(md))
            {
                worker.Emit(OpCodes.Ldnull);
            }
            
            if (!NetworkEntityProcess.ReadArguments(md, readers, logger, worker, RpcType.TargetRpc, ref isFailed))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkEntityProcess.AddInvokeParameters(processor, rpc.Parameters);
            type.Methods.Add(rpc);
            return rpc;
        }

        public static MethodDefinition ProcessTargetRpcInvoke(Processor processor, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute targetRpcAttr, ref bool isFailed)
        {
            MethodDefinition rpc = MethodProcess.SubstituteMethod(logger, td, md, ref isFailed);

            ILProcessor worker = md.Body.GetILProcessor();

            NetworkEntityProcess.WriteSetupLocals(worker, processor);

            NetworkEntityProcess.WriteGetWriter(worker, processor);
            if (!NetworkEntityProcess.WriteArguments(worker, writers, logger, md, RpcType.TargetRpc, ref isFailed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(HasNetworkConnectionParameter(md) ? OpCodes.Ldarg_1 : OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4, targetRpcAttr.GetField("channel", 0));
            worker.Emit(OpCodes.Callvirt, processor.sendTargetRpcInternal);

            NetworkEntityProcess.WriteReturnWriter(worker, processor);

            worker.Emit(OpCodes.Ret);

            return rpc;
        }
        
        public static bool HasNetworkConnectionParameter(MethodDefinition md)
        {
            if (md.Parameters.Count <= 0) return false;
            TypeReference type = md.Parameters[0].ParameterType;
            return type.Is<NetworkConnection>() || type.IsDerivedFrom<NetworkConnection>();
        }
    }
}