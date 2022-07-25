using FluentValidation;
using Sewer56.Update.Tool.Options;

namespace Sewer56.Update.Tool.Validation;

internal class AutoCreateDeltaValidator : AbstractValidator<AutoCreateDeltaOptions>
{
    public AutoCreateDeltaValidator()
    {
        Include(new PackageResolverOptionsValidator());
        Include(new CurrentPackageDetailsValidator());
        RuleFor(x => x.NumReleases).Must(x => x > 0).WithMessage("Number of releases must be greater than 0.");
    }
}