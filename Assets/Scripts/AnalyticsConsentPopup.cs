using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

namespace CyberSwipe
{
    public class AnalyticsConsentPopup : MonoBehaviour
    {
        private static AnalyticsConsentPopup instance;
        public static AnalyticsConsentPopup Instance => instance;

        [Header("Analytics Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool requireConsentEachSession = true;
        [SerializeField] private AnalyticsSettings analyticsSettings;

        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        private GameManager gameManager;
        private bool isClosing = false;
        private bool isServerReachable = false;

        private void Awake()
        {
            Debug.Log("AnalyticsConsentPopup: Awake called");
            
            // Handle singleton pattern
            if (instance == null)
            {
                instance = this;
                Debug.Log("AnalyticsConsentPopup: Set as singleton instance");
            }
            else
            {
                Debug.Log("AnalyticsConsentPopup: Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            Debug.Log($"Popup Panel reference: {(popupPanel != null ? "Set" : "Null")}");
            Debug.Log($"Title Text reference: {(titleText != null ? "Set" : "Null")}");
            Debug.Log($"Description Text reference: {(descriptionText != null ? "Set" : "Null")}");
            Debug.Log($"Accept Button reference: {(acceptButton != null ? "Set" : "Null")}");
            Debug.Log($"Decline Button reference: {(declineButton != null ? "Set" : "Null")}");

            gameManager = Object.FindFirstObjectByType<GameManager>();
            Debug.Log($"GameManager reference: {(gameManager != null ? "Found" : "Not Found")}");
            
            // Add button listeners
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }
            
            if (declineButton != null)
            {
                declineButton.onClick.AddListener(OnDeclineClicked);
            }

            // Clear consent in development mode or if we require consent each session
            #if UNITY_EDITOR
            PlayerPrefs.DeleteKey("AnalyticsConsent");
            PlayerPrefs.Save();
            Debug.Log("AnalyticsConsentPopup: Cleared consent for testing");
            #endif

            if (requireConsentEachSession)
            {
                PlayerPrefs.DeleteKey("AnalyticsConsent");
                PlayerPrefs.Save();
                Debug.Log("AnalyticsConsentPopup: Cleared consent for new session");
            }
        }

        private void Start()
        {
            Debug.Log("AnalyticsConsentPopup: Start called");
            Debug.Log($"HasConsent: {HasConsent()}");
            Debug.Log($"Analytics Enabled: {enableAnalytics}");

            if (!enableAnalytics)
            {
                Debug.Log("AnalyticsConsentPopup: Analytics disabled, starting game");
                if (gameManager != null)
                {
                    gameManager.StartGame();
                }
                return;
            }
            
            StartCoroutine(CheckServerConnection());
        }

        private IEnumerator CheckServerConnection()
        {
            if (analyticsSettings == null)
            {
                Debug.LogError("AnalyticsConsentPopup: AnalyticsSettings not assigned!");
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
                    Debug.Log("AnalyticsConsentPopup: Server connection successful");
                    
                    if (!HasConsent())
                    {
                        ShowPopup();
                    }
                    else
                    {
                        if (gameManager != null)
                        {
                            gameManager.StartGame();
                        }
                    }
                }
                else
                {
                    isServerReachable = false;
                    Debug.LogWarning($"AnalyticsConsentPopup: Server connection failed: {request.error}");
                    StartGameWithoutAnalytics();
                }
            }
        }

        private void StartGameWithoutAnalytics()
        {
            Debug.Log("AnalyticsConsentPopup: Starting game without analytics");
            
            // Close the popup if it's open
            if (popupPanel != null && popupPanel.activeSelf)
            {
                popupPanel.SetActive(false);
                Debug.Log("AnalyticsConsentPopup: Closed popup since analytics are not available");
            }
            
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        public void ShowPopup()
        {
            Debug.Log("AnalyticsConsentPopup: ShowPopup called");
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                Debug.Log("AnalyticsConsentPopup: Panel set to active");
            }
            else
            {
                Debug.LogError("AnalyticsConsentPopup: Popup Panel reference is null!");
            }
        }

        private void OnAcceptClicked()
        {
            Debug.Log("AnalyticsConsentPopup: Accept clicked");
            PlayerPrefs.SetInt("AnalyticsConsent", 1);
            PlayerPrefs.Save();

            // Initialize analytics session
            if (AnalyticsService.Instance != null)
            {
                AnalyticsService.Instance.InitializeSession();
            }

            // Only hide the popup panel, don't destroy the GameObject
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
                Debug.Log("AnalyticsConsentPopup: Panel hidden, keeping GameObject active");
            }

            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        private void OnDeclineClicked()
        {
            if (isClosing) return;
            isClosing = true;

            Debug.Log("AnalyticsConsentPopup: Decline clicked");
            PlayerPrefs.SetInt("AnalyticsConsent", 0);
            PlayerPrefs.Save();

            Debug.Log("AnalyticsConsentPopup: Closing game now");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public static bool HasConsent()
        {
            return PlayerPrefs.GetInt("AnalyticsConsent", 0) == 1;
        }

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

        // Test method to verify the popup works
        public void TestPopup()
        {
            Debug.Log("AnalyticsConsentPopup: TestPopup called");
            // Clear any existing consent
            PlayerPrefs.DeleteKey("AnalyticsConsent");
            PlayerPrefs.Save();

            // Show the popup
            ShowPopup();
        }

        public bool IsPopupActive()
        {
            return popupPanel != null && popupPanel.activeSelf;
        }
    }
} 