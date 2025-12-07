using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Utilities;
    /// <summary>
    /// Validates that a field is required only if another field has a specific value.
    /// Useful for conditional validation in forms.
    /// </summary>
    /// <example>
    /// [RequiredIf("IsNewCustomer", true, ErrorMessage = "First name is required for new customer")]
    /// public string? NewCustomerFirstName { get; set; }
    /// </example>
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _dependentProperty;
        private readonly object _targetValue;

        public RequiredIfAttribute(string dependentProperty, object targetValue)
        {
            _dependentProperty = dependentProperty;
            _targetValue = targetValue;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var property = validationContext.ObjectType.GetProperty(_dependentProperty);
            if (property == null)
            {
                return new ValidationResult($"Unknown property: {_dependentProperty}");
            }

            var dependentValue = property.GetValue(validationContext.ObjectInstance);
            
            // Check if the dependent property has the target value
            if (Equals(dependentValue, _targetValue))
            {
                // If it does, the current property is required
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validates that a string contains only valid name characters (letters, spaces, hyphens, apostrophes)
    /// </summary>
    public class ValidNameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Use [Required] for required validation
            }

            var name = value.ToString()!;
            
            // Allow letters, spaces, hyphens, and apostrophes
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$"))
            {
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} contains invalid characters. Only letters, spaces, hyphens and apostrophes are allowed.");
            }

            return ValidationResult.Success;
        }
}
