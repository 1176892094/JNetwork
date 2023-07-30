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
        private static MethodDefinition BaseRpcMethod(Logger logger, TypeDefinition td, MethodDefinition md,
            ref bool failed)
        {
            string newName = Process.GenerateMethodName(CONST.RPC_METHOD, md);
            var rpc = new MethodDefinition(newName, md.Attributes, md.ReturnType)
            {
                IsPublic = false,
                IsFamily = true
            };

            foreach (ParameterDefinition pd in md.Parameters)
            {
                rpc.Parameters.Add(new ParameterDefinition(pd.Name, ParameterAttributes.None, pd.ParameterType));
            }

            (rpc.Body, md.Body) = (md.Body, rpc.Body);

            foreach (SequencePoint sequencePoint in md.DebugInformation.SequencePoints)
            {
                rpc.DebugInformation.SequencePoints.Add(sequencePoint);
            }

            md.DebugInformation.SequencePoints.Clear();

            foreach (CustomDebugInformation customInfo in md.CustomDebugInformations)
            {
                rpc.CustomDebugInformations.Add(customInfo);
            }

            md.CustomDebugInformations.Clear();

            (md.DebugInformation.Scope, rpc.DebugInformation.Scope) =
                (rpc.DebugInformation.Scope, md.DebugInformation.Scope);
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
            string methodName = md.Name;
            if (!methodName.StartsWith(CONST.RPC_METHOD)) return;
            string baseRemoteCallName = md.Name.Substring(CONST.RPC_METHOD.Length);

            foreach (Instruction instruction in md.Body.Instructions)
            {
                if (IsInvokeToMethod(instruction, out MethodDefinition method))
                {
                    string methodNameGenerated = Process.GenerateMethodName("", method);
                    if (methodNameGenerated == baseRemoteCallName)
                    {
                        TypeDefinition baseType = td.BaseType.Resolve();
                        MethodDefinition baseMethod = baseType.GetMethodInBaseType(methodName);

                        if (baseMethod == null)
                        {
                            logger.Error($"找不到基本方法{methodName}", md);
                            failed = true;
                            return;
                        }

                        if (!baseMethod.IsVirtual)
                        {
                            logger.Error($"找不到 virtual 的基本方法{methodName}", md);
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