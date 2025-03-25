using System.ComponentModel.DataAnnotations;

namespace GameSphere_backend.Utils
{
    {
    /// <summary>
    /// Provides model validation utilities for ensuring data integrity during conversions and mappings.
    /// </summary>
    public class ConversionValidate
    {
        /// <summary>
        /// Validates whether all properties of a model object conform to their data annotations.
        /// </summary>
        /// <param name="model">The object to validate. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when the model parameter is null.</exception>
        /// <exception cref="ValidationException">
        /// Thrown when validation fails, containing all validation error messages.
        /// </exception>
        /// <remarks>
        /// This method performs comprehensive validation by:
        /// 1. Checking all properties decorated with validation attributes
        /// 2. Validating the entire object hierarchy when applicable
        /// 3. Aggregating all validation errors into a single exception
        /// 
        /// Typical validation attributes include:
        /// - [Required]
        /// - [StringLength]
        /// - [Range]
        /// - [RegularExpression]
        /// - Custom validation attributes
        /// </remarks>
        /// <example>
        /// <code>
        /// var user = new UserDto { /* properties */ };
        /// ConversionValidate.ValidateModel(user);
        /// </code>
        /// </example>
        public static void ValidateModel(object model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), "Model object cannot be null for validation.");
            }

            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();

            if (!Validator.TryValidateObject(model, context, results, validateAllProperties: true))
            {
                var errorMessages = results.Select(r => r.ErrorMessage);
                throw new ValidationException(
                    "Validation failed: " + string.Join("; ", errorMessages));
            }
        }
    }
}
