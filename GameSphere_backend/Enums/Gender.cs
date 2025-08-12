namespace GameSphere_backend.Enums
{
    /// <summary>
    /// Represents the gender identity options for users in the system.
    /// </summary>
    /// <remarks>
    /// This enum is used throughout the application to standardize gender representation
    /// and ensure consistent handling of gender-related data. The values are intentionally
    /// in Portuguese to match the application's primary language context.
    /// </remarks>
    public enum Gender
    {
        /// <summary>
        /// Male gender identity.
        /// </summary>
        /// <remarks>
        /// Used when the user identifies as male.
        /// </remarks>
        MASCULINO,

        /// <summary>
        /// Female gender identity.
        /// </summary>
        /// <remarks>
        /// Used when the user identifies as female.
        /// </remarks>
        FEMININO,

        /// <summary>
        /// Other gender identity or non-binary.
        /// </summary>
        /// <remarks>
        /// Used when the user identifies with a gender that isn't exclusively
        /// male or female, or prefers not to specify.
        /// </remarks>
        OUTRO
    }
}
