using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal class SyncVarProcess
    {
        private readonly Logger logger;
        private readonly Models models;
        private readonly AssemblyDefinition assembly;

        public SyncVarProcess(AssemblyDefinition assembly, Models models, Logger logger)
        {
            this.assembly = assembly;
            this.models = models;
            this.logger = logger;
        }

        /// <summary>
        /// 从挂钩方法中生成新的方法
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="worker"></param>
        /// <param name="hookMethod"></param>
        public void GenerateNewActionFromHookMethod(FieldDefinition syncVar, ILProcessor worker,
            MethodDefinition hookMethod)
        {
            worker.Emit(hookMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            MethodReference hookMethodRef;
            if (hookMethod.DeclaringType.HasGenericParameters)
            {
                var instanceType = hookMethod.DeclaringType.MakeGenericInstanceType(hookMethod.DeclaringType
                    .GenericParameters.Cast<TypeReference>().ToArray());
                hookMethodRef = hookMethod.MakeHostInstanceGeneric(hookMethod.Module, instanceType);
            }
            else
            {
                hookMethodRef = hookMethod;
            }

            if (hookMethod.IsVirtual)
            {
                worker.Emit(OpCodes.Dup);
                worker.Emit(OpCodes.Ldvirtftn, hookMethodRef);
            }
            else
            {
                worker.Emit(OpCodes.Ldftn, hookMethodRef);
            }

            TypeReference actionRef = assembly.MainModule.ImportReference(typeof(Action<,>));
            GenericInstanceType genericInstance =
                actionRef.MakeGenericInstanceType(syncVar.FieldType, syncVar.FieldType);
            worker.Emit(OpCodes.Newobj,
                models.HookMethodRef.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
        }

        /// <summary>
        /// 获取挂钩方法
        /// </summary>
        /// <param name="td"></param>
        /// <param name="syncVar"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public MethodDefinition GetHookMethod(TypeDefinition td, FieldDefinition syncVar, ref bool failed)
        {
            CustomAttribute attribute = syncVar.GetCustomAttribute<SyncVarAttribute>();
            string hookMethod = attribute.GetField<string>(null);
            return hookMethod == null ? null : FindHookMethod(td, syncVar, hookMethod, ref failed);
        }

        /// <summary>
        /// 寻找挂钩方法
        /// </summary>
        /// <param name="td"></param>
        /// <param name="serverVar"></param>
        /// <param name="hookMethod"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private MethodDefinition FindHookMethod(TypeDefinition td, FieldDefinition serverVar, string hookMethod,
            ref bool failed)
        {
            List<MethodDefinition> methods = td.GetMethods(hookMethod);

            List<MethodDefinition> fixMethods =
                new List<MethodDefinition>(methods.Where(method => method.Parameters.Count == 2));

            if (fixMethods.Count == 0)
            {
                logger.Error($"无法注册 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
                failed = true;
                return null;
            }

            foreach (var method in fixMethods.Where(method => MatchesParameters(serverVar, method)))
            {
                return method;
            }

            logger.Error($"参数类型错误 {serverVar.Name} 请修改为 {HookMethod(hookMethod, serverVar.FieldType)}", serverVar);
            failed = true;
            return null;
        }

        /// <summary>
        /// 钩子方法的模版
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        private static string HookMethod(string name, TypeReference valueType) =>
            $"void {name}({valueType} oldValue, {valueType} newValue)";

        /// <summary>
        /// 参数配对
        /// </summary>
        /// <param name="serverVar"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private static bool MatchesParameters(FieldDefinition serverVar, MethodDefinition method)
        {
            return method.Parameters[0].ParameterType.FullName == serverVar.FieldType.FullName &&
                   method.Parameters[1].ParameterType.FullName == serverVar.FieldType.FullName;
        }

        /// <summary>
        /// 处理每个NetworkBehaviour的SyncVar
        /// </summary>
        /// <param name="td"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public (List<FieldDefinition> syncVars, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds) ProcessSyncVars(TypeDefinition td, ref bool failed)
        {
            List<FieldDefinition> syncVars = new List<FieldDefinition>();
            Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds =
                new Dictionary<FieldDefinition, FieldDefinition>();
            int dirtyBitCounter = SyncVarHelpers.GetSyncVar(td.BaseType.FullName);

            foreach (var fd in td.Fields.Where(fd => fd.HasCustomAttribute<SyncVarAttribute>()))
            {
                if ((fd.Attributes & FieldAttributes.Static) != 0)
                {
                    logger.Error($"{fd.Name} 不能是静态字段。", fd);
                    failed = true;
                    continue;
                }

                if (fd.FieldType.IsGenericParameter)
                {
                    logger.Error($"{fd.Name} 不能用泛型参数。", fd);
                    failed = true;
                    continue;
                }

                if (fd.FieldType.IsArray)
                {
                    logger.Error($"{fd.Name} 不能使用数组。", fd);
                    failed = true;
                    continue;
                }

                syncVars.Add(fd);

                ProcessSyncVar(td, fd, syncVarNetIds, 1L << dirtyBitCounter, ref failed);
                dirtyBitCounter += 1;

                if (dirtyBitCounter > CONST.SYNC_LIMIT)
                {
                    logger.Error($"{td.Name} 网络变量数量大于 {CONST.SYNC_LIMIT}。", td);
                    failed = true;
                }
            }

            foreach (FieldDefinition fd in syncVarNetIds.Values)
            {
                td.Fields.Add(fd);
            }

            int parentSyncVarCount = SyncVarHelpers.GetSyncVar(td.BaseType.FullName);
            SyncVarHelpers.SetSyncVar(td.FullName, parentSyncVarCount + syncVars.Count);
            return (syncVars, syncVarNetIds);
        }

        /// <summary>
        /// 处理SyncVar
        /// </summary>
        /// <param name="td"></param>
        /// <param name="fd"></param>
        /// <param name="syncVarNetIds"></param>
        /// <param name="dirtyBit"></param>
        /// <param name="failed"></param>
        private void ProcessSyncVar(TypeDefinition td, FieldDefinition fd, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds, long dirtyBit, ref bool failed)
        {
            string originalName = fd.Name;

            FieldDefinition netIdField = null;
            if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                netIdField = new FieldDefinition($"{fd.Name}Id", FieldAttributes.Family, models.Import<NetworkValue>());
                netIdField.DeclaringType = td;
                syncVarNetIds[fd] = netIdField;
            }
            else if (fd.FieldType.IsNetworkObjectField())
            {
                netIdField = new FieldDefinition($"{fd.Name}Id", FieldAttributes.Family, models.Import<uint>());
                netIdField.DeclaringType = td;

                syncVarNetIds[fd] = netIdField;
            }

            MethodDefinition get = GenerateSyncVarGetter(fd, originalName, netIdField);
            MethodDefinition set = GenerateSyncVarSetter(td, fd, originalName, dirtyBit, netIdField, ref failed);


            PropertyDefinition propertyDefinition =
                new PropertyDefinition($"Network{originalName}", PropertyAttributes.None, fd.FieldType)
                {
                    GetMethod = get,
                    SetMethod = set
                };


            td.Methods.Add(get);
            td.Methods.Add(set);
            td.Properties.Add(propertyDefinition);

            SyncVarHelpers.setter[fd] = set;

            if (fd.FieldType.IsNetworkObjectField())
            {
                SyncVarHelpers.getter[fd] = get;
            }
        }

        /// <summary>
        /// 生成SyncVer的Getter
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="originalName"></param>
        /// <param name="netFieldId"></param>
        /// <returns></returns>
        private MethodDefinition GenerateSyncVarGetter(FieldDefinition fd, string originalName,
            FieldDefinition netFieldId)
        {
            MethodDefinition get = new MethodDefinition($"get_Network{originalName}", CONST.VAR_ATTRS, fd.FieldType);

            ILProcessor worker = get.Body.GetILProcessor();

            FieldReference fr = fd.DeclaringType.HasGenericParameters ? fd.MakeHostInstanceGeneric() : fd;

            FieldReference netIdFieldReference = null;
            if (netFieldId != null)
            {
                netIdFieldReference = netFieldId.DeclaringType.HasGenericParameters
                    ? netFieldId.MakeHostInstanceGeneric()
                    : netFieldId;
            }

            if (fd.FieldType.Is<GameObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                worker.Emit(OpCodes.Call, models.getSyncVarGameObject);
                worker.Emit(OpCodes.Ret);
            }
            else if (fd.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                worker.Emit(OpCodes.Call, models.getSyncVarNetworkObject);
                worker.Emit(OpCodes.Ret);
            }
            else if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, netIdFieldReference);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, fr);
                MethodReference getFunc =
                    models.getSyncVarNetworkBehaviour.MakeGeneric(assembly.MainModule, fd.FieldType);
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

        /// <summary>
        /// 生成SyncVar的Setter
        /// </summary>
        /// <param name="td"></param>
        /// <param name="fd"></param>
        /// <param name="originalName"></param>
        /// <param name="dirtyBit"></param>
        /// <param name="netFieldId"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private MethodDefinition GenerateSyncVarSetter(TypeDefinition td, FieldDefinition fd, string originalName,
            long dirtyBit, FieldDefinition netFieldId, ref bool failed)
        {
            MethodDefinition set = new MethodDefinition($"set_Network{originalName}", CONST.VAR_ATTRS,
                models.Import(typeof(void)));

            ILProcessor worker = set.Body.GetILProcessor();
            FieldReference fr = fd.DeclaringType.HasGenericParameters ? fd.MakeHostInstanceGeneric() : fd;

            FieldReference netIdFieldReference = null;
            if (netFieldId != null)
            {
                netIdFieldReference = netFieldId.DeclaringType.HasGenericParameters
                    ? netFieldId.MakeHostInstanceGeneric()
                    : netFieldId;
            }

            Instruction endOfMethod = worker.Create(OpCodes.Nop);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldflda, fr);
            worker.Emit(OpCodes.Ldc_I8, dirtyBit);

            MethodDefinition hookMethod = GetHookMethod(td, fd, ref failed);
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
                worker.Emit(OpCodes.Call, models.syncVarSetterGameObject);
            }
            else if (fd.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdFieldReference);
                worker.Emit(OpCodes.Call, models.syncVarSetterNetworkObject);
            }
            else if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdFieldReference);
                MethodReference getFunc =
                    models.syncVarSetterNetworkBehaviour.MakeGeneric(assembly.MainModule, fd.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                MethodReference generic = models.syncVarSetterGeneral.MakeGeneric(assembly.MainModule, fd.FieldType);
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