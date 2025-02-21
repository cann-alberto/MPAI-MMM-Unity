using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class ServerLauncher : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            UnityEngine.Debug.Log("Key C pressed");
            string sceneName = "TEST";
            int port = 7780;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "C:\\Users\\lab2a\\Documents\\Unity Projects\\MPAI-MMM\\Builds\\MPAI-MMM.exe"; //TODO: Make it a relative path
            startInfo.Arguments = $"-scene {sceneName} -port {port}";
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = false;

            Process serverProcess = new Process();
            serverProcess.StartInfo = startInfo;
            serverProcess.Start();

        }
    }
}
