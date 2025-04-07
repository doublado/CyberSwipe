using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace CyberSwipe
{
    /// <summary>
    /// Manages the communication with the analytics server.
    /// Handles event queuing, retry logic, and server connection status.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        private static AnalyticsManager instance;
        public static AnalyticsManager Instance => instance;

        [SerializeField] private AnalyticsSettings settings;
        
        // Session tracking
        private string sessionId;
        private bool isServerReachable = false;
        
        // Event queue management
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private Coroutine sendQueueCoroutine;
        private bool lastRequestSuccess = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                // Create AnalyticsService if it doesn't exist
                if (AnalyticsService.Instance == null)
                {
                    var service = gameObject.AddComponent<AnalyticsService>();
                }

                sessionId = Guid.NewGuid().ToString();
                StartCoroutine(CheckServerConnection());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Checks if the analytics server is reachable.
        /// </summary>
        private IEnumerator CheckServerConnection()
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{settings.serverUrl}/health"))
            {
                request.timeout = (int)settings.requestTimeout;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    isServerReachable = true;
                    StartSendingQueue();
                }
                else
                {
                    isServerReachable = false;
                }
            }
        }

        /// <summary>
        /// Starts the coroutine for sending queued events.
        /// </summary>
        private void StartSendingQueue()
        {
            if (sendQueueCoroutine == null)
            {
                sendQueueCoroutine = StartCoroutine(SendQueueCoroutine());
            }
        }

        /// <summary>
        /// Continuously processes the event queue and sends events to the server.
        /// </summary>
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

        /// <summary>
        /// Tracks an analytics event and adds it to the queue.
        /// </summary>
        /// <param name="eventType">Type of the event to track</param>
        /// <param name="eventData">Data associated with the event</param>
        /// <param name="isSession">Whether this is a session-related event</param>
        public void TrackEvent(string eventType, Dictionary<string, object> eventData, bool isSession = false)
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                return;
            }

            var analyticsEvent = new AnalyticsEvent
            {
                sessionId = sessionId,
                eventType = eventType,
                timestamp = DateTime.UtcNow.ToString("o"),
                data = eventData ?? new Dictionary<string, object>(),
                isSession = isSession
            };

            eventQueue.Enqueue(analyticsEvent);
        }

        /// <summary>
        /// Sends an event to the server with retry logic.
        /// </summary>
        /// <param name="endpoint">The API endpoint to send the event to</param>
        /// <param name="eventData">The event data to send</param>
        /// <param name="isSession">Whether this is a session-related event</param>
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
                    yield return new WaitForSeconds(retryDelay);
                }
            }
        }

        /// <summary>
        /// Sends a single event to the analytics server.
        /// </summary>
        /// <param name="endpoint">The API endpoint to send the event to</param>
        /// <param name="eventData">The event data to send</param>
        /// <param name="isSession">Whether this is a session-related event</param>
        private IEnumerator SendEvent(string endpoint, Dictionary<string, object> eventData, bool isSession = false)
        {
            lastRequestSuccess = false;
            string jsonData = JsonConvert.SerializeObject(eventData);

            string baseUrl = settings.serverUrl.TrimEnd('/');
            string fullEndpoint = GetEndpointForEventType(endpoint, isSession);
            string fullUrl = baseUrl + fullEndpoint;

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
                }
            }
        }

        /// <summary>
        /// Gets the appropriate API endpoint for the given event type.
        /// </summary>
        /// <param name="eventType">Type of the event</param>
        /// <param name="isSession">Whether this is a session-related event</param>
        /// <returns>The API endpoint path</returns>
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

        /// <summary>
        /// Gets the server URL from the settings.
        /// </summary>
        /// <returns>The analytics server URL</returns>
        public string GetServerUrl() => settings.serverUrl;

        /// <summary>
        /// Generates a JWT token for authentication.
        /// </summary>
        /// <returns>A JWT token string</returns>
        private string GenerateJWT()
        {
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

    /// <summary>
    /// Represents an analytics event to be sent to the server.
    /// </summary>
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