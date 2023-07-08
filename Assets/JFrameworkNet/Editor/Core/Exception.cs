using System;
using System.Runtime.Serialization;
using Mono.Cecil;

namespace JFramework.Editor
{
    internal abstract class ProcessException : Exception
    {
        public MemberReference MemberReference { get; }

        protected ProcessException(string message, MemberReference member) : base(message)
        {
            MemberReference = member;
        }

        protected ProcessException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
    
    [Serializable]
    internal class WriterException : ProcessException
    {
        public WriterException(string message, MemberReference member) : base(message, member)
        {
        }

        protected WriterException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}