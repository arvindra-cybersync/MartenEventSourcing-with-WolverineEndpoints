namespace Domain.Constants
{
    /// <summary>
    /// Centralized error messages for consistent error handling across the application.
    /// </summary>
    public static class ErrorMessages
    {
        // Order state errors
        public const string OrderAlreadyShipped = "Cannot modify order - already shipped";
        public const string OrderCancelled = "Cannot modify order - already cancelled";
        public const string OrderAlreadyExists = "Order already exists";
        public const string OrderNotFound = "Order not found";
        public const string CannotCancelShippedOrder = "Cannot cancel a shipped order";
        public const string OrderAlreadyCancelled = "Order is already cancelled";

        // Validation errors
        public const string InvalidQuantity = "Quantity must be greater than zero";
        public const string InvalidDescription = "Description cannot be empty";
        public const string InvalidItemName = "Item name cannot be empty";
        public const string InvalidCancellationReason = "Cancellation reason is required";

        // Generic errors
        public const string UnexpectedError = "An unexpected error occurred";
        public const string OperationNotAllowed = "Operation not allowed for current order state";
    }
}
