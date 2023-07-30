using System;
using System.Collections.Generic;
using System.Linq;
using JFramework.Net;
using Mono.Cecil;
using UnityEngine;

namespace JFramework.Editor
{
    internal static class Extensions
    {
        public static bool Is(this TypeReference td, Type type)
        {
            return type.IsGenericType ? td.GetElementType().FullName == type.FullName : td.FullName == type.FullName;
        }

        public static bool Is<T>(this TypeReference td)
        {
            return Is(td, typeof(T));
        }
        
        public static bool IsDerivedFrom<T>(this TypeReference tr) => IsDerivedFrom(tr, typeof(T));

        private static bool IsDerivedFrom(this TypeReference tr, Type type)
        {
            TypeDefinition td = tr.Resolve();
            if (!td.IsClass) return false;
            TypeReference parent = td.BaseType;
            if (parent == null) return false;
            if (parent.Is(type)) return true;
            return parent.CanBeResolved() && IsDerivedFrom(parent.Resolve(), type);
        }

        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, ModuleDefinition md, GenericInstanceType declaringType)
        {
            var mr = new MethodReference(self.Name, self.ReturnType, declaringType)
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
            {
                mr.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParameter in self.GenericParameters)
            {
                mr.GenericParameters.Add(new GenericParameter(genericParameter.Name, mr));
            }

            return md.ImportReference(mr);
        }
        
        public static GenericInstanceType MakeGenericInstanceType(this TypeReference self, params TypeReference[] arguments)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (arguments.Length == 0)
            {
                throw new ArgumentException();
            }

            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instanceType = new GenericInstanceType(self);
            foreach (var typeReference in arguments)
            {
                instanceType.GenericArguments.Add(typeReference);
            }

            return instanceType;
        }

        public static FieldReference SpecializeField(this FieldReference self, ModuleDefinition md, GenericInstanceType declaringType)
        {
            var reference = new FieldReference(self.Name, self.FieldType, declaringType);
            return md.ImportReference(reference);
        }
        
        public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }
            
            return !self.HasMethods ? Array.Empty<MethodDefinition>() : self.Methods.Where(method => method.IsConstructor);
        }
        
        public static bool Contains(this ModuleDefinition md, string nameSpace, string className)
        {
            return md.GetTypes().Any(typeDefinition => typeDefinition.Namespace == nameSpace && typeDefinition.Name == className);
        }
        
        public static AssemblyNameReference FindReference(this ModuleDefinition md, string name)
        {
            return md.AssemblyReferences.FirstOrDefault(reference => reference.Name == name);
        }
        
        public static bool HasCustomAttribute<T>(this ICustomAttributeProvider ar)
        {
            return ar.CustomAttributes.Any(attribute => attribute.AttributeType.Is<T>());
        }
        
        public static bool ImplementsInterface<T>(this TypeDefinition td)
        {
            var typeDefinition = td;
            while (typeDefinition != null)
            {
                if (typeDefinition.Interfaces.Any(implementation => implementation.InterfaceType.Is<T>()))
                {
                    return true;
                }
                try
                {
                    var parent = typeDefinition.BaseType;
                    typeDefinition = parent?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    break;
                }
            }

            return false;
        }
        
        public static TypeReference GetEnumUnderlyingType(this TypeDefinition td)
        {
            foreach (var field in td.Fields.Where(field => !field.IsStatic))
            {
                return field.FieldType;
            }

            throw new ArgumentException($"无效的枚举类型：{td.FullName}");
        }

        internal static bool CanBeResolved(this TypeReference parent)
        {
            while (parent != null)
            {
                if (parent.Scope.Name == "Windows")
                {
                    return false;
                }

                if (parent.Scope.Name == "mscorlib")
                {
                    var resolved = parent.Resolve();
                    return resolved != null;
                }

                try
                {
                    parent = parent.Resolve().BaseType;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        
        public static bool IsMultidimensionalArray(this TypeReference tr) => tr is ArrayType { Rank: > 1 };
        
        public static MethodReference MakeGeneric(this MethodReference mr, ModuleDefinition md, TypeReference variableReference)
        {
            var instance = new GenericInstanceMethod(mr);
            instance.GenericArguments.Add(variableReference);
            var readFunc = md.ImportReference(instance);
            return readFunc;
        }
        
        public static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeReference variable)
        {
            return FindAllPublicFields(variable.Resolve());
        }
        private static IEnumerable<FieldDefinition> FindAllPublicFields(this TypeDefinition typeDefinition)
        {
            while (typeDefinition != null)
            {
                foreach (FieldDefinition field in typeDefinition.Fields)
                {
                    if (field.IsStatic || field.IsPrivate || field.IsFamily) continue;
                    if (field.IsAssembly) continue;
                    if (field.IsNotSerialized) continue;
                    yield return field;
                }

                try
                {
                    typeDefinition = typeDefinition.BaseType?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    break;
                }
            }
        }
        
        public static MethodDefinition GetMethod(this TypeDefinition type, string methodName)
        {
            return type.Methods.FirstOrDefault(method => method.Name == methodName);
        }
        
        public static List<MethodDefinition> GetMethods(this TypeDefinition td, string methodName)
        {
            return td.Methods.Where(method => method.Name == methodName).ToList();
        }
        
        public static CustomAttribute GetCustomAttribute<TAttribute>(this ICustomAttributeProvider method)
        {
            return method.CustomAttributes.FirstOrDefault(custom => custom.AttributeType.Is<TAttribute>());
        }
        
        public static T GetField<T>(this CustomAttribute attribute, T defaultValue)
        {
            foreach (var custom in attribute.ConstructorArguments)
            {
                return (T)custom.Value;
            }
            return defaultValue;
        }

        public static MethodDefinition GetMethodInBaseType(this TypeDefinition td, string methodName)
        {
            TypeDefinition typedef = td;
            while (typedef != null)
            {
                foreach (var definition in typedef.Methods.Where(method => method.Name == methodName))
                {
                    return definition;
                }

                try
                {
                    TypeReference parent = typedef.BaseType;
                    typedef = parent?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    break;
                }
            }

            return null;
        }
        
        public static TypeReference ApplyGenericParameters(this TypeReference parentReference, TypeReference childReference)
        {
            if (!parentReference.IsGenericInstance) return parentReference;
            GenericInstanceType parentGeneric = (GenericInstanceType)parentReference;
            GenericInstanceType generic = new GenericInstanceType(parentReference.Resolve());
            foreach (TypeReference arg in parentGeneric.GenericArguments)
            {
                generic.GenericArguments.Add(arg);
            }

            for (int i = 0; i < generic.GenericArguments.Count; i++)
            {
                if (!generic.GenericArguments[i].IsGenericParameter) continue;
                string name = generic.GenericArguments[i].Name;
                TypeReference arg = FindMatchingGenericArgument(childReference, name);
                TypeReference imported = parentReference.Module.ImportReference(arg);
                generic.GenericArguments[i] = imported;
            }

            return generic;
        }
        
        private static TypeReference FindMatchingGenericArgument(TypeReference childReference, string paramName)
        {
            TypeDefinition def = childReference.Resolve();
            if (!def.HasGenericParameters) throw new InvalidOperationException("基类有泛型参数，但在子类中找不到它们。");
            for (int i = 0; i < def.GenericParameters.Count; i++)
            {
                GenericParameter param = def.GenericParameters[i];
                if (param.Name == paramName)
                {
                    GenericInstanceType generic = (GenericInstanceType)childReference;
                    return generic.GenericArguments[i];
                }
            }
            
            throw new InvalidOperationException("没有找到匹配的泛型");
        }
        
                
        public static FieldReference MakeHostInstanceGeneric(this FieldReference self)
        {
            var declaringType = new GenericInstanceType(self.DeclaringType);
            foreach (var parameter in self.DeclaringType.GenericParameters)
            {
                declaringType.GenericArguments.Add(parameter);
            }
            return new FieldReference(self.Name, self.FieldType, declaringType);
        }
        
        public static bool IsNetworkObjectField(this TypeReference tr) => tr.Is<GameObject>() || tr.Is<NetworkObject>() || tr.IsDerivedFrom<NetworkBehaviour>() || tr.Is<NetworkBehaviour>();
    }
}