using kcp2k;
using Mirror;
using ReadyPlayerMe.Samples.QuickStart;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameMenuEvents : MonoBehaviour
{
    private UIDocument _document;
    private VisualElement _inGameMenuVisualElem;
    private Button _buttonMMSend;
    private Button _buttonCreateRoom;
    private Button _buttonMenu;
    private Label _messageLabel;

    private PlayerTracker _playerTracker;    
    public NetworkManager networkManager;
    public KcpTransport transport;
    public CameraOrbit cameraOrbit;

    private Process _serverProcess;
    private ThirdPersonControllerNetworked _thirdPersonControllerNetworked;        

    void Awake()
    {        
        _document = GetComponent<UIDocument>();
        var root = _document.rootVisualElement;
        
        _buttonMenu = _document.rootVisualElement.Q("MenuButton") as Button;
        _buttonMenu.RegisterCallback<ClickEvent>(OnMenuButtonClicked);

        _inGameMenuVisualElem = root.Q("InGameMenuPanel") as VisualElement;
        _inGameMenuVisualElem.style.visibility = UnityEngine.UIElements.Visibility.Hidden;

        _buttonMMSend = _document.rootVisualElement.Q("MMSendButton") as Button;
        _buttonMMSend.RegisterCallback<ClickEvent>(OnMMSendButtonClicked);

        _buttonCreateRoom = root.Q("CreateRoomButton") as Button;
        _buttonCreateRoom.RegisterCallback<ClickEvent>(OnCreateRoomButtonClicked);

        _messageLabel = _document.rootVisualElement.Q("ChatTextLabel") as Label;
        GameManager.Instance.ServerSocket.OnMessageReceived += UpdateMessageLabel;

    }

    private void OnMenuButtonClicked(ClickEvent evt)
    {
        if (_inGameMenuVisualElem != null)
        {
            _thirdPersonControllerNetworked = NetworkClient.localPlayer.GetComponent<ThirdPersonControllerNetworked>();

            if (_inGameMenuVisualElem.style.visibility.Equals(UnityEngine.UIElements.Visibility.Visible))
            {                
                _inGameMenuVisualElem.style.visibility = UnityEngine.UIElements.Visibility.Hidden;
                cameraOrbit.enabled = true;                
                _thirdPersonControllerNetworked.inputEnabled = true;
            }
            else
            {
                _inGameMenuVisualElem.style.visibility = UnityEngine.UIElements.Visibility.Visible;
                cameraOrbit.enabled = false;
                _thirdPersonControllerNetworked.inputEnabled = false;
            }
        }
    }
    
    void UpdateMessageLabel(MessageInfo message)
    {
        if (_messageLabel != null)
        {
            //Split the string by newlines
            string[] lines = _messageLabel.text.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            
            //Remove the first line
            if (lines.Length > 10)
            {
                lines = new ArraySegment<string>(lines, 1, lines.Length - 1).ToArray();
                //+= "[" + DateTime.Now + "]: " + message + "\n";
            }

            // Add a new line
            string[] updatedLines = new string[lines.Length + 1];
            Array.Copy(lines, updatedLines, lines.Length);
            updatedLines[updatedLines.Length - 1] = 
                "[" + message.time + "] "+ message.source + " : " + message.inItem + "\n";

            // Join the lines back with newlines and print the result
            _messageLabel.text = string.Join("\n", updatedLines);            
        }
    }

  


    /* MM-SEND */
    private void OnMMSendButtonClicked(ClickEvent evt)
    {
        VisualElement sendMsgVisualElement = _document.rootVisualElement.Q("SendMsgVisualElement") as VisualElement;
        Button sendMsgButton = _document.rootVisualElement.Q("SendMessageButton") as Button;

        if (sendMsgVisualElement.style.display.Equals(UnityEngine.UIElements.DisplayStyle.Flex))
        {
            sendMsgVisualElement.style.display = UnityEngine.UIElements.DisplayStyle.None;
            sendMsgButton.UnregisterCallback<ClickEvent>(SendMsg);
        }

        else
        {
            sendMsgVisualElement.style.display= UnityEngine.UIElements.DisplayStyle.Flex;
            sendMsgButton.RegisterCallback<ClickEvent>(SendMsg);
        }
        
    }

    private void SendMsg(ClickEvent evt)
    {
        // Retrieve data from the GUI
        TextField toTextField = _document.rootVisualElement.Q("toTextField") as TextField;
        TextField msgTextField = _document.rootVisualElement.Q("msgTextField") as TextField;

        // Create Json data to be sent
        string actionDataJson = CreateMessageJson(toTextField.value, msgTextField.value);
        StartCoroutine(GameManager.Instance.WebAPIManager.Upload("Activity/actions", actionDataJson, HandleResponse));
        UnityEngine.Debug.Log("Message sent to the WebApiServer");

        // Send message to the target user
        SendTextMessage(toTextField.value, actionDataJson);
        UnityEngine.Debug.Log("Message sent to " + toTextField.value);

        MessageInfo myMessage = new MessageInfo(DateTime.Now, GameManager.Instance.userID, toTextField.value, "MM-Send", msgTextField.value, GameManager.Instance.currentPlayerPosition, "Square", "string");
        UpdateMessageLabel(myMessage);
        // Reset the TextField
        msgTextField.value = "";
    }

    private void HandleResponse(string response)
    {
        UnityEngine.Debug.Log("Received response: " + response); // Log the response received from the server

        // TODO: send the message to the Unity server
    }

    private string CreateMessageJson(string toTextField, string msgTextField)
    {        
        string jsonData = "{" +
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"source\": \"" + GameManager.Instance.userID + "\"," +
            "\"destination\": \"" + toTextField + "\"," +
            "\"action\": \"MM-Send\"," +
            "\"inItem\": \"" + msgTextField + "\"," +
            "\"inLocation\": \"" + GameManager.Instance.currentPlayerPosition + "\"," +
            "\"outLocation\": \"Square\"," + // TODO: retrieve position of the target pleyer
            "\"rightsID\": \"string\"" +
            "}";
        return jsonData;
    }

    private void SendTextMessage(string toTextField, string msgTextField)
    {
        string[] parts = toTextField.Split(':');

        if (parts.Length == 2 && int.TryParse(parts[1], out int port))
        {
            string ipAddress = parts[0];
            // Start the connection process asynchronously and wait for it to finish
            StartCoroutine(ConnectAndSendMessage(ipAddress, port, msgTextField));            
        }
        else
        {
            UnityEngine.Debug.Log("Invalid input format.");
        }        
    }

    // Coroutine that waits for the connection to complete and then sends the message
    private IEnumerator ConnectAndSendMessage(string ipAddress, int port, string msgTextField)
    {
        // Start the connection process
        GameManager.Instance.ClientSocket.ConnectToServer(ipAddress, port);

        // Wait until the connection is successfully established
        while (GameManager.Instance.ClientSocket.client == null || !GameManager.Instance.ClientSocket.client.Connected)
        {
            yield return null; // Wait for one frame
        }

        // Once connected, send the message
        GameManager.Instance.ClientSocket.SendSocketMessage(msgTextField);
    }

    /* CREATE ROOM */
    private void OnCreateRoomButtonClicked(ClickEvent evt)
    {
        UnityEngine.Debug.Log("Create Room button clicked.");

        _playerTracker = GameManager.Instance.localPlayerPrefab.GetComponent<PlayerTracker>();

        if (_playerTracker == null)
        {
            UnityEngine.Debug.LogError("PlayerTracker not found in the local player.");
        }
        if (_playerTracker != null)
        {
            _playerTracker.OnRoomInstantiated += HandleRoomInstantiated;
            _playerTracker.InstantiateRoomPrefab();
        }
        else
        {
            UnityEngine.Debug.LogError("PlayerTracker is null or not assigned.");
        }        

    }
    private void HandleRoomInstantiated()
    {
        UnityEngine.Debug.Log("Room prefab instantiated, trying to connet to the server");

        _playerTracker.OnRoomInstantiated -= HandleRoomInstantiated; // Unsubscribe to avoid memory leaks        
        
        // Activate the camera orbit script
        cameraOrbit.enabled = true;
    }

    void OnApplicationQuit()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill();
            _serverProcess.Dispose();
            UnityEngine.Debug.Log("Server process terminated.");
        }
    }
    
}
