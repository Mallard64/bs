using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

// Make sure all data structures are properly defined
[System.Serializable]
public class RunData
{
    public int runNumber;
    public float completionTime;
    public int score;
    public int weaponsFound;
    public string timestamp; // Changed to string for Firebase compatibility

    public RunData() { }

    public RunData(int run, float time, int playerScore, int weapons)
    {
        runNumber = run;
        completionTime = time;
        score = playerScore;
        weaponsFound = weapons;
        timestamp = DateTime.Now.ToBinary().ToString();
    }

    public DateTime GetDateTime()
    {
        if (long.TryParse(timestamp, out long binary))
        {
            return DateTime.FromBinary(binary);
        }
        return DateTime.Now;
    }
}

[System.Serializable]
public class PlayerStats
{
    public int totalScore = 0;
    public int totalWeaponsFound = 0;
    public float totalPlayTime = 0f;
    public int bestRunScore = 0;
    public float fastestRunTime = float.MaxValue;
    public int totalGambles = 0;
    public int totalUnlocks = 0;
}

[System.Serializable]
public class PlayerData
{
    public int currentRun = 0;
    public int currentBattery = 1000;
    public List<string> unlockedWeapons = new List<string>();
    public List<string> unlockedSkins = new List<string>();
    public List<RunData> completedRuns = new List<RunData>();
    public string lastPlayTime = DateTime.Now.ToBinary().ToString();
    public int consecutiveFailures = 0;
    public PlayerStats stats = new PlayerStats();

    public PlayerData()
    {
        // Initialize with default unlocks
        if (unlockedWeapons.Count == 0)
        {
            unlockedWeapons.Add("weapon_basic_rifle");
        }
        if (unlockedSkins.Count == 0)
        {
            unlockedSkins.Add("skin_default");
        }
    }

    public DateTime GetLastPlayTime()
    {
        if (long.TryParse(lastPlayTime, out long binary))
        {
            return DateTime.FromBinary(binary);
        }
        return DateTime.Now;
    }

    public void UpdateLastPlayTime()
    {
        lastPlayTime = DateTime.Now.ToBinary().ToString();
    }
}

[System.Serializable]
public class GamblingReward
{
    public enum RewardType { Weapon, Currency, Upgrade, Skin }

    public RewardType type;
    public string itemId;
    public int amount;
    public float chance;
    public int requiredRuns = 0;

    public GamblingReward() { }

    public GamblingReward(RewardType rewardType, string id, int amt, float chancePercent, int minRuns = 0)
    {
        type = rewardType;
        itemId = id;
        amount = amt;
        chance = chancePercent;
        requiredRuns = minRuns;
    }
}

// Complete Firebase Authentication Manager
public class FirebaseAuthManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject profilePanel;
    public GameObject guestPanel;
    public GameObject linkAccountPanel;

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
    public Button linkAccountButton;
    public Button continueGameButton;

    [Header("Guest UI")]
    public TextMeshProUGUI guestStatusText;
    public Button createAccountFromGuestButton;
    public Button continueAsGuestButton;

    [Header("Link Account UI")]
    public TMP_InputField linkEmail;
    public TMP_InputField linkPassword;
    public Button linkSubmitButton;
    public Button linkCancelButton;

    [Header("Status UI")]
    public TextMeshProUGUI statusText;
    public GameObject loadingIndicator;
    public GameObject connectionStatus;
    public TextMeshProUGUI connectionText;

    private FirebaseAuth auth;
    private DatabaseReference databaseRef;
    private FirebaseUser currentUser;
    private bool firebaseInitialized = false;

    public event System.Action<FirebaseUser> OnUserSignedIn;
    public event System.Action OnUserSignedOut;
    public event System.Action<bool> OnAuthStateChanged;
    public event System.Action<bool> OnFirebaseInitialized;

    void Start()
    {
        InitializeFirebase();
        SetupUI();
    }

    async void InitializeFirebase()
    {
        try
        {
            SetStatus("Initializing Firebase...");
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseInitialized = true;

                // Listen for auth state changes
                auth.StateChanged += AuthStateChanged;

                SetStatus("Firebase initialized successfully");
                UpdateConnectionStatus(true);
                OnFirebaseInitialized?.Invoke(true);

                // Check if user was previously signed in
                CheckPreviousLogin();

                Debug.Log("Firebase Auth initialized successfully");
            }
            else
            {
                Debug.LogError($"Firebase dependency error: {dependencyStatus}");
                HandleFirebaseError($"Firebase setup failed: {dependencyStatus}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Firebase initialization failed: {e.Message}");
            HandleFirebaseError($"Firebase initialization failed: {e.Message}");
        }
    }

    void HandleFirebaseError(string error)
    {
        firebaseInitialized = false;
        SetStatus($"Offline mode: {error}");
        UpdateConnectionStatus(false);
        OnFirebaseInitialized?.Invoke(false);
        ShowOfflineMode();
    }

    void SetupUI()
    {
        // Login panel
        if (loginButton != null)
            loginButton.onClick.AddListener(() => LoginWithEmail(loginEmail.text, loginPassword.text));
        if (switchToRegisterButton != null)
            switchToRegisterButton.onClick.AddListener(() => ShowPanel("register"));
        if (guestLoginButton != null)
            guestLoginButton.onClick.AddListener(LoginAsGuest);

        // Register panel
        if (registerButton != null)
            registerButton.onClick.AddListener(() => RegisterWithEmail(
                registerEmail.text, registerPassword.text, registerConfirmPassword.text, registerUsername.text));
        if (switchToLoginButton != null)
            switchToLoginButton.onClick.AddListener(() => ShowPanel("login"));

        // Profile panel
        if (logoutButton != null)
            logoutButton.onClick.AddListener(Logout);
        if (linkAccountButton != null)
            linkAccountButton.onClick.AddListener(() => ShowPanel("link"));
        if (continueGameButton != null)
            continueGameButton.onClick.AddListener(ContinueGame);

        // Guest panel
        if (createAccountFromGuestButton != null)
            createAccountFromGuestButton.onClick.AddListener(() => ShowPanel("register"));
        if (continueAsGuestButton != null)
            continueAsGuestButton.onClick.AddListener(ContinueGame);

        // Link account panel
        if (linkSubmitButton != null)
            linkSubmitButton.onClick.AddListener(() => LinkGuestAccount(linkEmail.text, linkPassword.text));
        if (linkCancelButton != null)
            linkCancelButton.onClick.AddListener(() => ShowPanel("profile"));

        // Start with login panel
        ShowPanel("login");
    }

    void CheckPreviousLogin()
    {
        string savedUserId = PlayerPrefs.GetString("UserId", "");
        bool wasGuest = PlayerPrefs.GetInt("WasGuest", 0) == 1;

        if (!string.IsNullOrEmpty(savedUserId))
        {
            if (wasGuest)
            {
                LoginAsGuest();
            }
            else if (auth?.CurrentUser != null)
            {
                HandleSuccessfulLogin(auth.CurrentUser);
            }
        }
    }

    #region Authentication Methods

    async void LoginWithEmail(string email, string password)
    {
        if (!ValidateEmailLogin(email, password)) return;

        ShowLoading(true);
        SetStatus("Signing in...");

        try
        {
            var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            HandleSuccessfulLogin(result.User);
        }
        catch (System.Exception e)
        {
            HandleAuthError(e);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    async void RegisterWithEmail(string email, string password, string confirmPassword, string username)
    {
        if (!ValidateRegistration(email, password, confirmPassword, username)) return;

        ShowLoading(true);
        SetStatus("Creating account...");

        try
        {
            var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            // Update user profile with username
            var profile = new UserProfile { DisplayName = username };
            await result.User.UpdateUserProfileAsync(profile);

            // Save username to database
            await SaveUserProfile(result.User.UserId, username, email);

            HandleSuccessfulLogin(result.User);
        }
        catch (System.Exception e)
        {
            HandleAuthError(e);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    async void LoginAsGuest()
    {
        if (!firebaseInitialized)
        {
            // Pure offline mode
            string guestId = PlayerPrefs.GetString("GuestUserId", System.Guid.NewGuid().ToString());
            PlayerPrefs.SetString("GuestUserId", guestId);
            PlayerPrefs.SetString("UserId", guestId);
            PlayerPrefs.SetInt("WasGuest", 1);
            PlayerPrefs.Save();

            HandleOfflineLogin(guestId);
            return;
        }

        ShowLoading(true);
        SetStatus("Signing in as guest...");

        try
        {
            var result = await auth.SignInAnonymouslyAsync();

            // Save guest status
            PlayerPrefs.SetString("UserId", result.User.UserId);
            PlayerPrefs.SetInt("WasGuest", 1);
            PlayerPrefs.Save();

            HandleSuccessfulLogin(result.User);
        }
        catch (System.Exception e)
        {
            // Fallback to offline guest mode
            string guestId = PlayerPrefs.GetString("GuestUserId", System.Guid.NewGuid().ToString());
            PlayerPrefs.SetString("GuestUserId", guestId);
            PlayerPrefs.SetString("UserId", guestId);
            PlayerPrefs.SetInt("WasGuest", 1);
            PlayerPrefs.Save();

            HandleOfflineLogin(guestId);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    async void LinkGuestAccount(string email, string password)
    {
        if (!firebaseInitialized || currentUser == null || !currentUser.IsAnonymous) return;

        ShowLoading(true);
        SetStatus("Linking account...");

        try
        {
            var credential = EmailAuthProvider.GetCredential(email, password);
            var result = await currentUser.LinkWithCredentialAsync(credential);

            // Update guest status
            PlayerPrefs.SetInt("WasGuest", 0);
            PlayerPrefs.Save();

            HandleSuccessfulLogin(result.User);
            SetStatus("Account linked successfully!");
        }
        catch (System.Exception e)
        {
            HandleAuthError(e);
        }
        finally
        {
            ShowLoading(false);
        }
    }

    async void Logout()
    {
        if (firebaseInitialized && auth.CurrentUser != null)
        {
            auth.SignOut();
        }

        // Clear local data
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.DeleteKey("WasGuest");
        PlayerPrefs.DeleteKey("GuestUserId");
        PlayerPrefs.Save();

        currentUser = null;
        OnUserSignedOut?.Invoke();
        ShowPanel("login");
        SetStatus("Signed out");
    }

    void ContinueGame()
    {
        // This should be handled by your main game manager
        Debug.Log("Continuing to game...");
        gameObject.SetActive(false); // Hide auth UI
    }

    #endregion

    #region Validation

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

    #endregion

    #region Event Handlers

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != null;
            currentUser = auth.CurrentUser;

            OnAuthStateChanged?.Invoke(signedIn);

            if (signedIn)
            {
                HandleSuccessfulLogin(currentUser);
            }
        }
    }

    void HandleSuccessfulLogin(FirebaseUser user)
    {
        currentUser = user;

        // Save user ID locally
        PlayerPrefs.SetString("UserId", user.UserId);
        PlayerPrefs.Save();

        // Update UI
        UpdateProfileUI(user);

        if (user.IsAnonymous)
        {
            ShowPanel("guest");
            SetStatus("Playing as guest");
        }
        else
        {
            ShowPanel("profile");
            SetStatus($"Welcome, {GetDisplayName(user)}!");
        }

        OnUserSignedIn?.Invoke(user);
    }

    void HandleOfflineLogin(string guestId)
    {
        SetStatus("Playing offline as guest");
        ShowPanel("guest");

        if (guestStatusText != null)
        {
            guestStatusText.text = "Offline Mode - Progress saved locally only";
        }

        // Create a mock user for offline mode
        OnUserSignedIn?.Invoke(null);
    }

    void HandleAuthError(System.Exception e)
    {
        string errorMessage = "Authentication failed";

        if (e is FirebaseException firebaseEx)
        {
            switch (firebaseEx.ErrorCode)
            {
                case (int)AuthError.WrongPassword:
                    errorMessage = "Incorrect password";
                    break;
                case (int)AuthError.InvalidEmail:
                    errorMessage = "Invalid email address";
                    break;
                case (int)AuthError.UserNotFound:
                    errorMessage = "No account found with this email";
                    break;
                case (int)AuthError.EmailAlreadyInUse:
                    errorMessage = "Email is already registered";
                    break;
                case (int)AuthError.WeakPassword:
                    errorMessage = "Password is too weak";
                    break;
                default:
                    errorMessage = $"Error: {e.Message}";
                    break;
            }
        }

        SetStatus(errorMessage);
        Debug.LogError($"Auth error: {e.Message}");
    }

    #endregion

    #region UI Management

    void ShowPanel(string panelName)
    {
        if (loginPanel != null) loginPanel.SetActive(panelName == "login");
        if (registerPanel != null) registerPanel.SetActive(panelName == "register");
        if (profilePanel != null) profilePanel.SetActive(panelName == "profile");
        if (guestPanel != null) guestPanel.SetActive(panelName == "guest");
        if (linkAccountPanel != null) linkAccountPanel.SetActive(panelName == "link");
    }

    void UpdateProfileUI(FirebaseUser user)
    {
        if (profileEmailText != null)
        {
            profileEmailText.text = user.Email ?? "Guest User";
        }

        if (profileUsernameText != null)
        {
            profileUsernameText.text = GetDisplayName(user);
        }

        if (accountTypeText != null)
        {
            accountTypeText.text = user.IsAnonymous ? "Guest Account" : "Registered Account";
        }

        // Show link account button only for anonymous users
        if (linkAccountButton != null)
        {
            linkAccountButton.gameObject.SetActive(user.IsAnonymous);
        }
    }

    void ShowOfflineMode()
    {
        SetStatus("Playing in offline mode");
        ShowPanel("guest");
        UpdateConnectionStatus(false);
    }

    void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Auth Status: {message}");
    }

    void ShowLoading(bool show)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    void UpdateConnectionStatus(bool online)
    {
        if (connectionText != null)
        {
            connectionText.text = online ? "Online" : "Offline";
            connectionText.color = online ? Color.green : Color.red;
        }
    }

    #endregion

    #region Database Operations

    async Task SaveUserProfile(string userId, string username, string email)
    {
        if (!firebaseInitialized) return;

        try
        {
            var userProfile = new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["createdAt"] = DateTime.Now.ToBinary(),
                ["lastLogin"] = DateTime.Now.ToBinary()
            };

            await databaseRef.Child("userProfiles").Child(userId).UpdateChildrenAsync(userProfile);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save user profile: {e.Message}");
        }
    }

    #endregion

    #region Utility Methods

    string GetDisplayName(FirebaseUser user)
    {
        if (user == null) return "Guest";

        if (!string.IsNullOrEmpty(user.DisplayName))
            return user.DisplayName;

        if (!string.IsNullOrEmpty(user.Email))
            return user.Email.Split('@')[0];

        return user.IsAnonymous ? "Guest" : "Player";
    }

    public string GetCurrentUserId()
    {
        if (currentUser != null)
            return currentUser.UserId;

        return PlayerPrefs.GetString("UserId", "");
    }

    public bool IsLoggedIn()
    {
        return currentUser != null || !string.IsNullOrEmpty(PlayerPrefs.GetString("UserId", ""));
    }

    public bool IsGuest()
    {
        if (currentUser != null)
            return currentUser.IsAnonymous;
        return PlayerPrefs.GetInt("WasGuest", 0) == 1;
    }

    public bool IsOnline()
    {
        return firebaseInitialized && currentUser != null;
    }

    public FirebaseUser CurrentUser => currentUser;
    public bool FirebaseInitialized => firebaseInitialized;

    #endregion

    void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}

// Complete Progression System with proper Firebase integration
public class CompleteProgressionSystem : MonoBehaviour
{
    [Header("System References")]
    public FirebaseAuthManager authManager;

    [Header("Progression Settings")]
    public int targetRuns = 50;
    public int batteryPerRun = 150;
    public int batteryBonusThreshold = 5;
    public int batteryBonus = 300;

    [Header("Sync Settings")]
    public float autoSyncInterval = 30f;
    public bool syncOnlyWhenChanged = true;

    private PlayerData playerData = new PlayerData();
    private DatabaseReference databaseRef;
    private bool dataChanged = false;
    private float lastSyncTime = 0f;

    // Game state
    private float currentRunStartTime;
    private int currentRunScore = 0;
    private int currentRunWeapons = 0;

    public event System.Action<RunData> OnRunCompleted;
    public event System.Action<int> OnBatteryChanged;
    public event System.Action<string> OnItemUnlocked;
    public event System.Action<int, int> OnProgressUpdated;
    public event System.Action OnDataSynced;
    public event System.Action<PlayerData> OnDataLoaded;

    void Start()
    {
        if (authManager != null)
        {
            authManager.OnUserSignedIn += HandleUserSignedIn;
            authManager.OnUserSignedOut += HandleUserSignedOut;
            authManager.OnFirebaseInitialized += HandleFirebaseInitialized;
        }
        else
        {
            Debug.LogError("AuthManager reference missing!");
        }

        LoadLocalData();
    }

    void Update()
    {
        // Auto-sync if user is online and data changed
        if (authManager != null && authManager.IsOnline() && dataChanged &&
            Time.time - lastSyncTime > autoSyncInterval)
        {
            SyncToFirebase();
        }
    }

    void HandleFirebaseInitialized(bool initialized)
    {
        if (initialized)
        {
            databaseRef = Firebase.Database.FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("Database reference initialized");
        }
    }

    void HandleUserSignedIn(FirebaseUser user)
    {
        Debug.Log("User signed in, syncing data...");
        if (user != null && authManager.FirebaseInitialized)
        {
            LoadFromFirebase(user.UserId);
        }
        else
        {
            // Offline mode - use local data
            TriggerUIUpdates();
        }
    }

    void HandleUserSignedOut()
    {
        Debug.Log("User signed out");
        // Don't clear data immediately - let them continue playing
        TriggerUIUpdates();
    }

    #region Data Management

    void LoadLocalData()
    {
        string userId = authManager?.GetCurrentUserId() ?? "local";

        playerData.currentRun = PlayerPrefs.GetInt($"CurrentRun_{userId}", 0);
        playerData.currentBattery = PlayerPrefs.GetInt($"CurrentBattery_{userId}", 1000);
        playerData.consecutiveFailures = PlayerPrefs.GetInt($"ConsecutiveFailures_{userId}", 0);

        // Load unlocks
        string weaponData = PlayerPrefs.GetString($"UnlockedWeapons_{userId}", "");
        if (!string.IsNullOrEmpty(weaponData))
        {
            try
            {
                playerData.unlockedWeapons = JsonConvert.DeserializeObject<List<string>>(weaponData);
            }
            catch
            {
                playerData.unlockedWeapons = new List<string>(weaponData.Split(','));
            }
        }

        if (playerData.unlockedWeapons.Count == 0)
        {
            playerData.unlockedWeapons.Add("weapon_basic_rifle");
        }

        string skinData = PlayerPrefs.GetString($"UnlockedSkins_{userId}", "");
        if (!string.IsNullOrEmpty(skinData))
        {
            try
            {
                playerData.unlockedSkins = JsonConvert.DeserializeObject<List<string>>(skinData);
            }
            catch
            {
                playerData.unlockedSkins = new List<string>(skinData.Split(','));
            }
        }

        if (playerData.unlockedSkins.Count == 0)
        {
            playerData.unlockedSkins.Add("skin_default");
        }

        // Load stats
        string statsData = PlayerPrefs.GetString($"PlayerStats_{userId}", "");
        if (!string.IsNullOrEmpty(statsData))
        {
            try
            {
                playerData.stats = JsonConvert.DeserializeObject<PlayerStats>(statsData);
            }
            catch
            {
                playerData.stats = new PlayerStats();
            }
        }

        // Load recent runs
        string runsData = PlayerPrefs.GetString($"RecentRuns_{userId}", "");
        if (!string.IsNullOrEmpty(runsData))
        {
            try
            {
                playerData.completedRuns = JsonConvert.DeserializeObject<List<RunData>>(runsData);
            }
            catch
            {
                playerData.completedRuns = new List<RunData>();
            }
        }

        TriggerUIUpdates();
        OnDataLoaded?.Invoke(playerData);
        Debug.Log($"Loaded local data for user: {userId}");
    }

    void SaveLocalData()
    {
        string userId = authManager?.GetCurrentUserId() ?? "local";

        PlayerPrefs.SetInt($"CurrentRun_{userId}", playerData.currentRun);
        PlayerPrefs.SetInt($"CurrentBattery_{userId}", playerData.currentBattery);
        PlayerPrefs.SetInt($"ConsecutiveFailures_{userId}", playerData.consecutiveFailures);

        PlayerPrefs.SetString($"UnlockedWeapons_{userId}", JsonConvert.SerializeObject(playerData.unlockedWeapons));
        PlayerPrefs.SetString($"UnlockedSkins_{userId}", JsonConvert.SerializeObject(playerData.unlockedSkins));
        PlayerPrefs.SetString($"PlayerStats_{userId}", JsonConvert.SerializeObject(playerData.stats));
        PlayerPrefs.SetString($"RecentRuns_{userId}", JsonConvert.SerializeObject(playerData.completedRuns));

        PlayerPrefs.SetString($"LastSaveTime_{userId}", DateTime.Now.ToBinary().ToString());
        PlayerPrefs.Save();

        dataChanged = true;
    }

    async void LoadFromFirebase(string userId)
    {
        if (databaseRef == null) return;

        try
        {
            var snapshot = await databaseRef.Child("players").Child(userId).GetValueAsync();

            if (snapshot.Exists)
            {
                string jsonData = snapshot.GetRawJsonValue();
                var firebaseData = JsonConvert.DeserializeObject<PlayerData>(jsonData);

                // Compare with local data timestamp
                string localTimeStr = PlayerPrefs.GetString($"LastSaveTime_{userId}", "0");
                DateTime localTime = DateTime.FromBinary(Convert.ToInt64(localTimeStr));

                if (firebaseData.GetLastPlayTime() > localTime)
                {
                    // Firebase data is newer
                    playerData = firebaseData;
                    SaveLocalData();
                    Debug.Log("Loaded newer data from Firebase");
                }
                else
                {
                    // Local data is newer or same
                    await SyncToFirebase();
                    Debug.Log("Local data is newer, synced to Firebase");
                }
            }
            else
            {
                // No Firebase data, upload local
                await SyncToFirebase();
                Debug.Log("No Firebase data found, uploaded local data");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load from Firebase: {e.Message}");
        }

        TriggerUIUpdates();
        OnDataLoaded?.Invoke(playerData);
    }

    async Task SyncToFirebase()
    {
        if (databaseRef == null || authManager == null || !authManager.IsOnline()) return;

        try
        {
            string userId = authManager.GetCurrentUserId();
            playerData.UpdateLastPlayTime();

            string jsonData = JsonConvert.SerializeObject(playerData);
            await databaseRef.Child("players").Child(userId).SetRawJsonValueAsync(jsonData);

            lastSyncTime = Time.time;
            dataChanged = false;
            OnDataSynced?.Invoke();
            Debug.Log("Data synced to Firebase successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to sync to Firebase: {e.Message}");
        }
    }

    public async void ForceFirebaseSync()
    {
        if (authManager != null && authManager.IsOnline())
        {
            await SyncToFirebase();
        }
    }

    #endregion

    #region Game Logic

    public void StartNewRun()
    {
        playerData.currentRun++;
        currentRunStartTime = Time.time;
        currentRunScore = 0;
        currentRunWeapons = 0;

        SaveLocalData();
        Debug.Log($"Starting Run #{playerData.currentRun}");
    }

    public void AddScore(int points)
    {
        currentRunScore += points;
        playerData.stats.totalScore += points;
    }

    public void AddWeaponFound()
    {
        currentRunWeapons++;
        playerData.stats.totalWeaponsFound++;
    }

    public void CompleteRun()
    {
        float runTime = Time.time - currentRunStartTime;
        var runData = new RunData(playerData.currentRun, runTime, currentRunScore, currentRunWeapons);
        playerData.completedRuns.Add(runData);

        // Update stats
        playerData.stats.totalPlayTime += runTime;
        if (currentRunScore > playerData.stats.bestRunScore)
            playerData.stats.bestRunScore = currentRunScore;
        if (runTime < playerData.stats.fastestRunTime)
            playerData.stats.fastestRunTime = runTime;

        // Keep only last 20 runs for performance
        if (playerData.completedRuns.Count > 20)
        {
            playerData.completedRuns.RemoveAt(0);
        }

        // Calculate rewards
        int batteryReward = batteryPerRun;
        if (playerData.currentRun % batteryBonusThreshold == 0)
        {
            batteryReward += batteryBonus;
        }

        AddBattery(batteryReward);
        CheckRunMilestoneUnlocks(playerData.currentRun);

        SaveLocalData();

        OnRunCompleted?.Invoke(runData);
        OnProgressUpdated?.Invoke(playerData.currentRun, targetRuns);

        Debug.Log($"Run #{playerData.currentRun} completed! Time: {runTime:F1}s, Score: {currentRunScore}");
    }

    public bool SpendBattery(int amount)
    {
        if (playerData.currentBattery >= amount)
        {
            playerData.currentBattery -= amount;
            SaveLocalData();
            OnBatteryChanged?.Invoke(playerData.currentBattery);
            return true;
        }
        return false;
    }

    public void AddBattery(int amount)
    {
        playerData.currentBattery += amount;
        SaveLocalData();
        OnBatteryChanged?.Invoke(playerData.currentBattery);
    }

    public void UpdateGamblingState(int consecutiveFailures)
    {
        playerData.consecutiveFailures = consecutiveFailures;
        SaveLocalData();
    }

    private void CheckRunMilestoneUnlocks(int runNumber)
    {
        // Define unlock milestones
        var milestones = new Dictionary<int, (string itemId, string message)>
        {
            { 5, ("weapon_shotgun", "Shotgun unlocked!") },
            { 10, ("skin_red_player", "Red skin unlocked!") },
            { 15, ("weapon_sniper", "Sniper rifle unlocked!") },
            { 20, ("weapon_assault", "Assault rifle unlocked!") },
            { 25, ("skin_gold_weapon", "Golden weapon skin unlocked!") },
            { 30, ("weapon_rocket", "Rocket launcher unlocked!") },
            { 35, ("weapon_plasma", "Plasma gun unlocked!") },
            { 40, ("skin_diamond", "Diamond skin unlocked!") },
            { 45, ("weapon_laser", "Laser weapon unlocked!") },
            { 50, ("skin_legendary", "Legendary skin unlocked!") }
        };

        if (milestones.ContainsKey(runNumber))
        {
            var unlock = milestones[runNumber];
            UnlockItem(unlock.itemId, unlock.message);
        }
    }

    private void UnlockItem(string itemId, string message)
    {
        bool unlocked = false;

        if (itemId.StartsWith("weapon_") && !playerData.unlockedWeapons.Contains(itemId))
        {
            playerData.unlockedWeapons.Add(itemId);
            unlocked = true;
        }
        else if (itemId.StartsWith("skin_") && !playerData.unlockedSkins.Contains(itemId))
        {
            playerData.unlockedSkins.Add(itemId);
            unlocked = true;
        }

        if (unlocked)
        {
            playerData.stats.totalUnlocks++;
            SaveLocalData();
            OnItemUnlocked?.Invoke(itemId);
            Debug.Log(message);
        }
    }

    private void TriggerUIUpdates()
    {
        OnBatteryChanged?.Invoke(playerData.currentBattery);
        OnProgressUpdated?.Invoke(playerData.currentRun, targetRuns);
    }

    #endregion

    #region Public Getters

    public int CurrentRun => playerData.currentRun;
    public int CurrentBattery => playerData.currentBattery;
    public List<string> UnlockedWeapons => playerData.unlockedWeapons;
    public List<string> UnlockedSkins => playerData.unlockedSkins;
    public List<RunData> CompletedRuns => playerData.completedRuns;
    public PlayerStats Stats => playerData.stats;
    public int ConsecutiveFailures => playerData.consecutiveFailures;

    public float GetProgressPercentage()
    {
        return (float)playerData.currentRun / targetRuns * 100f;
    }

    public bool IsComplete()
    {
        return playerData.currentRun >= targetRuns;
    }

    public RunData GetBestRun()
    {
        if (playerData.completedRuns.Count == 0) return null;

        RunData best = playerData.completedRuns[0];
        foreach (var run in playerData.completedRuns)
        {
            if (run.score > best.score)
                best = run;
        }
        return best;
    }

    public List<RunData> GetRecentRuns(int count = 5)
    {
        int startIndex = Mathf.Max(0, playerData.completedRuns.Count - count);
        return playerData.completedRuns.GetRange(startIndex, playerData.completedRuns.Count - startIndex);
    }

    public bool IsItemUnlocked(string itemId)
    {
        if (itemId.StartsWith("weapon_"))
            return playerData.unlockedWeapons.Contains(itemId);
        if (itemId.StartsWith("skin_"))
            return playerData.unlockedSkins.Contains(itemId);
        return false;
    }

    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && authManager != null && authManager.IsOnline())
        {
            ForceFirebaseSync();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && authManager != null && authManager.IsOnline())
        {
            ForceFirebaseSync();
        }
    }

    void OnDestroy()
    {
        if (authManager != null)
        {
            authManager.OnUserSignedIn -= HandleUserSignedIn;
            authManager.OnUserSignedOut -= HandleUserSignedOut;
            authManager.OnFirebaseInitialized -= HandleFirebaseInitialized;
        }

        // Final sync on destroy
        if (authManager != null && authManager.IsOnline())
        {
            ForceFirebaseSync();
        }
    }
}

// Complete Gambling System that integrates with progression
public class CompleteGamblingSystem : MonoBehaviour
{
    [Header("System References")]
    public CompleteProgressionSystem progressionSystem;

    [Header("Gambling Configuration")]
    public List<GamblingReward> possibleRewards = new List<GamblingReward>();
    public int baseCost = 100;
    public float costMultiplier = 1.2f;
    public int peakThreshold = 5;

    [Header("Peak System")]
    public float peakMultiplier = 2.0f;
    public Color normalColor = Color.white;
    public Color peakColor = Color.red;

    private bool isPeakActive = false;

    public event System.Action<GamblingReward> OnRewardWon;
    public event System.Action<bool> OnPeakStateChanged;
    public event System.Action<int> OnCostChanged;

    void Start()
    {
        if (progressionSystem == null)
        {
            progressionSystem = FindObjectOfType<CompleteProgressionSystem>();
        }

        if (progressionSystem != null)
        {
            progressionSystem.OnDataLoaded += HandleDataLoaded;
        }

        // Initialize default rewards if none set
        if (possibleRewards.Count == 0)
        {
            InitializeDefaultRewards();
        }

        CheckPeakState();
    }

    void HandleDataLoaded(PlayerData data)
    {
        CheckPeakState();
    }

    void InitializeDefaultRewards()
    {
        possibleRewards = new List<GamblingReward>
        {
            new GamblingReward(GamblingReward.RewardType.Currency, "battery", 50, 30f, 0),
            new GamblingReward(GamblingReward.RewardType.Currency, "battery", 100, 20f, 0),
            new GamblingReward(GamblingReward.RewardType.Currency, "battery", 200, 10f, 5),
            new GamblingReward(GamblingReward.RewardType.Weapon, "weapon_shotgun", 1, 15f, 5),
            new GamblingReward(GamblingReward.RewardType.Weapon, "weapon_sniper", 1, 10f, 15),
            new GamblingReward(GamblingReward.RewardType.Weapon, "weapon_plasma", 1, 5f, 35),
            new GamblingReward(GamblingReward.RewardType.Skin, "skin_red_player", 1, 12f, 10),
            new GamblingReward(GamblingReward.RewardType.Skin, "skin_gold_weapon", 1, 8f, 25),
            new GamblingReward(GamblingReward.RewardType.Skin, "skin_legendary", 1, 3f, 50)
        };
    }

    public bool CanGamble()
    {
        if (progressionSystem == null) return false;
        return progressionSystem.CurrentBattery >= GetCurrentCost();
    }

    public int GetCurrentCost()
    {
        if (progressionSystem == null) return baseCost;

        int totalGambles = progressionSystem.Stats.totalGambles;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, totalGambles / 10f));
    }

    public GamblingReward PerformGamble()
    {
        if (progressionSystem == null || !CanGamble()) return null;

        int cost = GetCurrentCost();
        if (!progressionSystem.SpendBattery(cost)) return null;

        // Update gambling stats
        progressionSystem.Stats.totalGambles++;

        // Filter rewards by run requirement
        var availableRewards = possibleRewards.FindAll(r =>
            r.requiredRuns <= progressionSystem.CurrentRun);

        if (availableRewards.Count == 0) return null;

        // Calculate total chance with peak bonus
        float totalChance = 0f;
        foreach (var reward in availableRewards)
        {
            float adjustedChance = reward.chance;
            if (isPeakActive) adjustedChance *= peakMultiplier;
            totalChance += adjustedChance;
        }

        // Roll for reward
        float roll = UnityEngine.Random.Range(0f, totalChance);
        float currentChance = 0f;

        foreach (var reward in availableRewards)
        {
            float adjustedChance = reward.chance;
            if (isPeakActive) adjustedChance *= peakMultiplier;
            currentChance += adjustedChance;

            if (roll <= currentChance)
            {
                // Won a reward
                HandleRewardWon(reward);
                ResetConsecutiveFailures();
                OnRewardWon?.Invoke(reward);
                OnCostChanged?.Invoke(GetCurrentCost());
                return reward;
            }
        }

        // No reward won
        IncrementConsecutiveFailures();
        OnRewardWon?.Invoke(null);
        OnCostChanged?.Invoke(GetCurrentCost());
        return null;
    }

    void HandleRewardWon(GamblingReward reward)
    {
        if (progressionSystem == null) return;

        switch (reward.type)
        {
            case GamblingReward.RewardType.Currency:
                progressionSystem.AddBattery(reward.amount);
                break;

            case GamblingReward.RewardType.Weapon:
                // The progression system will handle unlock logic
                break;

            case GamblingReward.RewardType.Skin:
                // The progression system will handle unlock logic
                break;

            case GamblingReward.RewardType.Upgrade:
                // Handle upgrades if implemented
                break;
        }
    }

    void IncrementConsecutiveFailures()
    {
        if (progressionSystem == null) return;

        int newFailures = progressionSystem.ConsecutiveFailures + 1;
        progressionSystem.UpdateGamblingState(newFailures);
        CheckPeakState();
    }

    void ResetConsecutiveFailures()
    {
        if (progressionSystem == null) return;

        progressionSystem.UpdateGamblingState(0);
        CheckPeakState();
    }

    void CheckPeakState()
    {
        if (progressionSystem == null) return;

        bool newPeakState = progressionSystem.ConsecutiveFailures >= peakThreshold;
        if (newPeakState != isPeakActive)
        {
            isPeakActive = newPeakState;
            OnPeakStateChanged?.Invoke(isPeakActive);
        }
    }

    public void ResetPeakSystem()
    {
        if (progressionSystem != null)
        {
            progressionSystem.UpdateGamblingState(0);
        }
        CheckPeakState();
    }

    #region Public Getters

    public bool IsPeakActive => isPeakActive;
    public int ConsecutiveFailures => progressionSystem?.ConsecutiveFailures ?? 0;
    public float PeakMultiplier => peakMultiplier;
    public List<GamblingReward> AvailableRewards => possibleRewards.FindAll(r =>
        progressionSystem != null && r.requiredRuns <= progressionSystem.CurrentRun);

    #endregion

    void OnDestroy()
    {
        if (progressionSystem != null)
        {
            progressionSystem.OnDataLoaded -= HandleDataLoaded;
        }
    }
}