using HashidsNet;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Services;

/// <summary>
/// Implementation of IInviteCodeService using Hashids.net to generate
/// short, unique, non-sequential invite codes from group IDs.
/// </summary>
public class InviteCodeService : IInviteCodeService
{
    private readonly Hashids _hashids;

    public InviteCodeService()
    {
        // Using a salt for security. In production, this should come from configuration.
        // Format: "TEAM-XXXX" where XXXX is the hashed ID
        _hashids = new Hashids("OjisanBackend-InviteCode-Salt-2024", minHashLength: 4, alphabet: "ABCDEFGHJKLMNPQRSTUVWXYZ23456789");
    }

    public string GenerateInviteCode(int groupId)
    {
        if (groupId <= 0)
            throw new ArgumentException("Group ID must be greater than zero.", nameof(groupId));

        var hash = _hashids.Encode(groupId);
        return $"TEAM-{hash}";
    }

    public int? DecodeInviteCode(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            return null;

        // Remove "TEAM-" prefix if present
        var code = inviteCode.StartsWith("TEAM-", StringComparison.OrdinalIgnoreCase)
            ? inviteCode.Substring(5)
            : inviteCode;

        var decoded = _hashids.Decode(code);
        return decoded.Length > 0 ? decoded[0] : null;
    }
}
