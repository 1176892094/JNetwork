using System.Runtime.CompilerServices;
using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        public static bool IsSceneObject(NetworkIdentity entity)
        {
            var gameObject = entity.gameObject;
            return gameObject.hideFlags != HideFlags.NotEditable && gameObject.hideFlags != HideFlags.HideAndDontSave && entity.sceneId != 0;
        }
        
        public static bool IsValidParent(NetworkIdentity entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkIdentity GetNetworkIdentity(uint netId)
        {
            // if (NetworkServer.isActive)
            // {
            //     NetworkServer.TryGetNetId(netId, out var identity);
            //     return identity;
            // }
            //
            // if (NetworkClient.isActive)
            // {
            //     NetworkClient.TryGetNetId(netId, out var identity);
            //     return identity;
            // }

            return null;
        }
    }
}