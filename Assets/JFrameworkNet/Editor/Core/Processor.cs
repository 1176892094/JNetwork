using System;
using JFramework.Net;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace JFramework.Editor
{
    internal class Processor
    {
        private readonly AssemblyDefinition assembly;

        public readonly MethodReference ArraySegmentConstructorReference;
        public readonly MethodReference ScriptableObjectCreateInstanceMethod;
        public readonly MethodReference readNetworkBehaviourGeneric;
        
        public readonly TypeDefinition initializeOnLoadMethodAttribute;
        public readonly TypeDefinition runtimeInitializeOnLoadMethodAttribute;

        public TypeReference Import<T>() => Import(typeof(T));
        public TypeReference Import(Type t) => assembly.MainModule.ImportReference(t);

        public Processor(AssemblyDefinition assembly, Logger logger, ref bool isFailed)
        {
            this.assembly = assembly;

            TypeReference ArraySegmentType = Import(typeof(ArraySegment<>));
            ArraySegmentConstructorReference = Resolvers.ResolveMethod(ArraySegmentType, assembly, logger, Const.CONSTRUCTOR, ref isFailed);
            
            TypeReference readerExtensions = Import(typeof(StreamExtensions));
            readNetworkBehaviourGeneric = Resolvers.ResolveMethod(readerExtensions, assembly, logger, method => method.Name == nameof(StreamExtensions.ReadNetworkBehaviour) && method.HasGenericParameters, ref isFailed);
            
            TypeReference ScriptableObjectType = Import<ScriptableObject>();
            ScriptableObjectCreateInstanceMethod = Resolvers.ResolveMethod(ScriptableObjectType, assembly, logger, method => method.Name == "CreateInstance" && method.HasGenericParameters, ref isFailed);
            
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