using JFramework.Udp;
using UnityEngine;

namespace JFramework.Net
{
    public class NetworkManager : MonoBehaviour
    {
        public Address address => transport.Address;
        private Transport transport;
    }
}