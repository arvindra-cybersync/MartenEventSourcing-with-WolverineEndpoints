# Security Setup Guide

This document provides instructions for securely configuring your Order Management API after the security improvements.

## 🔐 Critical: Database Connection String Setup

The application NO LONGER accepts hardcoded connection strings. You MUST configure the connection string using one of these secure methods:

### Option 1: User Secrets (Development) ✅ RECOMMENDED

```bash
# Navigate to the WebApi project directory
cd WebApi

# Set the connection string in user secrets
dotnet user-secrets set "ConnectionStrings:MartenDb" "Host=localhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=YOUR_PASSWORD_HERE"
```

### Option 2: Environment Variables (Production)

```bash
# Linux/Mac
export ConnectionStrings__MartenDb="Host=yourhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=YOUR_PASSWORD"

# Windows PowerShell
$env:ConnectionStrings__MartenDb="Host=yourhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=YOUR_PASSWORD"

# Windows CMD
set ConnectionStrings__MartenDb=Host=yourhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=YOUR_PASSWORD
```

### Option 3: Azure Key Vault (Production) ✅ BEST FOR PRODUCTION

Configure Azure Key Vault integration in Program.cs:

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## 🚀 Running the Application

### 1. Restore Packages

```bash
dotnet restore
```

### 2. Configure Connection String (see above)

### 3. Run the Application

```bash
dotnet run --project WebApi
```

If the connection string is not configured, the application will fail fast with a clear error message.

---

## 🔒 Security Features Implemented

### ✅ Credentials Management
- ❌ No hardcoded passwords in appsettings.json
- ✅ User secrets for development
- ✅ Environment variables for production
- ✅ Fail-fast validation on startup

### ✅ Input Validation
- All command DTOs have validation attributes
- Query parameters are constrained (e.g., top N products limited to 1-100)
- Business rule validation in aggregates

### ✅ Error Handling
- Structured exception handling with proper HTTP status codes
- Generic error messages for unexpected errors (no information disclosure)
- Detailed logging for debugging (server-side only)
- Proper use of 400 Bad Request, 404 Not Found, 409 Conflict, 500 Internal Server Error

### ✅ Logging Security
- Sensitive fields automatically redacted from logs
- Authorization headers masked
- JSON sanitization for passwords, tokens, secrets, etc.
- Pattern-based redaction for non-JSON content

### ✅ HTTPS & Security Headers
- HTTPS redirection enforced in production
- HSTS (HTTP Strict Transport Security) enabled
- Security headers added:
  - X-Frame-Options: DENY (prevent clickjacking)
  - X-Content-Type-Options: nosniff (prevent MIME sniffing)
  - X-XSS-Protection: enabled
  - Referrer-Policy: strict-origin-when-cross-origin
  - Content-Security-Policy: configured

### ✅ Health Checks
- `/health` - Overall application health
- `/health/ready` - Readiness probe (checks database connectivity)
- Suitable for Kubernetes liveness/readiness probes

### ✅ Code Quality
- Centralized error messages in `ErrorMessages` constants
- DRY principle: refactored duplicate code in handlers
- XML documentation comments on public APIs
- Proper async/await usage with CancellationToken support

---

## 🧪 Testing the Security Improvements

### Test 1: Verify Connection String Validation
```bash
# Without setting connection string - should fail with clear error
dotnet run --project WebApi
```

Expected: Application fails to start with message about missing connection string.

### Test 2: Verify Input Validation
```bash
# POST /api/orders with invalid data
curl -X POST https://localhost:7257/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"00000000-0000-0000-0000-000000000000","description":""}'
```

Expected: 400 Bad Request with validation error.

### Test 3: Verify Health Checks
```bash
# Check overall health
curl https://localhost:7257/health

# Check readiness (database connectivity)
curl https://localhost:7257/health/ready
```

Expected: HTTP 200 with "Healthy" status.

### Test 4: Verify Logging Sanitization
```bash
# POST with sensitive-looking field (if you add authentication later)
# Check logs - sensitive data should be [REDACTED]
```

### Test 5: Verify Security Headers
```bash
curl -I https://localhost:7257/api/orders
```

Expected: Response includes security headers (X-Frame-Options, X-Content-Type-Options, etc.)

---

## ✅ IMPLEMENTED: Authentication & Authorization

**STATUS: COMPLETE** - The application now includes JWT authentication and role-based authorization.

### ✅ Implemented Features:

1. **JWT Bearer Authentication** ✅
   - JWT token generation and validation
   - Secure token-based authentication
   - Configurable token expiration and validation

2. **Role-Based Authorization** ✅
   - Four authorization policies: OrderRead, OrderWrite, OrderManagement, AdminOnly
   - Three demo roles: Admin, OrderManager, OrderViewer
   - Policy-based access control on all endpoints

3. **Rate Limiting** ✅
   - IP-based rate limiting (100 req/min, 1000 req/hour)
   - Configurable limits per endpoint
   - HTTP 429 responses on limit exceeded

4. **API Versioning** ✅
   - Version 1.0 implemented
   - Versioning headers in responses
   - Support for future version migrations

### 📚 Usage Documentation

See [AUTHENTICATION_GUIDE.md](./AUTHENTICATION_GUIDE.md) for complete documentation including:
- How to configure JWT secret keys
- Demo user credentials
- Authorization policies and roles
- Testing with cURL, Swagger, and Postman
- Rate limiting configuration
- Production security considerations

### 🔑 Quick Setup

1. **Set JWT Secret Key** (REQUIRED):
   ```bash
   cd WebApi
   dotnet user-secrets set "JwtSettings:SecretKey" "YourSecretKeyAtLeast32CharactersLong!"
   ```

2. **Get Authentication Token**:
   ```bash
   POST /api/auth/login
   {
     "username": "admin",
     "password": "admin123"
   }
   ```

3. **Use Token in Requests**:
   ```bash
   curl -H "Authorization: Bearer YOUR_TOKEN" https://localhost:7257/api/orders
   ```

### ⚠️ Production Considerations

The demo authentication endpoint is **FOR TESTING ONLY**. For production:
- Replace with real user authentication (Azure AD, Auth0, IdentityServer, etc.)
- Implement proper password hashing
- Add account lockout policies
- Enable multi-factor authentication
- Log authentication events for auditing

---

## 📊 Additional Recommendations

### Rate Limiting
Consider adding AspNetCoreRateLimit:
```bash
dotnet add package AspNetCoreRateLimit
```

### API Versioning
```bash
dotnet add package Asp.Versioning.Mvc
```

### OpenTelemetry for Observability
```bash
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

### Unit Tests
Create test projects:
```bash
dotnet new xunit -n Domain.Tests
dotnet new xunit -n Application.Tests
dotnet new xunit -n WebApi.IntegrationTests
```

---

## 📝 Configuration Checklist

Before deploying to production:

- [ ] Connection string stored securely (Key Vault, not hardcoded)
- [ ] HTTPS enabled with valid certificate
- [ ] Authentication & authorization implemented
- [ ] Rate limiting configured
- [ ] Monitoring and alerting set up
- [ ] Health checks tested
- [ ] Error handling tested
- [ ] Input validation tested
- [ ] Security headers verified
- [ ] Logs reviewed for sensitive data leaks
- [ ] Unit and integration tests passing
- [ ] Penetration testing performed

---

## 🆘 Troubleshooting

### Error: "Database connection string 'MartenDb' is not configured"
**Solution**: Set the connection string using user secrets or environment variables (see above).

### Error: Package not found during restore
**Solution**: Run `dotnet restore` or restore packages in Visual Studio.

### Health check fails
**Solution**: Verify PostgreSQL is running and connection string is correct.

### 401 Unauthorized errors (after adding auth)
**Solution**: Verify JWT token is valid and included in Authorization header.

---

## 📚 Additional Resources

- [ASP.NET Core Security Best Practices](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [User Secrets Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [Health Checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

**Last Updated**: 2025-01-24
**Version**: 1.0.0
