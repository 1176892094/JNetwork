using System.Collections.Generic;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal class Comparator : IEqualityComparer<TypeReference>
    {
        public bool Equals(TypeReference x, TypeReference y) => x?.FullName == y?.FullName;

        public int GetHashCode(TypeReference obj) => obj.FullName.GetHashCode();
    }
}