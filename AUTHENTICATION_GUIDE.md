# Authentication & Authorization Guide

This guide explains how to use the JWT authentication and authorization features in the Order Management API.

---

## 🔐 Overview

The API now implements:
- **JWT Bearer Token Authentication** - Secure token-based authentication
- **Role-Based Authorization** - Fine-grained access control with policies
- **Rate Limiting** - API request throttling (100 requests/minute, 1000 requests/hour)
- **API Versioning** - Version 1.0 with support for future versions

---

## 🚀 Quick Start

### 1. Configure JWT Secret Key

The JWT secret key is **REQUIRED** and must be configured securely:

#### Development (User Secrets):
```bash
cd WebApi
dotnet user-secrets set "JwtSettings:SecretKey" "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
```

#### Production (Environment Variable):
```bash
# Linux/Mac
export JwtSettings__SecretKey="YourProductionSecretKey..."

# Windows
set JwtSettings__SecretKey=YourProductionSecretKey...
```

⚠️ **IMPORTANT**: Use a strong secret key (minimum 32 characters, random alphanumeric + special characters)

### 2. Run the Application

```bash
dotnet restore
dotnet run --project WebApi
```

### 3. Get a JWT Token

The application includes a demo authentication endpoint with test users:

**POST** `/api/auth/login`

**Demo Users:**
| Username | Password    | Role          | Permissions                                    |
|----------|-------------|---------------|------------------------------------------------|
| admin    | admin123    | Admin         | Full access to all endpoints                   |
| manager  | manager123  | OrderManager  | Create, read, update orders                    |
| viewer   | viewer123   | OrderViewer   | Read-only access to orders                     |

**Request:**
```json
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "roles": ["Admin"]
}
```

### 4. Use the Token in Requests

Include the token in the `Authorization` header:

```bash
curl -X GET https://localhost:7257/api/orders \
  -H "Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

---

## 🔑 Authorization Policies

### Policy Definitions

| Policy Name      | Required Roles                     | Description                           |
|------------------|------------------------------------|---------------------------------------|
| OrderRead        | Admin, OrderManager, OrderViewer   | View orders and product sales         |
| OrderWrite       | Admin, OrderManager                | Create, update, ship, cancel orders   |
| OrderManagement  | Admin, OrderManager                | Full order management access          |
| AdminOnly        | Admin                              | Administrative functions only         |

### Endpoint Authorization

#### Command Endpoints (Write Operations)
All require `OrderWrite` policy (Admin or OrderManager roles):
- `POST /api/orders` - Create order
- `POST /api/orders/{id}/items` - Add item to order
- `POST /api/orders/{id}/ship` - Ship order
- `POST /api/orders/{id}/cancel` - Cancel order

#### Query Endpoints (Read Operations)
All require `OrderRead` policy (Admin, OrderManager, or OrderViewer roles):
- `GET /api/orders` - List all orders
- `GET /api/orders/{id}` - Get order details
- `GET /api/orders/{id}/timeline` - Get order timeline
- `GET /api/products/{id}/sales` - Get product sales
- `GET /api/products/top` - Get top products

#### Public Endpoints
No authentication required:
- `POST /api/auth/login` - Get JWT token
- `GET /health` - Health check
- `GET /health/ready` - Readiness probe

---

## 📊 Rate Limiting

The API implements IP-based rate limiting:

**Default Limits:**
- **100 requests per minute** per IP address
- **1000 requests per hour** per IP address

**Response on Rate Limit Exceeded:**
- **HTTP 429 Too Many Requests**
- Headers include: `X-Rate-Limit-Limit`, `X-Rate-Limit-Remaining`, `X-Rate-Limit-Reset`

**Configuration:**
Edit `appsettings.json` to adjust limits:
```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ]
  }
}
```

---

## 🔧 API Versioning

**Current Version:** v1.0

The API supports versioning with the following features:
- Default version assumed when not specified: v1.0
- API version reported in response headers: `api-supported-versions`

**Future Versions:**
To specify a version explicitly (when v2 is released):
```
GET /api/v2/orders
```

---

## 🧪 Testing Authentication

### Using cURL

```bash
# 1. Login to get token
TOKEN=$(curl -s -X POST https://localhost:7257/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' \
  | jq -r '.token')

# 2. Use token in authenticated requests
curl -X GET https://localhost:7257/api/orders \
  -H "Authorization: Bearer $TOKEN"

# 3. Create an order
curl -X POST https://localhost:7257/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "description": "Test Order"
  }'
```

### Using Swagger UI

1. Navigate to `https://localhost:7257/swagger`
2. Click the **"Authorize"** button (🔒 icon) at the top
3. Enter: `Bearer YOUR_JWT_TOKEN_HERE`
4. Click **"Authorize"**
5. All requests will now include the JWT token

### Using Postman

1. Create a new request
2. Go to the **Authorization** tab
3. Select **Type**: Bearer Token
4. Paste your JWT token in the **Token** field
5. Send the request

---

## 🛡️ Security Best Practices

### JWT Secret Key

❌ **Don't:**
- Hardcode secret keys in appsettings.json
- Use short or simple secret keys
- Commit secret keys to source control
- Share secret keys across environments

✅ **Do:**
- Use user secrets for development
- Use Azure Key Vault or environment variables for production
- Generate strong, random secret keys (minimum 32 characters)
- Rotate secret keys periodically
- Use different keys for each environment

### Token Management

- **Token Expiration**: Default 60 minutes (configurable in appsettings.json)
- **Token Storage**: Store tokens securely in the client (HttpOnly cookies or secure storage)
- **Token Refresh**: Implement token refresh logic for long-lived sessions
- **Logout**: Clear tokens on client-side logout

### Production Considerations

⚠️ **IMPORTANT**: The demo authentication endpoint is for **TESTING ONLY**.

**For Production, you MUST:**
1. Replace the demo authentication with real user validation
2. Integrate with an identity provider:
   - Azure Active Directory
   - Auth0
   - IdentityServer
   - Your own user database with hashed passwords
3. Implement proper password hashing (bcrypt, PBKDF2, Argon2)
4. Add account lockout after failed login attempts
5. Implement multi-factor authentication (MFA)
6. Log authentication events for security auditing

---

## 🔍 Troubleshooting

### Error: "JWT SecretKey is not configured"

**Cause**: JWT secret key not set in user secrets or environment variables

**Solution**:
```bash
dotnet user-secrets set "JwtSettings:SecretKey" "YourSecretKeyHere"
```

### Error: 401 Unauthorized

**Causes:**
1. No token provided
2. Token expired
3. Invalid token
4. Wrong signature

**Solutions:**
1. Login again to get a new token
2. Ensure token is included in Authorization header
3. Verify token format: `Bearer YOUR_TOKEN`

### Error: 403 Forbidden

**Cause**: User doesn't have required role/permissions

**Solution**: Login with a user that has the required role (see demo users table)

### Error: 429 Too Many Requests

**Cause**: Rate limit exceeded

**Solution**: Wait for the rate limit window to reset or adjust limits in appsettings.json

---

## 📝 Configuration Reference

### appsettings.json

```json
{
  "JwtSettings": {
    "SecretKey": "",  // Set via user secrets or env variables
    "Issuer": "OrderManagementAPI",
    "Audience": "OrderManagementAPI",
    "ExpirationMinutes": 60,
    "ValidateLifetime": true,
    "ValidateIssuer": true,
    "ValidateAudience": true
  },
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

---

## 🔗 Related Documentation

- [SECURITY_SETUP.md](./SECURITY_SETUP.md) - Complete security setup guide
- [README.md](./README.md) - Project overview and architecture
- [Microsoft JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)

---

**Last Updated**: 2025-01-24
**API Version**: 1.0.0
