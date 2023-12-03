using System;
using System.Collections.Generic;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using Object = UnityEngine.Object;
using Component = UnityEngine.Component;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace JFramework.Editor
{
    internal class Readers
    {
        private readonly Dictionary<TypeReference, MethodReference> readFuncList = new Dictionary<TypeReference, MethodReference>(new Comparer());

        private readonly Models models;
        private readonly Logger logger;
        private readonly TypeDefinition generate;
        private readonly AssemblyDefinition assembly;

        public Readers(AssemblyDefinition assembly, Models models, TypeDefinition generate, Logger logger)
        {
            this.logger = logger;
            this.models = models;
            this.assembly = assembly;
            this.generate = generate;
        }

        internal void Register(TypeReference dataType, MethodReference md)
        {
            var imported = assembly.MainModule.ImportReference(dataType);
            readFuncList[imported] = md;
        }

        private void RegisterReadFunc(TypeReference td, MethodDefinition newReaderFunc)
        {
            Register(td, newReaderFunc);
            generate.Methods.Add(newReaderFunc);
        }

        public MethodReference GetReadFunc(TypeReference variable, ref bool failed)
        {
            if (readFuncList.TryGetValue(variable, out MethodReference foundFunc))
            {
                return foundFunc;
            }

            var importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateReader(importedVariable, ref failed);
        }

        private MethodReference GenerateReader(TypeReference tr, ref bool failed)
        {
            if (tr.IsArray)
            {
                if (tr.IsMultidimensionalArray())
                {
                    logger.Error($"无法为多维数组 {tr.Name} 生成 Reader", tr);
                    failed = true;
                    return null;
                }

                return GenerateReadCollection(tr, tr.GetElementType(), nameof(StreamExtensions.ReadArray), ref failed);
            }

            var variable = tr.Resolve();
            if (variable == null)
            {
                logger.Error($"无法为Null {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.IsByReference)
            {
                logger.Error($"无法为反射 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (variable.IsEnum)
            {
                return GenerateEnumReadFunc(tr, ref failed);
            }

            if (variable.Is(typeof(ArraySegment<>)))
            {
                return GenerateArraySegmentReadFunc(tr, ref failed);
            }

            if (variable.Is(typeof(List<>)))
            {
                var genericInstance = (GenericInstanceType)tr;
                var elementType = genericInstance.GenericArguments[0];

                return GenerateReadCollection(tr, elementType, nameof(StreamExtensions.ReadList), ref failed);
            }

            if (tr.IsDerivedFrom<NetworkBehaviour>() || tr.Is<NetworkBehaviour>())
            {
                return GetNetworkBehaviourReader(tr);
            }

            if (variable.IsDerivedFrom<Component>())
            {
                logger.Error($"无法为组件 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.Is<Object>())
            {
                logger.Error($"无法为对象 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (tr.Is<ScriptableObject>())
            {
                logger.Error($"无法为可视化脚本 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (variable.HasGenericParameters)
            {
                logger.Error($"无法为通用变量 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (variable.IsInterface)
            {
                logger.Error($"无法为接口 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            if (variable.IsAbstract)
            {
                logger.Error($"无法为抽象或泛型 {tr.Name} 生成 Reader", tr);
                failed = true;
                return null;
            }

            return GenerateClassOrStructReadFunction(tr, ref failed);
        }

        private MethodDefinition GenerateReadCollection(TypeReference variable, TypeReference elementType, string readerFunction, ref bool failed)
        {
            var readerFunc = GenerateReaderFunction(variable);
            GetReadFunc(elementType, ref failed);
            var module = assembly.MainModule;
            var readerExtensions = module.ImportReference(typeof(StreamExtensions));
            var listReader = Helper.ResolveMethod(readerExtensions, assembly, logger, readerFunction, ref failed);
            var methodRef = new GenericInstanceMethod(listReader);
            methodRef.GenericArguments.Add(elementType);
            var worker = readerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, methodRef);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodDefinition GenerateReaderFunction(TypeReference variable)
        {
            var functionName = $"Read{NetworkMessage.GetHashByName(variable.FullName)}";
            var readerFunc = new MethodDefinition(functionName, CONST.RAW_ATTRS, variable);
            readerFunc.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, models.Import<NetworkReader>()));
            readerFunc.Body.InitLocals = true;
            RegisterReadFunc(variable, readerFunc);
            return readerFunc;
        }

        private MethodDefinition GenerateEnumReadFunc(TypeReference variable, ref bool failed)
        {
            var readerFunc = GenerateReaderFunction(variable);
            var worker = readerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            var underlyingType = variable.Resolve().GetEnumUnderlyingType();
            var underlyingFunc = GetReadFunc(underlyingType, ref failed);
            worker.Emit(OpCodes.Call, underlyingFunc);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodDefinition GenerateArraySegmentReadFunc(TypeReference variable, ref bool failed)
        {
            var genericInstance = (GenericInstanceType)variable;
            var elementType = genericInstance.GenericArguments[0];
            var readerFunc = GenerateReaderFunction(variable);
            var worker = readerFunc.Body.GetILProcessor();
            var arrayType = new ArrayType(elementType);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetReadFunc(arrayType, ref failed));
            worker.Emit(OpCodes.Newobj, models.ArraySegmentRef.MakeHostInstanceGeneric(assembly.MainModule, genericInstance));
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private MethodReference GetNetworkBehaviourReader(TypeReference variableReference)
        {
            var generic = models.ReadNetworkBehaviourGeneric;
            var readFunc = generic.MakeGeneric(assembly.MainModule, variableReference);
            Register(variableReference, readFunc);
            return readFunc;
        }

        private MethodDefinition GenerateClassOrStructReadFunction(TypeReference variable, ref bool failed)
        {
            var readerFunc = GenerateReaderFunction(variable);

            readerFunc.Body.Variables.Add(new VariableDefinition(variable));

            var worker = readerFunc.Body.GetILProcessor();

            var td = variable.Resolve();

            if (!td.IsValueType)
            {
                GenerateNullCheck(worker, ref failed);
            }

            CreateNew(variable, worker, td, ref failed);
            ReadAllFields(variable, worker, ref failed);

            worker.Emit(OpCodes.Ldloc_0);
            worker.Emit(OpCodes.Ret);
            return readerFunc;
        }

        private void GenerateNullCheck(ILProcessor worker, ref bool failed)
        {
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Call, GetReadFunc(models.Import<bool>(), ref failed));
            var labelEmptyArray = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Brtrue, labelEmptyArray);
            worker.Emit(OpCodes.Ldnull);
            worker.Emit(OpCodes.Ret);
            worker.Append(labelEmptyArray);
        }

        private void CreateNew(TypeReference variable, ILProcessor worker, TypeDefinition td, ref bool failed)
        {
            if (variable.IsValueType)
            {
                worker.Emit(OpCodes.Ldloca, 0);
                worker.Emit(OpCodes.Initobj, variable);
            }
            else if (td.IsDerivedFrom<ScriptableObject>())
            {
                var genericInstanceMethod = new GenericInstanceMethod(models.CreateInstanceMethodRef);
                genericInstanceMethod.GenericArguments.Add(variable);
                worker.Emit(OpCodes.Call, genericInstanceMethod);
                worker.Emit(OpCodes.Stloc_0);
            }
            else
            {
                MethodDefinition ctor = Helper.ResolveDefaultPublicCtor(variable);
                if (ctor == null)
                {
                    logger.Error($"{variable.Name} 不能被反序列化，因为它没有默认的构造函数", variable);
                    failed = true;
                    return;
                }

                var ctorRef = assembly.MainModule.ImportReference(ctor);

                worker.Emit(OpCodes.Newobj, ctorRef);
                worker.Emit(OpCodes.Stloc_0);
            }
        }

        private void ReadAllFields(TypeReference variable, ILProcessor worker, ref bool failed)
        {
            foreach (var field in variable.FindAllPublicFields())
            {
                var opcode = variable.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;
                worker.Emit(opcode, 0);
                var readFunc = GetReadFunc(field.FieldType, ref failed);
                if (readFunc != null)
                {
                    worker.Emit(OpCodes.Ldarg_0);
                    worker.Emit(OpCodes.Call, readFunc);
                }
                else
                {
                    logger.Error($"{field.Name} 有不受支持的类型", field);
                    failed = true;
                }

                var fieldRef = assembly.MainModule.ImportReference(field);
                worker.Emit(OpCodes.Stfld, fieldRef);
            }
        }

        internal void InitializeReaders(ILProcessor worker)
        {
            var module = assembly.MainModule;
            var genericReaderClassRef = module.ImportReference(typeof(Reader<>));
            var fieldInfo = typeof(Reader<>).GetField(nameof(Reader<object>.read));
            var fieldRef = module.ImportReference(fieldInfo);
            var networkReaderRef = module.ImportReference(typeof(NetworkReader));
            var funcRef = module.ImportReference(typeof(Func<,>));
            var funcConstructorRef = module.ImportReference(typeof(Func<,>).GetConstructors()[0]);
            foreach (var (type, method) in readFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                var funcGenericInstance = funcRef.MakeGenericInstanceType(networkReaderRef, type);
                var funcConstructorInstance = funcConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, funcGenericInstance);
                worker.Emit(OpCodes.Newobj, funcConstructorInstance);
                var genericInstance = genericReaderClassRef.MakeGenericInstanceType(type);
                var specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}