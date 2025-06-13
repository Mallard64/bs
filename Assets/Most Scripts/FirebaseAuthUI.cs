using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Newtonsoft.Json;
using System.Text;

public class FirebaseAuthUI : MonoBehaviour
{
    [Header("Firebase Configuration")]
    [SerializeField] private string firebaseApiKey = "AIzaSyCXU2WmL5CfB5JBTR3_FOWJPFvYMAl-kkU";
    [SerializeField] private string firebaseProjectId = "smashbrawl-4fca6";

    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject userPanel;
    [SerializeField] private InputField emailInput;
    [SerializeField] private InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Text statusText;
    [SerializeField] private Text userEmailText;

    // Current user data
    private string currentUserId = "";
    private string currentEmail = "";
    private string currentIdToken = "";
    private string currentRefreshToken = "";
    private bool isLoggedIn = false;

    // Firebase REST API URLs
    private string authSignInUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={firebaseApiKey}";
    private string authSignUpUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={firebaseApiKey}";
    private string authRefreshUrl => $"https://securetoken.googleapis.com/v1/token?key={firebaseApiKey}";

    // Events
    public event System.Action<string> OnUserSignedIn;
    public event System.Action OnUserSignedOut;
    public event System.Action<bool> OnAuthStateChanged;

    void Start()
    {
        // Auto-generate UI if references are missing
        if (loginPanel == null || userPanel == null)
        {
            GenerateUI();
        }

        SetupUI();
        CheckSavedLogin();
    }

    private void SetupUI()
    {
        // Connect button events
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutButtonClicked);

        UpdateStatus("Ready to authenticate", Color.white);
        ShowLoginPanel();
    }

    private void CheckSavedLogin()
    {
        // Check for saved authentication data
        string savedUserId = PlayerPrefs.GetString("Firebase_UserId", "");
        string savedEmail = PlayerPrefs.GetString("Firebase_Email", "");
        string savedToken = PlayerPrefs.GetString("Firebase_IdToken", "");
        string savedRefreshToken = PlayerPrefs.GetString("Firebase_RefreshToken", "");

        if (!string.IsNullOrEmpty(savedUserId) && !string.IsNullOrEmpty(savedRefreshToken))
        {
            currentUserId = savedUserId;
            currentEmail = savedEmail;
            currentIdToken = savedToken;
            currentRefreshToken = savedRefreshToken;

            // Try to refresh the token
            StartCoroutine(RefreshTokenCoroutine());
        }
    }

    public void OnLoginButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (!ValidateInput(email, password)) return;

        SetButtonsInteractable(false);
        StartCoroutine(LoginCoroutine(email, password));
    }

    public void OnRegisterButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (!ValidateInput(email, password)) return;

        if (password.Length < 6)
        {
            UpdateStatus("Password must be at least 6 characters", Color.red);
            return;
        }

        SetButtonsInteractable(false);
        StartCoroutine(RegisterCoroutine(email, password));
    }

    public void OnLogoutButtonClicked()
    {
        Logout();
    }

    private bool ValidateInput(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password", Color.red);
            return false;
        }

        if (!IsValidEmail(email))
        {
            UpdateStatus("Please enter a valid email address", Color.red);
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
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

    private void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (registerButton != null) registerButton.interactable = interactable;
    }

    private IEnumerator LoginCoroutine(string email, string password)
    {
        UpdateStatus("Signing in...", Color.yellow);

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

            SetButtonsInteractable(true);

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<FirebaseAuthResponse>(www.downloadHandler.text);
                HandleSuccessfulAuth(response, "Signed in successfully!");
            }
            else
            {
                HandleAuthError(www.downloadHandler.text);
            }
        }
    }

    private IEnumerator RegisterCoroutine(string email, string password)
    {
        UpdateStatus("Creating account...", Color.yellow);

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

            SetButtonsInteractable(true);

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<FirebaseAuthResponse>(www.downloadHandler.text);
                HandleSuccessfulAuth(response, "Account created successfully!");
            }
            else
            {
                HandleAuthError(www.downloadHandler.text);
            }
        }
    }

    private IEnumerator RefreshTokenCoroutine()
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

                SaveAuthData();
                HandleSuccessfulLogin();
            }
            else
            {
                // Token refresh failed, logout
                Logout();
            }
        }
    }

    private void Logout()
    {
        // Clear saved data
        PlayerPrefs.DeleteKey("Firebase_UserId");
        PlayerPrefs.DeleteKey("Firebase_Email");
        PlayerPrefs.DeleteKey("Firebase_IdToken");
        PlayerPrefs.DeleteKey("Firebase_RefreshToken");
        PlayerPrefs.Save();

        // Clear current data
        currentUserId = "";
        currentEmail = "";
        currentIdToken = "";
        currentRefreshToken = "";
        isLoggedIn = false;

        // Update UI
        ShowLoginPanel();
        UpdateStatus("Signed out successfully", Color.green);

        // Trigger events
        OnUserSignedOut?.Invoke();
        OnAuthStateChanged?.Invoke(false);
    }

    private void HandleSuccessfulAuth(FirebaseAuthResponse response, string successMessage)
    {
        currentUserId = response.localId;
        currentEmail = response.email;
        currentIdToken = response.idToken;
        currentRefreshToken = response.refreshToken;
        isLoggedIn = true;

        SaveAuthData();
        HandleSuccessfulLogin();
        UpdateStatus(successMessage, Color.green);
    }

    private void HandleSuccessfulLogin()
    {
        ShowUserPanel();
        OnUserSignedIn?.Invoke(currentUserId);
        OnAuthStateChanged?.Invoke(true);
    }

    private void SaveAuthData()
    {
        PlayerPrefs.SetString("Firebase_UserId", currentUserId);
        PlayerPrefs.SetString("Firebase_Email", currentEmail);
        PlayerPrefs.SetString("Firebase_IdToken", currentIdToken);
        PlayerPrefs.SetString("Firebase_RefreshToken", currentRefreshToken);
        PlayerPrefs.Save();
    }

    private void HandleAuthError(string errorResponse)
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
                        UpdateStatus("No account found with this email", Color.red);
                        break;
                    case "INVALID_PASSWORD":
                        UpdateStatus("Incorrect password", Color.red);
                        break;
                    case "EMAIL_EXISTS":
                        UpdateStatus("Email is already registered", Color.red);
                        break;
                    case "WEAK_PASSWORD":
                        UpdateStatus("Password is too weak", Color.red);
                        break;
                    case "INVALID_EMAIL":
                        UpdateStatus("Invalid email address", Color.red);
                        break;
                    case "TOO_MANY_ATTEMPTS_TRY_LATER":
                        UpdateStatus("Too many attempts. Try again later", Color.red);
                        break;
                    default:
                        UpdateStatus($"Authentication failed: {message}", Color.red);
                        break;
                }
            }
            else
            {
                UpdateStatus("Authentication failed", Color.red);
            }
        }
        catch
        {
            UpdateStatus("Authentication failed", Color.red);
        }
    }

    private void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (userPanel != null) userPanel.SetActive(false);

        // Clear input fields
        if (emailInput != null) emailInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
    }

    private void ShowUserPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (userPanel != null) userPanel.SetActive(true);

        if (userEmailText != null && !string.IsNullOrEmpty(currentEmail))
        {
            userEmailText.text = $"Logged in as: {currentEmail}";
        }
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }

        Debug.Log($"[FirebaseAuth] {message}");
    }

    void OnDestroy()
    {
        // Clean up any remaining coroutines
        StopAllCoroutines();
    }

    // Public API methods
    public string GetUserToken()
    {
        return currentIdToken;
    }

    public bool IsUserLoggedIn()
    {
        return isLoggedIn && !string.IsNullOrEmpty(currentUserId);
    }

    public string GetUserEmail()
    {
        return currentEmail;
    }

    public string GetUserId()
    {
        return currentUserId;
    }

    public void SendPasswordResetEmail()
    {
        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            UpdateStatus("Please enter your email address", Color.red);
            return;
        }

        StartCoroutine(SendPasswordResetCoroutine(email));
    }

    private IEnumerator SendPasswordResetCoroutine(string email)
    {
        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseApiKey}";
        
        var requestData = new Dictionary<string, object>
        {
            ["requestType"] = "PASSWORD_RESET",
            ["email"] = email
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus("Password reset email sent", Color.green);
            }
            else
            {
                UpdateStatus("Failed to send reset email", Color.red);
            }
        }
    }

    [ContextMenu("Generate UI")]
    private void GenerateUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Find or create EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        // Create login panel
        loginPanel = CreatePanel("Login Panel", canvas.transform, new Vector2(400, 350));

        // Title
        CreateText("Title", loginPanel.transform, "Firebase Login", 24, TextAnchor.MiddleCenter, true);

        // Email field
        CreateLabel("Email Label", loginPanel.transform, "Email:");
        emailInput = CreateInputField("Email Input", loginPanel.transform, "Enter email...");

        // Password field  
        CreateLabel("Password Label", loginPanel.transform, "Password:");
        passwordInput = CreateInputField("Password Input", loginPanel.transform, "Enter password...", true);

        // Buttons
        GameObject buttonContainer = CreateHorizontalGroup("Button Container", loginPanel.transform);
        loginButton = CreateButton("Login Button", buttonContainer.transform, "Login", new Color(0.2f, 0.6f, 0.2f));
        registerButton = CreateButton("Register Button", buttonContainer.transform, "Register", new Color(0.2f, 0.4f, 0.6f));

        // Status text
        statusText = CreateText("Status Text", loginPanel.transform, "", 14, TextAnchor.MiddleCenter);
        statusText.color = Color.yellow;

        // Create user panel
        userPanel = CreatePanel("User Panel", canvas.transform, new Vector2(400, 250));
        userPanel.SetActive(false);

        // User info
        CreateText("Welcome Title", userPanel.transform, "Welcome!", 24, TextAnchor.MiddleCenter, true);
        userEmailText = CreateText("User Email", userPanel.transform, "user@example.com", 16, TextAnchor.MiddleCenter);

        // Logout button
        logoutButton = CreateButton("Logout Button", userPanel.transform, "Logout", new Color(0.6f, 0.2f, 0.2f));

        // Note: Buttons are already connected in SetupUI()

        Debug.Log("Firebase Auth UI generated successfully!");
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 30);
        layout.spacing = 15;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        return panel;
    }

    private GameObject CreateHorizontalGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);

        RectTransform rect = group.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        LayoutElement layoutElement = group.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 40;

        return group;
    }

    private void CreateLabel(string name, Transform parent, string text)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 20);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);

        Text label = go.AddComponent<Text>();
        label.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = 14;
        label.color = new Color(0.8f, 0.8f, 0.8f);
        label.alignment = TextAnchor.MiddleLeft;

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 20;
        layoutElement.minHeight = 20;
    }

    private InputField CreateInputField(string name, Transform parent, string placeholder, bool isPassword = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        InputField input = go.AddComponent<InputField>();
        if (isPassword) input.contentType = InputField.ContentType.Password;

        // Add layout element
        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 40;

        // Text component
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        Text text = textGO.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        RectTransform placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);

        Text placeholderText = placeholderGO.AddComponent<Text>();
        placeholderText.font = text.font;
        placeholderText.fontSize = 16;
        placeholderText.fontStyle = FontStyle.Italic;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.text = placeholder;
        placeholderText.alignment = TextAnchor.MiddleLeft;

        input.textComponent = text;
        input.placeholder = placeholderText;

        return input;
    }

    private Button CreateButton(string name, Transform parent, string text, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image bg = go.AddComponent<Image>();
        bg.color = color;

        Button button = go.AddComponent<Button>();

        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textGO.AddComponent<Text>();
        buttonText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        if (buttonText.font == null)
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.text = text;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = color * 0.5f;
        button.colors = colors;

        return button;
    }

    private Text CreateText(string name, Transform parent, string text, int fontSize = 16,
                           TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 10);

        Text textComponent = go.AddComponent<Text>();
        textComponent.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        if (textComponent.font == null)
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        textComponent.text = text;
        textComponent.color = Color.white;
        textComponent.alignment = alignment;

        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 10;

        return textComponent;
    }

    [ContextMenu("Debug UI")]
    private void DebugUI()
    {
        Debug.Log($"Login Panel: {loginPanel}");
        Debug.Log($"Login Panel Active: {loginPanel?.activeSelf}");
        Debug.Log($"Login Panel Children: {loginPanel?.transform.childCount}");

        if (loginPanel != null)
        {
            for (int i = 0; i < loginPanel.transform.childCount; i++)
            {
                Transform child = loginPanel.transform.GetChild(i);
                Debug.Log($"Child {i}: {child.name} - Active: {child.gameObject.activeSelf}");
            }
        }

        // Force canvas update
        Canvas.ForceUpdateCanvases();

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(loginPanel.GetComponent<RectTransform>());
    }
}