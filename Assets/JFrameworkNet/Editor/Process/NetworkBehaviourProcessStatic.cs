using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private void InjectIntoStaticConstructor()
        {
            if (serverRpcList.Count == 0 && clientRpcList.Count == 0 && targetRpcList.Count == 0) return;
            MethodDefinition cctor = generateCode.GetMethod(".cctor");
            bool cctorFound = cctor != null;
            if (cctor != null)
            {
                if (!RemoveFinalRetInstruction(cctor))
                {
                    logger.Error($"{generateCode.Name} 无效的静态构造函数。", cctor);
                    Command.failed = true;
                    return;
                }
            }
            else
            {
                cctor = new MethodDefinition(".cctor",CONST.CTOR_ATTRS, process.Import(typeof(void)));
            }

            ILProcessor cctorWorker = cctor.Body.GetILProcessor();
            for (int i = 0; i < serverRpcList.Count; ++i)
            {
                ServerRpcResult cmdResult = serverRpcList[i];
                GenerateRegisterServerRpcDelegate(cctorWorker, process.registerServerRpcReference, serverRpcFuncList[i], cmdResult);
            }
            
            for (int i = 0; i < clientRpcList.Count; ++i)
            {
                ClientRpcResult clientRpcResult = clientRpcList[i];
                GenerateRegisterClientRpcDelegate(cctorWorker, process.registerClientRpcReference, clientRpcFuncList[i], clientRpcResult.method.FullName);
            }
            
            for (int i = 0; i < targetRpcList.Count; ++i)
            {
                GenerateRegisterClientRpcDelegate(cctorWorker, process.registerClientRpcReference, targetRpcFuncList[i], targetRpcList[i].FullName);
            }
            
            cctorWorker.Append(cctorWorker.Create(OpCodes.Ret));
            if (!cctorFound)
            {
                generateCode.Methods.Add(cctor);
            }
            
            generateCode.Attributes &= ~TypeAttributes.BeforeFieldInit;
        }
        
        private static bool RemoveFinalRetInstruction(MethodDefinition method)
        {
            if (method.Body.Instructions.Count != 0)
            {
                Instruction retInstr = method.Body.Instructions[^1];
                if (retInstr.OpCode == OpCodes.Ret)
                {
                    method.Body.Instructions.RemoveAt(method.Body.Instructions.Count - 1);
                    return true;
                }
                return false;
            }
            
            return true;
        }
        
        
        private void GenerateRegisterClientRpcDelegate(ILProcessor worker, MethodReference registerMethod, MethodDefinition func, string functionFullName)
        {
            worker.Emit(OpCodes.Ldtoken, generateCode);
            worker.Emit(OpCodes.Call, process.getTypeFromHandleReference);
            worker.Emit(OpCodes.Ldstr, functionFullName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, process.RpcDelegateConstructor);
            worker.Emit(OpCodes.Call, registerMethod);
        }

        private void GenerateRegisterServerRpcDelegate(ILProcessor worker, MethodReference registerMethod, MethodDefinition func, ServerRpcResult cmdResult)
        {
            string cmdName = cmdResult.method.FullName;

            worker.Emit(OpCodes.Ldtoken, generateCode);
            worker.Emit(OpCodes.Call, process.getTypeFromHandleReference);
            worker.Emit(OpCodes.Ldstr, cmdName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, process.RpcDelegateConstructor);
            worker.Emit(OpCodes.Call, registerMethod);
        }
    }
}