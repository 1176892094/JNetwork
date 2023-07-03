using System;
using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance;
        public Address address => transport.address;
        [SerializeField] private Transport transport;
        [SerializeField] private bool runInBackground = true;
        [SerializeField] private bool dontListen;
        [SerializeField] private int tickRate = 30;
        [SerializeField] private int maxConnection = 100;
        private NetworkMode networkMode;

        private void Awake()
        {
            if (!SetSingleton(NetworkMode.None)) return;
        }

        /// <summary>
        /// 设置单例
        /// </summary>
        /// <returns>返回是否设置成功</returns>
        private bool SetSingleton(NetworkMode networkMode)
        {
            this.networkMode = networkMode;
            if (Instance != null && Instance == this)
            {
                return true;
            }

            if (transport == null)
            {
                if (TryGetComponent(out Transport newTransport))
                {
                    transport = newTransport;
                }
                else
                {
                    Debug.LogError("The NetworkManager has no Transport component.");
                    return false;
                }
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Transport.Instance = transport;
            Application.runInBackground = runInBackground;
            return true;
        }
        
   
    }
}