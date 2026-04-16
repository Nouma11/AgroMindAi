# 🌾 AgricultureAPI

A production-ready REST API backend for an AI-powered agricultural analysis platform. Built with **.NET 10**, **MongoDB Atlas**, and **JWT authentication**.

---

## 🏗️ Architecture

This project follows **Clean Architecture** principles, separated into 4 layers:

```
AgricultureAPI/
├── AgricultureAPI.Domain/          # Entities (User, AnalysisHistory, RefreshToken)
├── AgricultureAPI.Application/     # Interfaces, Services, DTOs
├── AgricultureAPI.Infrastructure/  # MongoDB repositories, DB context
└── AgricultureAPI.API/             # Controllers, Middleware, Program.cs
```

Each layer only depends inward — controllers never touch MongoDB directly.

---

## ✨ Features

- ✅ JWT Authentication with Access + Refresh token rotation
- ✅ Secure password hashing with BCrypt
- ✅ User registration, login, logout
- ✅ Profile management (get & update)
- ✅ AI analysis history per user (paginated)
- ✅ Centralized error handling middleware
- ✅ Swagger UI with Bearer token support
- ✅ CORS configured for React frontend (localhost:5173)
- ✅ Clean Architecture — fully decoupled layers

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 10 |
| Database | MongoDB Atlas |
| Authentication | JWT Bearer + Refresh Tokens |
| Password Hashing | BCrypt.Net |
| API Docs | Swashbuckle / Swagger |
| Logging | Serilog |
| Validation | FluentValidation |

---

## 📦 Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MongoDB Atlas](https://www.mongodb.com/atlas) account (free tier works)

---

## 🚀 Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Nouma11/AgricultureAPI.git
cd AgricultureAPI
```

### 2. Set up MongoDB Atlas

1. Go to [cloud.mongodb.com](https://cloud.mongodb.com) and create a free cluster
2. Create a database user under **Security → Database Access**
3. Allow network access under **Security → Network Access** → Add `0.0.0.0/0`
4. Get your connection string from **Connect → Connect your application**

### 3. Configure the app

Open `AgricultureAPI.API/appsettings.json` and fill in your values:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://YOUR_USER:YOUR_PASSWORD@cluster0.xxxxx.mongodb.net/?appName=Cluster0",
    "DatabaseName": "agriculture_db"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-characters!!",
    "Issuer": "AgricultureAPI",
    "Audience": "AgricultureClient",
    "ExpiryMinutes": "15",
    "RefreshTokenExpiryDays": "7"
  }
}
```

> ⚠️ **Never commit real credentials to GitHub.** Add `appsettings.json` to `.gitignore` or use environment variables in production.

### 4. Restore and run

```bash
dotnet restore
dotnet build
cd AgricultureAPI.API
dotnet run
```

Open **`http://localhost:5059/swagger`** in your browser.

---

## 📡 API Endpoints

### Auth — `/api/v1/auth`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/register` | Create a new account | ❌ |
| POST | `/signin` | Login with email & password | ❌ |
| POST | `/refresh` | Get new access token using refresh token | ❌ |
| POST | `/logout` | Invalidate refresh token | ❌ |

### Users — `/api/v1/users`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/me` | Get current user profile | ✅ |
| PUT | `/me` | Update name and avatar | ✅ |

### History — `/api/v1/history`

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get paginated history for current user | ✅ |
| GET | `/{id}` | Get a single history entry | ✅ |
| POST | `/` | Save a new AI analysis result | ✅ |
| DELETE | `/{id}` | Delete a history entry | ✅ |

---

## 🔐 Authentication Flow

```
1. POST /register or /signin  →  receive accessToken (15min) + refreshToken (7 days)
2. Include token in requests  →  Authorization: Bearer <accessToken>
3. Token expires (401)        →  POST /refresh with refreshToken → new token pair
4. User logs out              →  POST /logout → refreshToken invalidated in DB
```

---

## 📝 Request & Response Examples

### Register
```json
POST /api/v1/auth/register
{
  "email": "farmer@example.com",
  "password": "SecurePass123!",
  "fullName": "Mohamed Amine"
}
```

Response:
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "nTjq1y22...",
  "expiresAt": "2026-04-03T20:41:54Z",
  "user": {
    "id": "69d0228ee104085ad181919c",
    "email": "farmer@example.com",
    "fullName": "Mohamed Amine",
    "role": "free",
    "avatarUrl": null
  }
}
```

### Save Analysis History
```json
POST /api/v1/history
Authorization: Bearer <accessToken>
{
  "prompt": "Analyze this wheat field for disease",
  "aiResponse": "The field shows signs of healthy growth...",
  "zoneGeoJson": "{\"type\":\"Polygon\",\"coordinates\":[...]}",
  "modelUsed": "agriculture-ml-v1"
}
```

### Get History (paginated)
```
GET /api/v1/history?page=1&limit=20
Authorization: Bearer <accessToken>
```

Response:
```json
{
  "data": [...],
  "page": 1,
  "limit": 20,
  "total": 45,
  "totalPages": 3
}
```

---

## 🗄️ MongoDB Collections

| Collection | Purpose |
|---|---|
| `users` | User accounts (email, passwordHash, role, etc.) |
| `history` | AI analysis results per user |
| `refresh_tokens` | Active refresh tokens for session management |

---

## 🔧 Project Structure Detail

```
AgricultureAPI.Domain/
└── Entities/
    ├── User.cs
    ├── AnalysisHistory.cs
    └── RefreshToken.cs

AgricultureAPI.Application/
├── DTOs/
│   ├── Auth/        (SignInRequest, RegisterRequest, AuthResponse...)
│   ├── User/        (UserDto, UpdateProfileRequest)
│   ├── History/     (HistoryDto, CreateHistoryRequest)
│   └── Common/      (PagedResult<T>)
├── Interfaces/      (IUserRepository, IHistoryService, IAuthService...)
└── Services/
    ├── AuthService.cs
    ├── UserService.cs
    └── HistoryService.cs

AgricultureAPI.Infrastructure/
├── MongoDB/
│   ├── MongoDbContext.cs
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── HistoryRepository.cs
│       └── RefreshTokenRepository.cs
└── Settings/
    └── MongoDbSettings.cs

AgricultureAPI.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   └── HistoryController.cs
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
├── appsettings.json
└── Program.cs
```

---

## 🌐 Frontend

This API is designed to work with the React + Vite frontend:
👉 [Agriculture Frontend Repository](https://github.com/YaCin/agriculture)

CORS is pre-configured for `http://localhost:5173` (Vite default port).

---

## 📄 License

MIT License — feel free to use and modify for your own projects.

---

## 👤 Author

**Mohamed Amine Nouma**
- GitHub: [@Nouma11](https://github.com/Nouma11)
