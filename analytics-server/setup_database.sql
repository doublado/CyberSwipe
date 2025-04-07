-- Create the database if it doesn't exist
CREATE DATABASE IF NOT EXISTS cyber_swipe_analytics 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- Create a new user for the analytics application
CREATE USER IF NOT EXISTS 'analytics_user'@'localhost' 
IDENTIFIED BY 'your_password';

-- Grant all privileges on the analytics database to the new user
GRANT ALL PRIVILEGES ON cyber_swipe_analytics.* 
TO 'analytics_user'@'localhost';

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
    device_model VARCHAR(100),
    os_version VARCHAR(50),
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
    end_x FLOAT,
    max_rotation FLOAT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES sessions(session_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create performance_metrics table
CREATE TABLE IF NOT EXISTS performance_metrics (
    id INT AUTO_INCREMENT PRIMARY KEY,
    session_id VARCHAR(255) NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    fps FLOAT,
    memory_usage BIGINT,
    cpu_usage FLOAT,
    gpu_usage FLOAT,
    network_latency INT,
    FOREIGN KEY (session_id) REFERENCES sessions(session_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create category_stats table
CREATE TABLE IF NOT EXISTS category_stats (
    id INT AUTO_INCREMENT PRIMARY KEY,
    session_id VARCHAR(255) NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    total_cards INT DEFAULT 0,
    accepted_cards INT DEFAULT 0,
    rejected_cards INT DEFAULT 0,
    average_decision_time FLOAT DEFAULT 0,
    completion_time INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES sessions(session_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci; 