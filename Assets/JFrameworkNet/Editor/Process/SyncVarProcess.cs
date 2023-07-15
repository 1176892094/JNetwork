using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal class ServerVarProcess
    {
        private readonly Logger logger;
        private readonly Processor processor;
        private readonly SyncVarList syncVarList;
        private readonly AssemblyDefinition assembly;

        public ServerVarProcess(AssemblyDefinition assembly, Processor processor, SyncVarList syncVars, Logger logger)
        {
            this.assembly = assembly;
            this.processor = processor;
            this.syncVarList = syncVars;
            this.logger = logger;
        }
        
        public void GenerateNewActionFromHookMethod(FieldDefinition syncVar, ILProcessor worker, MethodDefinition hookMethod)
        {
            worker.Emit(hookMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            MethodReference hookMethodReference;
            if (hookMethod.DeclaringType.HasGenericParameters)
            {
                GenericParameter[] genericParameters = hookMethod.DeclaringType.GenericParameters.ToArray();
                TypeReference[] typeReferences = new TypeReference[genericParameters.Length];
                for (int i = 0; i < genericParameters.Length; i++)
                {
                    typeReferences[i] = genericParameters[i];
                }
                var genericInstanceType = hookMethod.DeclaringType.MakeGenericInstanceType(typeReferences);
                hookMethodReference = hookMethod.MakeHostInstanceGeneric(hookMethod.Module, genericInstanceType);
            }
            else
            {
                hookMethodReference = hookMethod;
            }

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
            worker.Emit(OpCodes.Newobj, processor.HookMethodReference.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
        }

        public MethodDefinition GetHookMethod(TypeDefinition td, FieldDefinition syncVar)
        {
            CustomAttribute attribute = syncVar.GetCustomAttribute<SyncVarAttribute>();
            string hookMethod = attribute.GetField<string>(null);
            return hookMethod == null ? null : FindHookMethod(td, syncVar, hookMethod);
        }

        private MethodDefinition FindHookMethod(TypeDefinition td, FieldDefinition serverVar, string hookMethod)
        {
            List<MethodDefinition> methods = td.GetMethods(hookMethod);
    
            List<MethodDefinition> fixMethods = new List<MethodDefinition>(methods.Where(method => method.Parameters.Count == 2));

            if (fixMethods.Count == 0)
            {
                logger.Error($"无法注册 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
                Injection.failed = true;
                return null;
            }

            foreach (var method in fixMethods.Where(method => MatchesParameters(serverVar, method)))
            {
                return method;
            }

            logger.Error($"参数类型错误 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
            Injection.failed = true;
            return null;
        }

        private static string HookMethod(string name, TypeReference valueType) => $"void {name}({valueType} oldValue, {valueType} newValue)";
        
        private static bool MatchesParameters(FieldDefinition serverVar, MethodDefinition method)
        {
            return method.Parameters[0].ParameterType.FullName == serverVar.FieldType.FullName && method.Parameters[1].ParameterType.FullName == serverVar.FieldType.FullName;
        }

        public (List<FieldDefinition> syncVars, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds) ProcessSyncVars(TypeDefinition td)
        { 
            List<FieldDefinition> syncVars = new List<FieldDefinition>();
            Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds = new Dictionary<FieldDefinition, FieldDefinition>(); 
            int dirtyBitCounter = syncVarList.GetServerVar(td.BaseType.FullName);
           
            foreach (var fd in td.Fields.Where(fd => fd.HasCustomAttribute<SyncVarAttribute>()))
            {
                if ((fd.Attributes & FieldAttributes.Static) != 0)
                {
                    logger.Error($"{fd.Name} 不能是静态字段。", fd);
                    Injection.failed = true;
                    continue;
                }

                if (fd.FieldType.IsGenericParameter)
                {
                    logger.Error($"{fd.Name} 不能用泛型参数。", fd);
                    Injection.failed = true;
                    continue;
                }

                if (fd.FieldType.IsArray)
                {
                    logger.Error($"{fd.Name} 不能使用数组。", fd);
                    Injection.failed = true;
                    continue;
                }

                syncVars.Add(fd);
                   
                ProcessSyncVar(td, fd, syncVarNetIds, 1L << dirtyBitCounter);
                dirtyBitCounter += 1;
                  
                if (dirtyBitCounter > CONST.SERVER_VAR_LIMIT)
                {
                    logger.Error($"{td.Name} 网络变量数量大于 {CONST.SERVER_VAR_LIMIT}。", td);
                    Injection.failed = true;
                }
            }
            
            foreach (FieldDefinition fd in syncVarNetIds.Values)
            {
                td.Fields.Add(fd);
            }
            
            int parentSyncVarCount = syncVarList.GetServerVar(td.BaseType.FullName);
            syncVarList.SetServerVarCount(td.FullName, parentSyncVarCount + syncVars.Count);
            return (syncVars, syncVarNetIds);
        }

        private void ProcessSyncVar(TypeDefinition td, FieldDefinition fd, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds, long dirtyBit)
        {
            string originalName = fd.Name;
            
            FieldDefinition netIdField = null;
            if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            { 
                netIdField = new FieldDefinition($"_{fd.Name}NetId", FieldAttributes.Family, processor.Import<NetworkVariable>());
                netIdField.DeclaringType = td;

                syncVarNetIds[fd] = netIdField;
            }
            else if (fd.FieldType.IsNetworkEntityField())
            {  
                netIdField = new FieldDefinition($"_{fd.Name}NetId", FieldAttributes.Family, processor.Import<uint>());
                netIdField.DeclaringType = td;

                syncVarNetIds[fd] = netIdField;
            }
         
            MethodDefinition get = GenerateSyncVarGetter(fd, originalName, netIdField);
            MethodDefinition set = GenerateSyncVarSetter(td, fd, originalName, dirtyBit, netIdField);

       
            PropertyDefinition propertyDefinition = new PropertyDefinition($"Network{originalName}", PropertyAttributes.None, fd.FieldType)
            {
                GetMethod = get,
                SetMethod = set
            };

         
            td.Methods.Add(get);
            td.Methods.Add(set);
            td.Properties.Add(propertyDefinition);
            syncVarList.setterProperties[fd] = set;

            if (fd.FieldType.IsNetworkEntityField())
            {
                syncVarList.getterProperties[fd] = get;
            }
        }

        private MethodDefinition GenerateSyncVarGetter(FieldDefinition fd, string originalName, FieldDefinition netFieldId)
        {
            MethodDefinition get = new MethodDefinition($"get_Network{originalName}", CONST.SERVER_VALUE, fd.FieldType);

            ILProcessor worker = get.Body.GetILProcessor();

            FieldReference fr = fd.DeclaringType.HasGenericParameters ? fd.MakeHostInstanceGeneric() : fd;

            FieldReference netIdFieldReference = null;
            if (netFieldId != null)
            {
                netIdFieldReference = netFieldId.DeclaringType.HasGenericParameters ? netFieldId.MakeHostInstanceGeneric() : netFieldId;
            }
            
            if (fd.FieldType.Is<GameObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                worker.Emit(OpCodes.Call, processor.getSyncVarGameObject);
                worker.Emit(OpCodes.Ret);
            }
            else if (fd.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                worker.Emit(OpCodes.Call, processor.getSyncVarNetworkObject);
                worker.Emit(OpCodes.Ret);
            }
            else if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                MethodReference getFunc = processor.getSyncVarNetworkBehaviour.MakeGeneric(assembly.MainModule, fd.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
                worker.Emit(OpCodes.Ret);
            }
            else
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, fr);
                worker.Emit(OpCodes.Ret);
            }

            get.Body.Variables.Add(new VariableDefinition(fd.FieldType));
            get.Body.InitLocals = true;
            get.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            return get;
        }

        private MethodDefinition GenerateSyncVarSetter(TypeDefinition td, FieldDefinition fd, string originalName, long dirtyBit, FieldDefinition netFieldId)
        {
            MethodDefinition set = new MethodDefinition($"set_Network{originalName}", CONST.SERVER_VALUE, processor.Import(typeof(void)));
            
            ILProcessor worker = set.Body.GetILProcessor();
            FieldReference fr = fd.DeclaringType.HasGenericParameters ? fd.MakeHostInstanceGeneric() : fd;

            FieldReference netIdFieldReference = null;
            if (netFieldId != null)
            {
                netIdFieldReference = netFieldId.DeclaringType.HasGenericParameters ? netFieldId.MakeHostInstanceGeneric() : netFieldId;
            }
          
            Instruction endOfMethod = worker.Create(OpCodes.Nop);
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldflda, fr);
            worker.Emit(OpCodes.Ldc_I8, dirtyBit);
        
            MethodDefinition hookMethod = GetHookMethod(td, fd);
            if (hookMethod != null)
            {
                GenerateNewActionFromHookMethod(fd, worker, hookMethod);
            }
            else
            {
                worker.Emit(OpCodes.Ldnull);
            }
          
            if (fd.FieldType.Is<GameObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdFieldReference);
                worker.Emit(OpCodes.Call, processor.syncVarSetterGameObject);
            }
            else if (fd.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdFieldReference);
                worker.Emit(OpCodes.Call, processor.syncVarSetterNetworkObject);
            }
            else if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdFieldReference);
                MethodReference getFunc = processor.syncVarSetterNetworkBehaviour.MakeGeneric(assembly.MainModule, fd.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                MethodReference generic = processor.syncVarSetterGeneral.MakeGeneric(assembly.MainModule, fd.FieldType);
                worker.Emit(OpCodes.Call, generic);
            }

            worker.Append(endOfMethod);

            worker.Emit(OpCodes.Ret);

            set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.In, fd.FieldType));
            set.SemanticsAttributes = MethodSemanticsAttributes.Setter;

            return set;
        }
    }
}