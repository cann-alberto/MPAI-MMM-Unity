using Mirror;
using UnityEngine;

public class ClosedRoomsManager : NetworkBehaviour
{   
    
    void Update()
    {
        if (isServer)
        {
            if (!PortManager.Instance.PortsToDestroy.IsEmpty)
            {
                while (PortManager.Instance.PortsToDestroy.TryDequeue(out int port))
                {
                    Debug.Log($"Try dequeue and destroy port: {port}");
                    DestroyRoom(port);
                }
            }            
        }        
    }
    
    public void DestroyRoom(int port)
    {
        if (!isServer) return;         
        
        Debug.Log("Searching the room");

        GameObject roomObject = PortManager.Instance.GetRoom(port);
        if (roomObject != null)
        {
            Debug.Log($"Destroying room on port {port}");

            // Free the port before destroying the object
            PortManager.Instance.FreePort(port);

            // destroy the GameObject 
            NetworkServer.Destroy(roomObject);
            Debug.Log($"Room on port {port} has been destroyed.");
        }
        else
        {
            Debug.LogWarning($"No room found on port {port}.");
        }
    }
}
