using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

namespace CyberSwipe
{
    public class AnalyticsConsentPopup : MonoBehaviour
    {
        [Header("Analytics Settings")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool requireConsentEachSession = true;

        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        private GameManager gameManager;
        private bool isClosing = false;

        private void Awake()
        {
            Debug.Log("AnalyticsConsentPopup: Awake called");
            Debug.Log($"Popup Panel reference: {(popupPanel != null ? "Set" : "Null")}");
            Debug.Log($"Title Text reference: {(titleText != null ? "Set" : "Null")}");
            Debug.Log($"Description Text reference: {(descriptionText != null ? "Set" : "Null")}");
            Debug.Log($"Accept Button reference: {(acceptButton != null ? "Set" : "Null")}");
            Debug.Log($"Decline Button reference: {(declineButton != null ? "Set" : "Null")}");

            gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
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
            
            // Only show popup if we don't have consent yet
            if (!HasConsent())
            {
                Debug.Log("AnalyticsConsentPopup: No consent found, showing popup");
                ShowPopup();
            }
            else
            {
                Debug.Log("AnalyticsConsentPopup: Consent found, starting game");
                // If we already have consent, start the game
                if (gameManager != null)
                {
                    gameManager.StartGame();
                }
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
            // Store the consent in PlayerPrefs
            PlayerPrefs.SetInt("AnalyticsConsent", 1);
            PlayerPrefs.Save();

            // Hide the popup and start the game
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            // Start the game
            if (gameManager != null)
            {
                gameManager.StartGame();
            }
        }

        private void OnDeclineClicked()
        {
            if (isClosing) return; // Prevent multiple clicks
            isClosing = true;

            Debug.Log("AnalyticsConsentPopup: Decline clicked");
            // Store the decline in PlayerPrefs
            PlayerPrefs.SetInt("AnalyticsConsent", 0);
            PlayerPrefs.Save();

            // Close the game immediately
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
            AnalyticsConsentPopup instance = UnityEngine.Object.FindFirstObjectByType<AnalyticsConsentPopup>();
            return instance != null && instance.enableAnalytics;
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
    }
} 