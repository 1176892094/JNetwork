using System;
using System.Linq;
using System.Runtime.CompilerServices;
using JFramework.Net;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace JFramework.Editor
{
    internal static class StreamingProcess
    {
        public static bool Process(AssemblyDefinition currentAssembly, IAssemblyResolver resolver, Logger logger, Writers writers, Readers readers, ref bool isFailed)
        {
            ProcessNetworkCode(currentAssembly, resolver, logger, writers, readers, ref isFailed);
            return ProcessCustomCode(currentAssembly, currentAssembly, writers, readers, ref isFailed);
        }

        private static void ProcessNetworkCode(AssemblyDefinition currentAssembly, IAssemblyResolver resolver,Logger logger, Writers writers, Readers readers, ref bool isFailed)
        {
            AssemblyNameReference assemblyReference = currentAssembly.MainModule.FindReference(Const.ASSEMBLY_NAME);
            if (assemblyReference != null)
            {
                AssemblyDefinition networkAssembly = resolver.Resolve(assemblyReference);
                if (networkAssembly != null)
                {
                    ProcessCustomCode(currentAssembly, networkAssembly, writers, readers, ref isFailed);
                }
                else
                {
                    logger.Error($"Failed to resolve {assemblyReference}");
                }
            }
            else
            {
                logger.Error("Can't register JFramework.Net.dll readers/writers");
            }
        }

        private static bool ProcessCustomCode(AssemblyDefinition CurrentAssembly, AssemblyDefinition assembly,Writers writers, Readers readers, ref bool isFailed)
        {
            bool modified = false;
            foreach (var definition in assembly.MainModule.Types.Where(definition => definition.IsAbstract && definition.IsSealed))
            {
                modified |= LoadDeclaredWriters(CurrentAssembly, definition, writers);
                modified |= LoadDeclaredReaders(CurrentAssembly, definition, readers);
            }

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                modified |= LoadMessageReadWriter(CurrentAssembly.MainModule, writers, readers, type, ref isFailed);
            }
            return modified;
        }

        private static bool LoadDeclaredWriters(AssemblyDefinition currentAssembly, TypeDefinition type, Writers writers)
        {
            bool modified = false;
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Parameters.Count != 2)
                    continue;

                if (!method.Parameters[0].ParameterType.Is<NetworkWriter>())
                    continue;

                if (!method.ReturnType.Is(typeof(void)))
                    continue;

                if (!method.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (method.HasGenericParameters)
                    continue;
                
                writers.Register(method.Parameters[1].ParameterType, currentAssembly.MainModule.ImportReference(method));
                modified = true;
            }
            return modified;
        }

        private static bool LoadDeclaredReaders(AssemblyDefinition currentAssembly, TypeDefinition type, Readers readers)
        {
            bool modified = false;
            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Parameters.Count != 1)
                    continue;

                if (!method.Parameters[0].ParameterType.Is<NetworkReader>())
                    continue;

                if (method.ReturnType.Is(typeof(void)))
                    continue;

                if (!method.HasCustomAttribute<ExtensionAttribute>())
                    continue;

                if (method.HasGenericParameters)
                    continue;

                readers.Register(method.ReturnType, currentAssembly.MainModule.ImportReference(method));
                modified = true;
            }
            return modified;
        }


        private static bool LoadMessageReadWriter(ModuleDefinition module, Writers writers, Readers readers, TypeDefinition type, ref bool isFailed)
        {
            bool modified = false;
            if (!type.IsAbstract && !type.IsInterface && type.ImplementsInterface<NetworkMessage>())
            {
                readers.GetReadFunc(module.ImportReference(type), ref isFailed);
                writers.GetWriteFunc(module.ImportReference(type), ref isFailed);
                modified = true;
            }

            foreach (TypeDefinition nested in type.NestedTypes)
            {
                modified |= LoadMessageReadWriter(module, writers, readers, nested, ref isFailed);
            }

            return modified;
        }


        private static void AddRuntimeInitializeOnLoadAttribute(AssemblyDefinition assembly, Processor process, MethodDefinition method)
        {
            MethodDefinition definition = process.runtimeInitializeOnLoadMethodAttribute.GetConstructors().Last();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(definition));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(process.Import<RuntimeInitializeLoadType>(), RuntimeInitializeLoadType.BeforeSceneLoad));
            method.CustomAttributes.Add(attribute);
        }
        
        private static void AddInitializeOnLoadAttribute(AssemblyDefinition assembly, Processor process, MethodDefinition method)
        {
            MethodDefinition ctor = process.initializeOnLoadMethodAttribute.GetConstructors().First();
            CustomAttribute attribute = new CustomAttribute(assembly.MainModule.ImportReference(ctor));
            method.CustomAttributes.Add(attribute);
        }
        
        public static void InitializeReaderAndWriters(AssemblyDefinition currentAssembly, Processor process, Writers writers, Readers readers, TypeDefinition generatedClass)
        {
            MethodDefinition initReadWriters = new MethodDefinition("RuntimeInitializeOnLoad", MethodAttributes.Public | MethodAttributes.Static, process.Import(typeof(void)));
            
            AddRuntimeInitializeOnLoadAttribute(currentAssembly, process, initReadWriters);
            
            if (Helpers.IsEditorAssembly(currentAssembly))
            {
                AddInitializeOnLoadAttribute(currentAssembly, process, initReadWriters);
            }
            
            ILProcessor worker = initReadWriters.Body.GetILProcessor();
            writers.InitializeWriters(worker);
            readers.InitializeReaders(worker);
            worker.Emit(OpCodes.Ret);
            generatedClass.Methods.Add(initReadWriters);
        }
    }
}