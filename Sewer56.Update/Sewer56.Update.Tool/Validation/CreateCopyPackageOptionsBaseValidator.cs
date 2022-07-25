using FluentValidation;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Validation;

internal class CreateCopyPackageOptionsBaseValidator : AbstractValidator<ICreateCopyPackageOptionsBase>
{
    public CreateCopyPackageOptionsBaseValidator()
    {
        Include(new CurrentPackageDetailsValidator());
    }
}