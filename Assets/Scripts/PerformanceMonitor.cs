using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CyberSwipe
{
    public class PerformanceMonitor : MonoBehaviour
    {
        private static PerformanceMonitor instance;
        public static PerformanceMonitor Instance => instance;

        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private float networkCheckInterval = 5.0f; // Check network every 5 seconds
        [SerializeField] private bool enablePerformanceLogging = true;

        private float fps;
        private long memoryUsage;
        private float cpuUsage;
        private float gpuUsage;
        private int networkLatency;
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
                stopwatch = new Stopwatch();
                lastCpuTime = Time.realtimeSinceStartup;
                lastGpuTime = Time.realtimeSinceStartup;
                
                // Start checking for consent
                StartCoroutine(WaitForConsent());
            }
            else
            {
                Destroy(gameObject);
            }
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

        private IEnumerator WaitForConsent()
        {
            // Wait until the consent popup is no longer being displayed
            while (AnalyticsConsentPopup.Instance != null && AnalyticsConsentPopup.Instance.IsPopupActive())
            {
                UnityEngine.Debug.Log("[PerformanceMonitor] Waiting for consent popup to be closed...");
                yield return new WaitForSeconds(1f);
            }

            // Only start tracking if analytics is enabled
            if (AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                isTracking = true;
                StartCoroutine(UpdateMetricsCoroutine());
                UnityEngine.Debug.Log("[PerformanceMonitor] Started tracking after consent received");
            }
            else
            {
                UnityEngine.Debug.Log("[PerformanceMonitor] Analytics not enabled, not starting tracking");
            }
        }

        private IEnumerator UpdateMetricsCoroutine()
        {
            while (isTracking)
            {
                // Check if analytics is still enabled
                if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
                {
                    UnityEngine.Debug.Log("[PerformanceMonitor] Analytics disabled, stopping tracking");
                    isTracking = false;
                    yield break;
                }

                UpdateMetrics();
                SendMetrics();
                yield return new WaitForSeconds(updateInterval);
            }
        }

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

            if (enablePerformanceLogging)
            {
                UnityEngine.Debug.Log($"[Performance] FPS: {fps:F0}, Memory: {memoryUsage / 1024 / 1024:F1}MB, CPU: {cpuUsage:F1}%, GPU: {gpuUsage:F1}%");
            }
        }

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
                
                if (enablePerformanceLogging)
                {
                    UnityEngine.Debug.Log($"[Performance] Network latency: {networkLatency}ms");
                }
            }
        }

        private void SendMetrics()
        {
            if (!isTracking || !AnalyticsConsentPopup.IsAnalyticsEnabled()) return;

            string sessionId = AnalyticsService.Instance?.GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                UnityEngine.Debug.LogWarning("[PerformanceMonitor] Cannot send metrics: No valid session ID");
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

            if (enablePerformanceLogging)
            {
                UnityEngine.Debug.Log($"[Performance] Sending metrics: {JsonConvert.SerializeObject(metrics)}");
            }

            AnalyticsManager.Instance.TrackEvent("performance", metrics);
            UnityEngine.Debug.Log("[Performance] Metrics sent to server");
        }

        public float GetFPS() => fps;
        public long GetMemoryUsage() => memoryUsage;
        public float GetCPUUsage() => cpuUsage;
        public float GetGPUUsage() => gpuUsage;
        public int GetNetworkLatency() => networkLatency;
    }
} 