using System;
using FluentValidation;
using Sewer56.Update.Tool.Options;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class PackageResolverOptionsValidator : AbstractValidator<IPackageResolverOptions>
{
    public PackageResolverOptionsValidator()
    {
        RuleFor(x => x.Source).IsInEnum().WithMessage("Specified download source must be valid.");
        RuleFor(x => x).Custom(ContainTheNecessaryFields);
    }

    private void ContainTheNecessaryFields(IPackageResolverOptions options, ValidationContext<IPackageResolverOptions> context)
    {
        switch (options.Source)
        {
            case DownloadSource.GitHub:
                new GitHubReleasesDownloadOptionsValidator().ValidateAndThrow(options);
                break;
            case DownloadSource.NuGet:
                new NuGetDownloadOptionsValidator().ValidateAndThrow(options);
                break;
            case DownloadSource.GameBanana:
                new GameBananaDownloadOptionsValidator().ValidateAndThrow(options);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}