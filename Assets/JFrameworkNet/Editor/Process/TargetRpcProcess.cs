using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal class TargetRpcProcess
    {
        public static MethodDefinition ProcessTargetRpcInvoke(Processor processor, Readers readers, Logger logger, TypeDefinition type, MethodDefinition method, MethodDefinition function, ref bool isFailed)
        {
            string rpcName = Process.GenerateMethodName(CONST.INVOKE_RPC, method);
            MethodDefinition rpc = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = rpc.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkEntityProcess.WriteClientActiveCheck(worker, processor, method.Name, label, "TargetRpc");
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, type);
            
            if (HasNetworkConnectionParameter(method))
            {
                worker.Emit(OpCodes.Ldnull);
            }
            
            if (!NetworkEntityProcess.ReadArguments(method, readers, logger, worker, RemoteType.TargetRpc, ref isFailed))
            {
                return null;
            }
            
            worker.Emit(OpCodes.Callvirt, function);
            worker.Emit(OpCodes.Ret);
            NetworkEntityProcess.AddInvokeParameters(processor, rpc.Parameters);
            type.Methods.Add(rpc);
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