using System.IO;
using FluentValidation;
using Sewer56.Update.Tool.Options;

namespace Sewer56.Update.Tool.Validation;

internal class CreateReleaseOptionsValidator : AbstractValidator<CreateReleaseOptions>
{
    public CreateReleaseOptionsValidator()
    {
        RuleFor(x => x.ExistingPackagesPath).Must(x => string.IsNullOrEmpty(x) || File.Exists(x)).WithMessage("Existing Packages Path must point to valid file.");
        RuleFor(x => x.Archiver).IsInEnum().WithMessage("Specified archiver must be valid.");
        RuleFor(x => x.Archiver).Must(BeACorrectNuGetItem);
    }

    private bool BeACorrectNuGetItem(CreateReleaseOptions options, Archiver archiver)
    {
        if (archiver != Archiver.NuGet)
            return true;

        RuleFor(x => x.NuGetId).NotNull().NotEmpty().WithMessage("NuGet Id Must not be Null or Empty");
        RuleFor(x => x.NuGetDescription).NotNull().NotEmpty().WithMessage("NuGet Description Must not be Null or Empty");
        RuleFor(x => x.NuGetAuthors).NotNull().NotEmpty().WithMessage("NuGet Authors Must not be Null or Empty");

        return true;
    }
}