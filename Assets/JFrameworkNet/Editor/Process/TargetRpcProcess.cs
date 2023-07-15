using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static class TargetRpcProcess
    {
        public static MethodDefinition ProcessTargetRpc(Processor processor, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func)
        {
            string rpcName = Injection.GenerateMethodName(CONST.INVOKE_RPC, md);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkBehaviourProcess.WriteClientActiveCheck(worker, processor, md.Name, label, "TargetRpc");
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);
            
            if (HasNetworkConnectionParameter(md))
            {
                worker.Emit(OpCodes.Ldnull);
            }
            
            if (!NetworkBehaviourProcess.ReadArguments(md, readers, logger, worker, RpcType.TargetRpc))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkBehaviourProcess.AddInvokeParameters(processor, rpc.Parameters);
            td.Methods.Add(rpc);
            return rpc;
        }

        public static MethodDefinition ProcessTargetRpcInvoke(Processor processor, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute attr)
        {
            MethodDefinition rpc = MethodProcess.SubstituteMethod(logger, td, md);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkBehaviourProcess.WriteSetupLocals(worker, processor);
            NetworkBehaviourProcess.WriteGetWriter(worker, processor);
            
            if (!NetworkBehaviourProcess.WriteArguments(worker, writers, logger, md, RpcType.TargetRpc))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(HasNetworkConnectionParameter(md) ? OpCodes.Ldarg_1 : OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, (int)NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4, attr.GetField("channel", 1));
            worker.Emit(OpCodes.Callvirt, processor.sendTargetRpcInternal);
            NetworkBehaviourProcess.WriteReturnWriter(worker, processor);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }
        
        public static bool HasNetworkConnectionParameter(MethodDefinition md)
        {
            if (md.Parameters.Count <= 0) return false;
            TypeReference td = md.Parameters[0].ParameterType;
            return td.Is<NetworkConnection>() || td.IsDerivedFrom<NetworkConnection>();
        }
    }
}