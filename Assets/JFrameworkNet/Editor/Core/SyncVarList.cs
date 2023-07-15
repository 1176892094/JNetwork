using System.Collections.Generic;
using Mono.Cecil;

namespace JFramework.Editor
{
    public class SyncVarList
    {
        /// <summary>
        /// 取代的 Setter 属性
        /// </summary>
        public Dictionary<FieldDefinition, MethodDefinition> setterProperties = new Dictionary<FieldDefinition, MethodDefinition>();
        
        /// <summary>
        /// 取代的Getter 属性
        /// </summary>
        public Dictionary<FieldDefinition, MethodDefinition> getterProperties = new Dictionary<FieldDefinition, MethodDefinition>();
        
        /// <summary>
        /// ServerVar 数量
        /// </summary>
        public readonly Dictionary<string, int> serverVarCount = new Dictionary<string, int>();
        
        /// <summary>
        /// 从类中获取 ServerVar
        /// </summary>
        /// <param name="name">从类名中获取</param>
        /// <returns></returns>
        public int GetServerVar(string name) => serverVarCount.TryGetValue(name, out var value) ? value : 0;
        
        /// <summary>
        /// 设置 ServerVar 数量
        /// </summary>
        /// <param name="name">传入类名</param>
        /// <param name="count">传入数量</param>
        public void SetServerVarCount(string name, int count) => serverVarCount[name] = count;
    }
}