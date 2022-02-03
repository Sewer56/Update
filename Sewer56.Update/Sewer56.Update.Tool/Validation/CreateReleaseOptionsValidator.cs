using System.IO;
using System.Linq;
using FluentValidation;
using Sewer56.Update.Tool.Options;

namespace Sewer56.Update.Tool.Validation;

internal class CreateReleaseOptionsValidator : AbstractValidator<CreateReleaseOptions>
{
    public CreateReleaseOptionsValidator()
    {
        RuleFor(x => x.ExistingPackagesPath).Must(x => string.IsNullOrEmpty(x) || File.Exists(x)).WithMessage("Existing Packages Path must point to valid file.");
        RuleFor(x => x.Archiver).IsInEnum().WithMessage("Specified archiver must be valid.");
        RuleFor(x => x).Custom(BeACorrectNuGetItem);
    }

    private void BeACorrectNuGetItem(CreateReleaseOptions options, ValidationContext<CreateReleaseOptions> context)
    {
        var archiver = options.Archiver;
        if (archiver != Archiver.NuGet)
            return;

        if (string.IsNullOrEmpty(options.NuGetId))
            context.AddFailure("NuGet Id Must not be Null or Empty");

        if (string.IsNullOrEmpty(options.NuGetDescription))
            context.AddFailure("NuGet Description Must not be Null or Empty");

        if (options.NuGetAuthors.ToArray().Length > 0)
            context.AddFailure("NuGet Authors Must not be Null or Empty");
    }
}