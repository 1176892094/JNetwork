using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static partial class NetworkRpcProcess
    {
        /// <summary>
        /// 处理基本的Rpc方法
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private static MethodDefinition BaseRpcMethod(Logger logger, TypeDefinition td, MethodDefinition md, ref bool failed)
        {
            var newName = Process.GenerateMethodName(CONST.RPC_METHOD, md);
            var rpc = new MethodDefinition(newName, md.Attributes, md.ReturnType)
            {
                IsPublic = false,
                IsFamily = true
            };

            foreach (var pd in md.Parameters)
            {
                rpc.Parameters.Add(new ParameterDefinition(pd.Name, ParameterAttributes.None, pd.ParameterType));
            }

            (rpc.Body, md.Body) = (md.Body, rpc.Body);

            foreach (var sequencePoint in md.DebugInformation.SequencePoints)
            {
                rpc.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            md.DebugInformation.SequencePoints.Clear();

            foreach (var info in md.CustomDebugInformations)
            {
                rpc.CustomDebugInformations.Add(info);
            }

            md.CustomDebugInformations.Clear();

            (md.DebugInformation.Scope, rpc.DebugInformation.Scope) = (rpc.DebugInformation.Scope, md.DebugInformation.Scope);
            td.Methods.Add(rpc);
            FixBaseRpcMethod(logger, td, rpc, ref failed);
            return rpc;
        }

        /// <summary>
        /// 处理修正的Rpc方法
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="td"></param>
        /// <param name="md"></param>
        /// <param name="failed"></param>
        private static void FixBaseRpcMethod(Logger logger, TypeDefinition td, MethodDefinition md, ref bool failed)
        {
            string rpcName = md.Name;
            if (!rpcName.StartsWith(CONST.RPC_METHOD)) return;
            string invokeName = md.Name.Substring(CONST.RPC_METHOD.Length);

            foreach (Instruction instruction in md.Body.Instructions)
            {
                if (IsInvokeToMethod(instruction, out MethodDefinition method))
                {
                    string newName = Process.GenerateMethodName("", method);
                    if (newName == invokeName)
                    {
                        var baseType = td.BaseType.Resolve();
                        var baseMethod = baseType.GetMethodInBaseType(rpcName);

                        if (baseMethod == null)
                        {
                            logger.Error($"找不到基本方法{rpcName}", md);
                            failed = true;
                            return;
                        }

                        if (!baseMethod.IsVirtual)
                        {
                            logger.Error($"找不到 virtual 的基本方法{rpcName}", md);
                            failed = true;
                            return;
                        }

                        instruction.Operand = baseMethod;
                    }
                }
            }
        }

        private static bool IsInvokeToMethod(Instruction instr, out MethodDefinition md)
        {
            if (instr.OpCode == OpCodes.Call && instr.Operand is MethodDefinition method)
            {
                md = method;
                return true;
            }

            md = null;
            return false;
        }
    }
}