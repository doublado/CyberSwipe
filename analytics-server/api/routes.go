package api

import (
	"cyber-swipe-analytics/storage"
	"net/http"

	"github.com/gin-gonic/gin"
)

type AnalyticsHandler struct {
	db *storage.DB
}

func SetupRoutes(router *gin.Engine, db *storage.DB) {
	handler := &AnalyticsHandler{db: db}

	// Analytics endpoints
	analytics := router.Group("/api/analytics")
	{
		analytics.POST("/session", handler.createSession)
		analytics.POST("/event", handler.recordEvent)
		analytics.GET("/stats", handler.getStats)
	}
}

func (h *AnalyticsHandler) createSession(c *gin.Context) {
	var session struct {
		SessionID  string `json:"session_id" binding:"required"`
		UserID     string `json:"user_id" binding:"required"`
		Platform   string `json:"platform" binding:"required"`
		Resolution string `json:"resolution" binding:"required"`
	}

	if err := c.ShouldBindJSON(&session); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO sessions (session_id, user_id, platform, resolution)
		VALUES ($1, $2, $3, $4)
	`, session.SessionID, session.UserID, session.Platform, session.Resolution)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create session"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{"status": "success"})
}

func (h *AnalyticsHandler) recordEvent(c *gin.Context) {
	var event struct {
		SessionID   string  `json:"session_id" binding:"required"`
		EventType   string  `json:"event_type" binding:"required"`
		CardID      string  `json:"card_id,omitempty"`
		Direction   string  `json:"direction,omitempty"`
		Success     bool    `json:"success,omitempty"`
		Duration    float64 `json:"duration,omitempty"`
		StartX      float64 `json:"start_x,omitempty"`
		StartY      float64 `json:"start_y,omitempty"`
		EndX        float64 `json:"end_x,omitempty"`
		EndY        float64 `json:"end_y,omitempty"`
		MaxRotation float64 `json:"max_rotation,omitempty"`
		FPS         float64 `json:"fps,omitempty"`
		MemoryUsage int64   `json:"memory_usage,omitempty"`
	}

	if err := c.ShouldBindJSON(&event); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	_, err := h.db.Exec(`
		INSERT INTO events (
			session_id, event_type, card_id, direction, success,
			duration, start_x, start_y, end_x, end_y,
			max_rotation, fps, memory_usage
		) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
	`,
		event.SessionID, event.EventType, event.CardID, event.Direction,
		event.Success, event.Duration, event.StartX, event.StartY,
		event.EndX, event.EndY, event.MaxRotation, event.FPS,
		event.MemoryUsage,
	)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to record event"})
		return
	}

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
