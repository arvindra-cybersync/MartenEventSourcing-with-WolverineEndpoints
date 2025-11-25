using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Configuration;
using Wolverine.Http;

namespace WebApi.Endpoints
{
    /// <summary>
    /// Authentication endpoints for JWT token generation.
    /// NOTE: This is a simplified implementation for demonstration.
    /// In production, integrate with your identity provider (Azure AD, Auth0, IdentityServer, etc.)
    /// </summary>
    public static class AuthenticationEndpoints
    {
        /// <summary>
        /// Login request model.
        /// </summary>
        public record LoginRequest(string Username, string Password);

        /// <summary>
        /// Login response with JWT token.
        /// </summary>
        public record LoginResponse(string Token, string TokenType, int ExpiresIn, string[] Roles);

        /// <summary>
        /// Authenticates user and generates JWT token.
        /// NOTE: This is a DEMO implementation. Replace with real authentication.
        /// </summary>
        [WolverinePost("/api/auth/login")]
        public static IResult Login(
            LoginRequest request,
            [FromServices] IConfiguration configuration,
            [FromServices] ILogger<LoginRequest> logger)
        {
            // ⚠️ DEMO ONLY - DO NOT USE IN PRODUCTION
            // In production, validate against your user store (database, Azure AD, etc.)
            var (username, password) = request;

            logger.LogInformation("Login attempt for user: {Username}", username);

            // Demo users for testing (REPLACE WITH REAL AUTHENTICATION)
            var validUsers = new Dictionary<string, (string Password, string[] Roles)>
            {
                { "admin", ("admin123", new[] { "Admin" }) },
                { "manager", ("manager123", new[] { "OrderManager" }) },
                { "viewer", ("viewer123", new[] { "OrderViewer" }) }
            };

            if (!validUsers.TryGetValue(username, out var userInfo) || userInfo.Password != password)
            {
                logger.LogWarning("Failed login attempt for user: {Username}", username);
                return Results.Unauthorized();
            }

            // Generate JWT token
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
            if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            {
                logger.LogError("JWT settings not configured");
                return Results.Problem("Authentication service is not properly configured", statusCode: 500);
            }

            var token = GenerateJwtToken(username, userInfo.Roles, jwtSettings);

            logger.LogInformation("Successful login for user: {Username}", username);

            return Results.Ok(new LoginResponse(
                Token: token,
                TokenType: "Bearer",
                ExpiresIn: jwtSettings.ExpirationMinutes * 60,
                Roles: userInfo.Roles
            ));
        }

        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        private static string GenerateJwtToken(string username, string[] roles, JwtSettings settings)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
                Issuer = settings.Issuer,
                Audience = settings.Audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
