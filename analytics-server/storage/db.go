package storage

import (
	"cyber-swipe-analytics/config"
	"database/sql"
	"fmt"

	_ "github.com/go-sql-driver/mysql"
)

// DB wraps the sql.DB type to provide database operations
// for the analytics server.
type DB struct {
	*sql.DB
}

// InitDB initializes a new database connection using the provided configuration.
// It establishes the connection, verifies it's working, and creates necessary tables.
// Returns a DB instance or an error if initialization fails.
func InitDB(cfg *config.Config) (*DB, error) {
	// Format the connection string for MySQL
	connectionString := fmt.Sprintf("%s:%s@tcp(%s:%s)/%s?parseTime=true",
		cfg.DBUser, cfg.DBPassword, cfg.DBHost, cfg.DBPort, cfg.DBName)

	// Open a new database connection
	database, err := sql.Open("mysql", connectionString)
	if err != nil {
		return nil, fmt.Errorf("error opening database: %v", err)
	}

	// Verify the connection is working
	if err := database.Ping(); err != nil {
		return nil, fmt.Errorf("error connecting to database: %v", err)
	}

	// Create required tables if they don't exist
	if err := createTables(database); err != nil {
		return nil, fmt.Errorf("error creating tables: %v", err)
	}

	return &DB{database}, nil
}

// createTables creates the necessary database tables for the analytics system.
// It creates tables for sessions and events if they don't already exist.
func createTables(database *sql.DB) error {
	// Create the sessions table to store user session information
	_, err := database.Exec(`
		CREATE TABLE IF NOT EXISTS sessions (
			id INT AUTO_INCREMENT PRIMARY KEY,
			session_id VARCHAR(255) NOT NULL UNIQUE,
			user_id VARCHAR(255) NOT NULL,
			platform VARCHAR(50) NOT NULL,
			resolution VARCHAR(50) NOT NULL,
			created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
		) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
	`)
	if err != nil {
		return err
	}

	// Create the events table to store user interaction events
	_, err = database.Exec(`
		CREATE TABLE IF NOT EXISTS events (
			id INT AUTO_INCREMENT PRIMARY KEY,
			session_id VARCHAR(255) NOT NULL,
			event_type VARCHAR(50) NOT NULL,
			card_id VARCHAR(255),
			direction VARCHAR(10),
			success BOOLEAN,
			duration FLOAT,
			start_x FLOAT,
			start_y FLOAT,
			end_x FLOAT,
			end_y FLOAT,
			max_rotation FLOAT,
			fps FLOAT,
			memory_usage BIGINT,
			created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
			FOREIGN KEY (session_id) REFERENCES sessions(session_id) ON DELETE CASCADE
		) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
	`)
	if err != nil {
		return err
	}

	return nil
}
