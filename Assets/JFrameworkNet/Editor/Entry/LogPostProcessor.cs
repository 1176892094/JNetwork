using System.Collections.Generic;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace JFramework.Editor
{
    internal class LogPostProcessor : Logger
    {
        public readonly List<DiagnosticMessage> logs = new List<DiagnosticMessage>();

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
        
        public void Warn(string message, MemberReference member)
        {
            Log(message, member, DiagnosticType.Warning);
        }

        public void Error(string message, MemberReference member)
        {
            Log(message, member, DiagnosticType.Error);
        }
    }
}