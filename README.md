Trip Analyzer 🧭
Trip Analyzer is an AI-powered full-stack travel planning and trip analysis web application built with ASP.NET Core MVC (.NET 10). It combines trip analysis, travel recommendations, routing, fare calculation, geographic data, AI assistance, authentication, user management, responsive UI, Docker, and cloud deployment.

🚀 Live Application
Production: https://trip-analyzer.onrender.com
GitHub: https://github.com/adityaxrahul/trip-analyzer
---


📌 Project Overview
Trip Analyzer allows visitors to explore travel information publicly while requiring authentication for protected services and user-specific operations.
Application flow
```text
User
 │
 ├── Public Website
 │   ├── Home
 │   ├── How It Works
 │   ├── FAQ
 │   └── Other public information
 │
 └── Authenticated Services
     ├── Trip Analysis
     ├── AI Chatbot
     ├── Contact/Enquiry
     ├── Trip History
     ├── Profile
     └── User-specific operations
```
If an anonymous user attempts to use a protected service, the application redirects the user to the login page with a return URL.
---
✨ Main Features
🗺️ Trip Analysis
Users can analyze trips using travel information and the application's recommendation services.
Features include:
Source and destination information
District/city information
Transportation options
Distance calculation
Fare calculation
Route information
Travel duration
Trip recommendations
Alternative transportation options
🤖 AI Travel Chatbot
The application integrates the Gemini API for AI-powered travel assistance.
Features include:
Natural-language travel questions
Travel assistance
Suggested questions
Loading/error handling
Responsive chatbot UI
Authenticated service access
The Gemini API key is supplied through environment configuration and is not intended to be hard-coded in source code.
🔐 Authentication & Authorization
The project uses ASP.NET Core Identity.
Supported functionality includes:
Registration
Login
Logout
Password policy
Account lockout
Google Sign-In
Profile management
Change password
Authenticated sessions
Return URL handling
Server-side authorization
Public pages remain accessible to visitors. Protected operations require authentication.
📧 Contact & Email
The application includes a contact/enquiry system and SMTP-based email functionality.
Configuration uses environment variables such as:
```text
SMTP_HOST
SMTP_PORT
SMTP_USERNAME
SMTP_PASSWORD
ADMIN_EMAIL
```
Sensitive SMTP credentials should never be committed to GitHub.
🏙️ India Geographic Data
The application includes seed data for Indian cities and districts:
```text
Data/Seed/
├── 2-district.csv
├── IndiaCities.csv
└── IndiaDistricts.csv
```
These CSV files are configured to be included in production publish output.
---
🛣️ Core Services
The project separates major application responsibilities into services, including:
```text
TripRecommendationService
ChatbotService
EmailSenderService
AdminAnalyticsService
DistrictService
DistanceCalculationService
FareCalculationService
RoutingService
```
This keeps business logic separate from controllers and views.
---
🏗️ Technology Stack
Backend
C#
ASP.NET Core MVC
.NET 10
Entity Framework Core 10
ASP.NET Core Identity
Npgsql
Database
PostgreSQL
Authentication
ASP.NET Core Identity
Google Authentication
Cookie authentication
AI
Google Gemini API
Frontend
Razor Views
HTML5
CSS3
JavaScript
Responsive design
Bootstrap/library assets where used
Deployment
Git
GitHub
Docker
Render
---
📁 Project Structure
```text
Trip Analyzer/
│
├── Areas/
│   └── Admin/
│       └── Views/
│
├── Controllers/
│
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DatabaseUrlParser.cs
│   ├── DbInitializer.cs
│   └── Seed/
│       ├── 2-district.csv
│       ├── IndiaCities.csv
│       └── IndiaDistricts.csv
│
├── Models/
│
├── Services/
│   ├── ChatbotService.cs
│   ├── DistrictService.cs
│   ├── DistanceCalculationService.cs
│   ├── EmailSenderService.cs
│   ├── FareCalculationService.cs
│   ├── RoutingService.cs
│   ├── TripRecommendationService.cs
│   └── ...
│
├── Views/
│   ├── Account/
│   ├── Home/
│   ├── Trip/
│   └── Shared/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   └── lib/
│
├── Migrations/
├── Program.cs
├── TripAnalyzer.csproj
├── Dockerfile
├── .dockerignore
├── .gitignore
└── README.md
```
---
🔄 System Architecture
```text
                    ┌─────────────────┐
                    │      User       │
                    └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │   Web Browser   │
                    └────────┬────────┘
                             │
                             ▼
                 ┌───────────────────────┐
                 │ ASP.NET Core MVC      │
                 │ Controllers + Views   │
                 └───────────┬───────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
       ┌────────────┐ ┌────────────┐ ┌──────────────┐
       │  Identity  │ │  Services  │ │  PostgreSQL  │
       │    Auth    │ │            │ │   Database   │
       └────────────┘ └─────┬──────┘ └──────────────┘
                            │
              ┌─────────────┼──────────────┐
              ▼             ▼              ▼
          Gemini API    Routing/Data      SMTP
          Chatbot       Services          Email
```
---
🗄️ Database
Trip Analyzer uses PostgreSQL with Entity Framework Core.
The production database URL is supplied through:
```text
DATABASE_URL
```
The project includes:
Entity Framework Core migrations
Database initialization
Startup seeding
ASP.NET Identity tables
Travel-related data
District/city data
Trip-related data
---
🔒 Security
Security-related features include:
ASP.NET Core Identity
Password requirements
Account lockout
Server-side authorization
Secure authentication cookies
HTTPS configuration
Security response headers
Environment-based secrets
Protected user-specific operations
No intentional hard-coded production API keys
Sensitive values such as the following must remain outside source control:
```text
DATABASE_URL
GEMINI_API_KEY
GOOGLE_CLIENT_SECRET
SMTP_PASSWORD
```
---
📱 Responsive UI
The frontend is designed to work across:
Small mobile phones
Large mobile phones
Tablets
Laptops
Desktop screens
Large/high-resolution displays
Responsive improvements focus on:
Text readability
Low-contrast text fixes
Mobile layouts
Responsive forms
Responsive cards
Responsive tables
Chatbot sizing
Touch-friendly controls
Prevention of horizontal overflow
The existing visual identity is preserved while improving readability and responsiveness.
---
⚡ Performance
Performance considerations include:
Efficient server-side authorization
Debounced client-side requests where appropriate
Prevention of duplicate chatbot submissions
Responsive layouts
Production publishing
Docker multi-stage builds
Static assets
Environment-based configuration
The goal is to keep the application responsive without unnecessary frontend or backend processing.
---
🐳 Docker
The project uses a multi-stage Docker build.
Build image:
```text
mcr.microsoft.com/dotnet/sdk:10.0
```
Runtime image:
```text
mcr.microsoft.com/dotnet/aspnet:10.0
```
The runtime image installs the required Linux GSSAPI/Kerberos dependency:
```text
libgssapi-krb5-2
```
The application is configured for dynamic deployment ports.
---
☁️ Production Deployment
The production application is deployed on Render.
```text
GitHub
   │
   ▼
Render
   │
   ├── Docker build
   ├── ASP.NET Core
   ├── PostgreSQL
   ├── Gemini API
   ├── Google Authentication
   └── SMTP configuration
```
Production URL:
https://trip-analyzer.onrender.com
A Git push to the connected production branch can trigger a new Render deployment.
---
⚙️ Environment Variables
Production secrets should be configured in the deployment environment.
Example configuration:
```text
DATABASE_URL=your-postgresql-url

GEMINI_API_KEY=your-gemini-api-key

GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret

SMTP_HOST=your-smtp-host
SMTP_PORT=587
SMTP_USERNAME=your-email
SMTP_PASSWORD=your-email-password

ADMIN_EMAIL=your-admin-email
```
Use the exact variable names expected by the application's configuration.
---
🔑 Google Sign-In
Google authentication uses the application's callback path:
```text
/signin-google
```
For production, the Google OAuth configuration should contain the production callback URL:
```text
https://trip-analyzer.onrender.com/signin-google
```
The Google Client ID and Client Secret must be configured as environment variables.
---
🧪 Local Development
Requirements
Install:
.NET 10 SDK
PostgreSQL
Git
Visual Studio or VS Code
Docker (optional)
Clone
```bash
git clone https://github.com/adityaxrahul/trip-analyzer.git
cd trip-analyzer
```
Restore
```bash
dotnet restore
```
Build
```bash
dotnet build
```
Run
```bash
dotnet run
```
---
🧹 Clean & Publish
Clean:
```bash
dotnet clean
```
Build:
```bash
dotnet build
```
Publish:
```bash
dotnet publish -c Release
```
Publish output:
```text
bin/Release/net10.0/publish/
```
---
🐳 Docker Commands
Build the image:
```bash
docker build -t trip-analyzer .
```
Run locally:
```bash
docker run -p 8080:8080 trip-analyzer
```
---
🛡️ Authentication Access Model
Trip Analyzer separates public exploration from protected service usage.
Anonymous users can
Open Home
Read public information
View FAQ
View How It Works
Access Login/Register
Explore public pages
Authenticated users can
Analyze trips
Use the AI chatbot
Submit protected enquiries
View trip history
Manage profile
Use user-specific services
When an anonymous user requests a protected operation, the application redirects to:
```text
/Account/Login?ReturnUrl=<requested-url>
```
After successful authentication, the user can continue to the requested destination.
Authorization is enforced on the server and is not dependent only on hiding frontend buttons.
---
📊 Main Modules
Module	Purpose
Home	Public landing and travel exploration
Account	Registration, login, profile and authentication
Trip	Trip analysis, recommendations and history
Chatbot	AI-powered travel assistance
District	Indian district/city data

Routing	Route and distance functionality
Fare	Transportation fare calculations
Contact	User enquiry functionality
Admin	Administrative management and analytics
Database	PostgreSQL persistence and EF Core
Authentication	Identity + Google Sign-In
Email	SMTP email functionality
---
🌱 Development Workflow
Before pushing changes:
```bash
dotnet clean
dotnet build
git status
```
If the build succeeds:
```bash
git add .
git commit -m "Update Trip Analyzer"
git push origin main
```
---
🎓 Project Purpose
Trip Analyzer is a college-level full-stack project demonstrating practical integration of:
Web application development
MVC architecture
Database management
Authentication and authorization
AI integration
Travel/geographic data
Route and fare calculation
Responsive UI
Security
Docker
Git/GitHub
Cloud deployment
The project demonstrates a complete development and deployment workflow:
```text
Planning
   ↓
UI / UX
   ↓
ASP.NET Core Development
   ↓
Database Integration
   ↓
Authentication
   ↓
AI Integration
   ↓
Security
   ↓
Dockerization
   ↓
GitHub
   ↓
Cloud Deployment
   ↓
Live Application
```
---
🚀 Future Improvements
Possible future enhancements include:
Real-time transportation availability
More route optimization
Advanced AI itinerary generation
Weather integration
Hotel/accommodation recommendations
Advanced trip budgeting
Improved map visualization
More travel data sources
Progressive Web App support
Advanced analytics
---
📜 License
See the `LICENSE` file included in this repository for the applicable license terms.
---
👨‍💻 Developer
Aditya Kumar Gupta
Project: Trip Analyzer — AI-Powered Travel Planning & Trip Analysis Platform
GitHub: https://github.com/adityaxrahul/trip-analyzer
Live: https://trip-analyzer.onrender.com
---
⭐ Final Summary
Trip Analyzer is a full-stack AI-powered travel platform built using ASP.NET Core .NET 10, Entity Framework Core, PostgreSQL, ASP.NET Identity, Google Authentication, Gemini AI, Docker, GitHub, and Render.
It combines trip analysis, recommendations, routing, fare calculation, geographic data, AI assistance, authentication, user management, responsive design, security, and cloud deployment into one practical application.

🧭 Trip Analyzer
Plan Smarter. Travel Better.

Created by Aditya Kumar Gupta and Team
