using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    public static class StreamingProcess
    {
        private static void AddRuntimeInitializeOnLoadAttribute(AssemblyDefinition assembly, WeaverTypes weaverTypes, MethodDefinition method)
        {
            MethodDefinition ctor = weaverTypes.runtimeInitializeOnLoadMethodAttribute.GetConstructors().Last();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(ctor));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(weaverTypes.Import<RuntimeInitializeLoadType>(), RuntimeInitializeLoadType.BeforeSceneLoad));
            method.CustomAttributes.Add(attribute);
        }
        
        private static void AddInitializeOnLoadAttribute(AssemblyDefinition assembly, WeaverTypes weaverTypes, MethodDefinition method)
        {
            MethodDefinition ctor = weaverTypes.initializeOnLoadMethodAttribute.GetConstructors().First();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(ctor));
            method.CustomAttributes.Add(attribute);
        }
        
        public static void InitializeReaderAndWriters(AssemblyDefinition currentAssembly, WeaverTypes weaverTypes, Writers writers, Readers readers, TypeDefinition GeneratedCodeClass)
        {
            MethodDefinition initReadWriters = new MethodDefinition("RuntimeInitializeOnLoad", MethodAttributes.Public | MethodAttributes.Static, weaverTypes.Import(typeof(void)));
            
            AddRuntimeInitializeOnLoadAttribute(currentAssembly, weaverTypes, initReadWriters);
            
            if (Helpers.IsEditorAssembly(currentAssembly))
            {
                AddInitializeOnLoadAttribute(currentAssembly, weaverTypes, initReadWriters);
            }
            
            ILProcessor worker = initReadWriters.Body.GetILProcessor();
            writers.InitializeWriters(worker);
            readers.InitializeReaders(worker);
            worker.Emit(OpCodes.Ret);
            
            GeneratedCodeClass.Methods.Add(initReadWriters);
        }
    }
}