using UnityEngine;

namespace CyberSwipe
{
    [CreateAssetMenu(fileName = "AnalyticsSettings", menuName = "CyberSwipe/Analytics Settings")]
    public class AnalyticsSettings : ScriptableObject
    {
        [Header("Server Configuration")]
        [Tooltip("The URL of the analytics server (e.g., http://localhost:8080)")]
        public string serverUrl = "http://localhost:8080";
        
        [Tooltip("The JWT secret for authentication")]
        public string jwtSecret = "your-secret-key";

        [Header("Connection Settings")]
        [Tooltip("Timeout in seconds for analytics requests")]
        public float requestTimeout = 5f;
        
        [Tooltip("Maximum number of retries for failed requests")]
        public int maxRetries = 3;
        
        [Tooltip("Delay between retries in seconds")]
        public float retryDelay = 1f;

        [Header("Debug Settings")]
        [Tooltip("Enable debug logging for analytics")]
        public bool enableDebugLogging = true;
    }
} 