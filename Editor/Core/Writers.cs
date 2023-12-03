using System;
using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using Object = UnityEngine.Object;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace JFramework.Editor
{
    internal class Writers
    {
        private readonly Dictionary<TypeReference, MethodReference> writeFuncList = new Dictionary<TypeReference, MethodReference>(new Comparer());
        private readonly Models models;
        private readonly Logger logger;
        private readonly TypeDefinition generate;
        private readonly AssemblyDefinition assembly;

        public Writers(AssemblyDefinition assembly, Models models, TypeDefinition generate, Logger logger)
        {
            this.logger = logger;
            this.models = models;
            this.assembly = assembly;
            this.generate = generate;
        }

        public void Register(TypeReference dataType, MethodReference md)
        {
            var imported = assembly.MainModule.ImportReference(dataType);
            writeFuncList[imported] = md;
        }

        private void RegisterWriteFunc(TypeReference tr, MethodDefinition func)
        {
            Register(tr, func);
            generate.Methods.Add(func);
        }

        public MethodReference GetWriteFunc(TypeReference variable, ref bool failed)
        {
            if (writeFuncList.TryGetValue(variable, out MethodReference func))
            {
                return func;
            }

            var importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateWriter(importedVariable, ref failed);
        }

        private MethodReference GenerateWriter(TypeReference tr, ref bool failed)
        {
            if (tr.IsArray)
            {
                if (tr.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {tr.Name} 生成 Writer", tr);
                }

                var elementType = tr.GetElementType();
                return GenerateCollectionWriter(tr, elementType, nameof(StreamExtensions.WriteArray), ref failed);
            }

            if (tr.IsByReference)
            {
                logger.Error($"无法为反射 {tr.Name} 生成 Writer", tr);
            }

            if (tr.Resolve()?.IsEnum ?? false)
            {
                return GenerateEnumWriteFunc(tr, ref failed);
            }

            if (tr.Is(typeof(ArraySegment<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];
                return GenerateCollectionWriter(tr, elementType, nameof(StreamExtensions.WriteArraySegment), ref failed);
            }

            if (tr.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];
                return GenerateCollectionWriter(tr, elementType, nameof(StreamExtensions.WriteList), ref failed);
            }

            if (tr.IsDerivedFrom<NetworkBehaviour>() || tr.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviourWriter(tr);
            }

            var variable = tr.Resolve();
            if (variable == null)
            {
                logger.Error($"无法为Null {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (variable.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (tr.Is<Object>())
            {
                logger.Error($"无法为对象 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (tr.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (variable.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (variable.IsInterface)
            {
                logger.Error($"无法为接口 {tr.Name} 生成 Writer", tr);
                return null;
            }

            if (variable.IsAbstract)
            {
                logger.Error($"无法为抽象或泛型 {tr.Name} 生成 Writer", tr);
                return null;
            }

            return GenerateClassOrStructWriterFunction(tr, ref failed);
        }

        private MethodReference GetNetworkBehaviourWriter(TypeReference variable)
        {
            if (writeFuncList.TryGetValue(models.Import<NetworkBehaviour>(), out MethodReference func))
            {
                Register(variable, func);
                return func;
            }

            throw new MissingMethodException($"无法从 NetworkBehaviour 获取 Writer");
        }

        private MethodDefinition GenerateEnumWriteFunc(TypeReference variable, ref bool failed)
        {
            var writerFunc = GenerateWriterFunc(variable);
            var worker = writerFunc.Body.GetILProcessor();
            var underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType(), ref failed);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, underlyingWriter);

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private MethodDefinition GenerateWriterFunc(TypeReference variable)
        {
            var functionName = $"Write{NetworkMessage.GetHashByName(variable.FullName)}";
            var writerFunc = new MethodDefinition(functionName, CONST.RAW_ATTRS, models.Import(typeof(void)));
            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, models.Import<NetworkWriter>()));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
            writerFunc.Body.InitLocals = true;

            RegisterWriteFunc(variable, writerFunc);
            return writerFunc;
        }

        private MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable, ref bool failed)
        {
            var writerFunc = GenerateWriterFunc(variable);
            var worker = writerFunc.Body.GetILProcessor();

            if (!variable.Resolve().IsValueType)
            {
                WriteNullCheck(worker, ref failed);
            }

            if (!WriteAllFields(variable, worker, ref failed))
            {
                return null;
            }

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private void WriteNullCheck(ILProcessor worker, ref bool failed)
        {
            var labelNotNull = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, labelNotNull);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetWriteFunc(models.Import<bool>(), ref failed));
            worker.Emit(OpCodes.Ret);
            worker.Append(labelNotNull);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetWriteFunc(models.Import<bool>(), ref failed));
        }

        private bool WriteAllFields(TypeReference variable, ILProcessor worker, ref bool failed)
        {
            foreach (var field in variable.FindAllPublicFields())
            {
                var writeFunc = GetWriteFunc(field.FieldType, ref failed);
                if (writeFunc == null)
                {
                    return false;
                }

                var fieldRef = assembly.MainModule.ImportReference(field);
                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldfld, fieldRef);
                worker.Emit(OpCodes.Call, writeFunc);
            }

            return true;
        }

        private MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType, string writerFunction, ref bool failed)
        {
            var writerFunc = GenerateWriterFunc(variable);
            var elementWriteFunc = GetWriteFunc(elementType, ref failed);

            if (elementWriteFunc == null)
            {
                logger.Error($"无法为 {variable} 生成 Writer", variable);
                failed = true;
                return writerFunc;
            }

            var module = assembly.MainModule;
            var readerExtensions = module.ImportReference(typeof(StreamExtensions));
            var collectionWriter = Helper.ResolveMethod(readerExtensions, assembly, logger, writerFunction,ref failed);

            var methodRef = new GenericInstanceMethod(collectionWriter);
            methodRef.GenericArguments.Add(elementType);

            var worker = writerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, methodRef);
            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        internal void InitializeWriters(ILProcessor worker)
        {
            var module = assembly.MainModule;
            var genericWriterClassRef = module.ImportReference(typeof(Writer<>));
            var fieldInfo = typeof(Writer<>).GetField(nameof(Writer<object>.write));
            var fieldRef = module.ImportReference(fieldInfo);
            var networkWriterRef = module.ImportReference(typeof(NetworkWriter));
            var actionRef = module.ImportReference(typeof(Action<,>));
            var actionConstructorRef = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            foreach (var (type, method) in writeFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                var actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, type);
                var actionRefInstance = actionConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, actionGenericInstance);
                worker.Emit(OpCodes.Newobj, actionRefInstance);
                var genericInstance = genericWriterClassRef.MakeGenericInstanceType(type);
                var specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}