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
            MethodDefinition cctor = generate.GetMethod(".cctor");
            if (cctor != null)
            {
                if (!RemoveFinalRetInstruction(cctor))
                {
                    logger.Error($"{generate.Name} 无效的静态构造函数。", cctor);
                    failed = true;
                    return;
                }
            }
            else
            {
                cctor = new MethodDefinition(".cctor",CONST.CTOR_ATTRS, models.Import(typeof(void)));
            }

            ILProcessor worker = cctor.Body.GetILProcessor();
            for (int i = 0; i < serverRpcList.Count; ++i)
            {
                GenerateServerRpcDelegate(worker, models.registerServerRpcRef, serverRpcFuncList[i], serverRpcList[i].FullName);
            }
            
            for (int i = 0; i < clientRpcList.Count; ++i)
            {
                GenerateClientRpcDelegate(worker, models.registerClientRpcRef, clientRpcFuncList[i], clientRpcList[i].FullName);
            }
            
            for (int i = 0; i < targetRpcList.Count; ++i)
            {
                GenerateClientRpcDelegate(worker, models.registerClientRpcRef, targetRpcFuncList[i], targetRpcList[i].FullName);
            }
            
            worker.Append(worker.Create(OpCodes.Ret));
            generate.Methods.Add(cctor);
            generate.Attributes &= ~TypeAttributes.BeforeFieldInit;
        }
        
        /// <summary>
        /// 判断自身静态构造函数是否被创建
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool RemoveFinalRetInstruction(MethodDefinition md)
        {
            if (md.Body.Instructions.Count != 0)
            {
                Instruction retInstr = md.Body.Instructions[^1];
                if (retInstr.OpCode == OpCodes.Ret)
                {
                    md.Body.Instructions.RemoveAt(md.Body.Instructions.Count - 1);
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
        /// <param name="mr"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        private void GenerateClientRpcDelegate(ILProcessor worker, MethodReference mr, MethodDefinition md, string func)
        {
            worker.Emit(OpCodes.Ldtoken, generate);
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, func);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, md);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, mr);
        }

        /// <summary>
        /// 在静态构造函数中注入ServerRpc委托
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="mr"></param>
        /// <param name="md"></param>
        /// <param name="func"></param>
        private void GenerateServerRpcDelegate(ILProcessor worker, MethodReference mr, MethodDefinition md, string func)
        {
            worker.Emit(OpCodes.Ldtoken, generate);
            worker.Emit(OpCodes.Call, models.getTypeFromHandleRef);
            worker.Emit(OpCodes.Ldstr, func);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ldftn, md);
            worker.Emit(OpCodes.Newobj, models.RpcDelegateRef);
            worker.Emit(OpCodes.Call, mr);
        }
    }
}