using Mirror;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSocket : MonoBehaviour
{
    public NetworkManager networkManager;
    public TcpClient client;
    private NetworkStream stream;
    

    public void ConnectToServer(string serverAddress, int serverPort)
    {
        try
        {
            client = new TcpClient(serverAddress, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server!");            
            ReceiveMessage();
        }
        catch (Exception ex)
        {
            Debug.LogError("Client error: " + ex.Message);
        }
    }    

    public void SendSocketMessage(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.BeginWrite(buffer, 0, buffer.Length, OnWriteComplete, null);            
        }
    }

    private void OnWriteComplete(IAsyncResult ar)
    {
        try
        {
            stream.EndWrite(ar); // Completes the async write operation
            Debug.Log("Message write completed.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error writing to stream: " + ex.Message);
        }
    }

    void ReceiveMessage()
    {
        if (stream == null || !stream.CanRead) return; // Prevent issues if the stream is closed

        byte[] buffer = new byte[1024];
        stream.BeginRead(buffer, 0, buffer.Length, ar =>
        {
            try
            {
                int bytesRead = stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("Message received: " + message);
                }
                ReceiveMessage(); // Continue reading
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading from stream: " + ex.Message);
            }
        }, null);
    }

    void OnDestroy()
    {
        stream?.Close();
        client?.Close();
        Debug.Log("Client disconnected.");
    }

    // Coroutine for attempting to connect to the server
    public IEnumerator TryConnectToServerCoroutine(string serverAddress, int serverPort, int maxRetries, float retryDelay)
    {
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            bool connected = false;

            // Start a Task to attempt connecting to the server (asynchronous operation)
            var connectTask = TryConnectAsync(serverAddress, serverPort);

            // Wait for the Task to complete
            yield return new WaitUntil(() => connectTask.IsCompleted);

            // Check if the connection was successful
            connected = connectTask.Result;

            if (connected)
            {
                Debug.Log("Server is fully initialized!");

                // Move the user to the room
                if (networkManager == null)
                    networkManager = FindObjectOfType<NetworkManager>();

                networkManager.StopClient();
                SceneManager.LoadScene("Room");
                yield break;  // Exit the coroutine once connected
            }
            else
            {
                retryCount++;
                Debug.LogError($"Connection attempt {retryCount} failed.");

                if (retryCount >= maxRetries)
                {
                    Debug.LogError("Max retries reached. Could not connect to the server.");
                    yield break; // Exit after reaching max retries
                }

                Debug.Log($"Retrying in {retryDelay} seconds...");
                yield return new WaitForSeconds(retryDelay); // Wait before retrying
            }
        }
    }

    // Asynchronous function to attempt connecting to the server
    private async Task<bool> TryConnectAsync(string serverAddress, int serverPort)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                await client.ConnectAsync(serverAddress, serverPort);  // Asynchronous connection
                return true;  // Connection successful
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Connection failed: " + ex.Message);
            return false;  // Connection failed
        }
    }

}
