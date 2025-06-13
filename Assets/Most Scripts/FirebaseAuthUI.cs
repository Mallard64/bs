using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class FirebaseAuthManager : MonoBehaviour
{
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

    private FirebaseAuth auth;
    private FirebaseUser user;
    private bool firebaseReady = false;

    void Start()
    {
        // Auto-generate UI if references are missing
        if (loginPanel == null || userPanel == null)
        {
            GenerateUI();
        }

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                auth.StateChanged += AuthStateChanged;
                AuthStateChanged(this, null);
                firebaseReady = true;
                UpdateStatus("Firebase initialized successfully", Color.green);
            }
            else
            {
                UpdateStatus($"Failed to initialize Firebase: {task.Result}", Color.red);
            }
        });
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out: " + user.Email);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in: " + user.Email);
                ShowUserPanel();
            }
            else
            {
                ShowLoginPanel();
            }
        }
    }

    public void OnLoginButtonClicked()
    {
        if (!firebaseReady) return;

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password", Color.red);
            return;
        }

        loginButton.interactable = false;
        registerButton.interactable = false;

        SignInWithEmailPassword(email, password);
    }

    public void OnRegisterButtonClicked()
    {
        if (!firebaseReady) return;

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password", Color.red);
            return;
        }

        if (password.Length < 6)
        {
            UpdateStatus("Password must be at least 6 characters", Color.red);
            return;
        }

        loginButton.interactable = false;
        registerButton.interactable = false;

        CreateUserWithEmailPassword(email, password);
    }

    private void SignInWithEmailPassword(string email, string password)
    {
        UpdateStatus("Signing in...", Color.yellow);

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            loginButton.interactable = true;
            registerButton.interactable = true;

            if (task.IsCanceled)
            {
                UpdateStatus("Sign in was canceled", Color.red);
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthError(task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result.User;
            UpdateStatus($"Signed in successfully as {newUser.Email}", Color.green);
        });
    }

    private void CreateUserWithEmailPassword(string email, string password)
    {
        UpdateStatus("Creating account...", Color.yellow);

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            loginButton.interactable = true;
            registerButton.interactable = true;

            if (task.IsCanceled)
            {
                UpdateStatus("Registration was canceled", Color.red);
                return;
            }
            if (task.IsFaulted)
            {
                HandleAuthError(task.Exception);
                return;
            }

            FirebaseUser newUser = task.Result.User;
            UpdateStatus($"Account created successfully for {newUser.Email}", Color.green);
        });
    }

    public void OnLogoutButtonClicked()
    {
        if (auth != null && user != null)
        {
            auth.SignOut();
            UpdateStatus("Signed out successfully", Color.green);
        }
    }

    private void HandleAuthError(AggregateException exception)
    {
        FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

        if (firebaseEx != null)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            string message = "Authentication failed: ";

            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    message += "Invalid email address";
                    break;
                case AuthError.WrongPassword:
                    message += "Wrong password";
                    break;
                case AuthError.UserNotFound:
                    message += "User not found";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message += "Email already in use";
                    break;
                case AuthError.WeakPassword:
                    message += "Password is too weak";
                    break;
                case AuthError.NetworkRequestFailed:
                    message += "Network error. Check your connection";
                    break;
                default:
                    message += errorCode.ToString();
                    break;
            }

            UpdateStatus(message, Color.red);
        }
        else
        {
            UpdateStatus($"Authentication failed: {exception.Message}", Color.red);
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

        if (userEmailText != null && user != null)
        {
            userEmailText.text = $"Logged in as: {user.Email}";
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
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }

    // Additional authentication methods

    public async Task<string> GetUserTokenAsync()
    {
        if (user == null) return null;

        try
        {
            var tokenResult = await user.TokenAsync(false);
            return tokenResult;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get user token: {e.Message}");
            return null;
        }
    }

    public void SendPasswordResetEmail()
    {
        string email = emailInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            UpdateStatus("Please enter your email address", Color.red);
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                UpdateStatus("Failed to send reset email", Color.red);
                return;
            }

            UpdateStatus("Password reset email sent", Color.green);
        });
    }

    public bool IsUserLoggedIn()
    {
        return user != null;
    }

    public string GetUserEmail()
    {
        return user?.Email;
    }

    public string GetUserId()
    {
        return user?.UserId;
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

        // Connect buttons
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        logoutButton.onClick.AddListener(OnLogoutButtonClicked);

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