using CollageMaker.Validation;
using FluentValidation;

namespace CollageMaker.Services;

/// <summary>
/// Validates application options.
/// </summary>
internal static class OptionsValidationService
{
    /// <summary>
    /// Validates options and throws when invalid.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    public static void Validate(AppOptions options)
    {
        IValidator<AppOptions> validator = new AppOptionsValidator();
        validator.ValidateAndThrow(options);
    }
}
