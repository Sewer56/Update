using FluentValidation;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class GitHubReleasesDownloadOptionsValidator : AbstractValidator<IGitHubReleasesDownloadOptions>
{
    public GitHubReleasesDownloadOptionsValidator()
    {
        RuleFor(x => x.GitHubUserName)
            .Must(s => !string.IsNullOrEmpty(s))
            .WithMessage("GitHub User Name Must not be Null or Empty");

        RuleFor(x => x.GitHubRepositoryName)
            .Must(s => !string.IsNullOrEmpty(s))
            .WithMessage("GitHub Repository Name Must not be Null or Empty");
    }
}