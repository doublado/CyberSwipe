using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CyberSwipe
{
    /// <summary>
    /// Monitors and tracks performance metrics of the game.
    /// Collects data about FPS, memory usage, CPU/GPU usage, and network latency.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        private static PerformanceMonitor instance;
        public static PerformanceMonitor Instance => instance;

        [Header("Monitoring Settings")]
        [Tooltip("Interval in seconds between performance metric updates")]
        [SerializeField] private float updateInterval = 1.0f;
        
        [Tooltip("Interval in seconds between network latency checks")]
        [SerializeField] private float networkCheckInterval = 5.0f;
        
        // Performance metrics
        private float fps;
        private long memoryUsage;
        private float cpuUsage;
        private float gpuUsage;
        private int networkLatency;
        
        // Timing and state tracking
        private float lastUpdateTime;
        private float lastNetworkCheckTime;
        private Stopwatch stopwatch;
        private bool isTracking = false;
        private int frameCount;
        private float frameTime;
        private float lastCpuTime;
        private float lastGpuTime;

        private void Start()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePerformanceMonitoring();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes the performance monitoring system.
        /// </summary>
        private void InitializePerformanceMonitoring()
        {
            stopwatch = new Stopwatch();
            lastCpuTime = Time.realtimeSinceStartup;
            lastGpuTime = Time.realtimeSinceStartup;
            
            // Start checking for consent
            StartCoroutine(WaitForConsent());
        }

        private void Update()
        {
            if (!isTracking || !AnalyticsConsentPopup.IsAnalyticsEnabled()) return;

            // Count frames and accumulate frame time
            frameCount++;
            frameTime += Time.deltaTime;

            // Calculate CPU usage based on real time
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastCpuTime;
            if (deltaTime > 0)
            {
                cpuUsage = (Time.deltaTime / deltaTime) * 100f;
            }
            lastCpuTime = currentTime;

            // Calculate GPU usage based on frame time
            float targetFrameTime = 1f / 60f; // Target 60 FPS
            float frameTimeRatio = Time.deltaTime / targetFrameTime;
            gpuUsage = Mathf.Clamp((1f - frameTimeRatio) * 100f, 0f, 100f);
        }

        /// <summary>
        /// Waits for analytics consent before starting performance monitoring.
        /// </summary>
        private IEnumerator WaitForConsent()
        {
            // Wait until the consent popup is no longer being displayed
            while (AnalyticsConsentPopup.Instance != null && AnalyticsConsentPopup.Instance.IsPopupActive())
            {
                yield return new WaitForSeconds(1f);
            }

            // Only start tracking if analytics is enabled
            if (AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                isTracking = true;
                StartCoroutine(UpdateMetricsCoroutine());
            }
        }

        /// <summary>
        /// Continuously updates and sends performance metrics.
        /// </summary>
        private IEnumerator UpdateMetricsCoroutine()
        {
            while (isTracking)
            {
                // Check if analytics is still enabled
                if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
                {
                    isTracking = false;
                    yield break;
                }

                UpdateMetrics();
                SendMetrics();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        /// <summary>
        /// Updates the current performance metrics.
        /// </summary>
        private void UpdateMetrics()
        {
            if (!isTracking || !AnalyticsConsentPopup.IsAnalyticsEnabled()) return;

            // Calculate FPS based on actual frame count and time
            if (frameTime > 0)
            {
                fps = frameCount / frameTime;
            }
            else
            {
                fps = 0;
            }

            // Reset frame counting
            frameCount = 0;
            frameTime = 0;

            // Get memory usage
            memoryUsage = System.GC.GetTotalMemory(false);

            // Only check network latency periodically
            if (Time.time - lastNetworkCheckTime >= networkCheckInterval)
            {
                StartCoroutine(MeasureNetworkLatency());
                lastNetworkCheckTime = Time.time;
            }
        }

        /// <summary>
        /// Measures the current network latency to the analytics server.
        /// </summary>
        private IEnumerator MeasureNetworkLatency()
        {
            if (!isTracking || !AnalyticsConsentPopup.IsAnalyticsEnabled()) yield break;

            string url = AnalyticsManager.Instance.GetServerUrl() + "/health";
            stopwatch.Reset();
            stopwatch.Start();

            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                stopwatch.Stop();
                networkLatency = (int)stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Sends the current performance metrics to the analytics server.
        /// </summary>
        private void SendMetrics()
        {
            if (!isTracking || !AnalyticsConsentPopup.IsAnalyticsEnabled()) return;

            string sessionId = AnalyticsService.Instance?.GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            var metrics = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "fps", fps },
                { "memory_usage", memoryUsage },
                { "cpu_usage", cpuUsage },
                { "gpu_usage", gpuUsage },
                { "network_latency", networkLatency }
            };

            AnalyticsManager.Instance.TrackEvent("performance", metrics);
        }

        /// <summary>
        /// Gets the current FPS value.
        /// </summary>
        public float GetFPS() => fps;

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        public long GetMemoryUsage() => memoryUsage;

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        public float GetCPUUsage() => cpuUsage;

        /// <summary>
        /// Gets the current GPU usage percentage.
        /// </summary>
        public float GetGPUUsage() => gpuUsage;

        /// <summary>
        /// Gets the current network latency in milliseconds.
        /// </summary>
        public int GetNetworkLatency() => networkLatency;
    }
} 