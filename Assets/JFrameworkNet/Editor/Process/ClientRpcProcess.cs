using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal class ClientRpcProcess
    {
        public static MethodDefinition ProcessClientRpc(Processor processor, Writers writers, Readers readers, Logger logger, TypeDefinition type, MethodDefinition md, MethodDefinition func, ref bool isFailed)
        {
            string rpcName = Process.GenerateMethodName(CONST.INVOKE_RPC, md);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkEntityProcess.WriteClientActiveCheck(worker, processor, md.Name, label,"ClientRpc");
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, type);
        
            if (!NetworkEntityProcess.ReadArguments(md, readers, logger, worker, RpcType.ClientRpc, ref isFailed))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkEntityProcess.AddInvokeParameters(processor, rpc.Parameters);
            type.Methods.Add(rpc);
            return rpc;
        }
        
        public static MethodDefinition ProcessClientRpcInvoke(Processor processor, Writers writers, Logger logger, TypeDefinition type, MethodDefinition md, CustomAttribute attribute, ref bool isFailed)
        {
            MethodDefinition rpc = MethodProcess.SubstituteMethod(logger, type, md, ref isFailed);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkEntityProcess.WriteSetupLocals(worker, processor);
            NetworkEntityProcess.WriteGetWriter(worker, processor);
            
            if (!NetworkEntityProcess.WriteArguments(worker, writers, logger, md, RpcType.ClientRpc, ref isFailed))
            {
                return null;
            }
        
            int channel = attribute.GetField("channel", 1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4, channel);
            worker.Emit(OpCodes.Callvirt, processor.sendClientRpcInternal);
            NetworkEntityProcess.WriteReturnWriter(worker, processor);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }
    }
}