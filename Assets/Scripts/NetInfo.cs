using Mirror;

public class NetInfo : NetworkBehaviour
{
    [SyncVar] private string _ipAddress;
    [SyncVar] private int _port;
    [SyncVar] private string _ipAddressComServer;
    [SyncVar] private int _portComServer;

    // Getter e Setter
    public string IpAddress => _ipAddress;
    public int Port => _port;
    public string IpAddressComServer => _ipAddressComServer;
    public int PortComServer => _portComServer;

    // Set network information
    [Server] // Only the serve can modify these values 
    public void SetNetworkInfo(string ip, int port, string ipCom, int portCom)
    {
        _ipAddress = ip;
        _port = port;
        _ipAddressComServer = ipCom;
        _portComServer = portCom;
    }
}
