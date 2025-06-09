using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class FirebaseAuthResponse
{
    public string localId;
    public string email;
    public string displayName;
    public string idToken;
    public string refreshToken;
    public string expiresIn;
    public bool registered;
}

[System.Serializable]
public class FirebaseAuthRequest
{
    public string email;
    public string password;
    public bool returnSecureToken = true;
}

[System.Serializable]
public class FirestoreField
{
    public object stringValue;
    public object integerValue;
    public object doubleValue;
    public object booleanValue;
    public object timestampValue;
    public object arrayValue;
    public object mapValue;
}

[System.Serializable]
public class FirestoreDocument2
{
    public string name;
    public Dictionary<string, FirestoreField> fields;
    public string createTime;
    public string updateTime;
}

public class FirebaseRestManager : MonoBehaviour
{
    [Header("Firebase Configuration")]
    public string firebaseProjectId = "smashbrawl-4fca6";
    public string firebaseApiKey = "AIzaSyCXU2WmL5CfB5JBTR3_FOWJPFvYMAl-kkU";

    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject profilePanel;
    public GameObject guestPanel;

    [Header("Login UI")]
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;
    public Button loginButton;
    public Button switchToRegisterButton;
    public Button guestLoginButton;

    [Header("Register UI")]
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField registerConfirmPassword;
    public TMP_InputField registerUsername;
    public Button registerButton;
    public Button switchToLoginButton;

    [Header("Profile UI")]
    public TextMeshProUGUI profileEmailText;
    public TextMeshProUGUI profileUsernameText;
    public TextMeshProUGUI accountTypeText;
    public Button logoutButton;
    public Button continueGameButton;

    [Header("Status UI")]
    public TextMeshProUGUI statusText;
    public GameObject loadingIndicator;

    private string currentUserId = "";
    private string currentIdToken = "";
    private string currentRefreshToken = "";
    private bool isGuest = false;
    private bool isLoggedIn = false;

    // Firestore REST API URLs
    private string authSignInUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
    private string authSignUpUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={firebaseApiKey}";
    private string authRefreshUrl => $"https://securetoken.googleapis.com/v1/token?key={firebaseApiKey}";
    private string firestoreBaseUrl => $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents";

    public event System.Action<string> OnUserSignedIn;
    public event System.Action OnUserSignedOut;
    public event System.Action<bool> OnAuthStateChanged;

    void Start()
    {
        SetupUI();
        CheckPreviousLogin();
    }

    void SetupUI()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(() => StartCoroutine(LoginWithEmail(loginEmail.text, loginPassword.text)));
        if (switchToRegisterButton != null)
            switchToRegisterButton.onClick.AddListener(() => ShowPanel("register"));
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(LoginAsGuest);

        if (registerButton != null)
            registerButton.onClick.AddListener(() => StartCoroutine(RegisterWithEmail(
                registerEmail.text, registerPassword.text, registerConfirmPassword.text, registerUsername.text)));
        if (switchToLoginButton != null)
            switchToLoginButton.onClick.AddListener(() => ShowPanel("login"));

        if (logoutButton != null)
            logoutButton.onClick.AddListener(Logout);
        if (continueGameButton != null)
            continueGameButton.onClick.AddListener(ContinueGame);

        ShowPanel("login");
    }

    void CheckPreviousLogin()
    {
        string savedUserId = PlayerPrefs.GetString("UserId", "");
        string savedToken = PlayerPrefs.GetString("IdToken", "");
        string savedRefreshToken = PlayerPrefs.GetString("RefreshToken", "");
        bool wasGuest = PlayerPrefs.GetInt("WasGuest", 0) == 1;

        if (!string.IsNullOrEmpty(savedUserId))
        {
            currentUserId = savedUserId;
            currentIdToken = savedToken;
            currentRefreshToken = savedRefreshToken;
            isGuest = wasGuest;

            if (wasGuest)
            {
                HandleSuccessfulLogin(savedUserId, "", "Guest", true);
            }
            else if (!string.IsNullOrEmpty(savedRefreshToken))
            {
                StartCoroutine(RefreshToken());
            }
        }
    }

    #region Authentication Methods

    IEnumerator LoginWithEmail(string email, string password)
    {
        if (!ValidateEmailLogin(email, password)) yield break;

        ShowLoading(true);
        SetStatus("Signing in...");

        var request = new FirebaseAuthRequest
        {
            email = email,
            password = password
        };

        string jsonData = JsonConvert.SerializeObject(request);

        using (UnityWebRequest www = new UnityWebRequest(authSignInUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            ShowLoading(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<FirebaseAuthResponse>(www.downloadHandler.text);
                SaveAuthData(response.localId, response.idToken, response.refreshToken, false);
                HandleSuccessfulLogin(response.localId, response.email, response.displayName ?? response.email.Split('@')[0], false);
            }
            else
            {
                HandleAuthError(www.downloadHandler.text);
            }
        }
    }

    IEnumerator RegisterWithEmail(string email, string password, string confirmPassword, string username)
    {
        if (!ValidateRegistration(email, password, confirmPassword, username)) yield break;

        ShowLoading(true);
        SetStatus("Creating account...");

        var request = new FirebaseAuthRequest
        {
            email = email,
            password = password
        };

        string jsonData = JsonConvert.SerializeObject(request);

        using (UnityWebRequest www = new UnityWebRequest(authSignUpUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            ShowLoading(false);

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<FirebaseAuthResponse>(www.downloadHandler.text);
                SaveAuthData(response.localId, response.idToken, response.refreshToken, false);

                // Save user profile to Firestore
                StartCoroutine(SaveUserProfile(response.localId, username, email));

                HandleSuccessfulLogin(response.localId, response.email, username, false);
            }
            else
            {
                HandleAuthError(www.downloadHandler.text);
            }
        }
    }

    IEnumerator RefreshToken()
    {
        if (string.IsNullOrEmpty(currentRefreshToken)) yield break;

        var requestData = new Dictionary<string, object>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = currentRefreshToken
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest www = new UnityWebRequest(authRefreshUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                currentIdToken = response["id_token"].ToString();
                currentRefreshToken = response["refresh_token"].ToString();

                PlayerPrefs.SetString("IdToken", currentIdToken);
                PlayerPrefs.SetString("RefreshToken", currentRefreshToken);
                PlayerPrefs.Save();

                HandleSuccessfulLogin(currentUserId, "", "", isGuest);
            }
            else
            {
                Logout();
            }
        }
    }

    void LoginAsGuest()
    {
        string guestId = PlayerPrefs.GetString("GuestUserId", System.Guid.NewGuid().ToString());
        PlayerPrefs.SetString("GuestUserId", guestId);

        SaveAuthData(guestId, "", "", true);
        HandleSuccessfulLogin(guestId, "", "Guest", true);
    }

    void Logout()
    {
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.DeleteKey("IdToken");
        PlayerPrefs.DeleteKey("RefreshToken");
        PlayerPrefs.DeleteKey("WasGuest");
        PlayerPrefs.DeleteKey("GuestUserId");
        PlayerPrefs.Save();

        currentUserId = "";
        currentIdToken = "";
        currentRefreshToken = "";
        isGuest = false;
        isLoggedIn = false;

        OnUserSignedOut?.Invoke();
        ShowPanel("login");
        SetStatus("Signed out");
    }

    void ContinueGame()
    {
        Debug.Log("Continuing to game...");
        gameObject.SetActive(false);
    }

    #endregion

    #region Firestore Operations

    public IEnumerator SaveUserData(string userId, object data)
    {
        string url = $"{firestoreBaseUrl}/players/{userId}";

        // Convert object to Firestore format
        var firestoreDoc = ConvertToFirestoreDocument(data);
        string jsonData = JsonConvert.SerializeObject(firestoreDoc);

        using (UnityWebRequest www = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (!isGuest && !string.IsNullOrEmpty(currentIdToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {currentIdToken}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data saved to Firestore successfully");
            }
            else
            {
                Debug.LogError($"Failed to save to Firestore: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
            }
        }
    }

    public IEnumerator LoadUserData(string userId, System.Action<string> onComplete)
    {
        string url = $"{firestoreBaseUrl}/players/{userId}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            if (!isGuest && !string.IsNullOrEmpty(currentIdToken))
            {
                www.SetRequestHeader("Authorization", $"Bearer {currentIdToken}");
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var firestoreDoc = JsonConvert.DeserializeObject<FirestoreDocument2>(www.downloadHandler.text);
                var convertedData = ConvertFromFirestoreDocument(firestoreDoc);
                onComplete?.Invoke(JsonConvert.SerializeObject(convertedData));
            }
            else if (www.responseCode == 404)
            {
                // Document doesn't exist yet - this is normal for new users
                onComplete?.Invoke(null);
            }
            else
            {
                Debug.LogError($"Failed to load from Firestore: {www.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    IEnumerator SaveUserProfile(string userId, string username, string email)
    {
        var userProfile = new Dictionary<string, object>
        {
            ["username"] = username,
            ["email"] = email,
            ["createdAt"] = DateTime.Now.ToBinary(),
            ["lastLogin"] = DateTime.Now.ToBinary()
        };

        yield return StartCoroutine(SaveUserData(userId, userProfile));
    }

    // Convert regular C# object to Firestore document format
    private FirestoreDocument2 ConvertToFirestoreDocument(object data)
    {
        var doc = new FirestoreDocument2
        {
            fields = new Dictionary<string, FirestoreField>()
        };

        string jsonData = JsonConvert.SerializeObject(data);
        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);

        foreach (var kvp in dataDict)
        {
            doc.fields[kvp.Key] = ConvertValueToFirestoreField(kvp.Value);
        }

        return doc;
    }

    private FirestoreField ConvertValueToFirestoreField(object value)
    {
        var field = new FirestoreField();

        if (value == null)
        {
            field.stringValue = "";
            return field;
        }

        switch (value.GetType().Name)
        {
            case "String":
                field.stringValue = value.ToString();
                break;
            case "Int32":
            case "Int64":
                field.integerValue = value.ToString();
                break;
            case "Single":
            case "Double":
                field.doubleValue = value.ToString();
                break;
            case "Boolean":
                field.booleanValue = (bool)value;
                break;
            default:
                // For complex objects, serialize to JSON string
                field.stringValue = JsonConvert.SerializeObject(value);
                break;
        }

        return field;
    }

    // Convert Firestore document back to regular C# object
    private Dictionary<string, object> ConvertFromFirestoreDocument(FirestoreDocument2 doc)
    {
        var result = new Dictionary<string, object>();

        if (doc?.fields == null) return result;

        foreach (var kvp in doc.fields)
        {
            var field = kvp.Value;

            if (field.stringValue != null)
            {
                string strValue = field.stringValue.ToString();
                // Try to deserialize JSON strings back to objects
                if (strValue.StartsWith("{") || strValue.StartsWith("["))
                {
                    try
                    {
                        result[kvp.Key] = JsonConvert.DeserializeObject(strValue);
                    }
                    catch
                    {
                        result[kvp.Key] = strValue;
                    }
                }
                else
                {
                    result[kvp.Key] = strValue;
                }
            }
            else if (field.integerValue != null)
            {
                result[kvp.Key] = Convert.ToInt32(field.integerValue);
            }
            else if (field.doubleValue != null)
            {
                result[kvp.Key] = Convert.ToDouble(field.doubleValue);
            }
            else if (field.booleanValue != null)
            {
                result[kvp.Key] = (bool)field.booleanValue;
            }
        }

        return result;
    }

    #endregion

    #region Validation & Helper Methods

    bool ValidateEmailLogin(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
        {
            SetStatus("Please enter an email address");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            SetStatus("Please enter a password");
            return false;
        }

        if (!IsValidEmail(email))
        {
            SetStatus("Please enter a valid email address");
            return false;
        }

        return true;
    }

    bool ValidateRegistration(string email, string password, string confirmPassword, string username)
    {
        if (!ValidateEmailLogin(email, password)) return false;

        if (string.IsNullOrEmpty(username))
        {
            SetStatus("Please enter a username");
            return false;
        }

        if (username.Length < 3)
        {
            SetStatus("Username must be at least 3 characters");
            return false;
        }

        if (password.Length < 6)
        {
            SetStatus("Password must be at least 6 characters");
            return false;
        }

        if (password != confirmPassword)
        {
            SetStatus("Passwords do not match");
            return false;
        }

        return true;
    }

    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    void SaveAuthData(string userId, string idToken, string refreshToken, bool guest)
    {
        currentUserId = userId;
        currentIdToken = idToken;
        currentRefreshToken = refreshToken;
        isGuest = guest;
        isLoggedIn = true;

        PlayerPrefs.SetString("UserId", userId);
        PlayerPrefs.SetString("IdToken", idToken);
        PlayerPrefs.SetString("RefreshToken", refreshToken);
        PlayerPrefs.SetInt("WasGuest", guest ? 1 : 0);
        PlayerPrefs.Save();
    }

    void HandleSuccessfulLogin(string userId, string email, string displayName, bool guest)
    {
        currentUserId = userId;
        isGuest = guest;
        isLoggedIn = true;

        UpdateProfileUI(email, displayName, guest);

        if (guest)
        {
            ShowPanel("guest");
            SetStatus("Playing as guest");
        }
        else
        {
            ShowPanel("profile");
            SetStatus($"Welcome, {displayName}!");
        }

        OnUserSignedIn?.Invoke(userId);
        OnAuthStateChanged?.Invoke(true);
    }

    void HandleAuthError(string errorResponse)
    {
        try
        {
            var errorData = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorResponse);
            if (errorData.ContainsKey("error"))
            {
                var error = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorData["error"].ToString());
                string message = error["message"].ToString();

                switch (message)
                {
                    case "EMAIL_NOT_FOUND":
                        SetStatus("No account found with this email");
                        break;
                    case "INVALID_PASSWORD":
                        SetStatus("Incorrect password");
                        break;
                    case "EMAIL_EXISTS":
                        SetStatus("Email is already registered");
                        break;
                    case "WEAK_PASSWORD":
                        SetStatus("Password is too weak");
                        break;
                    default:
                        SetStatus($"Error: {message}");
                        break;
                }
            }
        }
        catch
        {
            SetStatus("Authentication failed");
        }
    }

    void UpdateProfileUI(string email, string displayName, bool guest)
    {
        if (profileEmailText != null)
            profileEmailText.text = guest ? "Guest User" : email;

        if (profileUsernameText != null)
            profileUsernameText.text = displayName;

        if (accountTypeText != null)
            accountTypeText.text = guest ? "Guest Account" : "Registered Account";
    }

    void ShowPanel(string panelName)
    {
        if (loginPanel != null) loginPanel.SetActive(panelName == "login");
        if (registerPanel != null) registerPanel.SetActive(panelName == "register");
        if (profilePanel != null) profilePanel.SetActive(panelName == "profile");
        if (guestPanel != null) guestPanel.SetActive(panelName == "guest");
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"Auth Status: {message}");
    }

    void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }

    #endregion

    #region Public Getters

    public string GetCurrentUserId() => currentUserId;
    public bool IsLoggedIn() => isLoggedIn;
    public bool IsGuest() => isGuest;
    public bool IsOnline() => !isGuest && !string.IsNullOrEmpty(currentIdToken);
    public string GetIdToken() => currentIdToken;

    #endregion
}