using System;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Resolvers
    {
        public static FieldReference ResolveField(TypeReference type, AssemblyDefinition assembly, Logger logger, string name, ref bool isFailed)
        {
            if (type == null)
            {
                logger.Error($"没有无法解析字段: {name}");
                isFailed = true;
                return null;
            }

            FieldReference field = ResolveField(type, assembly, logger, field => field.Name == name, ref isFailed);
            if (field == null)
            {
                logger.Error($"在类型 {type.Name} 中没有找到名称 {name} 的字段", type);
                isFailed = true;
            }

            return field;
        }

        private static FieldReference ResolveField(TypeReference type, AssemblyDefinition assembly, Logger logger,Func<FieldDefinition, bool> predicate, ref bool isFailed)
        {
            foreach (var field in type.Resolve().Fields.Where(predicate))
            {
                return assembly.MainModule.ImportReference(field);
            }

            logger.Error($"在类型 {type.Name} 中没有找到字段", type);
            isFailed = true;
            return null;
        }
        
        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, string name, ref bool isFailed)
        {
            if (type == null)
            {
                logger.Error($"没有无法解析方法: {name}");
                isFailed = true;
                return null;
            }

            MethodReference method = ResolveMethod(type, assembly, logger, method => method.Name == name, ref isFailed);
            if (method == null)
            {
                logger.Error($"在类型 {type.Name} 中没有找到名称 {name} 的方法", type);
                isFailed = true;
            }

            return method;
        }

        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, Func<MethodDefinition, bool> predicate, ref bool isFailed)
        {
            foreach (var method in type.Resolve().Methods.Where(predicate))
            {
                return assembly.MainModule.ImportReference(method);
            }

            logger.Error($"在类型 {type.Name} 中没有找到方法", type);
            isFailed = true;
            return null;
        }
        
        public static MethodDefinition ResolveDefaultPublicCtor(TypeReference variable)
        {
            foreach (MethodDefinition methodRef in variable.Resolve().Methods)
            {
                if (methodRef.Name == CONST.CONSTRUCTOR && methodRef.Resolve().IsPublic && methodRef.Parameters.Count == 0)
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