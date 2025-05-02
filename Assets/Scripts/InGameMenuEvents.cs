using kcp2k;
using Mirror;
using ReadyPlayerMe.Core;
using ReadyPlayerMe.Samples.QuickStart;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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
            }

            // Add a new line
            string[] updatedLines = new string[lines.Length + 1];
            Array.Copy(lines, updatedLines, lines.Length);
            updatedLines[updatedLines.Length - 1] = "[" + message.descrMetadata + "]" + " : " + message.messageData.messagePayload + "\n";

            // Join the lines back with newlines and print the result
            _messageLabel.text = string.Join("\n", updatedLines);            
        }
    }

    #region MM-SEND
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
        TextField toTextField = _document.rootVisualElement.Q<TextField>("toTextField");
        TextField msgTextField = _document.rootVisualElement.Q<TextField>("msgTextField");

        if (toTextField == null || msgTextField == null)
        {
            UnityEngine.Debug.LogError("Input fields not found.");
            return;
        }

        // Retrieve comInfo by humanID        
        StartCoroutine(RetrieveComInfo(toTextField.value, (comInfo) =>
        {
            if (string.IsNullOrEmpty(comInfo))
            {
                UnityEngine.Debug.LogError("Failed to retrieve communication info.");
                return;
            }

            // Send the message                        
            string messageDataJson = CreateMessageJson(comInfo, msgTextField.value);
            StartCoroutine(GameManager.Instance.WebAPIManager.UploadRequest("Communication/messages", messageDataJson, HandleResponse));

            SendTextMessage(comInfo, messageDataJson);
            //UnityEngine.Debug.Log("Message sent to " + comInfo);

            MessageInfo myMessage = JsonUtility.FromJson<MessageInfo>(messageDataJson);                          
            UpdateMessageLabel(myMessage);

            //msgTextField.value = "";
        }));
    }

    private IEnumerator RetrieveComInfo(string humanID, Action<string> onCompleted)
    {
        User targetUser = null;
        yield return GameManager.Instance.WebAPIManager.GetRequest("Activity/users/humanid/" + humanID, (responseText) =>
        {
            if (!string.IsNullOrEmpty(responseText))
            {
                targetUser = JsonUtility.FromJson<User>(responseText);
            }
            else
            {
                UnityEngine.Debug.LogError("Error while retrieving the account.");
            }
        });
        
        if (targetUser == null)
        {
            UnityEngine.Debug.LogError("Account is null.");
            onCompleted?.Invoke(null);
            yield break;
        }        

        string comInfo = targetUser.comIp + ":" + targetUser.comPort;        
        onCompleted?.Invoke(comInfo);
    }

    private void HandleResponse(string response)
    {
        UnityEngine.Debug.Log("Received response: " + response); // Log the response received from the server        
    }

    private string CreateMessageJson(string toTextField, string msgTextField)
    {              
        string jsonData = "{" +
            " \"header\": \"MMM-MSG-V1.0\",  " +
            " \"mInstanceID\": \"MInstance00\",  " +
            " \"messageData\": {" +
                "\"messagePayload\": \"" + msgTextField + "\", " +
                "\"payloadData\": {}}, " +                
            "\"descrMetadata\": \"" + DateTime.Now + " From: " + GameManager.Instance.humanID + "\"}";
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
    #endregion

    #region ROOM
    private void OnCreateRoomButtonClicked(ClickEvent evt)
    {      
        _playerTracker = GameManager.Instance.localPlayerPrefab.GetComponent<PlayerTracker>();

        if (_playerTracker == null)
        {
            UnityEngine.Debug.LogError("PlayerTracker not found in the local player.");
        }
        if (_playerTracker != null)
        {
            StartCoroutine(BuyAndCreateRoom(_playerTracker));                     
        }              
    }


    private IEnumerator BuyAndCreateRoom(PlayerTracker playerTracker)
    {
        
        
        
        
        // User buys the Parcel        
        bool isTransactSucces = false;
        string newTransact = CreateTransaction();        

        yield return GameManager.Instance.WebAPIManager.UploadRequest("Transaction/transactions", newTransact, (responseText) =>
        {
            if (responseText != null)
            {
                UnityEngine.Debug.Log("Transaction created successfully");                
                isTransactSucces = true;
            }
            else
            {
                UnityEngine.Debug.LogError("Error while creating the trasaction");
            }
        });
        if (!isTransactSucces) yield break;

        // Request to istantiate the room pref on the server
        _playerTracker.OnRoomInstantiated += HandleRoomInstantiated;
        _playerTracker.InstantiateRoomPrefab();
    }

    private void HandleRoomInstantiated()
    {                
        _playerTracker.OnRoomInstantiated -= HandleRoomInstantiated; // Unsubscribe to avoid memory leaks        
        // Activate the camera orbit script
        cameraOrbit.enabled = true;
    }

    private string CreateTransaction()
    {
        
        string jsonData = "{" +
            " \"header\": \"MMM-TRA-V1.0\",  " +
            " \"mInstanceID\": \"MInstance00\",  " +
            " \"transactionData\": {" +
            "   \"assetID\": \"" + GameManager.Instance.currentPlayerLocation + "\"," +
            "   \"senderData\": {" +
            "   \"senderID\": \"" + GameManager.Instance.userID + "\"}" +
            "},  " +
            "\"descrMetadata\": \"" + DateTime.Now + "\"}";
        
        return jsonData;
    }
    #endregion

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
