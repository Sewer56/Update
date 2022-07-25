using FluentValidation;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class GameBananaDownloadOptionsValidator : AbstractValidator<IGameBananaDownloadOptions>
{
    public GameBananaDownloadOptionsValidator()
    {
        RuleFor(x => x.GameBananaModType)
            .Must(s => !string.IsNullOrEmpty(s))
            .WithMessage("GameBanana Mod Type Must not be Null or Empty");

        RuleFor(x => x.GameBananaItemId)
            .Must(s => s > 0)
            .WithMessage("GameBanana Mod ID Must be Greater than 0");
    }
}