using System.Text.Json;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
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
    private readonly IInviteCodeService _inviteCodeService;

    public CreateGroupCommandHandler(
        IApplicationDbContext context, 
        IUser user,
        IInviteCodeService inviteCodeService)
    {
        _context = context;
        _user = user;
        _inviteCodeService = inviteCodeService;
    }

    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.OutOfRange(request.MaxMembers, nameof(request.MaxMembers), 2, int.MaxValue);
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        // Verify the product exists and is active before creating the group
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                $"Product with ID {request.ProductId} not found or is not active.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var group = new Group
            {
                LeaderUserId = _user.Id!,
                ProductId = request.ProductId,
                MaxMembers = request.MaxMembers,
                BaseDesignJson = JsonSerializer.Serialize(
                    request.BaseDesign ?? new BaseDesignDto(),
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                Status = GroupStatus.Recruiting
            };

            _context.Groups.Add(group);

            // Save first to get the generated ID
            await _context.SaveChangesAsync(cancellationToken);

            // Generate and set the invite code based on the group's integer ID
            group.InviteCode = _inviteCodeService.GenerateInviteCode(group.Id);

            // Save again to persist the invite code
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return group.PublicId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

