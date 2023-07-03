using UnityEngine;

namespace JFramework.Net
{
    public static class NetworkUtils
    {
        public static bool IsSceneObject(NetworkEntity entity)
        {
            var gameObject = entity.gameObject;
            return gameObject.hideFlags != HideFlags.NotEditable && gameObject.hideFlags != HideFlags.HideAndDontSave && entity.sceneId != 0;
        }
        
        public static bool IsValidParent(NetworkEntity entity)
        {
            var parent = entity.transform.parent;
            return parent == null || parent.gameObject.activeInHierarchy;
        }
    }
}