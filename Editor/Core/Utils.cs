using System;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Utils
    {
        public static MethodReference ResolveMethod(TypeReference type, AssemblyDefinition assembly, Logger logger, string name)
        {
            if (type == null)
            {
                logger.Error($"没有无法解析方法: {name}");
                Process.failed = true;
                return null;
            }

            MethodReference method = ResolveMethod(type, assembly, logger, method => method.Name == name);
            if (method == null)
            {
                logger.Error($"在类型 {type.Name} 中没有找到名称 {name} 的方法", type);
                Process.failed = true;
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
            Process.failed = true;
            return null;
        }
        
        public static MethodReference TryResolveMethodInParents(TypeReference tr, AssemblyDefinition assembly, string name)
        {
            if (tr == null)
            {
                return null;
            }
            foreach (var md in tr.Resolve().Methods)
            {
                if (md.Name == name)
                {
                    MethodReference mr = md;
                    if (tr.IsGenericInstance)
                    {
                        mr = mr.MakeHostInstanceGeneric(tr.Module, (GenericInstanceType)tr);
                    }
                    return assembly.MainModule.ImportReference(mr);
                }
            }
            
            return TryResolveMethodInParents(tr.Resolve().BaseType.ApplyGenericParameters(tr), assembly, name);
        }
        
        public static MethodDefinition ResolveDefaultPublicCtor(TypeReference variable)
        {
            return variable.Resolve().Methods.FirstOrDefault(md => md.Name == CONST.CTOR && md.Resolve().IsPublic && md.Parameters.Count == 0);
        }

        public static MethodReference ResolveProperty(TypeReference tr, AssemblyDefinition assembly, string name)
        {
            return (from pd in tr.Resolve().Properties where pd.Name == name select assembly.MainModule.ImportReference(pd.GetMethod)).FirstOrDefault();
        }
    }
}