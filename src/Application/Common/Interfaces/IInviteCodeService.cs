namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating unique invite codes for groups.
/// </summary>
public interface IInviteCodeService
{
    /// <summary>
    /// Generates a unique invite code based on the group's integer ID.
    /// </summary>
    /// <param name="groupId">The integer ID of the group.</param>
    /// <returns>A unique, URL-friendly invite code (e.g., "TEAM-XJ92").</returns>
    string GenerateInviteCode(int groupId);

    /// <summary>
    /// Decodes an invite code back to the original group ID.
    /// </summary>
    /// <param name="inviteCode">The invite code to decode.</param>
    /// <returns>The group ID if valid, otherwise null.</returns>
    int? DecodeInviteCode(string inviteCode);
}
