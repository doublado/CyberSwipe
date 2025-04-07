package api

import (
	"cyber-swipe-analytics/storage"
	"fmt"
	"io"
	"net/http"
	"os"
	"time"

	"bytes"

	"database/sql"

	"github.com/gin-gonic/gin"
)

// AnalyticsHandler handles all analytics-related HTTP requests.
// It provides methods for session management, event recording,
// and statistics retrieval.
type AnalyticsHandler struct {
	db *storage.DB
}

// SetupRoutes configures all HTTP routes for the analytics server.
// It sets up endpoints for health checks, session management,
// event recording, and statistics retrieval.
func SetupRoutes(router *gin.Engine, db *storage.DB) {
	handler := &AnalyticsHandler{db: db}

	// Health check endpoint (no authentication required)
	router.GET("/health", HealthCheck)

	// Analytics API endpoints group
	analytics := router.Group("/api/analytics")
	{
		// Session management endpoints
		analytics.POST("/session", handler.createSession)
		analytics.POST("/session/end", handler.endSession)

		// Event recording endpoints
		analytics.POST("/event", handler.recordEvent)
		analytics.POST("/performance", handler.recordPerformanceMetrics)
		analytics.POST("/category", handler.recordCategoryStats)

		// Statistics retrieval endpoint
		analytics.GET("/stats", handler.getStats)
	}
}

// HealthCheck handles the health check endpoint.
// Returns a simple status response indicating the server is operational.
func HealthCheck(c *gin.Context) {
	c.JSON(http.StatusOK, gin.H{
		"status":  "ok",
		"version": "1.0.0",
	})
}

// SessionRequest represents the data required to create a new analytics session.
type SessionRequest struct {
	SessionID   string `json:"session_id" binding:"required"`
	UserID      string `json:"user_id" binding:"required"`
	Platform    string `json:"platform" binding:"required"`
	Resolution  string `json:"resolution" binding:"required"`
	DeviceModel string `json:"device_model,omitempty"`
	OSVersion   string `json:"os_version,omitempty"`
}

// createSession handles the creation of a new analytics session.
// It validates the incoming request data and creates a new session in the database.
func (h *AnalyticsHandler) createSession(c *gin.Context) {
	var session SessionRequest

	if err := c.ShouldBindJSON(&session); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO sessions (session_id, user_id, platform, resolution, device_model, os_version)
		VALUES (?, ?, ?, ?, ?, ?)
	`, session.SessionID, session.UserID, session.Platform, session.Resolution, session.DeviceModel, session.OSVersion)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create session"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

// EndSessionRequest represents the data required to end an existing analytics session.
type EndSessionRequest struct {
	SessionID string `json:"session_id" binding:"required"`
}

// endSession handles the termination of an existing analytics session.
// It validates the session ID and updates the session's end time in the database.
func (h *AnalyticsHandler) endSession(c *gin.Context) {
	var request EndSessionRequest

	if err := c.ShouldBindJSON(&request); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		UPDATE sessions 
		SET ended_at = CURRENT_TIMESTAMP 
		WHERE session_id = ? AND ended_at IS NULL
	`, request.SessionID)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to end session"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"status": "success"})
}

// EventRequest represents the data required to record a user interaction event.
type EventRequest struct {
	SessionID   string  `json:"session_id" binding:"required"`
	EventType   string  `json:"event_type" binding:"required"`
	CardID      string  `json:"card_id,omitempty"`
	Direction   string  `json:"direction,omitempty"`
	Success     bool    `json:"success,omitempty"`
	Duration    float64 `json:"duration,omitempty"`
	StartX      float64 `json:"start_x,omitempty"`
	EndX        float64 `json:"end_x,omitempty"`
	MaxRotation float64 `json:"max_rotation,omitempty"`
}

// recordEvent handles the recording of a user interaction event.
// It validates the incoming request data and stores the event in the database.
func (h *AnalyticsHandler) recordEvent(c *gin.Context) {
	// Log the raw request body for debugging
	requestBody, err := c.GetRawData()
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Failed to read request body"})
		return
	}

	// Reset the request body for binding
	c.Request.Body = io.NopCloser(bytes.NewBuffer(requestBody))

	var event EventRequest

	if err := c.ShouldBindJSON(&event); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err = h.db.Exec(`
		INSERT INTO events (
			session_id, event_type, card_id, direction, success,
			duration, start_x, end_x, max_rotation
		) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
	`,
		event.SessionID, event.EventType, event.CardID, event.Direction,
		event.Success, event.Duration, event.StartX, event.EndX,
		event.MaxRotation,
	)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record event"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

// PerformanceMetricsRequest represents the data required to record performance metrics.
type PerformanceMetricsRequest struct {
	SessionID      string  `json:"session_id" binding:"required"`
	FPS            float64 `json:"fps,omitempty"`
	MemoryUsage    float64 `json:"memory_usage" binding:"required"`
	CPUUsage       float64 `json:"cpu_usage,omitempty"`
	GPUUsage       float64 `json:"gpu_usage,omitempty"`
	NetworkLatency float64 `json:"network_latency,omitempty"`
}

// recordPerformanceMetrics handles the recording of performance metrics.
// It validates the incoming request data and stores the metrics in the database.
func (h *AnalyticsHandler) recordPerformanceMetrics(c *gin.Context) {
	var metrics PerformanceMetricsRequest

	if err := c.ShouldBindJSON(&metrics); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO performance_metrics (
			session_id, fps, memory_usage, cpu_usage, gpu_usage, network_latency
		) VALUES (?, ?, ?, ?, ?, ?)
	`,
		metrics.SessionID, metrics.FPS, metrics.MemoryUsage,
		metrics.CPUUsage, metrics.GPUUsage, metrics.NetworkLatency,
	)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record performance metrics"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

// CategoryStatsRequest represents the data required to record category statistics.
type CategoryStatsRequest struct {
	SessionID   string  `json:"session_id" binding:"required"`
	Category    string  `json:"category,omitempty"`
	SuccessRate float64 `json:"success_rate,omitempty"`
}

// recordCategoryStats handles the recording of category statistics.
// It validates the incoming request data and stores the statistics in the database.
func (h *AnalyticsHandler) recordCategoryStats(c *gin.Context) {
	var stats CategoryStatsRequest
	if err := c.ShouldBindJSON(&stats); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Check if the session exists
	var sessionExists bool
	err := h.db.QueryRow("SELECT EXISTS(SELECT 1 FROM sessions WHERE session_id = ?)", stats.SessionID).Scan(&sessionExists)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to verify session"})
		return
	}

	if !sessionExists {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Session not found"})
		return
	}

	// Insert or update category stats
	_, err = h.db.Exec(`
		INSERT INTO category_stats (
			session_id, category_name, total_cards, accepted_cards, 
			average_decision_time, completion_time
		) VALUES (?, ?, 1, ?, 0, 0)
		ON DUPLICATE KEY UPDATE
			accepted_cards = accepted_cards + VALUES(accepted_cards),
			total_cards = total_cards + 1
	`,
		stats.SessionID,
		stats.Category,
		stats.SuccessRate,
	)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record category statistics"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

// getStats handles the retrieval of aggregated analytics data.
// It requires admin authentication and returns comprehensive statistics
// about sessions, events, and performance metrics.
func (h *AnalyticsHandler) getStats(c *gin.Context) {
	// Verify admin authentication
	adminSecret := c.GetHeader("X-Admin-Secret")
	if adminSecret == "" {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "Missing admin secret key"})
		return
	}

	if adminSecret != os.Getenv("ADMIN_SECRET_KEY") {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid admin secret key"})
		return
	}

	// Retrieve raw data
	sessionStats, err := h.getSessionStatistics()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get session statistics"})
		return
	}

	performanceStats, err := h.getPerformanceStatistics()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get performance statistics"})
		return
	}

	eventStats, err := h.getEventStatistics()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get event statistics"})
		return
	}

	// Calculate aggregated statistics
	aggregatedStats, err := h.getAggregatedStatistics()
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to calculate aggregated statistics"})
		return
	}

	// Combine all data into a single response
	response := gin.H{
		"raw_data": gin.H{
			"sessions":    sessionStats,
			"performance": performanceStats,
			"events":      eventStats,
		},
		"statistics": aggregatedStats,
	}

	c.JSON(http.StatusOK, response)
}

// getAggregatedStatistics calculates comprehensive aggregated statistics
// from the collected analytics data.
func (h *AnalyticsHandler) getAggregatedStatistics() (gin.H, error) {
	// Session statistics
	var totalSessions int
	err := h.db.QueryRow(`
		SELECT COUNT(*) as total_sessions
		FROM sessions
	`).Scan(&totalSessions)
	if err != nil {
		return nil, fmt.Errorf("error getting session statistics: %v", err)
	}

	// Performance metrics averages
	var avgFPS, avgMemoryUsage, avgCPUUsage, avgGPUUsage, avgNetworkLatency sql.NullFloat64
	err = h.db.QueryRow(`
		SELECT 
			AVG(COALESCE(fps, 0)) as avg_fps,
			AVG(COALESCE(memory_usage, 0)) as avg_memory,
			AVG(COALESCE(cpu_usage, 0)) as avg_cpu,
			AVG(COALESCE(gpu_usage, 0)) as avg_gpu,
			AVG(COALESCE(network_latency, 0)) as avg_network
		FROM performance_metrics
	`).Scan(&avgFPS, &avgMemoryUsage, &avgCPUUsage, &avgGPUUsage, &avgNetworkLatency)
	if err != nil {
		return nil, fmt.Errorf("error getting performance metrics: %v", err)
	}

	// Event statistics
	var totalEvents, totalSwipes, successfulSwipes int
	var avgSwipeDuration, avgSwipeDistance, avgRotation sql.NullFloat64
	err = h.db.QueryRow(`
		SELECT 
			COUNT(*) as total_events,
			COUNT(CASE WHEN event_type = 'card_swipe' THEN 1 END) as total_swipes,
			COUNT(CASE WHEN event_type = 'card_swipe' AND success = true THEN 1 END) as successful_swipes,
			AVG(CASE WHEN event_type = 'card_swipe' THEN COALESCE(duration, 0) ELSE NULL END) as avg_duration,
			AVG(CASE WHEN event_type = 'card_swipe' THEN COALESCE(ABS(end_x - start_x), 0) ELSE NULL END) as avg_distance,
			AVG(CASE WHEN event_type = 'card_swipe' THEN COALESCE(max_rotation, 0) ELSE NULL END) as avg_rotation
		FROM events
	`).Scan(&totalEvents, &totalSwipes, &successfulSwipes, &avgSwipeDuration, &avgSwipeDistance, &avgRotation)
	if err != nil {
		return nil, fmt.Errorf("error getting event statistics: %v", err)
	}

	// Category statistics
	rows, err := h.db.Query(`
		SELECT 
			category_name,
			COALESCE(SUM(total_cards), 0) as total_cards,
			COALESCE(SUM(accepted_cards), 0) as accepted_cards,
			AVG(COALESCE(average_decision_time, 0)) as avg_decision_time,
			AVG(COALESCE(completion_time, 0)) as avg_completion_time,
			COUNT(DISTINCT session_id) as unique_sessions
		FROM category_stats
		GROUP BY category_name
		ORDER BY total_cards DESC
	`)
	if err != nil {
		return nil, fmt.Errorf("error getting category statistics: %v", err)
	}
	defer rows.Close()

	var categoryStats []map[string]interface{}
	for rows.Next() {
		var category string
		var totalCards, acceptedCards, avgDecisionTime, avgCompletionTime float64
		var uniqueSessions int
		if err := rows.Scan(&category, &totalCards, &acceptedCards, &avgDecisionTime, &avgCompletionTime, &uniqueSessions); err != nil {
			return nil, fmt.Errorf("error scanning category statistics: %v", err)
		}
		successRate := 0.0
		if totalCards > 0 {
			successRate = (acceptedCards / totalCards) * 100
		}
		categoryStats = append(categoryStats, map[string]interface{}{
			"category":            category,
			"total_cards":         totalCards,
			"accepted_cards":      acceptedCards,
			"success_rate":        successRate,
			"avg_decision_time":   avgDecisionTime,
			"avg_completion_time": avgCompletionTime,
			"unique_sessions":     uniqueSessions,
		})
	}

	// Platform distribution
	rows, err = h.db.Query(`
		SELECT 
			platform,
			COUNT(*) as total_sessions,
			COUNT(DISTINCT user_id) as unique_users
		FROM sessions
		GROUP BY platform
		ORDER BY total_sessions DESC
	`)
	if err != nil {
		return nil, fmt.Errorf("error getting platform statistics: %v", err)
	}
	defer rows.Close()

	var platformStats []map[string]interface{}
	for rows.Next() {
		var platform string
		var totalSessions, uniqueUsers int
		if err := rows.Scan(&platform, &totalSessions, &uniqueUsers); err != nil {
			return nil, fmt.Errorf("error scanning platform statistics: %v", err)
		}
		platformStats = append(platformStats, map[string]interface{}{
			"platform":       platform,
			"total_sessions": totalSessions,
			"unique_users":   uniqueUsers,
		})
	}

	// Calculate swipe success rate (handle division by zero)
	swipeSuccessRate := 0.0
	if totalSwipes > 0 {
		swipeSuccessRate = float64(successfulSwipes) / float64(totalSwipes) * 100
	}

	return gin.H{
		"sessions": gin.H{
			"total_sessions": totalSessions,
		},
		"performance": gin.H{
			"avg_fps":             avgFPS.Float64,
			"avg_memory_usage":    avgMemoryUsage.Float64,
			"avg_cpu_usage":       avgCPUUsage.Float64,
			"avg_gpu_usage":       avgGPUUsage.Float64,
			"avg_network_latency": avgNetworkLatency.Float64,
		},
		"events": gin.H{
			"total_events":       totalEvents,
			"total_swipes":       totalSwipes,
			"successful_swipes":  successfulSwipes,
			"swipe_success_rate": swipeSuccessRate,
			"avg_swipe_duration": avgSwipeDuration.Float64,
			"avg_swipe_distance": avgSwipeDistance.Float64,
			"avg_rotation":       avgRotation.Float64,
		},
		"categories": categoryStats,
		"platforms":  platformStats,
	}, nil
}

// getSessionStatistics retrieves aggregated statistics about user sessions.
func (h *AnalyticsHandler) getSessionStatistics() ([]map[string]interface{}, error) {
	rows, err := h.db.Query(`
		SELECT 
			session_id,
			user_id,
			platform,
			resolution,
			device_model,
			os_version,
			created_at
		FROM sessions
		ORDER BY created_at DESC
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var sessions []map[string]interface{}
	for rows.Next() {
		var sessionID, userID, platform, resolution, deviceModel, osVersion string
		var createdAt time.Time
		if err := rows.Scan(&sessionID, &userID, &platform, &resolution, &deviceModel, &osVersion, &createdAt); err != nil {
			return nil, err
		}
		sessions = append(sessions, map[string]interface{}{
			"session_id":   sessionID,
			"user_id":      userID,
			"platform":     platform,
			"resolution":   resolution,
			"device_model": deviceModel,
			"os_version":   osVersion,
			"created_at":   createdAt,
		})
	}

	return sessions, nil
}

// getPerformanceStatistics retrieves aggregated statistics about performance metrics.
func (h *AnalyticsHandler) getPerformanceStatistics() ([]map[string]interface{}, error) {
	rows, err := h.db.Query(`
		SELECT 
			session_id,
			fps,
			memory_usage,
			cpu_usage,
			gpu_usage,
			network_latency,
			timestamp
		FROM performance_metrics
		ORDER BY timestamp DESC
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var metrics []map[string]interface{}
	for rows.Next() {
		var sessionID string
		var fps, memoryUsage, cpuUsage, gpuUsage, networkLatency float64
		var timestamp time.Time
		if err := rows.Scan(&sessionID, &fps, &memoryUsage, &cpuUsage, &gpuUsage, &networkLatency, &timestamp); err != nil {
			return nil, err
		}
		metrics = append(metrics, map[string]interface{}{
			"session_id":      sessionID,
			"fps":             fps,
			"memory_usage":    memoryUsage,
			"cpu_usage":       cpuUsage,
			"gpu_usage":       gpuUsage,
			"network_latency": networkLatency,
			"timestamp":       timestamp,
		})
	}

	return metrics, nil
}

// getEventStatistics retrieves aggregated statistics about user events.
func (h *AnalyticsHandler) getEventStatistics() ([]map[string]interface{}, error) {
	rows, err := h.db.Query(`
		SELECT 
			session_id,
			event_type,
			card_id,
			direction,
			success,
			duration,
			start_x,
			end_x,
			max_rotation,
			created_at
		FROM events
		ORDER BY created_at DESC
	`)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var events []map[string]interface{}
	for rows.Next() {
		var sessionID, eventType, cardID, direction string
		var success bool
		var duration, startX, endX, maxRotation float64
		var createdAt time.Time
		if err := rows.Scan(&sessionID, &eventType, &cardID, &direction, &success, &duration, &startX, &endX, &maxRotation, &createdAt); err != nil {
			return nil, err
		}
		events = append(events, map[string]interface{}{
			"session_id":   sessionID,
			"event_type":   eventType,
			"card_id":      cardID,
			"direction":    direction,
			"success":      success,
			"duration":     duration,
			"start_x":      startX,
			"end_x":        endX,
			"max_rotation": maxRotation,
			"created_at":   createdAt,
		})
	}

	return events, nil
}
