using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static class ServerRpcProcess
    {
        public static MethodDefinition ProcessServerRpc(Processor processor, Readers readers, Logger logger, TypeDefinition td, MethodDefinition md, MethodDefinition func)
        {
            string rpcName = Injection.GenerateMethodName(CONST.INVOKE_RPC, md);
            MethodDefinition cmd = new MethodDefinition(rpcName, CONST.METHOD_RPC, processor.Import(typeof(void)));
            ILProcessor worker = cmd.Body.GetILProcessor();
            Instruction label = worker.Create(OpCodes.Nop);
            NetworkEntityProcess.WriteServerActiveCheck(worker, processor, md.Name, label, "ServerRpc");
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Castclass, td);

            if (!NetworkEntityProcess.ReadArguments(md, readers, logger, worker, RpcType.ServerRpc))
            {
                return null;
            }

            AddSenderConnection(md, worker);
            worker.Emit(OpCodes.Callvirt, func);
            worker.Emit(OpCodes.Ret);
            NetworkEntityProcess.AddInvokeParameters(processor, cmd.Parameters);
            td.Methods.Add(cmd);
            return cmd;
        }
        
        public static MethodDefinition ProcessServerRpcInvoke(Processor processor, Writers writers, Logger logger, TypeDefinition td, MethodDefinition md, CustomAttribute commandAttr)
        {
            MethodDefinition rpc = MethodProcess.SubstituteMethod(logger, td, md);
            ILProcessor worker = md.Body.GetILProcessor();
            NetworkEntityProcess.WriteSetupLocals(worker, processor);
            NetworkEntityProcess.WriteGetWriter(worker, processor);

            if (!NetworkEntityProcess.WriteArguments(worker, writers, logger, md, RpcType.ServerRpc))
            {
                return null;
            }

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldstr, md.FullName);
            worker.Emit(OpCodes.Ldc_I4, NetworkEvent.GetHashByName(md.FullName));
            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ldc_I4, commandAttr.GetField("channel", 1));
            worker.Emit(OpCodes.Call, processor.sendServerRpcInternal);
            NetworkEntityProcess.WriteReturnWriter(worker, processor);
            worker.Emit(OpCodes.Ret);
            return rpc;
        }
        
        private static void AddSenderConnection(MethodDefinition method, ILProcessor worker)
        {
            foreach (var definition in method.Parameters)
            {
                if (NetworkEntityProcess.IsSenderConnection(definition, RpcType.ServerRpc))
                {
                    worker.Emit(OpCodes.Ldarg_2);
                }
            }
        }
    }
}