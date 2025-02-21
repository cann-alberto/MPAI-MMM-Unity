using Mirror;
using UnityEditor;
using UnityEngine;

public class NetworkClientManager : MonoBehaviour
{
    [SerializeField] private string serverAddress = "localhost"; // Server IP or hostname
    //[SerializeField] private int serverPort = 7777; // Server port (should match the server's port)

    private NetworkManager networkManager;

    void Start()
    {
        networkManager = GetComponent<NetworkManager>();

        // Assign the server address and port programmatically
        networkManager.networkAddress = serverAddress;

        // Optionally, set the transport layer port (if using Transport component)
        //Transport.activeTransport.port = (ushort)serverPort;

        // Connect to the server
        // ConnectToServer();
    }

    public void ConnectToServer()
    {        

        // Connect the client to the server
        if (!NetworkClient.isConnected)
        {
            Debug.Log("Attempting to connect to the server...");
            networkManager.StartClient();            
        }
        else
        {
            Debug.Log("Client is already connected.");
        }
    }

    public void DisconnectFromServer()
    {
        // Disconnect the client
        if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
            Debug.Log("Disconnected from the server.");
        }
    }
}
