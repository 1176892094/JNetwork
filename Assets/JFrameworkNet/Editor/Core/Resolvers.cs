using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Resolvers
    {
        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, string name, ref bool isFailed)
        {
            if (type == null)
            {
                logger.Error($"Cannot resolve method {name} without a class");
                isFailed = true;
                return null;
            }

            MethodReference method = ResolveMethod(type, assembly, logger, m => m.Name == name, ref isFailed);
            if (method == null)
            {
                logger.Error($"Method not found with name {name} in type {type.Name}", type);
                isFailed = true;
            }

            return method;
        }

        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, System.Func<MethodDefinition, bool> predicate, ref bool isFailed)
        {
            foreach (var method in type.Resolve().Methods.Where(predicate))
            {
                return assembly.MainModule.ImportReference(method);
            }

            logger.Error($"Method not found in type {type.Name}", type);
            isFailed = true;
            return null;
        }
        
        public static MethodDefinition ResolveDefaultPublicCtor(TypeReference variable)
        {
            foreach (MethodDefinition methodRef in variable.Resolve().Methods)
            {
                if (methodRef.Name == Const.CONSTRUCTOR && methodRef.Resolve().IsPublic && methodRef.Parameters.Count == 0)
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