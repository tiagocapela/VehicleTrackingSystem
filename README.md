# 🚗 Vehicle Tracking System

A comprehensive, production-ready vehicle tracking solution built with .NET 8, designed specifically for **Micodus MV730 GPS devices**. This system provides real-time vehicle monitoring, fleet management, and historical tracking data through a modern, responsive web interface.

## 🌟 Features

### 📊 **Real-time Dashboard**
- **Live vehicle status** overview with statistics
- **Interactive maps** with real-time vehicle positions using Leaflet.js
- **Fleet statistics** (online/offline/moving vehicles)
- **Real-time updates** via SignalR WebSocket connections
- **Responsive design** works on desktop, tablet, and mobile

### 🚙 **Vehicle Management**
- **Complete CRUD operations** for vehicles
- **GPS device association** with vehicles
- **Driver assignment** and contact details
- **License plate** management
- **Vehicle status** tracking (active/inactive)
- **Bulk operations** and filtering

### 🗺️ **Live Tracking**
- **Real-time vehicle tracking** with automatic updates
- **Location history** and route playback
- **Speed and course** monitoring
- **GPS signal strength** display
- **Geofencing** capabilities (coming soon)
- **Custom map markers** with vehicle information

### 📈 **Historical Analysis**
- **Route history** with interactive timeline
- **Speed analysis** with charts and statistics
- **Stop detection** and duration tracking
- **Distance calculations** using GPS coordinates
- **Data export** in CSV format
- **Custom date range** filtering
- **Route animation** playback

### 📡 **GPS Data Processing**
- **Micodus MV730 protocol** parsing
- **GPRMC message** support
- **TCP server** for device connections (port 8888)
- **Data validation** and error handling
- **Raw message logging** for debugging
- **Multiple device** support

## 🏗️ System Architecture

```
┌─────────────────┐    TCP:8888    ┌──────────────────┐    HTTP/REST    ┌─────────────────┐
│   MV730 GPS     │──────────────→ │  TCP Microservice│──────────────→ │   Web Dashboard │
│    Devices      │                │    (.NET 8)      │                │    (MVC .NET 8) │
└─────────────────┘                └──────────────────┘                └─────────────────┘
                                            │                                     │
                                            ▼                                     ▼
                                   ┌──────────────────┐                ┌─────────────────┐
                                   │   SQL Server     │◄───────────────│   SignalR Hub   │
                                   │    Database      │                │  (Real-time)    │
                                   └──────────────────┘                └─────────────────┘
```

### **Components:**
- **Web Application**: ASP.NET Core MVC with SignalR for real-time updates
- **TCP Microservice**: Handles GPS device connections and data parsing
- **SQL Server Database**: Stores vehicle information and GPS tracking data
- **SignalR Hub**: Provides real-time communication for live updates

## 🚀 Quick Start

### **Prerequisites**
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (recommended)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB for development)

### **🐳 Option 1: Docker (Recommended)**

```bash
# Clone the repository
git clone https://github.com/yourusername/vehicle-tracking-system.git
cd VehicleTrackingSolution

# Start all services with Docker Compose
docker-compose up -d

# View logs (optional)
docker-compose logs -f
```

**What this starts:**
- 🐘 **SQL Server** on port `1433`
- 🌐 **Web Application** on port `5000` → http://localhost:5000
- 🔌 **TCP Service** on port `5001` (API) and `8888` (GPS devices)

### **⚙️ Option 2: Manual Development**

```bash
# 1. Start SQL Server (LocalDB or full instance)

# 2. Run TCP Microservice
cd src/TcpGpsService
dotnet restore
dotnet ef database update  # Create GPS database
dotnet run                 # Starts on https://localhost:5001

# 3. Run Web Application (in new terminal)
cd src/VehicleTracking.Web
dotnet restore
dotnet ef database update  # Create main database
dotnet run                 # Starts on https://localhost:5000
```

### **🎯 First Steps**
1. **Open your browser**: Navigate to http://localhost:5000
2. **Add a vehicle**: Go to Vehicles → Add Vehicle
3. **Configure GPS device**: Set your MV730 to connect to your server on port 8888
4. **Start tracking**: Watch real-time data appear on the dashboard

## 📱 MV730 GPS Device Configuration

### **Server Connection Setup**
Configure your Micodus MV730 device with these AT commands:

```bash
# Set server details (replace with your server IP/domain)
AT+GTBSI=gv300,1,your-server.com,8888,,,,0000$

# Set reporting interval (30 seconds)
AT+GTTRI=gv300,1,30,,,,0000$

# Enable GPS tracking
AT+GTRTO=gv300,1,0,,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0000$

# Set APN for your mobile carrier
AT+GTAPN=gv300,internet,,,,0000$

# Check device status
AT+GTSTG=gv300,0000$
```

### **For Local Testing:**
```bash
# If testing locally, use your local IP address
AT+GTBSI=gv300,1,192.168.1.100,8888,,,,0000$
```

### **Common MV730 Settings:**
- **Default Password**: `0000`
- **Protocol**: TCP
- **Port**: 8888
- **Data Format**: GPRMC or custom MV730 protocol

## 🧪 Testing Your Setup

### **1. Test with Telnet**
```bash
# Connect to the TCP server
telnet localhost 8888

# Send a test GPS message
$$A123,TESTDEVICE,GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394*6A

# You should see "ACK" response
```

### **2. Add Test Vehicle**
1. Open http://localhost:5000
2. Click "Vehicles" → "Add Vehicle"
3. Fill in:
   - **Device ID**: `TESTDEVICE`
   - **Vehicle Name**: `Test Vehicle`
   - **License Plate**: `TEST-123`
4. Click "Create Vehicle"

### **3. Verify Data Flow**
- Check the dashboard for your test vehicle
- Send GPS data via telnet
- Watch real-time updates appear

## 📁 Project Structure

```
VehicleTrackingSolution/
├── 📄 VehicleTrackingSolution.sln              # Visual Studio solution
├── 🐳 docker-compose.yml                       # Docker configuration
├── 📋 README.md                                # This file
├── 🚫 .gitignore                               # Git ignore rules
│
├── 📁 src/                                     # Source code
│   ├── 📁 VehicleTracking.Web/                # Main web application
│   │   ├── 📄 VehicleTracking.Web.csproj      # Project file
│   │   ├── 📄 Program.cs                       # Application entry point
│   │   ├── 📄 appsettings.json                 # Configuration
│   │   ├── 🐳 Dockerfile                       # Web app container
│   │   │
│   │   ├── 📁 Controllers/                     # MVC Controllers
│   │   │   ├── 📄 HomeController.cs            # Dashboard controller
│   │   │   ├── 📄 VehicleController.cs         # Vehicle management
│   │   │   └── 📁 Api/
│   │   │       └── 📄 GpsController.cs         # REST API endpoints
│   │   │
│   │   ├── 📁 Models/                          # Data models
│   │   │   ├── 📄 Vehicle.cs                   # Vehicle entity
│   │   │   ├── 📄 GpsLocation.cs               # GPS location entity
│   │   │   ├── 📄 VehicleTrackingContext.cs    # Database context
│   │   │   ├── 📁 DTOs/                        # Data transfer objects
│   │   │   └── 📁 ViewModels/                  # View models
│   │   │
│   │   ├── 📁 Services/                        # Business logic
│   │   │   ├── 📄 VehicleService.cs            # Vehicle operations
│   │   │   ├── 📄 TcpMicroserviceClient.cs     # TCP service client
│   │   │   └── 📄 MappingExtensions.cs         # Object mapping
│   │   │
│   │   ├── 📁 Views/                           # Razor views
│   │   │   ├── 📁 Shared/                      # Shared layouts
│   │   │   ├── 📁 Home/                        # Dashboard views
│   │   │   └── 📁 Vehicle/                     # Vehicle management views
│   │   │
│   │   ├── 📁 wwwroot/                         # Static files
│   │   │   ├── 📁 css/                         # Stylesheets
│   │   │   ├── 📁 js/                          # JavaScript files
│   │   │   └── 📁 lib/                         # Third-party libraries
│   │   │
│   │   └── 📁 Hubs/                           # SignalR hubs
│   │       └── 📄 VehicleTrackingHub.cs        # Real-time communication
│   │
│   └── 📁 TcpGpsService/                       # TCP microservice
│       ├── 📄 TcpGpsService.csproj             # Project file
│       ├── 📄 Program.cs                        # Service entry point
│       ├── 🐳 Dockerfile                        # Service container
│       │
│       ├── 📁 Models/                          # Data models
│       │   ├── 📄 GpsData.cs                   # GPS data entity
│       │   └── 📄 GpsDataContext.cs            # Database context
│       │
│       ├── 📁 Services/                        # Service logic
│       │   ├── 📄 TcpServerService.cs          # TCP server implementation
│       │   ├── 📄 Mv730Parser.cs               # GPS message parser
│       │   ├── 📄 GpsDataService.cs            # Data operations
│       │   └── 📄 TcpServerHostedService.cs    # Background service
│       │
│       └── 📁 Controllers/                     # API controllers
│           └── 📄 GpsController.cs             # GPS data endpoints
│
└── 📁 tests/                                   # Test projects
    ├── 📁 VehicleTracking.Tests/               # Web app tests
    └── 📁 TcpGpsService.Tests/                 # TCP service tests
```

## 🔧 Configuration

### **Connection Strings**

**Development (appsettings.Development.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VehicleTrackingDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "TcpMicroservice": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

**Production (appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=VehicleTrackingDb;User ID=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False"
  },
  "TcpMicroservice": {
    "BaseUrl": "https://your-tcp-service.azurewebsites.net"
  }
}
```

### **TCP Server Settings**
```json
{
  "TcpServer": {
    "Port": 8888,
    "MaxConnections": 1000
  }
}
```

### **Logging Configuration**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "TcpGpsService": "Debug"
    }
  }
}
```

## 📊 API Documentation

### **Vehicle Management APIs**

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/gps/vehicles` | Get all vehicles with latest locations |
| `GET` | `/api/gps/vehicle/{id}/latest` | Get latest location for specific vehicle |
| `GET` | `/api/gps/vehicle/{id}/history` | Get location history with date filtering |
| `GET` | `/api/gps/vehicle/{id}/statistics` | Get vehicle statistics (distance, speed, etc.) |
| `POST` | `/api/gps/location` | Add new GPS location |

### **Example API Response**
```json
{
  "id": 1,
  "vehicleName": "Fleet Vehicle 001",
  "deviceId": "MV730001",
  "licensePlate": "ABC-1234",
  "driverName": "John Smith",
  "isActive": true,
  "lastLocation": {
    "latitude": 39.7392,
    "longitude": -104.9903,
    "speed": 45.5,
    "course": 180.0,
    "satellites": 10,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### **Statistics API Response**
```json
{
  "totalPoints": 156,
  "totalDistance": 45.7,
  "averageSpeed": 35.2,
  "maxSpeed": 78.5,
  "movingTime": 120,
  "stoppedTime": 45,
  "stops": 3
}
```

## 🚀 Deployment

### **🌐 Azure Deployment**

#### **1. Create Azure Resources**
```bash
# Create resource group
az group create --name vehicle-tracking-rg --location "East US"

# Create SQL Server
az sql server create --name your-sql-server --resource-group vehicle-tracking-rg --location "East US" --admin-user sqladmin --admin-password YourPassword123!

# Create databases
az sql db create --resource-group vehicle-tracking-rg --server your-sql-server --name VehicleTrackingDb --service-objective Basic
az sql db create --resource-group vehicle-tracking-rg --server your-sql-server --name GpsTrackingDb --service-objective Basic

# Create App Service Plan
az appservice plan create --name vehicle-tracking-plan --resource-group vehicle-tracking-rg --sku B1

# Create Web Apps
az webapp create --resource-group vehicle-tracking-rg --plan vehicle-tracking-plan --name your-web-app --runtime "DOTNETCORE:8.0"
az webapp create --resource-group vehicle-tracking-rg --plan vehicle-tracking-plan --name your-tcp-service --runtime "DOTNETCORE:8.0"
```

#### **2. Configure App Settings**
```bash
# Web App Settings
az webapp config appsettings set --resource-group vehicle-tracking-rg --name your-web-app --settings ConnectionStrings__DefaultConnection="Server=tcp:your-sql-server.database.windows.net,1433;Database=VehicleTrackingDb;User ID=sqladmin;Password=YourPassword123!;Encrypt=True"

# TCP Service Settings
az webapp config appsettings set --resource-group vehicle-tracking-rg --name your-tcp-service --settings ConnectionStrings__DefaultConnection="Server=tcp:your-sql-server.database.windows.net,1433;Database=GpsTrackingDb;User ID=sqladmin;Password=YourPassword123!;Encrypt=True" TcpServer__Port=8888
```

#### **3. Deploy Applications**
```bash
# Build and deploy Web App
cd src/VehicleTracking.Web
dotnet publish -c Release -o ./publish
zip -r deploy.zip ./publish/*
az webapp deployment source config-zip --resource-group vehicle-tracking-rg --name your-web-app --src deploy.zip

# Build and deploy TCP Service
cd ../TcpGpsService
dotnet publish -c Release -o ./publish
zip -r deploy.zip ./publish/*
az webapp deployment source config-zip --resource-group vehicle-tracking-rg --name your-tcp-service --src deploy.zip
```

### **🐳 Docker Production Deployment**

**docker-compose.prod.yml:**
```yaml
version: '3.8'
services:
  web-app:
    build: 
      context: ./src/VehicleTracking.Web
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${SQL_CONNECTION_STRING}
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./certs:/https:ro
    
  tcp-service:
    build:
      context: ./src/TcpGpsService
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${GPS_CONNECTION_STRING}
    ports:
      - "8888:8888"
```

## 🔒 Security Considerations

### **Production Security Checklist**
- [ ] **HTTPS Only**: Force HTTPS redirects
- [ ] **SQL Injection**: Use parameterized queries (Entity Framework handles this)
- [ ] **Connection Strings**: Store in Azure Key Vault or environment variables
- [ ] **Authentication**: Implement user authentication and authorization
- [ ] **API Rate Limiting**: Protect APIs from abuse
- [ ] **CORS Policy**: Configure appropriate CORS policies
- [ ] **Input Validation**: Validate all user inputs
- [ ] **GPS Device Authentication**: Implement device authentication for TCP connections

### **Network Security**
```json
// Configure CORS in Program.cs
builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigins", policy => {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### **Database Security**
- Use strong passwords and Azure AD authentication
- Enable Transparent Data Encryption (TDE)
- Configure firewall rules
- Regular security updates

## 📈 Monitoring & Performance

### **Application Insights Integration**
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom metrics
telemetryClient.TrackMetric("GpsDataReceived", 1, new Dictionary<string, string> {
    ["DeviceId"] = deviceId,
    ["IsValid"] = isValid.ToString()
});
```

### **Health Checks**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<VehicleTrackingContext>()
    .AddDbContext<GpsDataContext>()
    .AddUrlGroup(new Uri("https://tcp-service-url/api/gps/latest"), "TCP Service");
```

### **Performance Optimization**
- **Database Indexing**: Proper indexes on GPS data queries
- **Connection Pooling**: TCP connection management
- **Caching**: Redis for frequently accessed data
- **CDN**: Static assets delivery
- **Database Partitioning**: For large datasets

## 🧪 Testing

### **Unit Tests**
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/VehicleTracking.Tests/
```

### **Integration Testing**
```bash
# Test TCP connection
telnet your-server.com 8888

# Test API endpoints
curl -X GET "https://your-app.azurewebsites.net/api/gps/vehicles"

# Load testing with Apache Bench
ab -n 1000 -c 10 https://your-app.azurewebsites.net/api/gps/vehicles
```

### **GPS Device Testing**
```bash
# Simulate MV730 device
echo '$$A123,TESTDEV,GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394*6A' | nc your-server.com 8888
```

## 🔧 Troubleshooting

### **Common Issues**

#### **🔌 TCP Connection Issues**
**Problem**: GPS devices can't connect to TCP server
```bash
# Check if port is open
netstat -an | grep 8888
telnet your-server.com 8888

# Check firewall rules
sudo ufw status
sudo ufw allow 8888

# Check Docker port mapping
docker-compose ps
```

#### **🗄️ Database Connection Issues**
**Problem**: Cannot connect to SQL Server
```bash
# Test connection
sqlcmd -S your-server.database.windows.net -U sqladmin -P YourPassword123! -d VehicleTrackingDb

# Check connection string
dotnet ef database update --verbose

# Verify firewall rules in Azure
az sql server firewall-rule list --resource-group vehicle-tracking-rg --server your-sql-server
```

#### **📊 No GPS Data Appearing**
**Problem**: Devices connected but no data in dashboard
1. Check TCP service logs: `docker-compose logs tcp-microservice`
2. Verify device ID matches vehicle registration
3. Check GPS message format in logs
4. Ensure database connectivity between services

#### **🌐 SignalR Connection Issues**
**Problem**: Real-time updates not working
```javascript
// Check browser console for SignalR errors
// Verify WebSocket support
if (typeof WebSocket !== 'undefined') {
    console.log('WebSocket supported');
} else {
    console.log('WebSocket not supported');
}
```

### **Logging and Debugging**
```json
// Enable detailed logging
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "TcpGpsService.Services.TcpServerService": "Trace",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/AmazingFeature`
3. **Make your changes**: Follow coding standards and add tests
4. **Commit your changes**: `git commit -m 'Add some AmazingFeature'`
5. **Push to the branch**: `git push origin feature/AmazingFeature`
6. **Open a Pull Request**: Describe your changes and link any related issues

### **Development Guidelines**
- Follow C# coding conventions
- Add unit tests for new features
- Update documentation for API changes
- Use conventional commit messages
- Ensure all tests pass before submitting

### **Code Style**
- Use EditorConfig settings
- Follow Microsoft's C# coding conventions
- Use async/await patterns consistently
- Implement proper error handling and logging

## 📋 Roadmap

### **Version 2.0 (Planned)**
- [ ] **Mobile Application** (React Native)
- [ ] **Advanced Geofencing** with alerts
- [ ] **Driver Behavior Analysis**
- [ ] **Fuel Consumption Tracking**
- [ ] **Route Optimization**
- [ ] **Multi-tenant Support**
- [ ] **Advanced Analytics Dashboard**
- [ ] **Notification System** (SMS, Email, Push)

### **Version 2.1 (Future)**
- [ ] **AI-Powered Insights**
- [ ] **Predictive Maintenance**
- [ ] **Integration APIs** (3rd party systems)
- [ ] **Advanced Reporting**
- [ ] **Fleet Cost Management**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2024 Vehicle Tracking System

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 🆘 Support

### **Getting Help**
- 📖 **Documentation**: Check the `/docs` folder for detailed guides
- 🐛 **Issues**: Create an issue on GitHub for bugs or feature requests
- 💬 **Discussions**: Use GitHub Discussions for questions and ideas
- 📧 **Email**: support@yourcompany.com for direct support

### **Community**
- ⭐ **Star the repo** if you find it useful
- 🍴 **Fork and contribute** to make it better
- 📢 **Share with others** who might benefit from it

### **Enterprise Support**
For enterprise deployments, custom features, or professional support:
- 📞 Contact: enterprise@yourcompany.com
- 🌐 Website: https://yourcompany.com/enterprise

---

## 🙏 Acknowledgments

- **Leaflet.js** for excellent mapping capabilities
- **SignalR** for real-time communication
- **Bootstrap** for responsive UI components
- **Chart.js** for data visualization
- **Entity Framework Core** for database operations
- **Docker** for containerization
- **Azure** for cloud hosting capabilities

---

<div align="center">

**Made with ❤️ for fleet management and vehicle tracking**

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)](https://www.docker.com/)
[![Azure](https://img.shields.io/badge/Azure-Compatible-blue.svg)](https://azure.microsoft.com/)

</div>