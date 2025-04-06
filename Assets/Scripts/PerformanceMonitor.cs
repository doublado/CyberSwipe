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
        [SerializeField] private bool enablePerformanceLogging = true;

        private float fps;
        private long memoryUsage;
        private float cpuUsage;
        private float gpuUsage;
        private int networkLatency;
        private float lastUpdateTime;
        private Stopwatch stopwatch;

        private void Start()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                stopwatch = new Stopwatch();
                StartCoroutine(UpdateMetricsCoroutine());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator UpdateMetricsCoroutine()
        {
            while (true)
            {
                UpdateMetrics();
                SendMetrics();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void UpdateMetrics()
        {
            // Calculate FPS
            float timeSinceLastUpdate = Time.time - lastUpdateTime;
            fps = 1.0f / timeSinceLastUpdate;
            lastUpdateTime = Time.time;

            // Get memory usage
            memoryUsage = System.GC.GetTotalMemory(false);

            // Calculate CPU usage (simplified)
            stopwatch.Stop();
            float elapsedMs = stopwatch.ElapsedMilliseconds;
            cpuUsage = (elapsedMs / (updateInterval * 1000f)) * 100f;
            stopwatch.Reset();
            stopwatch.Start();

            // Get GPU usage (simplified - Unity doesn't provide direct GPU usage)
            gpuUsage = QualitySettings.GetQualityLevel() * 10f;

            // Measure network latency
            StartCoroutine(MeasureNetworkLatency());

            if (enablePerformanceLogging)
            {
                UnityEngine.Debug.Log($"[Performance] FPS: {fps:F1}, Memory: {memoryUsage / 1024 / 1024:F1}MB, CPU: {cpuUsage:F1}%, GPU: {gpuUsage:F1}%");
            }
        }

        private IEnumerator MeasureNetworkLatency()
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                yield break;
            }

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

        private void SendMetrics()
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                return;
            }

            var metrics = new Dictionary<string, object>
            {
                { "session_id", AnalyticsService.Instance.GetSessionId() },
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