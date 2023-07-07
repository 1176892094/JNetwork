using System;
using System.Collections.Generic;
using System.Reflection;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    public class Readers
    {
        private readonly Dictionary<TypeReference, MethodReference> readFuncs = new Dictionary<TypeReference, MethodReference>(new Comparator());
        private AssemblyDefinition assembly;
        private WeaverTypes weaverTypes;
        private TypeDefinition GeneratedCodeClass;

        public Readers(AssemblyDefinition assembly, WeaverTypes weaverTypes, TypeDefinition GeneratedCodeClass)
        {
            this.assembly = assembly;
            this.weaverTypes = weaverTypes;
            this.GeneratedCodeClass = GeneratedCodeClass;
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
            foreach (var (type, method) in readFuncs)
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