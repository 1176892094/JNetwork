// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-05  22:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    internal class WriterBatch
    {
        /// <summary>
        /// 批处理队列
        /// </summary>
        private Queue<NetworkWriter> writers = new Queue<NetworkWriter>();

        /// <summary>
        /// 写入器
        /// </summary>
        [SerializeField] private NetworkWriter writer;

        /// <summary>
        /// 消息大小
        /// </summary>
        [SerializeField] private int messageSize;

        /// <summary>
        /// 设置阈值
        /// </summary>
        public WriterBatch(byte channel)
        {
            messageSize = NetworkManager.Transport.MessageSize(channel);
        }

        /// <summary>
        /// 添加到队列末尾并写入数据
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="remoteTime">时间戳</param>
        public void AddMessage(ArraySegment<byte> segment, double remoteTime)
        {
            int header = NetworkUtility.VarUIntSize((ulong)segment.Count);
            if (writer != null && writer.position + header + segment.Count > messageSize)
            {
                writers.Enqueue(writer);
                writer = null;
            }

            if (writer == null)
            {
                writer = NetworkWriter.Pop();
                writer.WriteDouble(remoteTime);
            }

            NetworkUtility.CompressVarUInt(writer, (ulong)segment.Count);
            writer.WriteBytes(segment.Array, segment.Offset, segment.Count);
        }

        /// <summary>
        /// 尝试从队列中取出元素并写入到目标
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool GetBatch(NetworkWriter target)
        {
            if (writers.TryDequeue(out var first))
            {
                if (target.position != 0)
                {
                    Debug.LogError("拷贝目标不是空的！");
                }

                ArraySegment<byte> segment = first;
                target.WriteBytes(segment.Array, segment.Offset, segment.Count);
                NetworkWriter.Push(first);
                return true;
            }

            if (writer != null)
            {
                if (target.position != 0)
                {
                    Debug.LogError("拷贝目标不是空的！");
                }

                ArraySegment<byte> segment = writer;
                target.WriteBytes(segment.Array, segment.Offset, segment.Count);
                NetworkWriter.Push(writer);
                writer = null;
                return true;
            }

            return false;
        }
    }
}