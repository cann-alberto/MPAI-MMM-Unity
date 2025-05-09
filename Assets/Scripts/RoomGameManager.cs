using UnityEngine;
using Mirror;
using kcp2k;
using System.IO;
using System;
using System.Collections.Generic;
using GLTFast.Schema;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Unity.VisualScripting.Antlr3.Runtime;

public class RoomGameManager : MonoBehaviour
{
    [SerializeField]
    private ServerSocket _serverSocket;
    private NetworkManager _networkManager;
    private KcpTransport _transport;
    private string _logFilePath;    
    
    void Awake()
    {               
        Dictionary<string, string> arguments = new Dictionary<string, string>();                                                    
        // parsing arguments
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {                
            if (args[i].StartsWith("-"))
            {
                string key = args[i];
                string value = (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ? args[i + 1] : null;
                arguments[key] = value;
            }
        }

        //Select network manager
        if (_networkManager == null)
            _networkManager = FindObjectOfType<NetworkManager>();
        if (_transport == null)
            _transport = FindObjectOfType<KcpTransport>();

        // Server automatically launched
        if (arguments.ContainsKey("-port"))
        {
            _logFilePath = Path.Combine(Application.persistentDataPath, "log.txt");
            Application.logMessageReceived += LogToFileHandler;

            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
            {
                _transport.port = (ushort)int.Parse(arguments["-port"]);
                writer.WriteLine(DateTime.Now + ": Starting server on port: " + _transport.port);

                if (!_networkManager.isNetworkActive)
                {
                    _networkManager.StartServer();  // Start Mirror server
                    writer.WriteLine(DateTime.Now + ": Server started at port " + _transport.port);
                }

                // Start Communication Server 
                _serverSocket.StartServer("0.0.0.0", _transport.port + 1); //TODO specify the ip address of the comunication server for the room
            }
        }
        else // A client is connecting to the room
        {            
            string moveDataJson = CreateEmbedDataJson();
            StartCoroutine(GameManager.Instance.WebAPIManager.UploadRequest("Activity/process-actions", moveDataJson, HandleResponse));

            _transport.port = (ushort)RoomData.netInfo.Port;            
            _networkManager.StartClient(); // Start Mirror client            
        }
        
    }

    private string CreateEmbedDataJson()
    {
        string position = transform.position.ToString().Replace(",", ".");
        string rotation = transform.localRotation.ToString().Replace(",", ".");

        string jsonData = "{" +
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"action\": \"MM-Embed\"," +
            "\"sProcess\": \"" + GameManager.Instance.userID + "\"," +
            "\"sComplements\": [{ " +
               "\"key\": \"Item\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"Persona\"}," +

               "{\"key\": \"From\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + GameManager.Instance.userID + "\"}," +

               "{\"key\": \"At\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + GameManager.Instance.currentPlayerLocation + "\"}," +

               "{\"key\": \"With\"," +
               "\"valueType\": \"string\"," +
               "\"value\": \"" + "SA position: " + position + " rotation: " + rotation + "\"}]," +
            "\"dProcess\": \"Location Service\"" +
            "}";
        return jsonData;        
    }


    private void HandleResponse(string response)
    {
        Debug.Log("Received response: " + response); // Log the response received from the server        
    }

    private void LogToFileHandler(string logString, string stackTrace, LogType type)
    {
        using (StreamWriter writer = new StreamWriter(_logFilePath, true))
        {
            writer.WriteLine($"{type}: {logString}");
            if (type == LogType.Error || type == LogType.Exception)
            {
                writer.WriteLine(stackTrace);
            }
        }
    }
    
}


public static class RoomData
{
    public static string roomName;
    public static NetInfo netInfo;
}