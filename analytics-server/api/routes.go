package api

import (
	"cyber-swipe-analytics/storage"
	"fmt"
	"io"
	"net/http"

	"bytes"

	"github.com/gin-gonic/gin"
)

type AnalyticsHandler struct {
	db *storage.DB
}

// HealthCheck handles the health check endpoint
func HealthCheck(c *gin.Context) {
	c.JSON(http.StatusOK, gin.H{
		"status":  "ok",
		"version": "1.0.0",
	})
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
	// Example: Get average swipe duration
	var avgDuration float64
	err := h.db.QueryRow(`
		SELECT AVG(duration)
		FROM events
		WHERE event_type = 'card_swipe'
	`).Scan(&avgDuration)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to get statistics"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"average_swipe_duration": avgDuration,
		// Add more statistics as needed
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
