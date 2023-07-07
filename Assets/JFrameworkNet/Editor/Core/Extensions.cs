using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    public static class Extensions
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
        
        public static bool IsMultidimensionalArray(this TypeReference tr) => tr is ArrayType { Rank: > 1 };
    }
}