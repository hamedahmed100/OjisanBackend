using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Users.Commands.UpdateUserAddress;

public record UpdateUserAddressCommand : IRequest
{
    public string Street { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string District { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;
}

public class UpdateUserAddressCommandHandler : IRequestHandler<UpdateUserAddressCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateUserAddressCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateUserAddressCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        Guard.Against.NullOrWhiteSpace(request.Street, nameof(request.Street));
        Guard.Against.NullOrWhiteSpace(request.City, nameof(request.City));
        Guard.Against.NullOrWhiteSpace(request.District, nameof(request.District));
        Guard.Against.NullOrWhiteSpace(request.PostalCode, nameof(request.PostalCode));
        Guard.Against.NullOrWhiteSpace(request.PhoneNumber, nameof(request.PhoneNumber));

        var existing = await _context.UserAddresses
            .FirstOrDefaultAsync(a => a.UserId == _user.Id, cancellationToken);

        if (existing is null)
        {
            existing = new UserAddress
            {
                UserId = _user.Id!
            };

            _context.UserAddresses.Add(existing);
        }

        existing.Street = request.Street;
        existing.City = request.City;
        existing.District = request.District;
        existing.PostalCode = request.PostalCode;
        existing.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

