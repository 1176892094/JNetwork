# JNetwork
内部包含一个可靠的UDP传输协议，用于开发Unity网络游戏

1.NetworkManager的使用
```c#
    private void Start()
    {
        NetworkManager.StartHost(); // 开启主机
        
        NetworkManager.StartHost(EntryMode.None); // 取消传输层调用，单机模式可用
        
        NetworkManager.StopHost(); // 停止主机
        
        NetworkManager.StartServer(); // 开启服务器
        
        NetworkManager.StopServer(); // 停止服务器
        
        NetworkManager.StartClient(); // 开启客户端
        
        NetworkManager.StartClient(new Uri("127.0.0.1")); // 开启客户端
        
        NetworkManager.StopClient(); // 停止客户端
    }
```
2.NetworkServer的使用
```c#
    private void Start()
    {
        NetworkManager.Server.OnConnect += OnServerConnect; // 当有客户端连接到服务器(客户端使用无效)
        
        NetworkManager.Server.OnDisconnect += OnServerDisconnect; // 当有客户端从服务器断开(客户端使用无效)
        
        NetworkManager.Server.OnReady += OnReady; // 当客户端在服务器准备就绪 (可以发送Rpc和网络变量同步)(客户端使用无效)
    }

    private void OnServerConnect(NetworkClient client)
    {
        Debug.Log(client.clientId); //连接的客户端Id
    }

    private void OnServerDisconnect(NetworkClient client)
    {
        Debug.Log(client.clientId); //断开的客户端Id
    }

    private async void OnReady(NetworkClient client) 
    {
        var player = await GlobalManager.Asset.Load<GameObject>("Player");
        NetworkManager.Server.Spawn(player, client); // 在这里为客户端生成玩家

        GlobalManager.Time.Pop(5).Invoke(() => // 等待5秒后销毁
        {
            NetworkManager.Server.Destroy(player.GetComponent<NetworkObject>());
        });
    }
```
3.NetworkClient的使用
```c#
   private void Start()
    {
        NetworkManager.Client.OnConnect += OnClientConnect; // 当客户端连接到服务器(服务器使用无效)
        
        NetworkManager.Client.OnDisconnect += OnClientDisconnect; // 当客户端从服务器断开(服务器使用无效)
        
        NetworkManager.Client.OnReady += OnClientReady; // 在场景准备加载时会调用该方法(服务器使用无效)
    }

    private void OnClientConnect()
    {
        Debug.Log("连接成功");
    }

    private void OnClientDisconnect()
    {
        Debug.Log("连接断开");
    }

    private void OnClientReady() 
    {
        Debug.Log("客户端取消准备");
    }
```

4.NetworkScene的使用
```c#
    private void Start()
    {
        NetworkManager.Scene.Load("GameScene"); // 让服务器加载场景(自动同步给各个客户端)
        
        NetworkManager.Scene.OnClientChangeScene += OnClientChangeScene; // 当客户端准备改变场景

        NetworkManager.Scene.OnClientSceneChanged += OnClientSceneChanged; // 当客户端场景加载完成后(服务器不调用)

        NetworkManager.Scene.OnServerChangeScene += OnServerChangeScene; // 当服务器场景加载完成后

        NetworkManager.Scene.OnServerSceneChanged += OnServerSceneChanged; // 当服务器场景加载完成后(客户端不调用)
    }

    private void OnClientChangeScene(string newSceneName)
    {
        Debug.Log("客户端准备改变场景");
    }

    private void OnClientSceneChanged(string sceneName)
    {
        Debug.Log("客户端场景加载完成");
    }

    private void OnServerChangeScene(string newSceneName)
    {
        Debug.Log("服务器准备改变场景");
    }

    private void OnServerSceneChanged(string sceneName)
    {
        Debug.Log("服务器场景加载完成");
    }
```

5.远程调用和网络变量
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
