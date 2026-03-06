using FluentValidation;

namespace CollageMaker.Validation;

/// <summary>
/// FluentValidation validator for <see cref="OutputOptions"/>.
/// </summary>
internal sealed class OutputOptionsValidator : AbstractValidator<OutputOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputOptionsValidator"/> class.
    /// </summary>
    public OutputOptionsValidator()
    {
        RuleFor(options => options.Width).GreaterThan(0).WithMessage("Output width must be positive.");
        RuleFor(options => options.Height).GreaterThan(0).WithMessage("Output height must be positive.");
        RuleFor(options => options.Format).NotEmpty().WithMessage("Output format is required.");
        RuleFor(options => options.DownloadedImagesDirectory)
            .NotEmpty()
            .WithMessage("DownloadedImagesDirectory is required when SaveDownloadedImages is enabled.")
            .When(options => options.SaveDownloadedImages);
    }
}
