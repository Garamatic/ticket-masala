using System.ComponentModel.DataAnnotations;

namespace TicketMasala.Web.Utilities;



/// <summary>
/// Validate file upload size and type
/// </summary>
public class SafeFileUploadAttribute : ValidationAttribute
{
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public SafeFileUploadAttribute(long maxFileSizeInMB, params string[] allowedExtensions)
    {
        _maxFileSize = maxFileSizeInMB * 1024 * 1024; // Convert MB to bytes
        _allowedExtensions = allowedExtensions.Select(e => e.ToLowerInvariant()).ToArray();
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
            return ValidationResult.Success;

        // Check file size
        if (file.Length > _maxFileSize)
        {
            return new ValidationResult($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024} MB");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return new ValidationResult($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        return ValidationResult.Success;
    }
}
