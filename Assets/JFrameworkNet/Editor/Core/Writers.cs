using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly Dictionary<TypeReference, MethodReference> writeFuncList = new Dictionary<TypeReference, MethodReference>(new Comparator());
        private readonly AssemblyDefinition assembly;
        private readonly Logger logger;
        private readonly Processor processor;
        private readonly TypeDefinition generate;

        public Writers(AssemblyDefinition assembly, Processor processor, TypeDefinition generate, Logger logger)
        {
            this.assembly = assembly;
            this.processor = processor;
            this.generate = generate;
            this.logger = logger;
        }

        public void Register(TypeReference dataType, MethodReference methodReference)
        {
            TypeReference imported = assembly.MainModule.ImportReference(dataType);
            writeFuncList[imported] = methodReference;
        }

        private void RegisterWriteFunc(TypeReference typeReference, MethodDefinition newWriterFunc)
        {
            Register(typeReference, newWriterFunc);
            generate.Methods.Add(newWriterFunc);
        }
        
        public MethodReference GetWriteFunc(TypeReference variable, ref bool isFailed)
        {
            if (writeFuncList.TryGetValue(variable, out MethodReference foundFunc)) return foundFunc;
            
            try
            {
                TypeReference importedVariable = assembly.MainModule.ImportReference(variable);
                return GenerateWriter(importedVariable, ref isFailed);
            }
            catch (WriterException e)
            {
                logger.Error(e.Message, e.MemberReference);
                isFailed = true;
                return null;
            }
        }

        private MethodReference GenerateWriter(TypeReference variableReference, ref bool isFailed)
        {
            if (variableReference.IsByReference)
            {
                throw new WriterException($"Cannot pass {variableReference.Name} by reference", variableReference);
            }
            
            if (variableReference.IsArray)
            {
                if (variableReference.IsMultidimensionalArray())
                {
                    throw new WriterException($"{variableReference.Name} is an unsupported type. Multidimensional arrays are not supported", variableReference);
                }
                TypeReference elementType = variableReference.GetElementType();
                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteArray), ref isFailed);
            }

            if (variableReference.Resolve()?.IsEnum ?? false)
            {
                return GenerateEnumWriteFunc(variableReference, ref isFailed);
            }
            
            if (variableReference.Is(typeof(ArraySegment<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableReference;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteArraySegment), ref isFailed);
            }
            if (variableReference.Is(typeof(List<>)))
            {
                GenericInstanceType genericInstance = (GenericInstanceType)variableReference;
                TypeReference elementType = genericInstance.GenericArguments[0];

                return GenerateCollectionWriter(variableReference, elementType, nameof(StreamExtensions.WriteList), ref isFailed);
            }
            
            if (variableReference.IsDerivedFrom<NetworkEntity>() || variableReference.Is<NetworkEntity>())
            {
                return GetNetworkBehaviourWriter(variableReference);
            }
            
            TypeDefinition variableDefinition = variableReference.Resolve();
            if (variableDefinition == null)
            {
                throw new WriterException($"{variableReference.Name} is not a supported type.", variableReference);
            }
            if (variableDefinition.IsDerivedFrom<Component>())
            {
                throw new WriterException($"Cannot generate writer for component type {variableReference.Name}.", variableReference);
            }
            if (variableReference.Is<Object>())
            {
                throw new WriterException($"Cannot generate writer for {variableReference.Name}.", variableReference);
            }
            if (variableReference.Is<ScriptableObject>())
            {
                throw new WriterException($"Cannot generate writer for {variableReference.Name}.", variableReference);
            }
            if (variableDefinition.HasGenericParameters)
            {
                throw new WriterException($"Cannot generate writer for generic type {variableReference.Name}.", variableReference);
            }
            if (variableDefinition.IsInterface)
            {
                throw new WriterException($"Cannot generate writer for interface {variableReference.Name}.", variableReference);
            }
            if (variableDefinition.IsAbstract)
            {
                throw new WriterException($"Cannot generate writer for abstract class {variableReference.Name}.", variableReference);
            }
            
            return GenerateClassOrStructWriterFunction(variableReference, ref isFailed);
        }

        private MethodReference GetNetworkBehaviourWriter(TypeReference variableReference)
        {
            if (writeFuncList.TryGetValue(processor.Import<NetworkEntity>(), out MethodReference func))
            {
                Register(variableReference, func);
                return func;
            }
            
            throw new MissingMethodException($"Could not find writer for NetworkBehaviour");
        }

        private MethodDefinition GenerateEnumWriteFunc(TypeReference variable, ref bool isFailed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            MethodReference underlyingWriter = GetWriteFunc(variable.Resolve().GetEnumUnderlyingType(), ref isFailed);

            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, underlyingWriter);

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private MethodDefinition GenerateWriterFunc(TypeReference variable)
        {
            string functionName = $"Write{Math.Abs(variable.FullName.GetHashCode())}";
            MethodDefinition writerFunc = new MethodDefinition(functionName, Const.METHOD_ATTRS, processor.Import(typeof(void)));
            writerFunc.Parameters.Add(new ParameterDefinition("writer", ParameterAttributes.None, processor.Import<NetworkWriter>()));
            writerFunc.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, variable));
            writerFunc.Body.InitLocals = true;

            RegisterWriteFunc(variable, writerFunc);
            return writerFunc;
        }

        private MethodDefinition GenerateClassOrStructWriterFunction(TypeReference variable, ref bool isFailed)
        {
            MethodDefinition writerFunc = GenerateWriterFunc(variable);

            ILProcessor worker = writerFunc.Body.GetILProcessor();

            if (!variable.Resolve().IsValueType)
                WriteNullCheck(worker, ref isFailed);

            if (!WriteAllFields(variable, worker, ref isFailed))
                return null;

            worker.Emit(OpCodes.Ret);
            return writerFunc;
        }

        private void WriteNullCheck(ILProcessor worker, ref bool isFailed)
        {
            Instruction labelNotNull = worker.Create(OpCodes.Nop);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Brtrue, labelNotNull);
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_0);
            worker.Emit(OpCodes.Call, GetWriteFunc(processor.Import<bool>(), ref isFailed));
            worker.Emit(OpCodes.Ret);
            worker.Append(labelNotNull);
            
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldc_I4_1);
            worker.Emit(OpCodes.Call, GetWriteFunc(processor.Import<bool>(), ref isFailed));
        }
        
        private bool WriteAllFields(TypeReference variable, ILProcessor worker, ref bool isFailed)
        {
            foreach (FieldDefinition field in variable.FindAllPublicFields())
            {
                MethodReference writeFunc = GetWriteFunc(field.FieldType, ref isFailed);
                if (writeFunc == null) { return false; }

                FieldReference fieldRef = assembly.MainModule.ImportReference(field);

                worker.Emit(OpCodes.Ldarg_0);
                worker.Emit(OpCodes.Ldarg_1);
                worker.Emit(OpCodes.Ldfld, fieldRef);
                worker.Emit(OpCodes.Call, writeFunc);
            }

            return true;
        }

        private MethodDefinition GenerateCollectionWriter(TypeReference variable, TypeReference elementType, string writerFunction, ref bool isFailed)
        {

            MethodDefinition writerFunc = GenerateWriterFunc(variable);
            MethodReference elementWriteFunc = GetWriteFunc(elementType, ref isFailed);

            if (elementWriteFunc == null)
            {
                logger.Error($"Cannot generate writer for {variable}.", variable);
                isFailed = true;
                return writerFunc;
            }

            ModuleDefinition module = assembly.MainModule;
            TypeReference readerExtensions = module.ImportReference(typeof(StreamExtensions));
            MethodReference collectionWriter = Resolvers.ResolveMethod(readerExtensions, assembly, logger, writerFunction, ref isFailed);

            GenericInstanceMethod methodRef = new GenericInstanceMethod(collectionWriter);
            methodRef.GenericArguments.Add(elementType);
            
            ILProcessor worker = writerFunc.Body.GetILProcessor();
            worker.Emit(OpCodes.Ldarg_0);
            worker.Emit(OpCodes.Ldarg_1);
            worker.Emit(OpCodes.Call, methodRef);
            worker.Emit(OpCodes.Ret);

            return writerFunc;
        }
        
        internal void InitializeWriters(ILProcessor worker)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeReference genericWriterClassRef = module.ImportReference(typeof(Writer<>));
            FieldInfo fieldInfo = typeof(Writer<>).GetField(nameof(Writer<object>.write));
            FieldReference fieldRef = module.ImportReference(fieldInfo);
            TypeReference networkWriterRef = module.ImportReference(typeof(NetworkWriter));
            TypeReference actionRef = module.ImportReference(typeof(Action<,>));
            MethodReference actionConstructorRef = module.ImportReference(typeof(Action<,>).GetConstructors()[0]);

            foreach (var (type,method) in writeFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                GenericInstanceType actionGenericInstance = actionRef.MakeGenericInstanceType(networkWriterRef, type);
                MethodReference actionRefInstance = actionConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, actionGenericInstance);
                worker.Emit(OpCodes.Newobj, actionRefInstance);
                GenericInstanceType genericInstance = genericWriterClassRef.MakeGenericInstanceType(type);
                FieldReference specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}