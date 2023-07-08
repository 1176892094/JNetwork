using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal static class Extensions
    {
        public static bool Is(this TypeReference td, Type type) => type.IsGenericType ? td.GetElementType().FullName == type.FullName : td.FullName == type.FullName;
        public static bool Is<T>(this TypeReference td) => Is(td, typeof(T));
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, ModuleDefinition module, GenericInstanceType instanceType)
        {
            var reference = new MethodReference(self.Name, self.ReturnType, instanceType)
            {
                CallingConvention = self.CallingConvention,
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParameter in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));
            }

            return module.ImportReference(reference);
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

            var genericInstanceType = new GenericInstanceType(self);
            foreach (var typeReference in arguments)
            {
                genericInstanceType.GenericArguments.Add(typeReference);
            }

            return genericInstanceType;
        }
        
        public static FieldReference SpecializeField(this FieldReference self, ModuleDefinition module, GenericInstanceType instanceType)
        {
            var reference = new FieldReference(self.Name, self.FieldType, instanceType);
            return module.ImportReference(reference);
        }
        
        public static IEnumerable<MethodDefinition> GetConstructors(this TypeDefinition self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof (self));
            }
            
            
            return !self.HasMethods ? Array.Empty<MethodDefinition>() : self.Methods.Where(method => method.IsConstructor);
        }
        
        public static bool Contains(this ModuleDefinition module, string nameSpace, string className)
        {
            return module.GetTypes().Any(td => td.Namespace == nameSpace && td.Name == className);
        }
        
        public static AssemblyNameReference FindReference(this ModuleDefinition module, string referenceName)
        {
            return module.AssemblyReferences.FirstOrDefault(reference => reference.Name == referenceName);
        }
        
        public static bool HasCustomAttribute<TAttribute>(this ICustomAttributeProvider attributeProvider)
        {
            return attributeProvider.CustomAttributes.Any(attr => attr.AttributeType.Is<TAttribute>());
        }
        
        public static bool ImplementsInterface<TInterface>(this TypeDefinition td)
        {
            TypeDefinition typedef = td;
            while (typedef != null)
            {
                if (typedef.Interfaces.Any(implementation => implementation.InterfaceType.Is<TInterface>())) return true;
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

            return false;
        }
        
        public static TypeReference GetEnumUnderlyingType(this TypeDefinition td)
        {
            foreach (var field in td.Fields.Where(field => !field.IsStatic))
            {
                return field.FieldType;
            }

            throw new ArgumentException($"Invalid enum {td.FullName}");
        }
        
        public static bool IsDerivedFrom<T>(this TypeReference tr) => IsDerivedFrom(tr, typeof(T));

        private static bool IsDerivedFrom(this TypeReference tr, Type baseClass)
        {
            TypeDefinition td = tr.Resolve();
            if (!td.IsClass) return false;
            TypeReference parent = td.BaseType;
            if (parent == null) return false;
            if (parent.Is(baseClass)) return true;
            return parent.CanBeResolved() && IsDerivedFrom(parent.Resolve(), baseClass);
        }
        
        public static bool CanBeResolved(this TypeReference parent)
        {
            while (parent != null)
            {
                if (parent.Scope.Name == "Windows")
                {
                    return false;
                }

                if (parent.Scope.Name == "mscorlib")
                {
                    TypeDefinition resolved = parent.Resolve();
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
        
        public static MethodReference MakeGeneric(this MethodReference generic, ModuleDefinition module, TypeReference variableReference)
        {
            GenericInstanceMethod instance = new GenericInstanceMethod(generic);
            instance.GenericArguments.Add(variableReference);
            MethodReference readFunc = module.ImportReference(instance);
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
    }
}