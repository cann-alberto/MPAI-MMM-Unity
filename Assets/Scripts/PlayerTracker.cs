using Mirror;
using System;
using System.Diagnostics;
using UnityEngine;


public class PlayerTracker : NetworkBehaviour
{    
    public GameObject roomPrefab;
    public event Action OnRoomInstantiated;  // Define an event
    private bool hasTriedToConnect = false;    

    int port = 0; 

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
                string moveDataJson = CreateMoveDataJson(other.gameObject.name);                
                GameManager.Instance.currentPlayerLocation = other.gameObject.name;
            }
            if (other.CompareTag("RoomPlaceholder") && !hasTriedToConnect)
            {
                hasTriedToConnect = true; // To avoid trying multiple conenction ot the same room
                NetInfo netInfo = other.GetComponent<NetInfo>();
                UnityEngine.Debug.Log("NetInfo[Mirror Server]: " + netInfo.IpAddress + ":" + netInfo.Port + "\nNetInfo[Communication Server]: " + netInfo.IpAddressComServer + ":" + netInfo.PortComServer);

                RoomData.roomName = "Room_[" + netInfo.IpAddress+":"+netInfo.Port+"]";
                RoomData.netInfo = netInfo;                                

                StartCoroutine(GameManager.Instance.ClientSocket.TryConnectToServerCoroutine(netInfo.IpAddressComServer, netInfo.PortComServer, 10, 2f));
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (isLocalPlayer)
        {
            if (other.CompareTag("Parcel"))
                UnityEngine.Debug.Log("Exit from:" + other.gameObject.name);
        }
    }
    
    private void HandleResponse(string response)
    {
        UnityEngine.Debug.Log("Received response: " + response); // Log the response received from the server        
    }

    private string CreateMoveDataJson(string ParcelName)
    {                
        string position = transform.position.ToString().Replace(",", ".");        
        string rotation = transform.localRotation.ToString().Replace(",", ".");       
        string jsonData = "{" +
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"action\": \"MM-Move\"," +
            "\"sProcess\": \"" + GameManager.Instance.userID + "\"," +
            "\"sComplements\": \"Persona From " + GameManager.Instance.currentPlayerLocation + " To " + ParcelName + " With SA position: " + position + " rotation: " + rotation + "\"," +
            "\"dProcess\": \"Location Service\"" +
            "}";        
        return jsonData;       
    }    

    [Command]
    public void CmdInstantiateRoom(Vector3 position, Quaternion orientation)
    {
        if (!isServer) return;

        // Search for a free port
        int port = PortManager.Instance.FindFreePort();
        if (port == -1)
        {
            UnityEngine.Debug.LogError("No available ports to create a new room.");
            return;
        }

        GameObject roomObject = Instantiate(roomPrefab, position, orientation);
        NetInfo netInfo = roomObject.GetComponent<NetInfo>();        
        NetworkServer.Spawn(roomObject);
        netInfo.SetNetworkInfo("127.0.0.1", port, "127.0.0.1", port + 1); // TODO: replace the ipAddresses

        // Store the room in the dictionary
        PortManager.Instance.TrackRoom(port, roomObject);        

        UnityEngine.Debug.Log("Launching the server...");        
        LaunchRoomServer(port); // Launch the Mirror server
        RpcNotifyRoomCreated(); // Notify clients
    }

    [ClientRpc]
    void RpcNotifyRoomCreated()
    {
        OnRoomInstantiated?.Invoke(); // Invoke event when room is instatiated
    }

    public void InstantiateRoomPrefab()
    {
        if (isLocalPlayer)
        {            
            string actionDataJson = CreateActionDataJson();
            StartCoroutine(GameManager.Instance.WebAPIManager.UploadRequest("Activity/process-actions", actionDataJson, HandleResponse));
            CmdInstantiateRoom(transform.position, transform.localRotation);
        }
        else
        {
            UnityEngine.Debug.LogWarning("RequestRoomCreation called on a non-local player.");
        }
    }

    private string CreateActionDataJson()
    {
        string position = transform.position.ToString().Replace(",", ".");
        string rotation = transform.localRotation.ToString().Replace(",", ".");
        
        string jsonData = "{" +
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"action\": \"MM-Add\"," +
            "\"sProcess\": \"" + GameManager.Instance.userID + "\"," +
            "\"sComplements\": [{ " +
               "\"key\": \"Item\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"Room\"}," +

               "{\"key\": \"From\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + GameManager.Instance.userID + "\"}," +

               "{\"key\": \"At\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + GameManager.Instance.currentPlayerLocation + "\"}," +               

               "{\"key\": \"With\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + "SA position: " + position + " rotation: " + rotation  + "\"}]," +            
            "\"dProcess\": \"Location Service\"" +
            "}";
        return jsonData;        
    }

    private void LaunchRoomServer(int port)
    {
        //Launch the server        
        string sceneName = "Room Server";

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = GameManager.Instance.pathToRoomExe;        
        startInfo.Arguments = $"-scene {sceneName} -port {port}";
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        
        Process serverProcess = new Process();
        serverProcess.StartInfo = startInfo;
        serverProcess.EnableRaisingEvents = true;
        serverProcess.Exited += (sender, args) => OnRoomServerClosed(port); // Attach event        
        serverProcess.Start();        
    }
    
    public void OnRoomServerClosed(int port)
    {        
        UnityEngine.Debug.Log($"Room server closed for port {port}");
        PortManager.Instance.PortsToDestroy.Enqueue(port);        
    }

}
