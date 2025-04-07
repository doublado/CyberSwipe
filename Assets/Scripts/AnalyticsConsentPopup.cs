using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

namespace CyberSwipe
{
    /// <summary>
    /// Manages the analytics consent popup and user preferences for analytics tracking.
    /// Handles the display, user interaction, and persistence of analytics consent settings.
    /// </summary>
    public class AnalyticsConsentPopup : MonoBehaviour
    {
        private static AnalyticsConsentPopup instance;
        public static AnalyticsConsentPopup Instance => instance;

        [Header("Analytics Settings")]
        [Tooltip("Whether analytics tracking is enabled in the application")]
        [SerializeField] private bool enableAnalytics = true;
        
        [Tooltip("If true, requires user consent each time the application starts")]
        [SerializeField] private bool requireConsentEachSession = true;
        
        [Tooltip("Configuration settings for analytics")]
        [SerializeField] private AnalyticsSettings analyticsSettings;

        [Header("UI References")]
        [Tooltip("The main popup panel GameObject")]
        [SerializeField] private GameObject popupPanel;
        
        [Tooltip("Text component for the popup title")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Tooltip("Text component for the popup description")]
        [SerializeField] private TextMeshProUGUI descriptionText;
        
        [Tooltip("Button to accept analytics tracking")]
        [SerializeField] private Button acceptButton;
        
        [Tooltip("Button to decline analytics tracking")]
        [SerializeField] private Button declineButton;

        // Component references
        private GameManager gameManager;
        private bool isClosing = false;
        private bool isServerReachable = false;

        private void Awake()
        {
            Debug.Log("[AnalyticsConsentPopup] Initializing popup");
            
            // Handle singleton pattern
            if (instance == null)
            {
                instance = this;
                Debug.Log("[AnalyticsConsentPopup] Set as singleton instance");
            }
            else
            {
                Debug.Log("[AnalyticsConsentPopup] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            ValidateComponentReferences();
            InitializeButtonListeners();
            HandleDevelopmentModeConsent();
        }

        /// <summary>
        /// Validates that all required component references are set.
        /// </summary>
        private void ValidateComponentReferences()
        {
            Debug.Log($"[AnalyticsConsentPopup] Validating component references:");
            Debug.Log($"- Popup Panel: {(popupPanel != null ? "Set" : "Null")}");
            Debug.Log($"- Title Text: {(titleText != null ? "Set" : "Null")}");
            Debug.Log($"- Description Text: {(descriptionText != null ? "Set" : "Null")}");
            Debug.Log($"- Accept Button: {(acceptButton != null ? "Set" : "Null")}");
            Debug.Log($"- Decline Button: {(declineButton != null ? "Set" : "Null")}");

            gameManager = Object.FindFirstObjectByType<GameManager>();
            Debug.Log($"- GameManager: {(gameManager != null ? "Found" : "Not Found")}");
        }

        /// <summary>
        /// Sets up click listeners for the consent buttons.
        /// </summary>
        private void InitializeButtonListeners()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }
            
            if (declineButton != null)
            {
                declineButton.onClick.AddListener(OnDeclineClicked);
            }
        }

        /// <summary>
        /// Handles consent settings for development mode.
        /// </summary>
        private void HandleDevelopmentModeConsent()
        {
            #if UNITY_EDITOR
            PlayerPrefs.DeleteKey("AnalyticsConsent");
            PlayerPrefs.Save();
            Debug.Log("[AnalyticsConsentPopup] Cleared consent for testing");
            #endif

            if (requireConsentEachSession)
            {
                PlayerPrefs.DeleteKey("AnalyticsConsent");
                PlayerPrefs.Save();
                Debug.Log("[AnalyticsConsentPopup] Cleared consent for new session");
            }
        }

        private void Start()
        {
            Debug.Log("[AnalyticsConsentPopup] Starting popup initialization");
            Debug.Log($"- HasConsent: {HasConsent()}");
            Debug.Log($"- Analytics Enabled: {enableAnalytics}");

            if (!enableAnalytics)
            {
                Debug.Log("[AnalyticsConsentPopup] Analytics disabled, starting game");
                StartGameIfManagerAvailable();
                return;
            }
            
            StartCoroutine(CheckServerConnection());
        }

        /// <summary>
        /// Checks if the analytics server is reachable.
        /// </summary>
        private IEnumerator CheckServerConnection()
        {
            if (analyticsSettings == null)
            {
                Debug.LogError("[AnalyticsConsentPopup] AnalyticsSettings not assigned!");
                StartGameWithoutAnalytics();
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get($"{analyticsSettings.serverUrl}/health"))
            {
                request.timeout = (int)analyticsSettings.requestTimeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    isServerReachable = true;
                    Debug.Log("[AnalyticsConsentPopup] Server connection successful");
                    
                    if (!HasConsent())
                    {
                        ShowPopup();
                    }
                    else
                    {
                        StartGameIfManagerAvailable();
                    }
                }
                else
                {
                    isServerReachable = false;
                    Debug.LogWarning($"[AnalyticsConsentPopup] Server connection failed: {request.error}");
                    StartGameWithoutAnalytics();
                }
            }
        }

        /// <summary>
        /// Starts the game without analytics functionality.
        /// </summary>
        private void StartGameWithoutAnalytics()
        {
            Debug.Log("[AnalyticsConsentPopup] Starting game without analytics");
            
            if (popupPanel != null && popupPanel.activeSelf)
            {
                popupPanel.SetActive(false);
                Debug.Log("[AnalyticsConsentPopup] Closed popup since analytics are not available");
            }
            
            StartGameIfManagerAvailable();
        }

        /// <summary>
        /// Starts the game if the GameManager is available.
        /// </summary>
        private void StartGameIfManagerAvailable()
        {
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        /// <summary>
        /// Displays the analytics consent popup.
        /// </summary>
        public void ShowPopup()
        {
            Debug.Log("[AnalyticsConsentPopup] Showing popup");
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                Debug.Log("[AnalyticsConsentPopup] Panel set to active");
            }
            else
            {
                Debug.LogError("[AnalyticsConsentPopup] Popup Panel reference is null!");
            }
        }

        /// <summary>
        /// Handles the accept button click event.
        /// </summary>
        private void OnAcceptClicked()
        {
            Debug.Log("[AnalyticsConsentPopup] User accepted analytics");
            PlayerPrefs.SetInt("AnalyticsConsent", 1);
            PlayerPrefs.Save();

            if (AnalyticsService.Instance != null)
            {
                AnalyticsService.Instance.InitializeSession();
            }

            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
                Debug.Log("[AnalyticsConsentPopup] Panel hidden, keeping GameObject active");
            }

            StartGameIfManagerAvailable();
        }

        /// <summary>
        /// Handles the decline button click event.
        /// </summary>
        private void OnDeclineClicked()
        {
            if (isClosing) return;
            isClosing = true;

            Debug.Log("[AnalyticsConsentPopup] User declined analytics");
            PlayerPrefs.SetInt("AnalyticsConsent", 0);
            PlayerPrefs.Save();

            Debug.Log("[AnalyticsConsentPopup] Closing application");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Checks if the user has given consent for analytics tracking.
        /// </summary>
        /// <returns>True if consent has been given, false otherwise</returns>
        public static bool HasConsent()
        {
            return PlayerPrefs.GetInt("AnalyticsConsent", 0) == 1;
        }

        /// <summary>
        /// Checks if analytics tracking is enabled and available.
        /// </summary>
        /// <returns>True if analytics is enabled and the server is reachable</returns>
        public static bool IsAnalyticsEnabled()
        {
            if (instance == null)
            {
                Debug.Log("[AnalyticsConsentPopup] IsAnalyticsEnabled: No instance found");
                return false;
            }

            bool hasInstance = instance != null;
            bool analyticsEnabled = instance.enableAnalytics;
            bool serverReachable = instance.isServerReachable;
            
            Debug.Log($"[AnalyticsConsentPopup] IsAnalyticsEnabled check:");
            Debug.Log($"- Has instance: {hasInstance}");
            Debug.Log($"- Analytics enabled: {analyticsEnabled}");
            Debug.Log($"- Server reachable: {serverReachable}");
            
            return hasInstance && analyticsEnabled && serverReachable;
        }

        /// <summary>
        /// Checks if the popup is currently active and visible.
        /// </summary>
        /// <returns>True if the popup is active, false otherwise</returns>
        public bool IsPopupActive()
        {
            return popupPanel != null && popupPanel.activeSelf;
        }
    }
} 