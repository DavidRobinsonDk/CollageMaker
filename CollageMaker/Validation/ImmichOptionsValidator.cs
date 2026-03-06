using FluentValidation;

namespace CollageMaker.Validation;

/// <summary>
/// FluentValidation validator for <see cref="ImmichOptions"/>.
/// </summary>
internal sealed class ImmichOptionsValidator : AbstractValidator<ImmichOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImmichOptionsValidator"/> class.
    /// </summary>
    public ImmichOptionsValidator()
    {
        RuleFor(options => options.BaseUrl).NotEmpty().WithMessage("Immich baseUrl is required.");
        RuleFor(options => options.ApiKey).NotEmpty().WithMessage("Immich apiKey is required.");
        RuleFor(options => options.PersonId).NotEmpty().WithMessage("Immich personId is required.");
        RuleFor(options => options.ImageCount).InclusiveBetween(2, 30).WithMessage("ImageCount must be between 2 and 30.");
    }
}
