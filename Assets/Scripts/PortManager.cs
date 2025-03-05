using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class PortManager
{
    private static PortManager _instance;
    public static PortManager Instance => _instance ??= new PortManager();

    private SortedSet<int> availablePorts = new SortedSet<int>();
    private Dictionary<int, GameObject> portToRoom = new Dictionary<int, GameObject>();
    private int basePort = 7780; // Base port number

    public ConcurrentQueue<int> PortsToDestroy = new ConcurrentQueue<int>();


    private PortManager()
    {
        InitializePorts();
    }

    private void InitializePorts()
    {
        int maxPort = basePort + 100; // Define max range for ports
        for (int port = basePort; port <= maxPort; port += 2) // Reserve consecutive ports
        {
            availablePorts.Add(port);
        }        
    }

    public int FindFreePort()
    {
        if (availablePorts.Count < 2) return -1; // Ensure at least two available ports

        int port = availablePorts.Min; // Get the smallest available port
        availablePorts.Remove(port);
        //availablePorts.Remove(port + 1); // Reserve secondary port

        Debug.Log($"Assigned ports: {port} and {port + 1}");
        return port;
    }

    public void FreePort(int port)
    {
        Debug.Log("Free port starts");
        if (!availablePorts.Contains(port))
        {
            availablePorts.Add(port);
            //availablePorts.Add(port + 1); // Free the secondary port
            Debug.Log($"Freed ports: {port} and {port + 1}");
        }
    }

    public void TrackRoom(int port, GameObject roomObject)
    {
        portToRoom[port] = roomObject;
    }

    //public GameObject GetRoom(int port)
    //{
    //    Debug.Log("GetRoom");
    //    return portToRoom.TryGetValue(port, out var roomObject) ? roomObject : null;
    //}

    public GameObject GetRoom(int port)
    {
        UnityEngine.Debug.Log($"GetRoom called for port: {port}");

        if (portToRoom == null)
        {
            UnityEngine.Debug.LogError("portToRoom dictionary is NULL!");
            return null;
        }

        UnityEngine.Debug.Log("GetRoom 1" + portToRoom.Count);
        if (portToRoom.Count == 0)
        {
            UnityEngine.Debug.LogWarning("portToRoom dictionary is EMPTY!");
            return null;
        }

        UnityEngine.Debug.Log("GetRoom 2:" + portToRoom.ContainsKey(port));        
        if (portToRoom.TryGetValue(port, out GameObject roomObject))
        {
            if (roomObject != null)
            {
                UnityEngine.Debug.Log($"Room found for port {port}");
                return roomObject;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Room for port {port} exists in the dictionary but is NULL!");
                return null;
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"No room found for port {port} in portToRoom dictionary.");
            return null;
        }
    }

}
