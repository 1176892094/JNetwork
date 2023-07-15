using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static partial class NetworkRpcProcess
    {
        private static MethodDefinition SubstituteMethod(Logger logger, TypeDefinition td, MethodDefinition md)
        {
            string newName = Command.GenerateMethodName(CONST.RPC_METHOD, md);
            var cmd = new MethodDefinition(newName, md.Attributes, md.ReturnType)
            {
                IsPublic = false,
                IsFamily = true
            };
            
            foreach (ParameterDefinition pd in md.Parameters)
            {
                cmd.Parameters.Add(new ParameterDefinition(pd.Name, ParameterAttributes.None, pd.ParameterType));
            }
            
            (cmd.Body, md.Body) = (md.Body, cmd.Body);

            foreach (SequencePoint sequencePoint in md.DebugInformation.SequencePoints)
            {
                cmd.DebugInformation.SequencePoints.Add(sequencePoint);
            }
            md.DebugInformation.SequencePoints.Clear();

            foreach (CustomDebugInformation customInfo in md.CustomDebugInformations)
            {
                cmd.CustomDebugInformations.Add(customInfo);
            }
            md.CustomDebugInformations.Clear();

            (md.DebugInformation.Scope, cmd.DebugInformation.Scope) = (cmd.DebugInformation.Scope, md.DebugInformation.Scope);
            td.Methods.Add(cmd);
            FixBaseMethodForRpc(logger, td, cmd);
            return cmd;
        }

        private static void FixBaseMethodForRpc(Logger logger, TypeDefinition type, MethodDefinition method)
        {
            string methodName = method.Name;
            if (!methodName.StartsWith(CONST.RPC_METHOD)) return;
            string baseRemoteCallName = method.Name.Substring(CONST.RPC_METHOD.Length);

            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (IsCallToMethod(instruction, out MethodDefinition calledMethod))
                {
                    string calledMethodName_Generated = Command.GenerateMethodName("", calledMethod);
                    if (calledMethodName_Generated == baseRemoteCallName)
                    {
                        TypeDefinition baseType = type.BaseType.Resolve();
                        MethodDefinition baseMethod = baseType.GetMethodInBaseType(methodName);

                        if (baseMethod == null)
                        {
                            logger.Error($"找不到 base 方法{methodName}", method);
                            Command.failed = true;
                            return;
                        }

                        if (!baseMethod.IsVirtual)
                        {
                            logger.Error($"找不到 virtual 的 base 方法{methodName}", method);
                            Command.failed = true;
                            return;
                        }

                        instruction.Operand = baseMethod;
                    }
                }
            }
        }
        
        private static bool IsCallToMethod(Instruction instruction, out MethodDefinition invokeMethod)
        {
            if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodDefinition method)
            {
                invokeMethod = method;
                return true;
            }

            invokeMethod = null;
            return false;
        }
    }
}