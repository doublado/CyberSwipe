using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace CyberSwipe
{
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager instance;
        public static AnalyticsManager Instance => instance;

        [SerializeField] private AnalyticsSettings settings;
        private string sessionId;
        private bool isServerReachable = false;
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private Coroutine sendQueueCoroutine;
        private bool lastRequestSuccess = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                UnityEngine.Debug.Log("[AnalyticsManager] Awake called");

                // Create AnalyticsService if it doesn't exist
                if (AnalyticsService.Instance == null)
                {
                    var service = gameObject.AddComponent<AnalyticsService>();
                    UnityEngine.Debug.Log($"[AnalyticsManager] Added AnalyticsService component: {service != null}");
                }
                else
                {
                    UnityEngine.Debug.Log("[AnalyticsManager] AnalyticsService already exists");
                }

                sessionId = Guid.NewGuid().ToString();
                StartCoroutine(CheckServerConnection());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator CheckServerConnection()
        {
            if (settings.enableDebugLogging)
            {
                Debug.Log("[Analytics] Checking server connection...");
            }

            using (UnityWebRequest request = UnityWebRequest.Get($"{settings.serverUrl}/health"))
            {
                request.timeout = (int)settings.requestTimeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    isServerReachable = true;
                    if (settings.enableDebugLogging)
                    {
                        Debug.Log("[Analytics] Server connection successful");
                    }
                    StartSendingQueue();
                }
                else
                {
                    isServerReachable = false;
                    Debug.LogWarning($"[Analytics] Server connection failed: {request.error}");
                }
            }
        }

        private void StartSendingQueue()
        {
            if (sendQueueCoroutine == null)
            {
                sendQueueCoroutine = StartCoroutine(SendQueueCoroutine());
            }
        }

        private IEnumerator SendQueueCoroutine()
        {
            while (true)
            {
                if (eventQueue.Count > 0 && isServerReachable)
                {
                    AnalyticsEvent nextEvent = eventQueue.Dequeue();
                    yield return StartCoroutine(SendEventWithRetry(nextEvent.eventType, nextEvent.data, nextEvent.isSession));
                }
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void TrackEvent(string eventType, Dictionary<string, object> eventData, bool isSession = false)
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                Debug.Log("[Analytics] Analytics not enabled, skipping event tracking");
                return;
            }

            if (!isServerReachable)
            {
                if (settings.enableDebugLogging)
                {
                    Debug.Log($"[Analytics] Server not reachable, event queued: {eventType}");
                }
            }

            var analyticsEvent = new AnalyticsEvent
            {
                sessionId = sessionId,
                eventType = eventType,
                timestamp = DateTime.UtcNow.ToString("o"),
                data = eventData ?? new Dictionary<string, object>(),
                isSession = isSession
            };

            if (eventType == "session_end")
            {
                Debug.Log($"[Analytics] Sending session end event: {JsonConvert.SerializeObject(analyticsEvent)}");
            }

            eventQueue.Enqueue(analyticsEvent);
        }

        private IEnumerator SendEventWithRetry(string endpoint, Dictionary<string, object> eventData, bool isSession = false)
        {
            int retryCount = 0;
            const int maxRetries = 3;
            const float retryDelay = 1f;

            while (retryCount < maxRetries)
            {
                yield return StartCoroutine(SendEvent(endpoint, eventData, isSession));
                if (lastRequestSuccess)
                {
                    break;
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    Debug.Log($"[Analytics] Retrying event send ({retryCount}/{maxRetries})");
                    yield return new WaitForSeconds(retryDelay);
                }
            }

            if (!lastRequestSuccess)
            {
                Debug.LogWarning("[Analytics] Failed to send event after 3 retries");
            }
        }

        private IEnumerator SendEvent(string endpoint, Dictionary<string, object> eventData, bool isSession = false)
        {
            lastRequestSuccess = false;
            string jsonData = JsonConvert.SerializeObject(eventData);

            // Ensure the server URL doesn't end with a slash and the endpoint starts with one
            string baseUrl = settings.serverUrl.TrimEnd('/');
            string fullEndpoint = GetEndpointForEventType(endpoint, isSession);
            string fullUrl = baseUrl + fullEndpoint;

            Debug.Log($"[Analytics] Sending request to: {fullUrl}");
            Debug.Log($"[Analytics] Request data: {jsonData}");

            using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    lastRequestSuccess = true;
                    Debug.Log($"[Analytics] Event sent successfully to {fullUrl}");
                }
                else
                {
                    Debug.LogWarning($"[Analytics] Failed to send event: {request.error}");
                    if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                    {
                        Debug.LogWarning($"[Analytics] Server response: {request.downloadHandler.text}");
                    }
                }
            }
        }

        private string GetEndpointForEventType(string eventType, bool isSession)
        {
            if (isSession)
            {
                return "/api/analytics/session";
            }

            switch (eventType.ToLower())
            {
                case "performance":
                    return "/api/analytics/performance";
                case "category":
                    return "/api/analytics/category";
                case "session_end":
                    return "/api/analytics/session/end";
                default:
                    return "/api/analytics/event";
            }
        }

        public string GetServerUrl() => settings.serverUrl;

        private string GenerateJWT()
        {
            // In a real implementation, you would use a proper JWT library
            // This is a simplified version for demonstration
            var header = new { alg = "HS256", typ = "JWT" };
            var payload = new { 
                sub = "game-client",
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            };

            string headerJson = JsonUtility.ToJson(header);
            string payloadJson = JsonUtility.ToJson(payload);
            
            string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
            string payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
            
            string signature = $"{headerBase64}.{payloadBase64}";
            return signature;
        }
    }

    [Serializable]
    public class AnalyticsEvent
    {
        public string sessionId;
        public string eventType;
        public string timestamp;
        public Dictionary<string, object> data;
        public bool isSession;
    }
} 