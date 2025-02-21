using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;


public class MainMenuEvents : MonoBehaviour
{    
    [SerializeField]
    private NetworkClientManager _networkClientManager; // Interface to MMM server through Mirror lib

    private UIDocument _document;
    private Button _buttonLogin;
    private Button _buttonRegister;
    private VisualElement _registrationForm;
    private VisualElement _loginForm;    

    private void Awake()
    {
        _document = GetComponent<UIDocument>();

        _buttonLogin = _document.rootVisualElement.Q("LoginButton") as Button;
        _buttonLogin.RegisterCallback<ClickEvent>(OnLoginButtonClick);
        
        _buttonRegister = _document.rootVisualElement.Q("RegisterButton") as Button;
        _buttonRegister.RegisterCallback<ClickEvent>(OnRegisterButtonClick);

        _loginForm = _document.rootVisualElement.Q("LoginForm") as VisualElement;
        _registrationForm = _document.rootVisualElement.Q("RegistrationForm") as VisualElement;
    }

    private void OnDisable()
    {
        _buttonLogin.UnregisterCallback<ClickEvent>(OnLoginButtonClick);
        _buttonRegister.UnregisterCallback <ClickEvent>(OnRegisterButtonClick);        
    }

    private void OnLoginButtonClick(ClickEvent evt)
    {
        _registrationForm.style.visibility = Visibility.Hidden; 
        _loginForm.style.visibility = Visibility.Visible;
        Button submitLoginButton = _document.rootVisualElement.Q("submitLoginButton") as Button;
        submitLoginButton.RegisterCallback<ClickEvent>(OnSubmitLoginButtonClick);
           
    }

    private void OnSubmitLoginButtonClick(ClickEvent evt)
    {
        // Get HumanID
        TextField humanIdTextField = _document.rootVisualElement.Q("loginHumanIdTextField") as TextField;
        StartCoroutine(LoginUser(humanIdTextField.value));                                       
    }

    private IEnumerator LoginUser(string humanID)
    {
        Account userAccount = null;

        // Wait for the request to complete
        yield return GameManager.Instance.WebAPIManager.GetRequest("Registration/accounts/" + humanID, (responseText) =>
        {
            if (responseText != null)
            {                
                userAccount = JsonUtility.FromJson<Account>(responseText);                
            }
            else
            {
                Debug.LogError("Error while retrieving the account.");
            }
        });

        // Ensure the account data is available before proceeding
        if (userAccount == null)
        {
            Debug.LogError("Failed to login. Account is null.");
            yield break;
        }

        bool actionSent = false;
        string actionDataJson = CreateLoginMessageJson(userAccount.users[0].id);
        yield return GameManager.Instance.WebAPIManager.Upload("Activity/actions", actionDataJson, (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("Actions sent successfully:" + responseText);
                actionSent = true;
            }
            else
            {
                Debug.LogError("Error while sending login messagge.");                
            }
        });
        
        if (!actionSent) yield break;
        
        // Update the DataManager with the retrieved account data
        GameManager.Instance.humanID = humanID;
        GameManager.Instance.personaUrl = GameManager.Instance.WebAPIManager.BASE_URL + "Registration/avatars/" + userAccount.personae[0].model;
        GameManager.Instance.userID = userAccount.users[0].id;

        _networkClientManager.ConnectToServer(); // Start Mirror Server
        GameManager.Instance.ServerSocket.StartServer(GameManager.Instance.port); // Start Info Server
        
    }

    //Registration
    private void OnRegisterButtonClick(ClickEvent evt)
    {
        _loginForm.style.visibility = Visibility.Hidden;
        _registrationForm.style.visibility = Visibility.Visible;
        Button submitRegistrationButton = _document.rootVisualElement.Q("submitRegistrationButton") as Button;
        submitRegistrationButton.RegisterCallback<ClickEvent>(OnSubmitRegisterButtonClick);

        
        Button maleButton = _document.rootVisualElement.Q("AvatarButton0") as Button;
        maleButton.RegisterCallback<ClickEvent>(OnAvatar0ButtonClick);
        Button male2Button = _document.rootVisualElement.Q("AvatarButton1") as Button;
        male2Button.RegisterCallback<ClickEvent>(OnAvatar1ButtonClick);
        Button femaleButton = _document.rootVisualElement.Q("AvatarButton2") as Button;
        femaleButton.RegisterCallback<ClickEvent>(OnAvatar2ButtonClick);
        Button female2Button = _document.rootVisualElement.Q("AvatarButton3") as Button;
        female2Button.RegisterCallback<ClickEvent>(OnAvatar3ButtonClick);

    }

    private void OnAvatar3ButtonClick(ClickEvent evt)
    {
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;
        personaTextField.value = "Female2";
    }

    private void OnAvatar2ButtonClick(ClickEvent evt)
    {
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;
        personaTextField.value = "Female";
    }

    private void OnAvatar1ButtonClick(ClickEvent evt)
    {
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;
        personaTextField.value = "Male2";
    }

    private void OnAvatar0ButtonClick(ClickEvent evt)
    {
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;
        personaTextField.value = "Male";
    }

    private void OnSubmitRegisterButtonClick(ClickEvent evt)
    {
        // Collect personal data in JSON format
        string personalDataJson = CreatePersonalDataJson();

        // Start the process with the profile creation
        StartCoroutine(CreateProfileAndAccount(personalDataJson));
    }

    private IEnumerator CreateProfileAndAccount(string personalDataJson)
    {
        // Step 1: Create the profile
        Profile newProfile = null;        
        yield return GameManager.Instance.WebAPIManager.Upload("Registration/profiles", personalDataJson, (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("Profile created successfully\n Profile:" + responseText);
                newProfile = JsonUtility.FromJson<Profile>(responseText);                
            }
            else
            {
                Debug.LogError("Error while creating the profile.");
            }
        });
        if (newProfile == null) yield break;

        // Step 2: Create the account
        Account newAccount = null;
        string accountJson = ConvertToAccount(newProfile);
        yield return GameManager.Instance.WebAPIManager.Upload("Registration/accounts", accountJson, (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("Account created successfully\n Account:" + responseText);
                newAccount = JsonUtility.FromJson<Account>(responseText);
            }
            else
            {
                Debug.LogError("Error while creating the account.");
            }
        });
        if (newAccount == null) yield break;

        // Step 3: Create the user
        User newUser = null;        
        yield return GameManager.Instance.WebAPIManager.Upload("Registration/users", "{}", (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("User created successfully\n User:" + responseText);
                newUser = JsonUtility.FromJson<User>(responseText);
            }
            else
            {
                Debug.LogError("Error while creating the user");
            }
        });
        if (newUser == null) yield break;        

        // Step 4: Update the account
        bool accountUpdated = false;
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;        
        string accoutJson = CreateUpdatedAccountJson(newAccount, newUser, personaTextField.value);
        yield return GameManager.Instance.WebAPIManager.PutRequest("Registration/accounts/" + newAccount.accountID, accoutJson, (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("Account updated successfully for account ID: " + newAccount.accountID);
                accountUpdated = true;
            }
            else
            {
                Debug.LogError("Error while updating account.");
            }
        });
        if (!accountUpdated) yield break;
        GameManager.Instance.personaUrl = GameManager.Instance.WebAPIManager.BASE_URL + "Registration/avatars/" + personaTextField.value;
        GameManager.Instance.userID = newUser.id;

        // Step 5: Sending login message to the ActivitySrv
        bool actionSent = false;
        string actionDataJson = CreateLoginMessageJson(newUser.id);
        yield return GameManager.Instance.WebAPIManager.Upload("Activity/actions", actionDataJson, (responseText) =>
        {
            if (responseText != null)
            {
                Debug.Log("Actions sent successfully:" + responseText);
                actionSent = true;
            }
            else
            {
                Debug.LogError("Error while sending login messagge.");
            }
        });

        if (!actionSent) yield break;



        // Step 5: Connect to the server
        _networkClientManager.ConnectToServer();
        GameManager.Instance.ServerSocket.StartServer(GameManager.Instance.port); // Start Info Server
    }

    private string CreatePersonalDataJson()
    {
        TextField firstNameTextField = _document.rootVisualElement.Q("firstNameTextField") as TextField;
        TextField lastNameTextField = _document.rootVisualElement.Q("lastNameTextField") as TextField;
        IntegerField ageTextField = _document.rootVisualElement.Q("ageTextField") as IntegerField;
        TextField nationalityTextField = _document.rootVisualElement.Q("nationalityTextField") as TextField;
        TextField emailTextField = _document.rootVisualElement.Q("emailTextField") as TextField;
        TextField humanIdTextField = _document.rootVisualElement.Q("humanIdTextField") as TextField;

        string jsonData = "{" +
            " \"header\": \"MMM-ACC-V1.2\",  " +
            " \"mInstanceID\": \"MInstance00\",  " +
            " \"humanID\": \"" + humanIdTextField.value + "\", " +
            "\"personalProfileData\": {" +
            "   \"firstName\": \"" + firstNameTextField.value + "\", " +
            "   \"lastName\": \"" + lastNameTextField.value + "\", " +
            "   \"age\":" + ageTextField.value + ", " +
            "   \"nationality\": \"" + nationalityTextField.value + "\"," +
            "   \"email\": \"" + emailTextField.value + "\" " +
            "},  " +
            "\"descrMetadata\": \"" + DateTime.Now + "\"}";
        
        Debug.Log("jsonData to be sent [GetPersonalData]: " + jsonData);

        // Passing data to the online scene
        GameManager.Instance.playerName = firstNameTextField.value;
        GameManager.Instance.humanID = humanIdTextField.value;        
        return jsonData;
    }

    private string ConvertToAccount(Profile newProfile)
    {
        string jsonData = "{" +
            " \"header\": \"" + newProfile.header + "\", " +
            " \"mInstanceID\": \"" + newProfile.mInstanceID + "\", " +
            " \"mEnvironmentID\": \"Environment00\",  " +            
            " \"humanID\": \"" + newProfile.humanID + "\", " +
            " \"personalProfileID\": \"" + newProfile.personalProfileID + "\", " +
            " \"rights\": []," +
            " \"users\": []," +
            " \"personae\": []," +
            "\"descrMetadata\": \"" + DateTime.Now + "\"}";               
        return jsonData;
    }

    private string CreatePersonaJsonPart ()
    {
        TextField personaTextField = _document.rootVisualElement.Q("personaTextField") as TextField;
        string jsonData = "{" +
            "\"model\": \"" + personaTextField.value + "\"}";

        GameManager.Instance.personaUrl = GameManager.Instance.WebAPIManager.BASE_URL + "Registration/avatars/" + personaTextField.value;        
        return jsonData;
    }

    private string CreateLoginMessageJson(string userID) 
    {
        string jsonData = "{" +            
            "\"time\": \"" + DateTime.Now + "\"," +
            "\"source\": \"" + userID + "\"," +            
            "\"destination\": \"Activity Service\"," +
            "\"action\": \"MM-Send\"," +
            "\"inItem\": \"I’m on MMM\"," +
            "\"inLocation\": \"Square\"," +
            "\"outLocation\": \"Square\"," +
            "\"rightsID\": \"string\"" +
            "}";
        return jsonData;
    }

    private string CreateUpdatedAccountJson(Account accountToUpdate, User newUser, string persona)
    {
        string jsonData = "{" +
            " \"accountID\": \"" + accountToUpdate.accountID + "\", " +
            " \"header\": \"" + accountToUpdate.header + "\", " +
            " \"mInstanceID\": \"" + accountToUpdate.mInstanceID + "\", " +
            " \"mEnvironmentID\": \"Environment00\",  " +
            " \"humanID\": \"" + accountToUpdate.humanID + "\", " +
            " \"personalProfileID\": \"" + accountToUpdate.personalProfileID + "\", " +
            " \"rights\": []," +
            " \"users\": [{\"id\": \"" + newUser.id + "\"}]," +            
            " \"personae\": [{\"model\": \"" + persona + "\"}]," +
            "\"descrMetadata\": \"" + DateTime.Now + "\"}";
        Debug.Log(jsonData);
        return jsonData;        
    }


}






