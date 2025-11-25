namespace WebApi.Configuration
{
    /// <summary>
    /// JWT authentication settings.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        /// <summary>
        /// The secret key used to sign JWT tokens.
        /// IMPORTANT: Store this securely in user secrets or Azure Key Vault.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// The issuer of the JWT token (your API).
        /// </summary>
        public string Issuer { get; set; } = "OrderManagementAPI";

        /// <summary>
        /// The audience for the JWT token (your API consumers).
        /// </summary>
        public string Audience { get; set; } = "OrderManagementAPI";

        /// <summary>
        /// Token expiration time in minutes.
        /// </summary>
        public int ExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Whether to validate the token lifetime.
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Whether to validate the issuer.
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate the audience.
        /// </summary>
        public bool ValidateAudience { get; set; } = true;
    }
}
