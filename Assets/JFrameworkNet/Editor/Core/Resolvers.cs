using System;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Resolvers
    {
        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, string name)
        {
            if (type == null)
            {
                logger.Error($"没有无法解析方法: {name}");
                Command.failed = true;
                return null;
            }

            MethodReference method = ResolveMethod(type, assembly, logger, method => method.Name == name);
            if (method == null)
            {
                logger.Error($"在类型 {type.Name} 中没有找到名称 {name} 的方法", type);
                Command.failed = true;
            }

            return method;
        }

        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, Func<MethodDefinition, bool> predicate)
        {
            foreach (var method in type.Resolve().Methods.Where(predicate))
            {
                return assembly.MainModule.ImportReference(method);
            }

            logger.Error($"在类型 {type.Name} 中没有找到方法", type);
            Command.failed = true;
            return null;
        }
        
        public static MethodReference TryResolveMethodInParents(TypeReference tr, AssemblyDefinition assembly, string name)
        {
            if (tr == null)
            {
                return null;
            }
            foreach (MethodDefinition methodDef in tr.Resolve().Methods)
            {
                if (methodDef.Name == name)
                {
                    MethodReference methodRef = methodDef;
                    if (tr.IsGenericInstance)
                    {
                        methodRef = methodRef.MakeHostInstanceGeneric(tr.Module, (GenericInstanceType)tr);
                    }
                    return assembly.MainModule.ImportReference(methodRef);
                }
            }
            
            return TryResolveMethodInParents(tr.Resolve().BaseType.ApplyGenericParameters(tr), assembly, name);
        }
        
        public static MethodDefinition ResolveDefaultPublicCtor(TypeReference variable)
        {
            foreach (MethodDefinition methodRef in variable.Resolve().Methods)
            {
                if (methodRef.Name == CONST.CTOR && methodRef.Resolve().IsPublic && methodRef.Parameters.Count == 0)
                {
                    return methodRef;
                }
            }
            return null;
        }

        public static MethodReference ResolveProperty(TypeReference tr, AssemblyDefinition assembly, string name)
        {
            foreach (PropertyDefinition pd in tr.Resolve().Properties)
            {
                if (pd.Name == name)
                {
                    return assembly.MainModule.ImportReference(pd.GetMethod);
                }
            }
            return null;
        }
    }
}