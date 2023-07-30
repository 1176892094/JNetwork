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
            if (generateCode.GetMethod(CONST.SER_METHOD) != null) return;
            if (syncVars.Count == 0) return;

            MethodDefinition serialize =
                new MethodDefinition(CONST.SER_METHOD, CONST.SER_ATTRS, models.Import(typeof(void)));

            serialize.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None,
                models.Import<NetworkWriter>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            ILProcessor worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;
            MethodReference baseSerialize =
                Utils.TryResolveMethodInParents(generateCode.BaseType, assembly, CONST.SER_METHOD);
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
            foreach (FieldDefinition syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;

                if (generateCode.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);
                MethodReference writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>()
                    ? models.Import<NetworkBehaviour>()
                    : syncVar.FieldType,ref failed);

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
            MethodReference writeUint64Func = writers.GetWriteFunc(models.Import<ulong>(),ref failed);
            worker.Emit(OpCodes.Call, writeUint64Func);
            int dirty = SyncVarHelpers.GetSyncVar(generateCode.BaseType.FullName);
            foreach (FieldDefinition syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generateCode.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }

                Instruction varLabel = worker.Create(OpCodes.Nop);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Call, models.NetworkBehaviourDirtyRef);
                worker.Emit(OpCodes.Ldc_I8, 1L << dirty);
                worker.Emit(OpCodes.And);
                worker.Emit(OpCodes.Brfalse, varLabel);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);

                MethodReference writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>()
                    ? models.Import<NetworkBehaviour>()
                    : syncVar.FieldType,ref failed);

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
            generateCode.Methods.Add(serialize);
        }

        /// <summary>
        /// 生成SyncVar的反序列化方法
        /// </summary>
        private void GenerateDeserialize(ref bool failed)
        {
            if (generateCode.GetMethod(CONST.DES_METHOD) != null) return;
            if (syncVars.Count == 0) return;

            MethodDefinition serialize =
                new MethodDefinition(CONST.DES_METHOD, CONST.SER_ATTRS, models.Import(typeof(void)));

            serialize.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None,
                models.Import<NetworkReader>()));
            serialize.Parameters.Add(new ParameterDefinition("start", ParameterAttributes.None, models.Import<bool>()));
            ILProcessor serWorker = serialize.Body.GetILProcessor();
            serialize.Body.InitLocals = true;
            VariableDefinition dirtyBitsLocal = new VariableDefinition(models.Import<long>());
            serialize.Body.Variables.Add(dirtyBitsLocal);

            MethodReference baseDeserialize =
                Utils.TryResolveMethodInParents(generateCode.BaseType, assembly, CONST.DES_METHOD);
            if (baseDeserialize != null)
            {
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_2));
                serWorker.Append(serWorker.Create(OpCodes.Call, baseDeserialize));
            }

            Instruction isStart = serWorker.Create(OpCodes.Nop);

            serWorker.Append(serWorker.Create(OpCodes.Ldarg_2));
            serWorker.Append(serWorker.Create(OpCodes.Brfalse, isStart));

            foreach (FieldDefinition syncVar in syncVars)
            {
                DeserializeField(syncVar, serWorker, ref failed);
            }

            serWorker.Append(serWorker.Create(OpCodes.Ret));
            serWorker.Append(isStart);
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
            serWorker.Append(serWorker.Create(OpCodes.Call, readers.GetReadFunc(models.Import<ulong>(),ref failed)));
            serWorker.Append(serWorker.Create(OpCodes.Stloc_0));

            int dirty = SyncVarHelpers.GetSyncVar(generateCode.BaseType.FullName);
            foreach (FieldDefinition syncVar in syncVars)
            {
                Instruction varLabel = serWorker.Create(OpCodes.Nop);

                serWorker.Append(serWorker.Create(OpCodes.Ldloc_0));
                serWorker.Append(serWorker.Create(OpCodes.Ldc_I8, 1L << dirty));
                serWorker.Append(serWorker.Create(OpCodes.And));
                serWorker.Append(serWorker.Create(OpCodes.Brfalse, varLabel));

                DeserializeField(syncVar, serWorker, ref failed);

                serWorker.Append(varLabel);
                dirty += 1;
            }

            serWorker.Append(serWorker.Create(OpCodes.Ret));
            generateCode.Methods.Add(serialize);
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
            worker.Emit(OpCodes.Ldflda,
                generateCode.HasGenericParameters ? syncVar.MakeHostInstanceGeneric() : syncVar);

            MethodDefinition hookMethod = process.GetHookMethod(generateCode, syncVar, ref failed);
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
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                worker.Emit(OpCodes.Call, models.syncVarGetterGameObject);
            }
            else if (syncVar.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                worker.Emit(OpCodes.Call, models.syncVarGetterNetworkObject);
            }
            else if (syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() || syncVar.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                MethodReference getFunc = models.syncVarGetterNetworkBehaviour.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                MethodReference readFunc = readers.GetReadFunc(syncVar.FieldType,ref failed);
                if (readFunc == null)
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型。", syncVar);
                    failed = true;
                    return;
                }

                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
                MethodReference generic =
                    models.syncVarGetterGeneral.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, generic);
            }
        }
    }
}