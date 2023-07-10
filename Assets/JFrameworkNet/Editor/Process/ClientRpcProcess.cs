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
    }
}