using System.IO;
using FluentValidation;
using Sewer56.Update.Tool.Options;

namespace Sewer56.Update.Tool.Validation;

internal class DownloadPackageOptionsValidator : AbstractValidator<DownloadPackageOptions>
{
    public DownloadPackageOptionsValidator()
    {
        RuleFor(x => x.Source).IsInEnum().WithMessage("Specified download source must be valid.");
        RuleFor(x => x).Custom(ContainTheNecessaryFields);
    }

    private void ContainTheNecessaryFields(DownloadPackageOptions options, ValidationContext<DownloadPackageOptions> context)
    {
        switch (options.Source)
        {
            case DownloadSource.GitHub:
                if (string.IsNullOrEmpty(options.GitHubUserName))
                    context.AddFailure("GitHub User Name Must not be Null or Empty");

                if (string.IsNullOrEmpty(options.GitHubRepositoryName))
                    context.AddFailure("GitHub Repository Name Must not be Null or Empty");
                break;
            case DownloadSource.NuGet:
                if (string.IsNullOrEmpty(options.NuGetFeedUrl))
                    context.AddFailure("NuGet Feed URL Must not be Null or Empty");

                if (string.IsNullOrEmpty(options.NuGetPackageId))
                    context.AddFailure("NuGet Package ID Must not be Null or Empty");
                break;
            case DownloadSource.GameBanana:

                if (string.IsNullOrEmpty(options.GameBananaModType))
                    context.AddFailure("GameBanana Mod Type Must not be Null or Empty");

                if (options.GameBananaItemId <= 0)
                    context.AddFailure("GameBanana Mod ID Must be Greater than 0");
                
                break;
        }
    }
}