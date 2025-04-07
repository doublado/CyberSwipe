using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CyberSwipe
{
    /// <summary>
    /// Manages the collection and tracking of analytics data during gameplay.
    /// Handles session management, event tracking, and performance metrics.
    /// </summary>
    public class AnalyticsService : MonoBehaviour
    {
        private static AnalyticsService instance;
        public static AnalyticsService Instance => instance;

        // Session tracking
        private string sessionId;
        private string userId;
        private float sessionStartTime;
        private int totalCardsProcessed;
        private int totalCategoriesCompleted;
        private float totalDecisionTime;
        private int totalSuccessfulDecisions;
        private Dictionary<string, CategoryStats> categoryStats = new Dictionary<string, CategoryStats>();

        /// <summary>
        /// Represents statistics for a specific category of cards.
        /// </summary>
        public class CategoryStats
        {
            public int totalCards;
            public int acceptedCards;
            public int rejectedCards;
            public float totalDecisionTime;
            public float startTime;
        }

        /// <summary>
        /// Gets the current category statistics.
        /// </summary>
        public Dictionary<string, CategoryStats> GetCategoryStats() => categoryStats;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                UnityEngine.Debug.Log("[AnalyticsService] Initialized new instance");
            }
            else if (instance != this)
            {
                UnityEngine.Debug.Log("[AnalyticsService] Destroying duplicate instance");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (instance == this)
            {
                InitializeSession();
            }
        }

        /// <summary>
        /// Initializes a new analytics session with unique identifiers.
        /// </summary>
        public void InitializeSession()
        {
            Debug.Log("[AnalyticsService] Initializing session");
            
            // Generate unique identifiers
            sessionId = Guid.NewGuid().ToString();
            userId = PlayerPrefs.GetString("AnalyticsUserId", Guid.NewGuid().ToString());
            PlayerPrefs.SetString("AnalyticsUserId", userId);
            PlayerPrefs.Save();

            // Reset session metrics
            sessionStartTime = Time.time;
            totalCardsProcessed = 0;
            totalCategoriesCompleted = 0;
            totalDecisionTime = 0;
            totalSuccessfulDecisions = 0;
            categoryStats.Clear();

            // Create session on the server
            var sessionData = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "user_id", userId },
                { "platform", Application.platform.ToString() },
                { "resolution", $"{Screen.width}x{Screen.height}" },
                { "device_model", SystemInfo.deviceModel },
                { "os_version", SystemInfo.operatingSystem }
            };

            AnalyticsManager.Instance.TrackEvent("session", sessionData, isSession: true);
            Debug.Log($"[AnalyticsService] Session initialized - SessionId: {sessionId}, UserId: {userId}");
        }

        /// <summary>
        /// Tracks a card swipe event with associated metrics.
        /// </summary>
        /// <param name="cardData">The card that was swiped</param>
        /// <param name="duration">Time taken to complete the swipe</param>
        /// <param name="maxRotation">Maximum rotation angle during swipe</param>
        /// <param name="success">Whether the swipe was successful</param>
        /// <param name="startPosition">Starting position of the swipe</param>
        /// <param name="endPosition">Ending position of the swipe</param>
        public void TrackCardSwipe(CardData cardData, float duration, float maxRotation, bool success, Vector2 startPosition, Vector2 endPosition)
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                return;
            }

            // Update session metrics
            totalCardsProcessed++;
            totalDecisionTime += duration;
            if (success) totalSuccessfulDecisions++;

            // Update category-specific metrics
            string categoryName = cardData.cardCategory.ToString();
            if (!categoryStats.ContainsKey(categoryName))
            {
                categoryStats[categoryName] = new CategoryStats { startTime = Time.time };
            }

            var stats = categoryStats[categoryName];
            stats.totalCards++;
            if (success) stats.acceptedCards++;
            else stats.rejectedCards++;
            stats.totalDecisionTime += duration;

            // Prepare event data
            var eventData = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "event_type", "card_swipe" },
                { "card_id", cardData.CardId },
                { "category", categoryName },
                { "direction", endPosition.x > startPosition.x ? "right" : "left" },
                { "success", success },
                { "duration", duration },
                { "start_x", startPosition.x },
                { "end_x", endPosition.x },
                { "max_rotation", maxRotation }
            };

            AnalyticsManager.Instance.TrackEvent("card_swipe", eventData);
        }

        /// <summary>
        /// Resets the statistics for a specific category.
        /// </summary>
        /// <param name="categoryName">Name of the category to reset</param>
        private void ResetCategoryStats(string categoryName)
        {
            if (categoryStats.ContainsKey(categoryName))
            {
                categoryStats[categoryName] = new CategoryStats { startTime = Time.time };
                UnityEngine.Debug.Log($"[Analytics] Reset stats for category: {categoryName}");
            }
        }

        /// <summary>
        /// Tracks the completion of a category with its associated statistics.
        /// </summary>
        /// <param name="categoryName">Name of the completed category</param>
        public void TrackCategoryCompletion(string categoryName)
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                return;
            }

            if (categoryStats.TryGetValue(categoryName, out var stats))
            {
                var categoryData = new Dictionary<string, object>
                {
                    { "session_id", sessionId },
                    { "category_name", categoryName },
                    { "total_cards", stats.totalCards },
                    { "accepted_cards", stats.acceptedCards },
                    { "rejected_cards", stats.rejectedCards },
                    { "average_decision_time", stats.totalDecisionTime / stats.totalCards },
                    { "completion_time", (int)(Time.time - stats.startTime) }
                };

                UnityEngine.Debug.Log($"[Analytics] Sending category completion data: {JsonConvert.SerializeObject(categoryData)}");
                AnalyticsManager.Instance.TrackEvent("category", categoryData);
                totalCategoriesCompleted++;

                UnityEngine.Debug.Log($"[Analytics] Category {categoryName} completed. Stats: {JsonConvert.SerializeObject(categoryData)}");
                
                // Reset stats for this category after tracking completion
                ResetCategoryStats(categoryName);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Analytics] No stats found for category: {categoryName}");
            }
        }

        /// <summary>
        /// Ends the current analytics session and sends final statistics.
        /// </summary>
        public void EndSession()
        {
            if (!AnalyticsConsentPopup.IsAnalyticsEnabled())
            {
                return;
            }

            float sessionDuration = Time.time - sessionStartTime;
            float averageDecisionTime = totalCardsProcessed > 0 ? totalDecisionTime / totalCardsProcessed : 0;
            float successRate = totalCardsProcessed > 0 ? (float)totalSuccessfulDecisions / totalCardsProcessed : 0;

            var sessionData = new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "session_duration", Mathf.RoundToInt(sessionDuration) },
                { "total_cards_processed", totalCardsProcessed },
                { "total_categories_completed", totalCategoriesCompleted },
                { "average_decision_time", Mathf.Round(averageDecisionTime * 100f) / 100f },
                { "success_rate", Mathf.Round(successRate * 100f) / 100f }
            };

            UnityEngine.Debug.Log($"[Analytics] Ending session with data: {JsonConvert.SerializeObject(sessionData)}");
            AnalyticsManager.Instance.TrackEvent("session_end", sessionData);
        }

        /// <summary>
        /// Gets the current session ID.
        /// </summary>
        /// <returns>The unique identifier for the current session</returns>
        public string GetSessionId() => sessionId;
    }
} 