using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal class ClientRpcProcess
    {
        public static MethodDefinition ProcessRpcInvoke(Processor processor, Writers writers, Readers readers, Logger logger, TypeDefinition type, MethodDefinition method, MethodDefinition function, ref bool isFailed)
        {
            string rpcName = Process.GenerateMethodName(CONST.INVOKE_RPC, method);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkEntityProcess.WriteClientActiveCheck(worker, processor, method.Name, label,"ClientRpc");
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, type);
        
            if (!NetworkEntityProcess.ReadArguments(method, readers, logger, worker, RemoteType.ClientRpc, ref isFailed))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, function);
            worker.Emit(OpCodes.Ret);
            NetworkEntityProcess.AddInvokeParameters(processor, rpc.Parameters);
            type.Methods.Add(rpc);
            return rpc;
        }
        
        // public static MethodDefinition ProcessRpcCall(Processor processor, Writers writers, Logger logger, TypeDefinition type, MethodDefinition method, CustomAttribute attribute, ref bool isFailed)
        // {
        //     MethodDefinition rpc = MethodProcessor.SubstituteMethod(logger, type, method, ref isFailed);
        //     ILProcessor worker = method.Body.GetILProcessor();
        //     NetworkEntityProcess.WriteSetupLocals(worker, processor);
        //     NetworkEntityProcess.WriteGetWriter(worker, processor);
        //     
        //     if (!NetworkEntityProcess.WriteArguments(worker, writers, logger, method, RemoteType.ClientRpc, ref isFailed))
        //     {
        //         return null;
        //     }
        //
        //     int channel = attribute.GetField("channel", 0);
        //     worker.Emit(OpCodes.Ldarg_0);
        //     worker.Emit(OpCodes.Ldstr, method.FullName);
        //     worker.Emit(OpCodes.Ldc_I4, NetworkEvent.GetHashByName(method.FullName));
        //     worker.Emit(OpCodes.Ldloc_0);
        //     worker.Emit(OpCodes.Ldc_I4, channel);
        //     worker.Emit(OpCodes.Callvirt, processor.sendRpcInternal);
        //     NetworkEntityProcess.WriteReturnWriter(worker, processor);
        //     worker.Emit(OpCodes.Ret);
        //     return rpc;
        // }
    }
}