using FluentValidation;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class NuGetDownloadOptionsValidator : AbstractValidator<INuGetDownloadOptions>
{
    public NuGetDownloadOptionsValidator()
    {
        RuleFor(x => x.NuGetFeedUrl)
            .Must(s => !string.IsNullOrEmpty(s))
            .WithMessage("NuGet Feed URL Must not be Null or Empty");

        RuleFor(x => x.NuGetPackageId)
            .Must(s => !string.IsNullOrEmpty(s))
            .WithMessage("NuGet Package ID Must not be Null or Empty");
    }
}