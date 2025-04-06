# CyberSwipe Analytics Server

This is the backend server for collecting and analyzing user interaction data from the CyberSwipe game.

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

5. Update the `.env` file with your MariaDB credentials:
   ```
   DB_HOST=localhost
   DB_PORT=3306
   DB_USER=your-username
   DB_PASSWORD=your-password
   DB_NAME=cyber_swipe_analytics
   ```

6. Run the server:
   ```bash
   go run main.go
   ```

## API Endpoints

### Create Session
```
POST /api/analytics/session
```
Creates a new analytics session for a user.

Request body:
```json
{
    "session_id": "unique-session-id",
    "user_id": "anonymous-user-id",
    "platform": "Windows",
    "resolution": "1920x1080"
}
```

### Record Event
```
POST /api/analytics/event
```
Records a user interaction event.

Request body:
```json
{
    "session_id": "unique-session-id",
    "event_type": "card_swipe",
    "card_id": "card-123",
    "direction": "right",
    "success": true,
    "duration": 1.5,
    "start_x": 100,
    "start_y": 200,
    "end_x": 500,
    "end_y": 200,
    "max_rotation": 30,
    "fps": 60,
    "memory_usage": 1024
}
```

### Get Statistics
```
GET /api/analytics/stats
```
Returns aggregated analytics data.

## Data Collection

The server collects the following types of data:

1. Session Information:
   - Session ID
   - User ID (anonymous)
   - Platform
   - Screen Resolution

2. Card Interaction Events:
   - Swipe direction
   - Success/failure
   - Duration
   - Start/end positions
   - Maximum rotation
   - Performance metrics (FPS, memory usage)

## Privacy

- All user data is anonymized
- No personal information is collected
- Data is used solely for improving the game's UI/UX
- Users must consent to data collection before it begins 