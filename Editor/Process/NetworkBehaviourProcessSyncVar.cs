using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        /// <summary>
        /// 生成SyncVar的序列化方法
        /// </summary>
        private void GenerateSerialize(ref bool failed)
        {
            if (generate.GetMethod(CONST.SER_METHOD) != null) return;
            if (syncVars.Count == 0) return;
            var serialize = new MethodDefinition(CONST.SER_METHOD, CONST.SER_ATTRS, models.Import(typeof(void)));
            serialize.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, models.Import<NetworkWriter>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            var worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;
            var baseSerialize = Utils.TryResolveMethodInParents(generate.BaseType, assembly, CONST.SER_METHOD);
            if (baseSerialize != null)
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_2);
                worker.Emit(OpCodes.Call, baseSerialize);
            }

            Instruction isStart = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_2);
            worker.Emit(OpCodes.Brfalse, isStart);
            foreach (var syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generate.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);
                var writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? models.Import<NetworkBehaviour>() : syncVar.FieldType, ref failed);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    failed = true;
                    return;
                }
            }

            worker.Emit(OpCodes.Ret);
            worker.Append(isStart);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, models.NetworkBehaviourDirtyRef);
            var writeUint64Func = writers.GetWriteFunc(models.Import<ulong>(), ref failed);
            worker.Emit(OpCodes.Call, writeUint64Func);
            int dirty = access.GetSyncVar(generate.BaseType.FullName);
            foreach (var syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generate.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                var varLabel = worker.Create(OpCodes.Nop);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Call, models.NetworkBehaviourDirtyRef);
                worker.Emit(OpCodes.Ldc_I8, 1L << dirty);
                worker.Emit(OpCodes.And);
                worker.Emit(OpCodes.Brfalse, varLabel);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);

                var writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? models.Import<NetworkBehaviour>() : syncVar.FieldType, ref failed);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    failed = true;
                    return;
                }

                worker.Append(varLabel);
                dirty += 1;
            }

            worker.Emit(OpCodes.Ret);
            generate.Methods.Add(serialize);
        }

        /// <summary>
        /// 生成SyncVar的反序列化方法
        /// </summary>
        private void GenerateDeserialize(ref bool failed)
        {
            if (generate.GetMethod(CONST.DES_METHOD) != null) return;
            if (syncVars.Count == 0) return;
            var serialize = new MethodDefinition(CONST.DES_METHOD, CONST.SER_ATTRS, models.Import(typeof(void)));
            serialize.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, models.Import<NetworkReader>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            var worker = serialize.Body.GetILProcessor();
            
            serialize.Body.InitLocals = true;
            var dirtyBitsLocal = new VariableDefinition(models.Import<long>());
            serialize.Body.Variables.Add(dirtyBitsLocal);

            var baseDeserialize = Utils.TryResolveMethodInParents(generate.BaseType, assembly, CONST.DES_METHOD);
            if (baseDeserialize != null)
            {
                worker.Append(worker.Create(OpCodes.Ldarg_0));
                worker.Append(worker.Create(OpCodes.Ldarg_1));
                worker.Append(worker.Create(OpCodes.Ldarg_2));
                worker.Append(worker.Create(OpCodes.Call, baseDeserialize));
            }

            var isStart = worker.Create(OpCodes.Nop);

            worker.Append(worker.Create(OpCodes.Ldarg_2));
            worker.Append(worker.Create(OpCodes.Brfalse, isStart));

            foreach (var syncVar in syncVars)
            {
                DeserializeField(syncVar, worker, ref failed);
            }

            worker.Append(worker.Create(OpCodes.Ret));
            worker.Append(isStart);
            worker.Append(worker.Create(OpCodes.Ldarg_1));
            worker.Append(worker.Create(OpCodes.Call, readers.GetReadFunc(models.Import<ulong>(), ref failed)));
            worker.Append(worker.Create(OpCodes.Stloc_0));

            int dirtyBits = access.GetSyncVar(generate.BaseType.FullName);
            foreach (var syncVar in syncVars)
            {
                var varLabel = worker.Create(OpCodes.Nop);
                worker.Append(worker.Create(OpCodes.Ldloc_0));
                worker.Append(worker.Create(OpCodes.Ldc_I8, 1L << dirtyBits));
                worker.Append(worker.Create(OpCodes.And));
                worker.Append(worker.Create(OpCodes.Brfalse, varLabel));

                DeserializeField(syncVar, worker, ref failed);

                worker.Append(varLabel);
                dirtyBits += 1;
            }

            worker.Append(worker.Create(OpCodes.Ret));
            generate.Methods.Add(serialize);
        }

        /// <summary>
        /// 反序列化字段
        /// </summary>
        /// <param name="syncVar"></param>
        /// <param name="worker"></param>
        /// <param name="failed"></param>
        private void DeserializeField(FieldDefinition syncVar, ILProcessor worker, ref bool failed)
        {
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldflda, generate.HasGenericParameters ? syncVar.MakeHostInstanceGeneric() : syncVar);

            var hookMethod = process.GetHookMethod(generate, syncVar, ref failed);
            if (hookMethod != null)
            {
                process.GenerateNewActionFromHookMethod(syncVar, worker, hookMethod);
            }
            else
            {
                worker.Emit(OpCodes.Ldnull);
            }

            if (syncVar.FieldType.Is<GameObject>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                worker.Emit(OpCodes.Call, models.syncVarGetterGameObject);
            }
            else if (syncVar.FieldType.Is<NetworkObject>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                worker.Emit(OpCodes.Call, models.syncVarGetterNetworkObject);
            }
            else if (syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() || syncVar.FieldType.Is<NetworkBehaviour>())
            {
                var objectId = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, objectId);
                var getFunc = models.syncVarGetterNetworkBehaviour.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                var readFunc = readers.GetReadFunc(syncVar.FieldType, ref failed);
                if (readFunc == null)
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型。", syncVar);
                    failed = true;
                    return;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
                MethodReference generic = models.syncVarGetterGeneral.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, generic);
            }
        }
    }
}