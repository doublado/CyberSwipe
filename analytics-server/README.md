# CyberSwipe Analytics Server

This is the backend server for collecting and analyzing user interaction data from the CyberSwipe game. It provides a comprehensive analytics system for tracking user sessions, card interactions, performance metrics, and category statistics.

## Features

- Session management (create/end sessions)
- Event tracking (card swipes, interactions)
- Performance monitoring (FPS, memory usage)
- Category statistics tracking
- Comprehensive analytics dashboard
- Secure API endpoints with JWT authentication

## Setup

1. Install MariaDB if you haven't already
2. Create a new database:
   ```sql
   CREATE DATABASE cyber_swipe_analytics CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

3. Install Go dependencies:
   ```bash
   go mod tidy
   ```

4. Copy the environment file:
   ```bash
   cp .env.example .env
   ```

5. Update the `.env` file with your configuration:
   ```
   # Database Configuration
   DB_HOST=localhost
   DB_PORT=3306
   DB_USER=your-username
   DB_PASSWORD=your-password
   DB_NAME=cyber_swipe_analytics

   # Server Configuration
   PORT=8080
   JWT_SECRET=your-secret-key
   ```

6. Run the server:
   ```bash
   go run main.go
   ```

## API Endpoints

### Health Check
```
GET /health
```
Returns the server's health status.

### Session Management

#### Create Session
```
POST /api/analytics/session
```
Creates a new analytics session for a user.

Request body:
```json
{
    "userId": "anonymous-user-id",
    "deviceId": "device-identifier",
    "platform": "Windows",
    "version": "1.0.0",
    "timestamp": "2024-04-07T10:00:00Z"
}
```

#### End Session
```
POST /api/analytics/session/end
```
Ends an existing analytics session.

Request body:
```json
{
    "sessionId": "unique-session-id",
    "timestamp": "2024-04-07T11:00:00Z"
}
```

### Event Recording

#### Record Event
```
POST /api/analytics/event
```
Records a user interaction event.

Request body:
```json
{
    "sessionId": "unique-session-id",
    "eventType": "card_swipe",
    "cardId": "card-123",
    "direction": "right",
    "success": true,
    "duration": 1.5,
    "startX": 100,
    "startY": 200,
    "endX": 500,
    "endY": 200,
    "maxRotation": 30,
    "fps": 60,
    "memoryUsage": 1024,
    "timestamp": "2024-04-07T10:30:00Z"
}
```

#### Record Performance Metrics
```
POST /api/analytics/performance
```
Records performance metrics for a session.

Request body:
```json
{
    "sessionId": "unique-session-id",
    "fps": 60,
    "memoryUsage": 1024,
    "timestamp": "2024-04-07T10:30:00Z"
}
```

#### Record Category Stats
```
POST /api/analytics/category
```
Records statistics for a specific category.

Request body:
```json
{
    "sessionId": "unique-session-id",
    "category": "Phishing",
    "successRate": 0.85,
    "timestamp": "2024-04-07T10:30:00Z"
}
```

### Statistics

#### Get Analytics Statistics
```
GET /api/analytics/stats
```
Returns aggregated analytics data.

Response:
```json
{
    "totalSessions": 100,
    "averageSessionTime": 1800,
    "totalEvents": 500,
    "successRate": 0.85,
    "averageFPS": 60,
    "averageMemoryUsage": 1024,
    "categoryStats": [
        {
            "category": "Phishing",
            "totalCards": 50,
            "successRate": 0.85,
            "averageTime": 1.5
        }
    ]
}
```

## Data Collection

The server collects the following types of data:

1. Session Information:
   - Session ID
   - User ID (anonymous)
   - Device ID
   - Platform
   - Version
   - Start/End timestamps

2. Card Interaction Events:
   - Swipe direction
   - Success/failure
   - Duration
   - Start/end positions
   - Maximum rotation
   - Performance metrics (FPS, memory usage)

3. Performance Metrics:
   - Frames per second
   - Memory usage
   - Timestamp

4. Category Statistics:
   - Category name
   - Success rate
   - Total cards processed
   - Average decision time

## Privacy

- All user data is anonymized
- No personal information is collected
- Data is used solely for improving the game's UI/UX
- Users must consent to data collection before it begins
- Data is stored securely in a MariaDB database
- Regular data retention policies are implemented

## Security

- JWT-based authentication for API endpoints
- CORS protection for cross-origin requests
- Input validation and sanitization
- Secure database connections
- Environment-based configuration
- Regular security updates

## Development

To contribute to the project:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 