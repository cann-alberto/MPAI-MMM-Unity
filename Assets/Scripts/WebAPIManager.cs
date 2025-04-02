using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebAPIManager : MonoBehaviour
{
    [SerializeField]
    public string BASE_URL;
    
    public IEnumerator GetRequest(string uri)
    {
        uri = BASE_URL + uri;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    public IEnumerator GetRequest(string uri, System.Action<string> callback)
    {
        uri = BASE_URL + uri;

        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                    callback?.Invoke(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    public IEnumerator Upload(string uri, string jsonData)
    {
        uri = BASE_URL + uri;
        using (UnityWebRequest www = UnityWebRequest.Post(uri, jsonData, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Debug.Log("Form upload completed!");
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    public IEnumerator Upload(string uri, string jsonData, System.Action<string> callback)
    {
        uri = BASE_URL + uri;
        using (UnityWebRequest www = UnityWebRequest.Post(uri, jsonData, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                callback?.Invoke(null);
            }
            else
            {                
                Debug.Log(www.downloadHandler.text);
                callback?.Invoke(www.downloadHandler.text);
            }
        }
    }

    public IEnumerator PutRequest(string uri, string jsonData, System.Action<string> callback)
    {
        uri = BASE_URL + uri;
        using (UnityWebRequest webRequest = UnityWebRequest.Put(uri, jsonData))
        {
            // Set the content type to application/json
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("PUT Request Failed: " + webRequest.error); 
                callback?.Invoke(null);
            }
            else
            {
                Debug.Log("PUT Request Successful: " + webRequest.downloadHandler.text);
                callback?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
}


[Serializable]
public class PersonalProfileData
{
    public string firstName;
    public string lastName;
    public int age;
    public string nationality;
    public string email;
}

[Serializable]
public class Profile
{
    public string personalProfileID;
    public string header;
    public string mInstanceID;
    public string humanID;
    public PersonalProfileData personalProfileData;
    public string descrMetadata;
}

[Serializable]
public class Account
{

    public string accountID;
    public string header;
    public string mInstanceID;
    public string mEnvironmentID;
    public string humanID;
    public string personalProfileID;
    public List<Right> rights;
    public List<User> users;
    public List<Persona> personae;
    public string descrMetadata;
}

[Serializable]
public class Right
{
    public string rightID;
    public string header;
    public string mInstanceID;
    public List<RightData> rightsData;
    public string descrMetadata;
}

[Serializable]
public class User
{
    public string userID;
    public string humanID;
    public string comIp;
    public string comPort;
}

[Serializable]
public class Persona
{
    public string personaID;

    public string model;
}

[Serializable]
public class RightData
{

    public string level;

    public string processAction;
}

