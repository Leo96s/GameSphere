namespace GameSphere_backend.ServicesResponses
{
    /// <summary>
    /// Represents a standardized response format for service operations in the application.
    /// </summary>
    /// <typeparam name="T">The type of the data payload included in the response.</typeparam>
    /// <remarks>
    /// This generic class provides a consistent structure for all service responses, including:
    /// - Operation success status
    /// - Result data (when successful)
    /// - Status messages
    /// - Response type categorization
    /// 
    /// Used throughout the application to maintain uniform response handling.
    /// </remarks>
    public class ServiceResponse<T>
    {
        /// <summary>
        /// Gets or sets the payload data returned by the service operation.
        /// </summary>
        /// <value>
        /// The operation result data of type T when successful; otherwise may be null or default.
        /// </value>
        public T Data { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if the operation completed successfully; <c>false</c> if any errors occurred.
        /// </value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a human-readable message describing the operation result.
        /// </summary>
        /// <value>
        /// Success or error message providing additional context about the operation outcome.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the response type categorization.
        /// </summary>
        /// <value>
        /// String identifier categorizing the response type (e.g., "NotFound", "BadRequest", "Ok").
        /// Typically maps to standard HTTP status code categories.
        /// </value>
        public string Type { get; set; }
    }
}
