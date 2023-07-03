namespace JFramework.Net
{
    public interface NetworkMessage
    {
    }

    public struct SceneMessage : NetworkMessage
    {
        public string sceneName;
    }
}