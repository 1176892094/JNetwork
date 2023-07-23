namespace JFramework.Net
{
    internal interface ISnapshot
    {
        double localTime { get; }
        
        double remoteTime { get; }
    }
}