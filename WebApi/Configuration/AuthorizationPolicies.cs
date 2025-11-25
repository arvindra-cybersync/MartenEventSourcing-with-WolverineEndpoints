using Microsoft.AspNetCore.Authorization;

namespace WebApi.Configuration
{
    /// <summary>
    /// Centralized authorization policy definitions.
    /// </summary>
    public static class AuthorizationPolicies
    {
        // Policy names
        public const string OrderManagement = "OrderManagement";
        public const string OrderRead = "OrderRead";
        public const string OrderWrite = "OrderWrite";
        public const string AdminOnly = "AdminOnly";

        // Role names
        public const string AdminRole = "Admin";
        public const string OrderManagerRole = "OrderManager";
        public const string OrderViewerRole = "OrderViewer";

        /// <summary>
        /// Configures authorization policies.
        /// </summary>
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Policy: Order Management (Admin or OrderManager)
            options.AddPolicy(OrderManagement, policy =>
                policy.RequireRole(AdminRole, OrderManagerRole));

            // Policy: Order Read (Admin, OrderManager, or OrderViewer)
            options.AddPolicy(OrderRead, policy =>
                policy.RequireRole(AdminRole, OrderManagerRole, OrderViewerRole));

            // Policy: Order Write (Admin or OrderManager)
            options.AddPolicy(OrderWrite, policy =>
                policy.RequireRole(AdminRole, OrderManagerRole));

            // Policy: Admin Only
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireRole(AdminRole));
        }
    }
}
