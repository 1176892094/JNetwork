using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace JFramework.Editor
{
    public static class Extensions
    {
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
    }
}