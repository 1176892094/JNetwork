using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal static class MethodProcess
    {
        public static MethodDefinition SubstituteMethod(Logger logger, TypeDefinition td, MethodDefinition md, ref bool isFailed)
        {
            string newName = Process.GenerateMethodName(CONST.USER_RPC, md);
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
            FixRemoteCallToBaseMethod(logger, td, cmd, ref isFailed);
            return cmd;
        }
        
        public static void FixRemoteCallToBaseMethod(Logger logger, TypeDefinition type, MethodDefinition method, ref bool isFailed)
        {
            string callName = method.Name;
            if (!callName.StartsWith(CONST.USER_RPC)) return;
            string baseRemoteCallName = method.Name.Substring(CONST.USER_RPC.Length);

            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (IsCallToMethod(instruction, out MethodDefinition calledMethod))
                {
                    string calledMethodName_Generated = Process.GenerateMethodName("", calledMethod);
                    if (calledMethodName_Generated == baseRemoteCallName)
                    {
                        TypeDefinition baseType = type.BaseType.Resolve();
                        MethodDefinition baseMethod = baseType.GetMethodInBaseType(callName);

                        if (baseMethod == null)
                        {
                            logger.Error($"Could not find base method for {callName}", method);
                            isFailed = true;
                            return;
                        }

                        if (!baseMethod.IsVirtual)
                        {
                            logger.Error($"Could not find base method that was virtual {callName}", method);
                            isFailed = true;
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