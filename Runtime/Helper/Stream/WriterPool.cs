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
    public class WriterPool
    {
        /// <summary>
        /// 批处理队列
        /// </summary>
        [SerializeField] private List<NetworkWriter> writers = new List<NetworkWriter>();

        /// <summary>
        /// 写入器
        /// </summary>
        [SerializeField] private NetworkWriter writer;

        /// <summary>
        /// 消息大小
        /// </summary>
        [SerializeField] private int maxSize;

        /// <summary>
        /// 设置阈值
        /// </summary>
        public WriterPool(int channel)
        {
            if (NetworkManager.Transport != null)
            {
                maxSize = NetworkManager.Transport.MessageSize(channel);
            }
        }

        /// <summary>
        /// 添加到队列末尾并写入数据
        /// </summary>
        /// <param name="segment">消息分段</param>
        /// <param name="remoteTime">时间戳</param>
        public void Write(ArraySegment<byte> segment, double remoteTime)
        {
            if (writer != null && writer.position + segment.Count > maxSize)
            {
                writers.Add(writer); // 加入到队列中
                writer = null;
            }

            if (writer == null)
            {
                writer = NetworkWriter.Pop(); // 从对象池中取出
                writer.WriteDouble(remoteTime); // 重新写入时间戳
            }

            writer.WriteBytes(segment.Array, segment.Offset, segment.Count); // 写入消息分段
        }

        /// <summary>
        /// 尝试从队列中取出元素并写入到目标
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool TryWrite(NetworkWriter target)
        {
            if (writers.Count > 0)
            {
                if (target.position != 0)
                {
                    Debug.LogError("拷贝目标不是空的！");
                }

                ArraySegment<byte> segment = writers[0];
                target.WriteBytes(segment.Array, segment.Offset, segment.Count);
                NetworkWriter.Push(writers[0]);
                writers.RemoveAt(0);
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