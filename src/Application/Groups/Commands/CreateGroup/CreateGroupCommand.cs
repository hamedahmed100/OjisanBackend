using System.Text.Json;
using Ardalis.GuardClauses;
using MediatR;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Groups.Commands.CreateGroup;

public record BaseDesignDto
{
    public string? Color { get; init; }

    public string? Material { get; init; }

    public string? Pattern { get; init; }
}

public record CreateGroupCommand : IRequest<Guid>
{
    public int MaxMembers { get; init; }

    public int ProductId { get; init; }

    public BaseDesignDto? BaseDesign { get; init; }
}

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateGroupCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.OutOfRange(request.MaxMembers, nameof(request.MaxMembers), 2, int.MaxValue);
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        var group = new Group
        {
            LeaderUserId = _user.Id!,
            MaxMembers = request.MaxMembers,
            BaseDesignJson = JsonSerializer.Serialize(
                request.BaseDesign ?? new BaseDesignDto(),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
            Status = GroupStatus.Recruiting
        };

        _context.Set<Group>().Add(group);

        await _context.SaveChangesAsync(cancellationToken);

        return group.PublicId;
    }
}

