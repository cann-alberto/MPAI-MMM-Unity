using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


public class ServerSocket : MonoBehaviour
{
    private TcpListener _server;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isRunning = false;

    private ConcurrentQueue<MessageInfo> messageQueue = new ConcurrentQueue<MessageInfo>(); // Thread-safe queue

    public event Action<MessageInfo> OnMessageReceived;
    public void StartServer(string ipAddress, int port)
    {
        try
        {
            //_server = new TcpListener(IPAddress.Any, port);
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);
            _server.Start();
            _isRunning = true;
            Debug.Log("Server socket started. Waiting for client...");
            _server.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (Exception ex)
        {
            Debug.LogError("Server error: " + ex.Message);
        }
    }

    void OnClientConnected(IAsyncResult result)
    {
        if (!_isRunning) return;
        
        _client = _server.EndAcceptTcpClient(result);
        _stream = _client.GetStream();
        Debug.Log("Client connected!");
        ReceiveMessage();
    }

    void ReceiveMessage()
    {        
        if (_stream == null || !_stream.CanRead)
            return; // Prevent issues if the stream is closed                    

        byte[] buffer = new byte[1024];
        _stream.BeginRead(buffer, 0, buffer.Length, ar =>
        {
            try
            {
                int bytesRead = _stream.EndRead(ar);                
                if (bytesRead > 0)
                {
                    string responseText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageInfo message = JsonUtility.FromJson<MessageInfo>(responseText);
                    messageQueue.Enqueue(message);
                    
                }
                
                // Closing current connection
                _client.Close();
                _stream.Close();                
                _server.BeginAcceptTcpClient(OnClientConnected, null);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading from stream: " + ex.Message);
            }
        }, null);
    }
   
    public void SendSocketMessage(string message)
    {
        if (_stream != null && _stream.CanWrite)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            _stream.BeginWrite(buffer, 0, buffer.Length, OnWriteComplete, null);
            Debug.Log("Message sent: " + message);
        }
    }

    private void OnWriteComplete(IAsyncResult ar)
    {
        try
        {
            _stream.EndWrite(ar); // Completes the async write operation
            Debug.Log("Message write completed.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error writing to stream: " + ex.Message);
        }
    }

    void OnDestroy()
    {
        // TODO: Close the server socket only when the entire application was closed, not only the current scene
        //isRunning = false;
        //stream?.Close();
        //client?.Close();
        //server?.Stop();        
    }

    private void Update()
    {
        // Process messages from the queue on the main thread
        if (messageQueue.TryDequeue(out MessageInfo message))
        {            
            OnMessageReceived?.Invoke(message);
        }
    }
}

[Serializable]
public class MessageInfo
{
    public string header;
    public string mInstanceID;
    public MessageData messageData;
    public string descrMetadata;            

    public MessageInfo(string header, string mInstanceID, MessageData messageData, string descrMetadata)
    {
        this.header = header;
        this.mInstanceID = mInstanceID;
        this.messageData = messageData;
        this.descrMetadata = descrMetadata;
    }     
}

[Serializable]
public class MessageData
{
    public string messagePayload;
    public PayloadData payloadData;    
}

[Serializable]
public class PayloadData
{
    public int payloadLength;
    public string payloadDataURI;
}

