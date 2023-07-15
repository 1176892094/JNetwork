using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static partial class NetworkRpcProcess
    {
        private static MethodDefinition TemplateMethod(Logger logger, TypeDefinition td, MethodDefinition md)
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
            FixBaseRpcMethod(logger, td, cmd);
            return cmd;
        }

        private static void FixBaseRpcMethod(Logger logger, TypeDefinition type, MethodDefinition md)
        {
            string methodName = md.Name;
            if (!methodName.StartsWith(CONST.RPC_METHOD)) return;
            string baseRemoteCallName = md.Name.Substring(CONST.RPC_METHOD.Length);

            foreach (Instruction instruction in md.Body.Instructions)
            {
                if (IsInvokeToMethod(instruction, out MethodDefinition method))
                {
                    string methodNameGenerated = Command.GenerateMethodName("", method);
                    if (methodNameGenerated == baseRemoteCallName)
                    {
                        TypeDefinition baseType = type.BaseType.Resolve();
                        MethodDefinition baseMethod = baseType.GetMethodInBaseType(methodName);

                        if (baseMethod == null)
                        {
                            logger.Error($"找不到基本方法{methodName}", md);
                            Command.failed = true;
                            return;
                        }

                        if (!baseMethod.IsVirtual)
                        {
                            logger.Error($"找不到 virtual 的基本方法{methodName}", md);
                            Command.failed = true;
                            return;
                        }

                        instruction.Operand = baseMethod;
                    }
                }
            }
        }
        
        private static bool IsInvokeToMethod(Instruction instruction, out MethodDefinition invokeMethod)
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