-- Create the database if it doesn't exist
CREATE DATABASE IF NOT EXISTS cyber_swipe_analytics 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- Create a new user for the analytics application
CREATE USER IF NOT EXISTS 'your_username'@'localhost' 
IDENTIFIED BY 'your_password';

-- Grant all privileges on the analytics database to the new user
GRANT ALL PRIVILEGES ON cyber_swipe_analytics.* 
TO 'your_username'@'localhost';

-- Flush privileges to apply changes
FLUSH PRIVILEGES;

-- Switch to the analytics database
USE cyber_swipe_analytics;

-- Create sessions table
CREATE TABLE IF NOT EXISTS sessions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    session_id VARCHAR(255) NOT NULL UNIQUE,
    user_id VARCHAR(255) NOT NULL,
    platform VARCHAR(50) NOT NULL,
    resolution VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create events table
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci; 