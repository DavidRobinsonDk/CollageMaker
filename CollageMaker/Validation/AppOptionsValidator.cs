using FluentValidation;

namespace CollageMaker.Validation;

/// <summary>
/// FluentValidation validator for <see cref="AppOptions"/>.
/// </summary>
internal sealed class AppOptionsValidator : AbstractValidator<AppOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppOptionsValidator"/> class.
    /// </summary>
    public AppOptionsValidator()
    {
        RuleFor(options => options.Immich).NotNull().SetValidator(new ImmichOptionsValidator());
        RuleFor(options => options.Output).NotNull().SetValidator(new OutputOptionsValidator());
    }
}
