# TRIP ANALYZER
### Intelligent Multi-Modal Travel & Route Analysis Platform

**Trip Analyzer** is an enterprise-grade ASP.NET Core MVC (.NET 10) travel intelligence platform designed to evaluate and compare multi-modal transport options (Train, Bus, Flight, Driving) across India and global destinations. It delivers personalized recommendations based on Cost, Duration, Availability, and Comfort with interactive Google Maps route mapping, AI assistant support, and comprehensive administrative controls.

---

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core MVC (.NET 10)
- **Database**: PostgreSQL / Neon Serverless Postgres with Entity Framework Core 10 (Npgsql)
- **Authentication & Security**: ASP.NET Core Identity with session-scoped authentication cookies, Google OAuth 2.0 OpenID Connect integration, and Role-Based Access Control (`Admin`, `User`)
- **Mapping & Geocoding**: Google Maps JavaScript API (browser client) & Google Routes API (backend server-side calculation with geodesic fallback)
- **AI & Assistance**: Google Gemini API & Database-driven intelligent chatbot FAQ assistance
- **Frontend & Design System**: Custom glassmorphism responsive design system (Vanilla CSS & Bootstrap 5) with 3D micro-animations and zero horizontal overflow across all screen viewports (375px to 1920px+)
- **Analytics**: Chart.js data visualization
- **Email & Notifications**: SMTP mail service with asynchronous dispatch and fallback logging
- **Deployment**: Multi-stage Docker container configured for Render and cloud hosts

---

## 🌟 Core Features & Responsive Design

1. **Multi-Modal Travel Recommendation Engine**:
   - Proprietary algorithm normalizing Cost (40%), Duration (30%), Availability (20%), and Comfort (10%).
   - Classifies options into **Recommended Overall**, **Cheapest**, **Fastest**, and **Most Convenient**.
   - Generates transparent, natural-language reasoning explaining why a route is recommended.

2. **Interactive Google Maps & Routing**:
   - Visual route polyline decoding directly between origin and destination with automatic viewport `fitBounds`.
   - Geodesic straight-line fallback rendering if road routing is unavailable.
   - Dual-tier key architecture: server-only Routes API key and browser-safe Maps JavaScript API key.

3. **Responsive 3D Travel-Tech UI**:
   - Fully tested and adapted across mobile (`375px`, `480px`), tablet (`768px`, `1024px`), and desktop (`1440px`, `1920px`).
   - Clean collapsible mobile navigation, touch targets (>= 44px), and adaptive glassmorphism panels.

4. **Session-Scoped Secure Authentication & Google OAuth**:
   - Standard password and Google OAuth login flows using non-persistent session cookies (discarded on browser exit).
   - Automatic account linking for existing email accounts without creating duplicate identities.
   - Preserves `returnUrl` destination when accessing protected pages like `/Trip/Analyze`.

5. **Strictly Controlled Admin Authority (`/Admin`)**:
   - Admin access is restricted solely to users explicitly assigned the `Admin` role in Identity.
   - Normal registrations and Google sign-ins never automatically acquire admin privileges.
   - Dynamic role management allows administrators to grant or revoke admin rights safely from `/Admin/Users`.

---

## 🔐 Environment Variables & Configuration

Configure these variables via `appsettings.Development.json`, .NET User Secrets, or cloud environment variables:

| Variable | Required | Description |
| :--- | :---: | :--- |
| `DATABASE_URL` | Yes | PostgreSQL / Neon connection string (`postgres://user:pass@host/db?sslmode=require` or standard Npgsql format) |
| `ADMIN_EMAIL` | Yes | Initial administrator email address (e.g. `admin@yourdomain.com`) |
| `ADMIN_PASSWORD` | Yes | Initial administrator password (must be provided securely via environment variables or User Secrets; never committed to source) |
| `Authentication__Google__ClientId` | Optional | Google OAuth 2.0 Client ID for web sign-in |
| `Authentication__Google__ClientSecret` | Optional | Google OAuth 2.0 Client Secret (server-side only) |
| `GOOGLE_MAPS_API_KEY` | Optional | Browser-restricted Google Maps JavaScript API key |
| `GOOGLE_ROUTES_API_KEY` | Optional | Server-side Google Routes API key |
| `GEMINI_API_KEY` | Optional | Google Gemini API key for AI assistant features |
| `SMTP_HOST` | Optional | SMTP mail server hostname (e.g. `smtp.gmail.com`) |
| `SMTP_PORT` | Optional | SMTP mail server port (e.g. `587`) |
| `SMTP_USERNAME` | Optional | SMTP username / sender address |
| `SMTP_PASSWORD` | Optional | SMTP app password |

> [!IMPORTANT]
> **Security Note**: Never commit real secrets, API keys, or passwords into source code or repository files. Use environment variables in production and .NET User Secrets during local development.

---

## 🚀 Local Development Setup

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL database (Local or [Neon Cloud PostgreSQL](https://neon.tech/))

### 2. Configure Local Secrets / Environment
In your terminal, set your environment variables:

**PowerShell (Windows):**
```powershell
$env:DATABASE_URL="postgres://neondb_owner:YOUR_PASSWORD@ep-sample.us-east-2.aws.neon.tech/neondb?sslmode=require"
$env:ADMIN_EMAIL="admin@tripanalyzer.com"
$env:ADMIN_PASSWORD="YourSecurePasswordHere!"
$env:Authentication__Google__ClientId="YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
$env:Authentication__Google__ClientSecret="YOUR_GOOGLE_CLIENT_SECRET"
$env:GOOGLE_MAPS_API_KEY="YOUR_BROWSER_KEY"
$env:GOOGLE_ROUTES_API_KEY="YOUR_SERVER_KEY"
```

**Bash / Zsh (macOS / Linux):**
```bash
export DATABASE_URL="postgres://neondb_owner:YOUR_PASSWORD@ep-sample.us-east-2.aws.neon.tech/neondb?sslmode=require"
export ADMIN_EMAIL="admin@tripanalyzer.com"
export ADMIN_PASSWORD="YourSecurePasswordHere!"
export Authentication__Google__ClientId="YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="YOUR_GOOGLE_CLIENT_SECRET"
export GOOGLE_MAPS_API_KEY="YOUR_BROWSER_KEY"
export GOOGLE_ROUTES_API_KEY="YOUR_SERVER_KEY"
```

### 3. Build & Run
```bash
# Restore packages and verify build
dotnet build

# Launch application with HTTPS profile
dotnet run --launch-profile https
```
The application will launch on `https://localhost:7203` (and `http://localhost:5236`).

---

## 🌐 Google OAuth Setup

1. Open the [Google Cloud Console](https://console.cloud.google.com/apis/credentials).
2. Create an **OAuth 2.0 Client ID** (Application type: *Web application*).
3. Add Authorized JavaScript Origins:
   - `https://localhost:7203` (Local)
   - `https://trip-analyzer.onrender.com` (Production)
4. Add Authorized Redirect URIs:
   - `https://localhost:7203/signin-google` (Local)
   - `https://trip-analyzer.onrender.com/signin-google` (Production)
5. Save the credentials and provide them via `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`.

---

## 🗺️ Google Maps & Routes Setup

1. Enable **Maps JavaScript API** and **Routes API** in Google Cloud Console.
2. Create two separate API keys:
   - **`GOOGLE_MAPS_API_KEY`**: Restrict to HTTP referrers (`localhost:7203/*`, `trip-analyzer.onrender.com/*`) and Maps JavaScript API.
   - **`GOOGLE_ROUTES_API_KEY`**: Restrict to IP / web server calls and Routes API (kept strictly backend-side).

---

## 🛡️ Admin Setup & Role Security Rules

- **Initial Admin Seeding**: At startup, `DbInitializer` checks `ADMIN_EMAIL` and `ADMIN_PASSWORD`. If configured, it provisions or updates the designated user with the `Admin` role without creating duplicates.
- **Strict Role Gating**: All controllers under `Areas/Admin/Controllers/` enforce `[Area("Admin")]` and `[Authorize(Roles = "Admin")]`.
- **Role Isolation**: Normal user registrations and Google logins are assigned the `User` role exclusively.
- **Admin Delegation**: Existing Administrators can grant or revoke the `Admin` role for any user through `/Admin/Users/Details/{id}`.

---

## ☁️ Production Deployment (Render)

1. Connect your repository to [Render](https://render.com).
2. Create a new **Web Service** selecting **Docker** environment.
3. Configure the environment variables in the Render dashboard:
   - `DATABASE_URL`: Neon PostgreSQL connection string
   - `ADMIN_EMAIL`: Production admin email
   - `ADMIN_PASSWORD`: Strong production administrator password
   - `Authentication__Google__ClientId`: Google OAuth Client ID
   - `Authentication__Google__ClientSecret`: Google OAuth Client Secret
   - `GOOGLE_MAPS_API_KEY`: Browser Maps key
   - `GOOGLE_ROUTES_API_KEY`: Server Routes key
4. Deploy the service. The multi-stage Docker build handles port binding and starts the application securely.

---

## 🧪 Testing Verification Checklist

- [x] **Fresh Session Behavior**: Cookies expire upon closing the browser session; accounts are not automatically cached across sessions.
- [x] **Password & Google Auth**: Supports standard login, registration, and "Continue with Google" with automatic email linking.
- [x] **Protected Navigation**: Accessing `/Trip/Analyze` while logged out redirects to `/Account/Login?returnUrl=...` and returns upon authentication.
- [x] **Admin Authorization**: Non-admin users are denied access to `/Admin` paths; explicitly authorized admins access `/Admin/Dashboard`.
- [x] **Multi-Viewport Responsiveness**: Tested across 375px, 480px, 768px, 1024px, 1440px, and 1920px with zero horizontal scroll.
