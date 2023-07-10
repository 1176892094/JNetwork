using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal class ServerVarProcess
    {
        private readonly Logger logger;
        private Processor processor;
        private ServerVarList serverVars;
        private AssemblyDefinition assembly;

        public ServerVarProcess(AssemblyDefinition assembly, Processor processor, ServerVarList serverVars, Logger logger)
        {
            this.assembly = assembly;
            this.processor = processor;
            this.serverVars = serverVars;
            this.logger = logger;
        }

        public MethodDefinition GetHookMethod(TypeDefinition td, FieldDefinition serverVar, ref bool isFailed)
        {
            CustomAttribute attribute = serverVar.GetCustomAttribute<ServerVarAttribute>();

            string hookMethod = attribute?.GetField<string>(CONST.VALUE_CHANGED, null);

            if (hookMethod == null)
            {
                return null;
            }

            return FindHookMethod(td, serverVar, hookMethod, ref isFailed);
        }

        private MethodDefinition FindHookMethod(TypeDefinition td, FieldDefinition serverVar, string hookMethod, ref bool isFailed)
        {
            List<MethodDefinition> methods = td.GetMethods(hookMethod);

            List<MethodDefinition> fixMethods = new List<MethodDefinition>(methods.Where(method => method.Parameters.Count == 2));

            if (fixMethods.Count == 0)
            {
                logger.Error($"无法注册 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
                isFailed = true;
                return null;
            }

            foreach (var method in fixMethods.Where(method => MatchesParameters(serverVar, method)))
            {
                return method;
            }

            logger.Error($"参数类型错误 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
            isFailed = true;
            return null;
        }

        private static string HookMethod(string name, TypeReference valueType) => $"void {name}({valueType} oldValue, {valueType} newValue)";
        
        private static bool MatchesParameters(FieldDefinition serverVar, MethodDefinition method)
        {
            return method.Parameters[0].ParameterType.FullName == serverVar.FieldType.FullName && method.Parameters[1].ParameterType.FullName == serverVar.FieldType.FullName;
        }

        public (List<FieldDefinition> syncVars, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds) ProcessSyncVars(TypeDefinition td, ref bool isFailed)
        {
            return (null, null);
        }
    }
}