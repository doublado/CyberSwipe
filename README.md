# CyberSwipe

CyberSwipe is an educational card game designed to teach cybersecurity awareness through interactive scenarios. Players must make quick decisions about whether to accept or reject various cybersecurity-related situations by swiping cards left or right.

## Project Structure

The project consists of two main components:

1. **Unity Game Client** (`/Assets`)
   - Card-based gameplay mechanics
   - Analytics tracking system
   - Performance monitoring
   - User interface and interactions

2. **Analytics Server** (`/analytics-server`)
   - Go-based backend server
   - MariaDB database
   - RESTful API endpoints
   - Analytics data collection and processing

## Features

### Game Features
- Interactive card swiping mechanics
- Multiple cybersecurity categories
- Real-time performance monitoring
- Analytics consent management
- Session tracking
- Category-based progression

### Analytics Features
- Session management
- Event tracking
- Performance metrics
- Category statistics
- Comprehensive analytics dashboard
- Secure API endpoints

## Getting Started

### Prerequisites
- Unity 2022.3 or later
- Go 1.21 or later
- MariaDB 10.6 or later
- Git

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/CyberSwipe.git
   cd CyberSwipe
   ```

2. Set up the analytics server:
   ```bash
   cd analytics-server
   cp .env.example .env
   # Edit .env with your configuration
   go mod tidy
   go run main.go
   ```

3. Open the Unity project:
   - Launch Unity Hub
   - Add the project folder
   - Open the project with Unity 2022.3 or later

4. Configure the analytics settings:
   - Open `AnalyticsSettings` in the Unity Editor
   - Set the server URL to match your analytics server
   - Configure other settings as needed

## Development

### Game Development
- The game client is developed in Unity using C#
- Main scripts are located in `/Assets/Scripts`
- Card data and settings are configured in the Unity Editor
- Analytics integration is handled through the `AnalyticsService` class

### Server Development
- The analytics server is written in Go
- API endpoints are defined in `/analytics-server/api`
- Database schema is managed through `/analytics-server/storage`
- Configuration is handled through environment variables

## Analytics Integration

The game client automatically tracks:
- Session start/end
- Card interactions
- Performance metrics
- Category completion
- User decisions

All analytics data is anonymized and requires user consent.

## Security

- All user data is anonymized
- No personal information is collected
- JWT-based authentication for API endpoints
- Secure database connections
- Regular security updates

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Unity Technologies for the game engine
- Go team for the programming language
- MariaDB team for the database system
- All contributors and supporters of the project 