// *********************************************************************************
// # Project: Test
// # Unity: 2022.3.5f1c1
// # Author: jinyijie
// # Version: 1.0.0
// # History: 2024-06-05  21:06
// # Copyright: 2024, jinyijie
// # Description: This is an automatically generated comment.
// *********************************************************************************

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JFramework.Net
{
    [Serializable]
    internal class ReaderPool
    {
        /// <summary>
        /// 写入器列表
        /// </summary>
        [SerializeField] private List<NetworkWriter> writers = new List<NetworkWriter>();

        /// <summary>
        /// 读取器
        /// </summary>
        [SerializeField] private NetworkReader reader = new NetworkReader();

        /// <summary>
        /// 批处理数量
        /// </summary>
        public int Count => writers.Count;

        /// <summary>
        /// 远端时间戳
        /// </summary>
        private double remoteTime;

        /// <summary>
        /// 将接收到的数据写入到列表
        /// </summary>
        /// <param name="segment">批处理数据</param>
        /// <returns>返回处理成功</returns>
        public bool Write(ArraySegment<byte> segment)
        {
            if (segment.Count < Const.HeaderSize)
            {
                return false;
            }

            var writer = NetworkWriter.Pop();
            writer.WriteBytes(segment.Array, segment.Offset, segment.Count);
            if (writers.Count == 0)
            {
                reader.Reset(writer);
                remoteTime = reader.ReadDouble();
            }

            writers.Add(writer);
            return true;
        }

        /// <summary>
        /// 读取数据并输出数据和时间戳
        /// </summary>
        /// <param name="newReader"></param>
        /// <param name="newTime"></param>
        /// <returns></returns>
        public bool TryRead(out NetworkReader newReader, out double newTime)
        {
            newTime = 0;
            newReader = null;
            if (writers.Count == 0)
            {
                return false;
            }

            if (reader.buffer.Count == 0)
            {
                return false;
            }

            if (reader.residue == 0)
            {
                var writer = writers[0];
                writers.RemoveAt(0);
                NetworkWriter.Push(writer);
                if (writers.Count > 0)
                {
                    writer = writers[0];
                    reader.Reset(writer);
                    remoteTime = reader.ReadDouble();
                }
                else
                {
                    return false;
                }
            }

            newTime = remoteTime;
            newReader = reader;
            return true;
        }
    }
}