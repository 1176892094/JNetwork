using System;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace JFramework.Editor
{
    public class WeaverTypes
    {
        private readonly AssemblyDefinition assembly;

        public readonly TypeDefinition initializeOnLoadMethodAttribute;
        public readonly TypeDefinition runtimeInitializeOnLoadMethodAttribute;
        
        public TypeReference Import<T>() => Import(typeof(T));
        public TypeReference Import(Type t) => assembly.MainModule.ImportReference(t);

        public WeaverTypes(AssemblyDefinition assembly, Logger Log, ref bool WeavingFailed)
        {
            this.assembly = assembly;
            
            if (Helpers.IsEditorAssembly(assembly))
            {
                TypeReference initializeOnLoadMethodAttributeRef = Import(typeof(InitializeOnLoadMethodAttribute));
                initializeOnLoadMethodAttribute = initializeOnLoadMethodAttributeRef.Resolve();
            }
            
            TypeReference runtimeInitializeOnLoadMethodAttributeRef = Import(typeof(RuntimeInitializeOnLoadMethodAttribute));
            runtimeInitializeOnLoadMethodAttribute = runtimeInitializeOnLoadMethodAttributeRef.Resolve();
        }
    }
}