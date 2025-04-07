package main

import (
	"log"
	"os"

	"cyber-swipe-analytics/api"
	"cyber-swipe-analytics/config"
	"cyber-swipe-analytics/storage"

	"github.com/gin-gonic/gin"
	"github.com/joho/godotenv"
)

// main is the entry point of the analytics server application.
// It initializes the server configuration, database connection,
// and sets up the HTTP routes with middleware.
func main() {
	// Load environment variables from .env file if it exists
	if err := godotenv.Load(); err != nil {
		log.Printf("Warning: .env file not found")
	}

	// Load server configuration from environment variables
	serverConfig, err := config.Load()
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Initialize database connection with the loaded configuration
	database, err := storage.InitDB(serverConfig)
	if err != nil {
		log.Fatalf("Failed to initialize database: %v", err)
	}
	defer database.Close()

	// Create and configure the HTTP router
	router := gin.Default()

	router.SetTrustedProxies([]string{"127.0.0.1"}) // Only trust localhost for security

	// Add CORS middleware to allow cross-origin requests
	router.Use(func(c *gin.Context) {
		c.Writer.Header().Set("Access-Control-Allow-Origin", "*")
		c.Writer.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
		c.Writer.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
		if c.Request.Method == "OPTIONS" {
			c.AbortWithStatus(204)
			return
		}
		c.Next()
	})

	// Register all API routes with the router
	api.SetupRoutes(router, database)

	// Start the HTTP server on the configured port
	serverPort := os.Getenv("PORT")
	if serverPort == "" {
		serverPort = "8080" // Default port if not specified
	}
	log.Printf("Server starting on port %s", serverPort)
	if err := router.Run(":" + serverPort); err != nil {
		log.Fatalf("Failed to start server: %v", err)
	}
}
