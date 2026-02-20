using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Groups.Queries.GetGroupInviteLink;

public record GetGroupInviteLinkQuery : IRequest<string>
{
    public Guid GroupId { get; init; }
}

public class GetGroupInviteLinkQueryHandler : IRequestHandler<GetGroupInviteLinkQuery, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public GetGroupInviteLinkQueryHandler(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> Handle(GetGroupInviteLinkQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));

        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                nameof(Group), request.GroupId);
        }

        var frontendBaseUrl = _configuration["FrontendBaseUrl"] 
            ?? throw new InvalidOperationException("FrontendBaseUrl is not configured in appsettings.json");

        // Ensure the URL doesn't end with a slash
        var baseUrl = frontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/join/{group.InviteCode}";
    }
}
