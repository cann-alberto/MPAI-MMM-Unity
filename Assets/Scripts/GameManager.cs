using System.Globalization;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public string playerName;
    public string personaUrl;
    public string humanID;
    public string userID;
    [Tooltip("Port for the Communication Server")]
    public int port;
    [Tooltip("Path to the Room.exe file")]
    public string pathToRoomExe;


    public string currentPlayerPosition;

    public GameObject localPlayerPrefab;

    private ClientSocket _clientSocket;
    public ClientSocket ClientSocket
    { 
        get
        { 
            if(_clientSocket == null)
                _clientSocket = FindObjectOfType<ClientSocket>();
            return _clientSocket;
        }
        private set { _clientSocket = value; }
    }
    
    private ServerSocket _serverSocket;
    public ServerSocket ServerSocket
    {
        get
        {
            if (_serverSocket == null)
                _serverSocket = FindObjectOfType<ServerSocket>();
            return _serverSocket;
        }
        private set { _serverSocket = value; }
    }

    private WebAPIManager _webAPIManager;
    public WebAPIManager WebAPIManager
    {
        get
        {
            if (_webAPIManager == null)
                _webAPIManager = FindObjectOfType<WebAPIManager>();
            return _webAPIManager;
        }
        private set { _webAPIManager = value; }
    }

        
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);            
        }
    }
}
