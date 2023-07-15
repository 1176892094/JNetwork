using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal partial class NetworkBehaviourProcess
    {
        private void GenerateSerialization()
        {   
            if (generateCode.GetMethod(CONST.SER_METHOD) != null) return;
            if (syncVars.Count == 0) return;
            
            MethodDefinition serialize = new MethodDefinition(CONST.SER_METHOD, CONST.SER_ATTRS, process.Import(typeof(void)));

            serialize.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, process.Import<NetworkWriter>()));
            serialize.Parameters.Add(new ParameterDefinition("force", ParameterAttributes.None, process.Import<bool>()));
            ILProcessor worker = serialize.Body.GetILProcessor();

            serialize.Body.InitLocals = true;
            MethodReference baseSerialize = Resolvers.TryResolveMethodInParents(generateCode.BaseType, assembly, CONST.SER_METHOD);
            if (baseSerialize != null)
            {
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_2);
                worker.Emit(OpCodes.Call, baseSerialize);
            }
            
            Instruction initialStateLabel = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_2);           
            worker.Emit(OpCodes.Brfalse, initialStateLabel);
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
                MethodReference writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? process.Import<NetworkBehaviour>() : syncVar.FieldType);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    Command.failed = true;
                    return;
                }
            }
            
            worker.Emit(OpCodes.Ret);
            worker.Append(initialStateLabel);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, process.NetworkBehaviourDirtyReference);
            MethodReference writeUint64Func = writers.GetWriteFunc(process.Import<ulong>());
            worker.Emit(OpCodes.Call, writeUint64Func);
            int dirty = syncVarList.GetServerVar(generateCode.BaseType.FullName);
            foreach (FieldDefinition syncVarDef in syncVars)
            {
                FieldReference syncVar = syncVarDef;
                if (generateCode.HasGenericParameters)
                {
                    syncVar = syncVarDef.MakeHostInstanceGeneric();
                }
                Instruction varLabel = worker.Create(OpCodes.Nop);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Call, process.NetworkBehaviourDirtyReference);
                worker.Emit(OpCodes.Ldc_I8, 1L << dirty);
                worker.Emit(OpCodes.And);
                worker.Emit(OpCodes.Brfalse, varLabel);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldfld, syncVar);

                MethodReference writeFunc = writers.GetWriteFunc(syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() ? process.Import<NetworkBehaviour>() : syncVar.FieldType);

                if (writeFunc != null)
                {
                    worker.Emit(OpCodes.Call, writeFunc);
                }
                else
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型", syncVar);
                    Command.failed = true;
                    return;
                }

                worker.Append(varLabel);
                dirty += 1;
            }

            worker.Emit(OpCodes.Ret);
            generateCode.Methods.Add(serialize);
        }

        private void GenerateDeserialization()
        {
            if (generateCode.GetMethod(CONST.DES_METHOD) != null) return;
            if (syncVars.Count == 0) return;

            MethodDefinition serialize = new MethodDefinition(CONST.DES_METHOD, CONST.SER_ATTRS, process.Import(typeof(void)));

            serialize.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, process.Import<NetworkReader>()));
            serialize.Parameters.Add(new ParameterDefinition("force", ParameterAttributes.None, process.Import<bool>()));
            ILProcessor serWorker = serialize.Body.GetILProcessor();
            serialize.Body.InitLocals = true;
            VariableDefinition dirtyBitsLocal = new VariableDefinition(process.Import<long>());
            serialize.Body.Variables.Add(dirtyBitsLocal);

            MethodReference baseDeserialize = Resolvers.TryResolveMethodInParents(generateCode.BaseType, assembly, CONST.DES_METHOD);
            if (baseDeserialize != null)
            {
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_0));
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
                serWorker.Append(serWorker.Create(OpCodes.Ldarg_2));
                serWorker.Append(serWorker.Create(OpCodes.Call, baseDeserialize));
            }
            
            Instruction initialStateLabel = serWorker.Create(OpCodes.Nop);

            serWorker.Append(serWorker.Create(OpCodes.Ldarg_2));
            serWorker.Append(serWorker.Create(OpCodes.Brfalse, initialStateLabel));

            foreach (FieldDefinition syncVar in syncVars)
            {
                DeserializeField(syncVar, serWorker);
            }

            serWorker.Append(serWorker.Create(OpCodes.Ret));
            serWorker.Append(initialStateLabel);
            serWorker.Append(serWorker.Create(OpCodes.Ldarg_1));
            serWorker.Append(serWorker.Create(OpCodes.Call, readers.GetReadFunc(process.Import<ulong>())));
            serWorker.Append(serWorker.Create(OpCodes.Stloc_0));
            
            int dirty = syncVarList.GetServerVar(generateCode.BaseType.FullName);
            foreach (FieldDefinition syncVar in syncVars)
            {
                Instruction varLabel = serWorker.Create(OpCodes.Nop);
                
                serWorker.Append(serWorker.Create(OpCodes.Ldloc_0));
                serWorker.Append(serWorker.Create(OpCodes.Ldc_I8, 1L << dirty));
                serWorker.Append(serWorker.Create(OpCodes.And));
                serWorker.Append(serWorker.Create(OpCodes.Brfalse, varLabel));

                DeserializeField(syncVar, serWorker);

                serWorker.Append(varLabel);
                dirty += 1;
            }
            
            serWorker.Append(serWorker.Create(OpCodes.Ret));
            generateCode.Methods.Add(serialize);
        }

        private void DeserializeField(FieldDefinition syncVar, ILProcessor worker)
        {
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldflda, generateCode.HasGenericParameters ? syncVar.MakeHostInstanceGeneric() : syncVar);

            MethodDefinition hookMethod = serverVarProcess.GetHookMethod(generateCode, syncVar);
            if (hookMethod != null)
            {
                serverVarProcess.GenerateNewActionFromHookMethod(syncVar, worker, hookMethod);
            }
            else
            {
                worker.Emit(OpCodes.Ldnull);
            }
            
            if (syncVar.FieldType.Is<GameObject>())
            {
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarNetIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                worker.Emit(OpCodes.Call, process.syncVarGetterGameObject);
            }
            else if (syncVar.FieldType.Is<NetworkObject>())
            {
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarNetIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                worker.Emit(OpCodes.Call, process.syncVarGetterNetworkObject);
            }
            else if (syncVar.FieldType.IsDerivedFrom<NetworkBehaviour>() || syncVar.FieldType.Is<NetworkBehaviour>())
            {
                worker.Emit(OpCodes.Ldarg_1);
                FieldDefinition netIdField = syncVarNetIds[syncVar];
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldflda, netIdField);
                MethodReference getFunc = process.syncVarGetterNetworkBehaviour.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, getFunc);
            }
            else
            {
                MethodReference readFunc = readers.GetReadFunc(syncVar.FieldType);
                if (readFunc == null)
                {
                    logger.Error($"不支持 {syncVar.Name} 的类型。", syncVar);
                    Command.failed = true;
                    return;
                }
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Call, readFunc);
                MethodReference generic = process.syncVarGetterGeneral.MakeGeneric(assembly.MainModule, syncVar.FieldType);
                worker.Emit(OpCodes.Call, generic);
            }
        }
    }
}