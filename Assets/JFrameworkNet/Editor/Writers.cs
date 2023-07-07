using System;
using System.Collections.Generic;
using System.Reflection;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    public class Writers
    {
        private readonly Dictionary<TypeReference, MethodReference> writeFuncList = new Dictionary<TypeReference, MethodReference>(new Comparator());
        private readonly AssemblyDefinition assembly;
        private readonly WeaverTypes weaverTypes;
        private readonly TypeDefinition GeneratedCodeClass;

        public Writers(AssemblyDefinition assembly, WeaverTypes weaverTypes, TypeDefinition GeneratedCodeClass)
        {
            this.assembly = assembly;
            this.weaverTypes = weaverTypes;
            this.GeneratedCodeClass = GeneratedCodeClass;
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