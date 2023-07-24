# JNetwork
内部包含一个可靠的UDP传输协议，用于开发Unity网络游戏

1.NetworkManager的使用
```c#
    private void Start()
    {
        NetworkManager.Instance.StartHost(); // 开启主机
        
        NetworkManager.Instance.StartHost(false); // 取消传输层调用，单机模式可用
        
        NetworkManager.Instance.StopHost(); // 停止主机
        
        NetworkManager.Instance.StartServer(); // 开启服务器
        
        NetworkManager.Instance.StopServer(); // 停止服务器
        
        NetworkManager.Instance.StartClient(); // 开启客户端
        
        NetworkManager.Instance.StartClient(); // 停止客户端

        NetworkManager.OnStartHost += Test; // 当主机启动时会调用这个方法
        
        NetworkManager.OnStopHost += Test; // 当主机停止时会调用这个方法

        NetworkManager.OnStartClient += Test; // 当客户端启动时会调用这个方法
        
        NetworkManager.OnStopClient += Test; // 当客户端停止时会调用这个方法

        NetworkManager.OnStartServer += Test; // 当服务器启启动时会调用这个方法

        NetworkManager.OnStopServer += Test; // 当服务器停止时会调用这个方法
        
        NetworkManager.LoadScene("SceneName"); // 服务器加载场景，会自动同步所有客户端加载场景
    }
```
2.NetworkServer的使用
```c#
    private void Start()
    {
        NetworkServer.OnServerConnect += OnServerConnect; // 当有客户端连接到服务器(客户端使用无效)
        
        NetworkServer.OnServerDisconnect += OnServerDisconnect; // 当有客户端从服务器断开(客户端使用无效)
        
        NetworkServer.OnServerReady += OnServerReady; // 当客户端在服务器准备就绪 (可以发送Rpc和网络变量同步)(客户端使用无效)
    }

    private void OnServerConnect(ClientEntity client)
    {
        Debug.Log(client.clientId); //连接的客户端Id
    }

    private void OnServerDisconnect(ClientEntity client)
    {
        Debug.Log(client.clientId); //断开的客户端Id
    }

    private void OnServerReady(ClientEntity client) 
    {
        var player = AssetManager.Load<GameObject>("Player");
        NetworkServer.Spawn(player, client); // 在这里为客户端生成玩家
    }
```
3.NetworkClient的使用
```c#
   private void Start()
    {
        NetworkClient.OnClientConnect += OnClientConnect; // 当客户端连接到服务器(服务器使用无效)
        
        NetworkClient.OnClientDisconnect += OnClientDisconnect; // 当客户端从服务器断开(服务器使用无效)
        
        NetworkClient.OnClientNotReady += OnClientNotReady; // 在场景准备加载时会调用该方法(服务器使用无效)
    }

    private void OnClientConnect()
    {
        Debug.Log("连接成功");
    }

    private void OnClientDisconnect()
    {
        Debug.Log("连接断开");
    }

    private void OnClientNotReady() 
    {
        Debug.Log("客户端取消准备");
    }
```

4.远程调用和网络变量
```c#
    public class Test : NetworkBehaviour // 继承NetworkBehaviour
    {
        /// <summary>
        /// 网络变量 支持 基本类型，结构体，GameObject，NetworkObject，NetworkBehaviour
        /// </summary>
        [SyncVar(nameof(OnHPChanged))] public int hp;

        private void OnHPChanged(int oldValue, int newValue) //网络变量绑定事件
        {
            Debug.Log(oldValue + "=>" + newValue);
        }

        [ServerRpc] // 可传参数
        public void Test1()
        {
            Debug.Log("ServerRpc"); // 由客户端向连接服务器 进行远程调用
        }

        [ClientRpc(Channel.Reliable)] //默认为可靠传输
        public void Test2()
        {
            Debug.Log("ClientRpc"); // 由服务器向所有客户端 进行远程调用
        }

        [TargetRpc(Channel.Unreliable)] //可设置为不可靠传输
        public void Test3(ClientEntity client)
        {
            Debug.Log("TargetRpc"); // 由服务器向指定客户端 进行远程调用
        }
    }
```