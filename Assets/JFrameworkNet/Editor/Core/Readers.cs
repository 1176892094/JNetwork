using System;
using System.Collections.Generic;
using System.Reflection;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    internal class Readers
    {
        private readonly Dictionary<TypeReference, MethodReference> readFuncList = new Dictionary<TypeReference, MethodReference>(new Comparator());
        private readonly AssemblyDefinition assembly;
        private Processor process;
        private TypeDefinition generate;
        private Logger logger;

        public Readers(AssemblyDefinition assembly, Processor process, TypeDefinition generate, Logger logger)
        {
            this.assembly = assembly;
            this.process = process;
            this.generate = generate;
            this.logger = logger;
        }

        internal void Register(TypeReference dataType, MethodReference methodReference)
        {
            TypeReference imported = assembly.MainModule.ImportReference(dataType);
            readFuncList[imported] = methodReference;
        }

        public MethodReference GetReadFunc(TypeReference variable, ref bool WeavingFailed)
        {
            if (readFuncList.TryGetValue(variable, out MethodReference foundFunc))
            {
                return foundFunc;
            }

            TypeReference importedVariable = assembly.MainModule.ImportReference(variable);
            return GenerateReader(importedVariable, ref WeavingFailed);
        }

        private MethodReference GenerateReader(TypeReference variableReference, ref bool weavingFailed)
        {
            return null;
        }

        internal void InitializeReaders(ILProcessor worker)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeReference genericReaderClassRef = module.ImportReference(typeof(Reader<>));
            FieldInfo fieldInfo = typeof(Reader<>).GetField(nameof(Reader<object>.read));
            FieldReference fieldRef = module.ImportReference(fieldInfo);
            TypeReference networkReaderRef = module.ImportReference(typeof(NetworkReader));
            TypeReference funcRef = module.ImportReference(typeof(Func<,>));
            MethodReference funcConstructorRef = module.ImportReference(typeof(Func<,>).GetConstructors()[0]);
            foreach (var (type, method) in readFuncList)
            {
                worker.Emit(OpCodes.Ldnull);
                worker.Emit(OpCodes.Ldftn, method);
                GenericInstanceType funcGenericInstance = funcRef.MakeGenericInstanceType(networkReaderRef, type);
                MethodReference funcConstructorInstance = funcConstructorRef.MakeHostInstanceGeneric(assembly.MainModule, funcGenericInstance);
                worker.Emit(OpCodes.Newobj, funcConstructorInstance);
                GenericInstanceType genericInstance = genericReaderClassRef.MakeGenericInstanceType(type);
                FieldReference specializedField = fieldRef.SpecializeField(assembly.MainModule, genericInstance);
                worker.Emit(OpCodes.Stsfld, specializedField);
            }
        }
    }
}