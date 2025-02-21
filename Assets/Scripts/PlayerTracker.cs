using Mirror;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerTracker : NetworkBehaviour
{    
    public GameObject roomPrefab;
    public event Action OnRoomInstantiated;  // Define an event
    private bool hasTriedToConnect = false;
    private static HashSet<int> usedPorts = new HashSet<int>(); // Track used ports
    private static int basePort = 7780; // Base port number



    void Start()
    {
        hasTriedToConnect = false;    
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLocalPlayer)
        {
            if (other.CompareTag("Parcel"))
            {
                UnityEngine.Debug.Log("Player enter: " + other.gameObject.name);                
                string actionDataJson = CreateEmbedJson(other.gameObject.name, GameManager.Instance.userID);
                StartCoroutine(GameManager.Instance.WebAPIManager.Upload("Activity/actions", actionDataJson, HandleResponse));
                GameManager.Instance.currentPlayerPosition = other.gameObject.name;
            }
            if (other.CompareTag("RoomPlaceholder") && !hasTriedToConnect)
            {
                hasTriedToConnect = true; // To avoid trying multiple conenction ot the same room
                NetInfo netInfo = other.GetComponent<NetInfo>();
                UnityEngine.Debug.Log("NetInfo[Mirror Server]: " + netInfo.IpAddress + ":" + netInfo.Port + "\nNetInfo[Communication Server]: " + netInfo.IpAddressComServer + ":" + netInfo.PortComServer);
                
                RoomData.port = netInfo.Port;
                RoomData.roomName = "Lobby Room";
                //SceneManager.LoadScene("Room");

                StartCoroutine(GameManager.Instance.ClientSocket.TryConnectToServerCoroutine(netInfo.IpAddressComServer, netInfo.PortComServer, 10, 2f));
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {        
        if (other.CompareTag("Parcel"))
            UnityEngine.Debug.Log("Exit from:" + other.gameObject.name);
    }


    // Callback method to handle the response from the GET request
    private void HandleResponse(string response)
    {
        UnityEngine.Debug.Log("Received response: " + response); // Log the response received from the server
        
    }

    private string CreateEmbedJson(string ParcelName, string userID)
    {
        string jsonData = "{" +
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"source\": \"" + userID + "\"," +            
            "\"destination\": \"Activity Service\"," +
            "\"action\": \"MM-Embed\"," +
            "\"inItem\": \"At " + ParcelName + "\"," +
            "\"inLocation\": \"At " + ParcelName + "\"," +
            "\"outLocation\": \"At " + ParcelName + "\"," +
            "\"rightsID\": \"string\"" +
            "}";
        return jsonData;
    }

    

    [Command]
    public void CmdInstantiateRoom(Vector3 position, Quaternion orientation)
    {
        if (!isServer) return;

        // TODO search for a free port
        int port = FindFreePort();
        if (port == -1)
        {
            UnityEngine.Debug.LogError("No available ports to create a new room.");
            return;
        }

        GameObject roomObject = Instantiate(roomPrefab, position, orientation);
        NetInfo netInfo = roomObject.GetComponent<NetInfo>();        
        NetworkServer.Spawn(roomObject);
        netInfo.SetNetworkInfo("127.0.0.1", port, "127.0.0.1", port + 1); // TODO: replace the ipAddresses

        UnityEngine.Debug.Log("Launching the server...");        
        LaunchRoomServer(port);
        RpcNotifyRoomCreated(); // Notify clients

    }

    [ClientRpc]
    void RpcNotifyRoomCreated()
    {
        OnRoomInstantiated?.Invoke(); // Invoke event when room is instatiated
    }

    // Public method to allow interaction from other scripts
    public void InstantiateRoomPrefab()
    {

        if (isLocalPlayer)
        {            
            CmdInstantiateRoom(transform.position, transform.localRotation);
        }
        else
        {
            UnityEngine.Debug.LogWarning("RequestRoomCreation called on a non-local player.");
        }
    }

    private void LaunchRoomServer(int port)
    {
        //Launch the server        
        string sceneName = "Room Server";

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = GameManager.Instance.pathToRoomExe;
        //startInfo.FileName = "C:\\Users\\lab2a\\Documents\\Unity Projects\\MPAI-MMM\\Builds\\MPAI-MMM.exe";
        startInfo.Arguments = $"-scene {sceneName} -port {port}";
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        Process serverProcess = new Process();
        serverProcess.StartInfo = startInfo;
        serverProcess.Start();
    }

    private int FindFreePort()
    {
        int maxPort = basePort + 100; // Arbitrary max range for ports

        for (int port = basePort; port <= maxPort; port += 2) // Increment by 2 to reserve consecutive ports
        {
            if (!usedPorts.Contains(port))
            {
                usedPorts.Add(port);
                return port;
            }
        }

        return -1; // No available ports
    }

    [Server]
    public static void FreePort(int port)
    {
        if (usedPorts.Contains(port))
        {
            usedPorts.Remove(port);
            UnityEngine.Debug.Log("Freed port: " + port);
        }
    }
}
