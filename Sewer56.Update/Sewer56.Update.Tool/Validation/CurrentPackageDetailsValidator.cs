using System.IO;
using FluentValidation;
using NuGet.Versioning;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class CurrentPackageDetailsValidator : AbstractValidator<ICurrentPackageDetails>
{
    public CurrentPackageDetailsValidator()
    {
        RuleFor(x => x.FolderPath).NotEmpty().WithMessage("Path for package folder must not be empty.");
        RuleFor(x => x.FolderPath).Must(Directory.Exists).WithMessage("Directory used for package must exist.");

        RuleFor(x => x.Version).Must(x => NuGetVersion.TryParse(x, out _)).WithMessage("Must be a valid semantic version compatible with NuGet.");
        RuleFor(x => x.IgnoreRegexesPath).Must(x => string.IsNullOrEmpty(x) || File.Exists(x)).WithMessage("Ignore regexes path must point to valid file.");
    }
}