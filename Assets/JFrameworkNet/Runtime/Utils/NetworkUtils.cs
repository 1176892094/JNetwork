using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        public static bool IsSceneObject(NetworkObject entity)
        {
            var gameObject = entity.gameObject;
            return gameObject.hideFlags != HideFlags.NotEditable && gameObject.hideFlags != HideFlags.HideAndDontSave && entity.sceneId != 0;
        }
        
        public static bool IsValidParent(NetworkObject entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }
    }
}