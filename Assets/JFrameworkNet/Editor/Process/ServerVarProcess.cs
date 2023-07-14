using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal class ServerVarProcess
    {
        private readonly Logger logger;
        private readonly Processor processor;
        private readonly ServerVarList serverVars;
        private readonly AssemblyDefinition assembly;

        public ServerVarProcess(AssemblyDefinition assembly, Processor processor, ServerVarList serverVars, Logger logger)
        {
            this.assembly = assembly;
            this.processor = processor;
            this.serverVars = serverVars;
            this.logger = logger;
        }
        
        public void GenerateNewActionFromHookMethod(FieldDefinition syncVar, ILProcessor worker, MethodDefinition hookMethod)
        {
            worker.Emit(hookMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);

            var genericInstanceType = hookMethod.DeclaringType.MakeGenericInstanceType(hookMethod.DeclaringType.GenericParameters.ToArray());
            var hookMethodReference = hookMethod.DeclaringType.HasGenericParameters ? hookMethod.MakeHostInstanceGeneric(hookMethod.Module,genericInstanceType) : hookMethod;
            
            if (hookMethod.IsVirtual)
            {
                worker.Emit(OpCodes.Dup);
                worker.Emit(OpCodes.Ldvirtftn, hookMethodReference);
            }
            else
            {
                worker.Emit(OpCodes.Ldftn, hookMethodReference);
            }
            
            TypeReference actionRef = assembly.MainModule.ImportReference(typeof(Action<,>));
            GenericInstanceType genericInstance = actionRef.MakeGenericInstanceType(syncVar.FieldType, syncVar.FieldType);
            worker.Emit(OpCodes.Newobj, processor.ActionDoubleReference.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
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
            List<FieldDefinition> syncVars = new List<FieldDefinition>();
            Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds = new Dictionary<FieldDefinition, FieldDefinition>(); 
            int dirtyBitCounter = serverVars.GetServerVar(td.BaseType.FullName);
            
            foreach (var fd in td.Fields.Where(fd => fd.HasCustomAttribute<ServerVarAttribute>()))
            {
                if ((fd.Attributes & FieldAttributes.Static) != 0)
                {
                    logger.Error($"{fd.Name} 不能是静态字段。", fd);
                    isFailed = true;
                    continue;
                }

                if (fd.FieldType.IsGenericParameter)
                {
                    logger.Error($"{fd.Name} 不能用泛型参数。", fd);
                    isFailed = true;
                    continue;
                }

                if (fd.FieldType.IsArray)
                {
                    logger.Error($"{fd.Name} 不能使用数组。", fd);
                    isFailed = true;
                    continue;
                }

                // if (SyncObjectInitializer.ImplementsSyncObject(fd.FieldType))
                // {
                //     logger.Warning($"{fd.Name} has [SyncVar] attribute. SyncLists should not be marked with SyncVar", fd);
                // }
                // else
                // {
                //     syncVars.Add(fd);
                //
                //     ProcessSyncVar(td, fd, syncVarNetIds, 1L << dirtyBitCounter, ref isFailed);
                //     dirtyBitCounter += 1;
                //
                //     if (dirtyBitCounter > SyncVarLimit)
                //     {
                //         logger.Error($"{td.Name} has > {CONST.SERVER_VAR_LIMIT} SyncVars. Consider refactoring your class into multiple components", td);
                //         isFailed = true;
                //         continue;
                //     }
                // }
            }
            
            foreach (FieldDefinition fd in syncVarNetIds.Values)
            {
                td.Fields.Add(fd);
            }
            
            int parentSyncVarCount = serverVars.GetServerVar(td.BaseType.FullName);
            serverVars.SetServerVarCount(td.FullName, parentSyncVarCount + syncVars.Count);
            return (syncVars, syncVarNetIds);
        }
    }
}