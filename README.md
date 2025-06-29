# Windows Desktop Stacks Service

A background service that automatically organizes desktop files into logical groups, similar to macOS Desktop Stacks functionality.

## 🚀 Project Overview

This service monitors the Windows desktop folder and automatically organizes files based on configurable rules:
- **File Type Grouping**: Groups files by extension (images, documents, etc.)
- **Date-based Organization**: Organizes by creation/modification date
- **Size-based Sorting**: Groups by file size categories
- **Custom Rules**: User-defined organization patterns

## 🏗️ Architecture

- **Technology Stack**: .NET 8 Worker Service
- **Deployment**: Docker-ready, Windows Service compatible
- **File Monitoring**: FileSystemWatcher for real-time detection
- **Configuration**: JSON-based with hot reload support
- **Logging**: Structured logging with Serilog

## 📁 Project Structure

```
DesktopStacksService/
├── src/
│   ├── DesktopStacksService/           # Main service project
│   │   ├── Services/                   # Core service implementations
│   │   ├── Models/                     # Data models and DTOs
│   │   ├── Configuration/              # Configuration classes
│   │   └── Extensions/                 # Service extensions
├── docker/
│   └── Dockerfile                      # Docker configuration
├── config/
│   └── appsettings.json               # Service configuration
└── docs/                              # Documentation
```

## 🔧 Development Progress

### ✅ Completed Features
- [x] Project structure setup
- [x] .NET 8 Worker Service foundation
- [x] Configuration management system
- [x] File monitoring service base
- [x] Docker containerization support

### 🚧 In Progress
- [ ] File organization engine
- [ ] Grouping algorithms implementation
- [ ] Stack creation logic

### 📋 Planned Features
- [ ] Multiple organization strategies
- [ ] Performance optimization
- [ ] Windows Service installer
- [ ] Configuration UI (optional tray app)
- [ ] Auto-startup configuration
- [ ] Logging and monitoring
- [ ] Error handling and recovery
- [ ] Unit tests

## 🛠️ Technology Stack

- **.NET 8**: Latest LTS version for performance and features
- **Worker Service**: Background service template
- **FileSystemWatcher**: Real-time file monitoring
- **Serilog**: Structured logging
- **Microsoft.Extensions.Hosting**: Service lifetime management
- **Microsoft.Extensions.Configuration**: Configuration management
- **Docker**: Containerization support

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Windows 10/11 (for desktop monitoring)
- Docker Desktop (optional, for containerization)

### Running the Service

#### Development Mode
```bash
cd src/DesktopStacksService
dotnet run
```

#### As Windows Service
```bash
# Install as Windows Service
sc create "DesktopStacksService" binPath="path\to\service.exe"
sc start "DesktopStacksService"
```

#### Docker Container
```bash
# Build image
docker build -t desktop-stacks-service .

# Run container
docker run -d --name desktop-stacks desktop-stacks-service
```

## ⚙️ Configuration

The service is configured via `appsettings.json`:

```json
{
  "DesktopStacks": {
    "MonitorPath": "%USERPROFILE%\\Desktop",
    "OrganizationStrategy": "FileType",
    "CheckInterval": "00:00:30",
    "EnableAutoOrganization": true
  }
}
```

## 📊 Monitoring

- **Logs**: Structured logging with Serilog
- **Metrics**: Performance counters for file operations
- **Health Checks**: Service health monitoring

## 🔒 Security Considerations

- Minimal file system permissions required
- No network access needed
- Runs with user context permissions
- Safe file operations with rollback capability

## Next Steps

1. Implement core file organization algorithms
2. Add comprehensive error handling
3. Create configuration validation
4. Add performance monitoring
5. Implement Windows Service installer
