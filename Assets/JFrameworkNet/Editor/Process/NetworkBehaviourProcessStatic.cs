using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 注入静态构造函数
        /// </summary>
        private void InjectStaticConstructor()
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
                GenerateRegisterServerRpcDelegate(cctorWorker, process.registerServerRpcRef, serverRpcFuncList[i], cmdResult);
            }
            
            for (int i = 0; i < clientRpcList.Count; ++i)
            {
                ClientRpcResult clientRpcResult = clientRpcList[i];
                GenerateRegisterClientRpcDelegate(cctorWorker, process.registerClientRpcRef, clientRpcFuncList[i], clientRpcResult.method.FullName);
            }
            
            for (int i = 0; i < targetRpcList.Count; ++i)
            {
                GenerateRegisterClientRpcDelegate(cctorWorker, process.registerClientRpcRef, targetRpcFuncList[i], targetRpcList[i].FullName);
            }
            
            cctorWorker.Append(cctorWorker.Create(OpCodes.Ret));
            if (!cctorFound)
            {
                generateCode.Methods.Add(cctor);
            }
            
            generateCode.Attributes &= ~TypeAttributes.BeforeFieldInit;
        }
        
        /// <summary>
        /// 判断自身静态构造函数是否被创建
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
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
        
        /// <summary>
        /// 在静态构造函数中注入ClientRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="registerMethod"></param>
        /// <param name="func"></param>
        /// <param name="functionFullName"></param>
        private void GenerateRegisterClientRpcDelegate(ILProcessor worker, MethodReference registerMethod, MethodDefinition func, string functionFullName)
        {
            worker.Emit(OpCodes.Ldtoken, generateCode);
            worker.Emit(OpCodes.Call, process.getTypeFromHandleReference);
            worker.Emit(OpCodes.Ldstr, functionFullName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, process.RpcDelegateRef);
            worker.Emit(OpCodes.Call, registerMethod);
        }

        /// <summary>
        /// 在静态构造函数中注入ServerRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="registerMethod"></param>
        /// <param name="func"></param>
        /// <param name="cmdResult"></param>
        private void GenerateRegisterServerRpcDelegate(ILProcessor worker, MethodReference registerMethod, MethodDefinition func, ServerRpcResult cmdResult)
        {
            string cmdName = cmdResult.method.FullName;
            worker.Emit(OpCodes.Ldtoken, generateCode);
            worker.Emit(OpCodes.Call, process.getTypeFromHandleReference);
            worker.Emit(OpCodes.Ldstr, cmdName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, process.RpcDelegateRef);
            worker.Emit(OpCodes.Call, registerMethod);
        }
    }
}