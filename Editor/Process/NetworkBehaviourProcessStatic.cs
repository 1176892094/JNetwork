using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 注入静态构造函数
        /// </summary>
        private void InjectStaticConstructor(ref bool failed)
        {
            if (serverRpcList.Count == 0 && clientRpcList.Count == 0 && targetRpcList.Count == 0) return;
            MethodDefinition cctor = generateCode.GetMethod(".cctor");
            bool cctorFound = cctor != null;
            if (cctor != null)
            {
                if (!RemoveFinalRetInstruction(cctor))
                {
                    logger.Error($"{generateCode.Name} 无效的静态构造函数。", cctor);
                    failed = true;
                    return;
                }
            }
            else
            {
                cctor = new MethodDefinition(".cctor",CONST.CTOR_ATTRS, models.Import(typeof(void)));
            }

            ILProcessor cctorWorker = cctor.Body.GetILProcessor();
            for (int i = 0; i < serverRpcList.Count; ++i)
            {
                var result = serverRpcList[i];
                GenerateRegisterServerRpcDelegate(cctorWorker, models.registerServerRpcRef, serverRpcFuncList[i], result);
            }
            
            for (int i = 0; i < clientRpcList.Count; ++i)
            {
                var result = clientRpcList[i];
                GenerateRegisterClientRpcDelegate(cctorWorker, models.registerClientRpcRef, clientRpcFuncList[i], result.FullName);
            }
            
            for (int i = 0; i < targetRpcList.Count; ++i)
            {
                GenerateRegisterClientRpcDelegate(cctorWorker, models.registerClientRpcRef, targetRpcFuncList[i], targetRpcList[i].FullName);
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
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, functionFullName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, registerMethod);
        }

        /// <summary>
        /// 在静态构造函数中注入ServerRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="registerMethod"></param>
        /// <param name="func"></param>
        /// <param name="rpcResult"></param>
        private void GenerateRegisterServerRpcDelegate(ILProcessor worker, MethodReference registerMethod, MethodDefinition func, MethodDefinition rpcResult)
        {
            string rpcName = rpcResult.FullName;
            worker.Emit(OpCodes.Ldtoken, generateCode);
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, rpcName);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, func);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, registerMethod);
        }
    }
}