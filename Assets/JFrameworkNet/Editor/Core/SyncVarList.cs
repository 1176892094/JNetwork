using System.Collections.Generic;

namespace JFramework.Editor
{
    public class SyncVarList
    {
        /// <summary>
        /// ServerVar 数量
        /// </summary>
        private readonly Dictionary<string, int> serverVars = new Dictionary<string, int>();
        
        /// <summary>
        /// 从类中获取 ServerVar
        /// </summary>
        /// <param name="name">从类名中获取</param>
        /// <returns></returns>
        public int GetServerVar(string name) => serverVars.TryGetValue(name, out var value) ? value : 0;
        
        /// <summary>
        /// 设置 ServerVar 数量
        /// </summary>
        /// <param name="name">传入类名</param>
        /// <param name="count">传入数值</param>
        public void SetServerVar(string name, int count) => serverVars[name] = count;
    }
}