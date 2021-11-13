using FluentValidation;
using Sewer56.Update.Tool.Options;

namespace Sewer56.Update.Tool.Validation;

internal class CreateCopyPackageOptionsValidator : AbstractValidator<CreateCopyPackageOptions>
{
    public CreateCopyPackageOptionsValidator() => Include(new CreateCopyPackageOptionsBaseValidator());
}