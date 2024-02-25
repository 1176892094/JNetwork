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
        private readonly SyncVarAccess access;
        private readonly AssemblyDefinition assembly;

        public SyncVarProcess(AssemblyDefinition assembly, SyncVarAccess access, Models models, Logger logger)
        {
            this.logger = logger;
            this.access = access;
            this.models = models;
            this.assembly = assembly;
        }

        /// <summary>
        /// 从挂钩方法中生成新的方法
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="worker"></param>
        /// <param name="hookMethod"></param>
        public void GenerateNewActionFromHookMethod(FieldDefinition syncVar, ILProcessor worker, MethodDefinition hookMethod)
        {
            worker.Emit(hookMethod.IsStatic ? OpCodes.Ldnull : OpCodes.Ldarg_0);
            MethodReference hookMethodRef;
            if (hookMethod.DeclaringType.HasGenericParameters)
            {
                var instanceType =
                    hookMethod.DeclaringType.MakeGenericInstanceType(hookMethod.DeclaringType.GenericParameters.Cast<TypeReference>()
                        .ToArray());
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

            var actionRef = assembly.MainModule.ImportReference(typeof(Action<,>));
            var genericInstance = actionRef.MakeGenericInstanceType(syncVar.FieldType, syncVar.FieldType);
            worker.Emit(OpCodes.Newobj, models.HookMethodRef.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
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
            var attribute = syncVar.GetCustomAttribute<SyncVarAttribute>();
            string hookMethod = attribute.GetField<string>(null);
            return hookMethod == null ? null : FindHookMethod(td, syncVar, hookMethod, ref failed);
        }

        /// <summary>
        /// 寻找挂钩方法
        /// </summary>
        /// <param name="td"></param>
        /// <param name="syncVar"></param>
        /// <param name="hookMethod"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        private MethodDefinition FindHookMethod(TypeDefinition td, FieldDefinition syncVar, string hookMethod, ref bool failed)
        {
            var methods = td.GetMethods(hookMethod);

            var fixMethods = new List<MethodDefinition>(methods.Where(method => method.Parameters.Count == 2));

            if (fixMethods.Count == 0)
            {
                logger.Error($"无法注册 {syncVar.Name} 请修改为 {HookMethod(hookMethod, syncVar.FieldType)}", syncVar);
                failed = true;
                return null;
            }

            foreach (var method in fixMethods.Where(method => MatchesParameters(syncVar, method)))
            {
                return method;
            }

            logger.Error($"参数类型错误 {syncVar.Name} 请修改为 {HookMethod(hookMethod, syncVar.FieldType)}", syncVar);
            failed = true;
            return null;
        }

        /// <summary>
        /// 挂钩方法的模版
        /// </summary>
        /// <param name="name"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        private static string HookMethod(string name, TypeReference valueType) =>
            $"void {name}({valueType} oldValue, {valueType} newValue)";

        /// <summary>
        /// 参数配对
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="md"></param>
        /// <returns></returns>
        private static bool MatchesParameters(FieldDefinition syncVar, MethodDefinition md)
        {
            return md.Parameters[0].ParameterType.FullName == syncVar.FieldType.FullName &&
                   md.Parameters[1].ParameterType.FullName == syncVar.FieldType.FullName;
        }

        /// <summary>
        /// 处理每个NetworkBehaviour的SyncVar
        /// </summary>
        /// <param name="td"></param>
        /// <param name="failed"></param>
        /// <returns></returns>
        public (List<FieldDefinition> syncVars, Dictionary<FieldDefinition, FieldDefinition> syncVarNetIds) ProcessSyncVars(
            TypeDefinition td, ref bool failed)
        {
            var syncVars = new List<FieldDefinition>();
            var syncVarIds = new Dictionary<FieldDefinition, FieldDefinition>();
            int dirtyBits = access.GetSyncVar(td.BaseType.FullName);

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

                ProcessSyncVar(td, fd, syncVarIds, 1L << dirtyBits, ref failed);
                dirtyBits += 1;

                if (dirtyBits > CONST.SYNC_LIMIT)
                {
                    logger.Error($"{td.Name} 网络变量数量大于 {CONST.SYNC_LIMIT}。", td);
                    failed = true;
                }
            }

            foreach (var fd in syncVarIds.Values)
            {
                td.Fields.Add(fd);
            }

            int parentSyncVarCount = access.GetSyncVar(td.BaseType.FullName);
            access.SetSyncVar(td.FullName, parentSyncVarCount + syncVars.Count);
            return (syncVars, syncVarIds);
        }

        /// <summary>
        /// 处理SyncVar
        /// </summary>
        /// <param name="td"></param>
        /// <param name="fd"></param>
        /// <param name="syncVarIds"></param>
        /// <param name="dirtyBits"></param>
        /// <param name="failed"></param>
        private void ProcessSyncVar(TypeDefinition td, FieldDefinition fd, Dictionary<FieldDefinition, FieldDefinition> syncVarIds,
            long dirtyBits, ref bool failed)
        {
            FieldDefinition objectId = null;
            if (fd.FieldType.IsDerivedFrom<NetworkBehaviour>() || fd.FieldType.Is<NetworkBehaviour>())
            {
                objectId = new FieldDefinition($"{fd.Name}Id", FieldAttributes.Family, models.Import<NetworkValue>())
                {
                    DeclaringType = td
                };
                syncVarIds[fd] = objectId;
            }
            else if (fd.FieldType.IsNetworkObjectField())
            {
                objectId = new FieldDefinition($"{fd.Name}Id", FieldAttributes.Family, models.Import<uint>())
                {
                    DeclaringType = td
                };
                syncVarIds[fd] = objectId;
            }

            var get = GenerateSyncVarGetter(fd, fd.Name, objectId);
            var set = GenerateSyncVarSetter(td, fd, fd.Name, dirtyBits, objectId, ref failed);

            var pd = new PropertyDefinition($"Network{fd.Name}", PropertyAttributes.None, fd.FieldType)
            {
                GetMethod = get,
                SetMethod = set
            };

            td.Methods.Add(get);
            td.Methods.Add(set);
            td.Properties.Add(pd);

            access.setter[fd] = set;

            if (fd.FieldType.IsNetworkObjectField())
            {
                access.getter[fd] = get;
            }
        }

        /// <summary>
        /// 生成SyncVer的Getter
        /// </summary>
        /// <param name="fd"></param>
        /// <param name="originalName"></param>
        /// <param name="netFieldId"></param>
        /// <returns></returns>
        private MethodDefinition GenerateSyncVarGetter(FieldDefinition fd, string originalName, FieldDefinition netFieldId)
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

    public static class SyncVarProcessReplace
    {
        /// <summary>
        /// 用于NetworkBehaviour注入后，修正SyncVar
        /// </summary>
        /// <param name="md"></param>
        /// <param name="access"></param>
        public static void Process(ModuleDefinition md, SyncVarAccess access)
        {
            foreach (var td in md.Types.Where(td => td.IsClass))
            {
                ProcessClass(td, access);
            }
        }

        /// <summary>
        /// 处理类
        /// </summary>
        /// <param name="td"></param>
        /// <param name="access"></param>
        private static void ProcessClass(TypeDefinition td, SyncVarAccess access)
        {
            foreach (MethodDefinition md in td.Methods)
            {
                ProcessMethod(md, access);
            }

            foreach (TypeDefinition nested in td.NestedTypes)
            {
                ProcessClass(nested, access);
            }
        }

        /// <summary>
        /// 处理方法
        /// </summary>
        /// <param name="md"></param>
        /// <param name="access"></param>
        private static void ProcessMethod(MethodDefinition md, SyncVarAccess access)
        {
            if (md.Name == ".cctor" || md.Name == CONST.GEN_FUNC || md.Name.StartsWith(CONST.INV_METHOD))
            {
                return;
            }

            if (md.IsAbstract)
            {
                return;
            }

            if (md.Body is { Instructions: not null })
            {
                for (int i = 0; i < md.Body.Instructions.Count;)
                {
                    Instruction instr = md.Body.Instructions[i];
                    i += ProcessInstruction(md, instr, i, access);
                }
            }
        }

        /// <summary>
        /// 处理指令
        /// </summary>
        /// <param name="md"></param>
        /// <param name="instr"></param>
        /// <param name="index"></param>
        /// <param name="access"></param>
        /// <returns></returns>
        private static int ProcessInstruction(MethodDefinition md, Instruction instr, int index, SyncVarAccess access)
        {
            if (instr.OpCode == OpCodes.Stfld && instr.Operand is FieldDefinition OpStfLd)
            {
                ProcessSetInstruction(md, instr, OpStfLd, access);
            }

            if (instr.OpCode == OpCodes.Ldfld && instr.Operand is FieldDefinition OpLdfLd)
            {
                ProcessGetInstruction(md, instr, OpLdfLd, access);
            }

            if (instr.OpCode == OpCodes.Ldflda && instr.Operand is FieldDefinition OpLdfLda)
            {
                return ProcessLoadAddressInstruction(md, instr, OpLdfLda, access, index);
            }

            return 1;
        }

        /// <summary>
        /// 设置指令
        /// </summary>
        /// <param name="md"></param>
        /// <param name="i"></param>
        /// <param name="opField"></param>
        /// <param name="access"></param>
        private static void ProcessSetInstruction(MethodDefinition md, Instruction i, FieldDefinition opField, SyncVarAccess access)
        {
            if (md.Name == ".ctor") return;

            if (access.setter.TryGetValue(opField, out MethodDefinition replacement))
            {
                i.OpCode = OpCodes.Call;
                i.Operand = replacement;
            }
        }

        /// <summary>
        /// 获取指令
        /// </summary>
        /// <param name="md"></param>
        /// <param name="i"></param>
        /// <param name="opField"></param>
        /// <param name="access"></param>
        private static void ProcessGetInstruction(MethodDefinition md, Instruction i, FieldDefinition opField, SyncVarAccess access)
        {
            if (md.Name == ".ctor") return;

            if (access.getter.TryGetValue(opField, out MethodDefinition replacement))
            {
                i.OpCode = OpCodes.Call;
                i.Operand = replacement;
            }
        }

        /// <summary>
        /// 处理加载地址指令
        /// </summary>
        /// <param name="md"></param>
        /// <param name="instr"></param>
        /// <param name="opField"></param>
        /// <param name="access"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static int ProcessLoadAddressInstruction(MethodDefinition md, Instruction instr, FieldDefinition opField,
            SyncVarAccess access, int index)
        {
            if (md.Name == ".ctor") return 1;

            if (access.setter.TryGetValue(opField, out MethodDefinition replacement))
            {
                Instruction nextInstr = md.Body.Instructions[index + 1];

                if (nextInstr.OpCode == OpCodes.Initobj)
                {
                    ILProcessor worker = md.Body.GetILProcessor();
                    VariableDefinition tmpVariable = new VariableDefinition(opField.FieldType);
                    md.Body.Variables.Add(tmpVariable);

                    worker.InsertBefore(instr, worker.Create(OpCodes.Ldloca, tmpVariable));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Initobj, opField.FieldType));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Ldloc, tmpVariable));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Call, replacement));

                    worker.Remove(instr);
                    worker.Remove(nextInstr);
                    return 4;
                }
            }

            return 1;
        }
    }
}