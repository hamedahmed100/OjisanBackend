using FluentValidation;

namespace OjisanBackend.Application.Users.Commands.UpdateUserAddress;

public class UpdateUserAddressCommandValidator : AbstractValidator<UpdateUserAddressCommand>
{
    public UpdateUserAddressCommandValidator()
    {
        RuleFor(v => v.Street).NotEmpty();
        RuleFor(v => v.City).NotEmpty();
        RuleFor(v => v.District).NotEmpty();
        RuleFor(v => v.PostalCode).NotEmpty();
        RuleFor(v => v.PhoneNumber).NotEmpty();
    }
}

