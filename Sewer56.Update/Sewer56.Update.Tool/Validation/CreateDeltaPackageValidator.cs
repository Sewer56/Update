using System.IO;
using FluentValidation;
using NuGet.Versioning;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class CreateDeltaPackageValidator : AbstractValidator<ICreateDeltaPackageOptions>
{
    public CreateDeltaPackageValidator()
    {
        Include(new CreateCopyPackageOptionsBaseValidator());
        RuleFor(x => x.LastVersionFolderPath).NotEmpty().WithMessage("Path for package folder must not be empty.");
        RuleFor(x => x.LastVersionFolderPath).Must(Directory.Exists).WithMessage("Directory used for package must exist.");
        RuleFor(x => x.LastVersion).Must(x => NuGetVersion.TryParse(x, out _)).WithMessage("Must be a valid semantic version compatible with NuGet.");
    }
}