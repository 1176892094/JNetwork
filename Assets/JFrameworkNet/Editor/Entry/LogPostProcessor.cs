using System.Collections.Generic;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace JFramework.Editor
{
    /// <summary>
    /// 网络代码注入日志
    /// </summary>
    internal class LogPostProcessor : Logger
    {
        /// <summary>
        /// 日志列表
        /// </summary>
        public readonly List<DiagnosticMessage> logs = new List<DiagnosticMessage>();

        /// <summary>
        /// 添加日志信息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logType"></param>
        private void Add(string message, DiagnosticType logType)
        {
            logs.Add(new DiagnosticMessage
            {
                DiagnosticType = logType,
                File = string.Empty,
                Line = 0,
                Column = 0,
                MessageData = message
            });
        }

        /// <summary>
        /// 处理日志
        /// </summary>
        /// <param name="message">日志信息</param>
        /// <param name="member">成员参数</param>
        /// <param name="logType">日志类型</param>
        private void Log(string message, MemberReference member, DiagnosticType logType)
        {
            if (member != null)
            {
                message = $"{message} (at {member})";
            }

            var split = message.Split('\n');

            if (split.Length == 1)
            {
                Add($"{message}", logType);
            }
            else
            {
                foreach (string log in split)
                {
                    Add(log, logType);
                }
            }
        }
        
        /// <summary>
        /// 警告日志
        /// </summary>
        /// <param name="message">日志信息</param>
        /// <param name="member">成员参数</param>
        public void Warn(string message, MemberReference member)
        {
            Log(message, member, DiagnosticType.Warning);
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="message">日志信息</param>
        /// <param name="member">成员参数</param>
        public void Error(string message, MemberReference member)
        {
            Log(message, member, DiagnosticType.Error);
        }
    }
}