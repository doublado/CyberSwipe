using UnityEngine;

namespace CyberSwipe
{
    /// <summary>
    /// Configuration settings for the analytics system.
    /// This ScriptableObject can be created and configured in the Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "CyberSwipe/Analytics Settings")]
    public class AnalyticsSettings : ScriptableObject
    {
        [Header("Server Configuration")]
        [Tooltip("The URL of the analytics server (e.g., http://localhost:8080)")]
        public string serverUrl = "http://localhost:8080";
        
        [Tooltip("The JWT secret key used for authentication with the analytics server")]
        public string jwtSecret = "your-secret-key";

        [Header("Connection Settings")]
        [Tooltip("Timeout in seconds for analytics requests")]
        [Range(1f, 30f)]
        public float requestTimeout = 5f;
        
        [Tooltip("Maximum number of retries for failed requests")]
        [Range(1, 5)]
        public int maxRetries = 3;
        
        [Tooltip("Delay in seconds between retry attempts")]
        [Range(0.1f, 5f)]
        public float retryDelay = 1f;

        [Header("Debug Settings")]
        [Tooltip("Enable detailed logging for analytics operations")]
        public bool enableDebugLogging = true;
    }
} 