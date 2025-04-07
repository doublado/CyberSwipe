package api

import (
	"cyber-swipe-analytics/storage"
	"fmt"
	"io"
	"net/http"
	"os"
	"time"

	"bytes"

	"github.com/gin-gonic/gin"
)

type AnalyticsHandler struct {
	db *storage.DB
}

func SetupRoutes(router *gin.Engine, db *storage.DB) {
	handler := &AnalyticsHandler{db: db}

	// Health check endpoint (no auth required)
	router.GET("/health", HealthCheck)

	// Analytics endpoints
	analytics := router.Group("/api/analytics")
	{
		analytics.POST("/session", handler.createSession)
		analytics.POST("/session/end", handler.endSession)
		analytics.POST("/event", handler.recordEvent)
		analytics.POST("/performance", handler.recordPerformanceMetrics)
		analytics.POST("/category", handler.recordCategoryStats)
		analytics.GET("/stats", handler.getStats)
	}
}

// HealthCheck handles the health check endpoint
func HealthCheck(c *gin.Context) {
	c.JSON(http.StatusOK, gin.H{
		"status":  "ok",
		"version": "1.0.0",
	})
}

func (h *AnalyticsHandler) createSession(c *gin.Context) {
	var session struct {
		SessionID   string `json:"session_id" binding:"required"`
		UserID      string `json:"user_id" binding:"required"`
		Platform    string `json:"platform" binding:"required"`
		Resolution  string `json:"resolution" binding:"required"`
		DeviceModel string `json:"device_model,omitempty"`
		OSVersion   string `json:"os_version,omitempty"`
	}

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

func (h *AnalyticsHandler) recordEvent(c *gin.Context) {
	// Log the raw request body
	body, err := c.GetRawData()
	if err != nil {
		fmt.Printf("[Server] Error reading request body: %v\n", err)
		c.JSON(http.StatusBadRequest, gin.H{"error": "Failed to read request body"})
		return
	}
	fmt.Printf("[Server] Received request body: %s\n", string(body))

	// Reset the request body for binding
	c.Request.Body = io.NopCloser(bytes.NewBuffer(body))

	var event struct {
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

	if err := c.ShouldBindJSON(&event); err != nil {
		fmt.Printf("[Server] Validation error: %v\n", err)
		fmt.Printf("[Server] Event data: %+v\n", event)
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	fmt.Printf("[Server] Successfully parsed event: %+v\n", event)

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
		fmt.Printf("[Server] Database error: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record event"})
		return
	}

	fmt.Printf("[Server] Successfully recorded event\n")
	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

func (h *AnalyticsHandler) getStats(c *gin.Context) {
	// Get the secret key from the Authorization header
	secretKey := c.GetHeader("X-Admin-Secret")
	if secretKey == "" {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "Missing admin secret key"})
		return
	}

	// Verify the secret key
	if secretKey != os.Getenv("ADMIN_SECRET_KEY") {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid admin secret key"})
		return
	}

	// Get all sessions
	sessionRows, err := h.db.Query(`
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
		fmt.Printf("[Server] Error getting sessions: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get sessions"})
		return
	}
	defer sessionRows.Close()

	var sessions []map[string]interface{}
	for sessionRows.Next() {
		var sessionID, userID, platform, resolution, deviceModel, osVersion string
		var createdAt time.Time
		err := sessionRows.Scan(&sessionID, &userID, &platform, &resolution, &deviceModel, &osVersion, &createdAt)
		if err != nil {
			fmt.Printf("[Server] Error scanning session row: %v\n", err)
			continue
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

	// Get all performance metrics
	perfRows, err := h.db.Query(`
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
		fmt.Printf("[Server] Error getting performance metrics: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get performance metrics"})
		return
	}
	defer perfRows.Close()

	var performanceMetrics []map[string]interface{}
	for perfRows.Next() {
		var sessionID string
		var fps, memoryUsage, cpuUsage, gpuUsage, networkLatency float64
		var timestamp time.Time
		err := perfRows.Scan(&sessionID, &fps, &memoryUsage, &cpuUsage, &gpuUsage, &networkLatency, &timestamp)
		if err != nil {
			fmt.Printf("[Server] Error scanning performance row: %v\n", err)
			continue
		}
		performanceMetrics = append(performanceMetrics, map[string]interface{}{
			"session_id":      sessionID,
			"fps":             fps,
			"memory_usage":    memoryUsage,
			"cpu_usage":       cpuUsage,
			"gpu_usage":       gpuUsage,
			"network_latency": networkLatency,
			"timestamp":       timestamp,
		})
	}

	// Get all events
	eventRows, err := h.db.Query(`
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
		fmt.Printf("[Server] Error getting events: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get events"})
		return
	}
	defer eventRows.Close()

	var events []map[string]interface{}
	for eventRows.Next() {
		var sessionID, eventType, cardID, direction string
		var success bool
		var duration, startX, endX, maxRotation float64
		var createdAt time.Time
		err := eventRows.Scan(&sessionID, &eventType, &cardID, &direction, &success, &duration, &startX, &endX, &maxRotation, &createdAt)
		if err != nil {
			fmt.Printf("[Server] Error scanning event row: %v\n", err)
			continue
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

	// Get all category stats
	categoryRows, err := h.db.Query(`
		SELECT 
			session_id,
			category_name,
			total_cards,
			accepted_cards,
			rejected_cards,
			average_decision_time,
			completion_time,
			created_at
		FROM category_stats
		ORDER BY created_at DESC
	`)
	if err != nil {
		fmt.Printf("[Server] Error getting category stats: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get category stats"})
		return
	}
	defer categoryRows.Close()

	var categoryStats []map[string]interface{}
	for categoryRows.Next() {
		var sessionID, categoryName string
		var totalCards, acceptedCards, rejectedCards int
		var avgDecisionTime, completionTime float64
		var createdAt time.Time
		err := categoryRows.Scan(&sessionID, &categoryName, &totalCards, &acceptedCards, &rejectedCards, &avgDecisionTime, &completionTime, &createdAt)
		if err != nil {
			fmt.Printf("[Server] Error scanning category row: %v\n", err)
			continue
		}
		categoryStats = append(categoryStats, map[string]interface{}{
			"session_id":            sessionID,
			"category_name":         categoryName,
			"total_cards":           totalCards,
			"accepted_cards":        acceptedCards,
			"rejected_cards":        rejectedCards,
			"average_decision_time": avgDecisionTime,
			"completion_time":       completionTime,
			"created_at":            createdAt,
		})
	}

	// Get aggregated statistics
	var totalSessions int
	var avgFPS, avgMemoryUsage, avgCPUUsage, avgGPUUsage, avgNetworkLatency float64
	var totalSwipes int
	var avgSwipeDuration, successRate float64

	// Get session count
	err = h.db.QueryRow(`SELECT COUNT(*) FROM sessions`).Scan(&totalSessions)
	if err != nil {
		fmt.Printf("[Server] Error getting total sessions: %v\n", err)
	}

	// Get average performance metrics
	err = h.db.QueryRow(`
		SELECT 
			AVG(fps),
			AVG(memory_usage),
			AVG(cpu_usage),
			AVG(gpu_usage),
			AVG(network_latency)
		FROM performance_metrics
	`).Scan(&avgFPS, &avgMemoryUsage, &avgCPUUsage, &avgGPUUsage, &avgNetworkLatency)
	if err != nil {
		fmt.Printf("[Server] Error getting average performance metrics: %v\n", err)
	}

	// Get swipe statistics
	err = h.db.QueryRow(`
		SELECT 
			COUNT(*) as total_swipes,
			AVG(duration) as avg_duration,
			AVG(CASE WHEN success THEN 1 ELSE 0 END) * 100 as success_rate
		FROM events
		WHERE event_type = 'card_swipe'
	`).Scan(&totalSwipes, &avgSwipeDuration, &successRate)
	if err != nil {
		fmt.Printf("[Server] Error getting swipe statistics: %v\n", err)
	}

	// Return comprehensive data
	c.JSON(http.StatusOK, gin.H{
		"raw_data": gin.H{
			"sessions":            sessions,
			"performance_metrics": performanceMetrics,
			"events":              events,
			"category_stats":      categoryStats,
		},
		"statistics": gin.H{
			"sessions": gin.H{
				"total_sessions": totalSessions,
			},
			"performance": gin.H{
				"avg_fps":             avgFPS,
				"avg_memory_usage":    avgMemoryUsage,
				"avg_cpu_usage":       avgCPUUsage,
				"avg_gpu_usage":       avgGPUUsage,
				"avg_network_latency": avgNetworkLatency,
			},
			"card_swipes": gin.H{
				"total_swipes":       totalSwipes,
				"avg_swipe_duration": avgSwipeDuration,
				"success_rate":       successRate,
			},
		},
	})
}

func (h *AnalyticsHandler) recordPerformanceMetrics(c *gin.Context) {
	var metrics struct {
		SessionID      string  `json:"session_id" binding:"required"`
		FPS            float64 `json:"fps"`
		MemoryUsage    int64   `json:"memory_usage"`
		CPUUsage       float64 `json:"cpu_usage"`
		GPUUsage       float64 `json:"gpu_usage"`
		NetworkLatency int     `json:"network_latency"`
	}

	if err := c.ShouldBindJSON(&metrics); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO performance_metrics (session_id, fps, memory_usage, cpu_usage, gpu_usage, network_latency)
		VALUES (?, ?, ?, ?, ?, ?)
	`, metrics.SessionID, metrics.FPS, metrics.MemoryUsage, metrics.CPUUsage, metrics.GPUUsage, metrics.NetworkLatency)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record performance metrics"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

func (h *AnalyticsHandler) recordCategoryStats(c *gin.Context) {
	var stats struct {
		SessionID           string  `json:"session_id" binding:"required"`
		CategoryName        string  `json:"category_name" binding:"required"`
		TotalCards          int     `json:"total_cards"`
		AcceptedCards       int     `json:"accepted_cards"`
		RejectedCards       int     `json:"rejected_cards"`
		AverageDecisionTime float64 `json:"average_decision_time"`
		CompletionTime      int     `json:"completion_time"`
	}

	if err := c.ShouldBindJSON(&stats); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO category_stats (session_id, category_name, total_cards, accepted_cards, rejected_cards, average_decision_time, completion_time)
		VALUES (?, ?, ?, ?, ?, ?, ?)
	`, stats.SessionID, stats.CategoryName, stats.TotalCards, stats.AcceptedCards, stats.RejectedCards, stats.AverageDecisionTime, stats.CompletionTime)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record category stats"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

func (h *AnalyticsHandler) endSession(c *gin.Context) {
	// Log the raw request body
	body, err := c.GetRawData()
	if err != nil {
		fmt.Printf("[Server] Error reading request body: %v\n", err)
		c.JSON(http.StatusBadRequest, gin.H{"error": "Failed to read request body"})
		return
	}
	fmt.Printf("[Server] Received session end request body: %s\n", string(body))

	// Reset the request body for binding
	c.Request.Body = io.NopCloser(bytes.NewBuffer(body))

	var session struct {
		SessionID                string  `json:"session_id" binding:"required"`
		SessionDuration          int     `json:"session_duration"`
		TotalCardsProcessed      int     `json:"total_cards_processed"`
		TotalCategoriesCompleted int     `json:"total_categories_completed"`
		AverageDecisionTime      float64 `json:"average_decision_time"`
		SuccessRate              float64 `json:"success_rate"`
	}

	if err := c.ShouldBindJSON(&session); err != nil {
		fmt.Printf("[Server] Error binding session end data: %v\n", err)
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	fmt.Printf("[Server] Parsed session end data: %+v\n", session)

	// First check if the session exists
	var exists bool
	err = h.db.QueryRow("SELECT EXISTS(SELECT 1 FROM sessions WHERE session_id = ?)", session.SessionID).Scan(&exists)
	if err != nil {
		fmt.Printf("[Server] Error checking session existence: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to check session existence"})
		return
	}

	if !exists {
		fmt.Printf("[Server] Session not found: %s\n", session.SessionID)
		c.JSON(http.StatusNotFound, gin.H{"error": "Session not found"})
		return
	}

	// Update the session
	result, err := h.db.Exec(`
		UPDATE sessions 
		SET ended_at = CURRENT_TIMESTAMP,
			session_duration = ?,
			total_cards_processed = ?,
			total_categories_completed = ?,
			average_decision_time = ?,
			success_rate = ?
		WHERE session_id = ? AND ended_at IS NULL
	`, session.SessionDuration, session.TotalCardsProcessed, session.TotalCategoriesCompleted,
		session.AverageDecisionTime, session.SuccessRate, session.SessionID)

	if err != nil {
		fmt.Printf("[Server] Error updating session: %v\n", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to update session"})
		return
	}

	rowsAffected, err := result.RowsAffected()
	if err != nil {
		fmt.Printf("[Server] Error getting rows affected: %v\n", err)
	} else {
		fmt.Printf("[Server] Rows affected by update: %d\n", rowsAffected)
	}

	fmt.Printf("[Server] Successfully ended session: %s\n", session.SessionID)
	c.JSON(http.StatusOK, gin.H{"status": "success"})
}
