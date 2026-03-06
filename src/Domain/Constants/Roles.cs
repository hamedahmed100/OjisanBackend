namespace OjisanBackend.Domain.Constants;

/// <summary>
/// Identity/authorization role names (used in ASP.NET Identity and RequireRole).
/// Only Administrator is assigned as an Identity role; group leader/member are derived from Group and GroupMember data.
/// </summary>
public abstract class Roles
{
    /// <summary>Site administrator. Assigned in Identity.</summary>
    public const string Administrator = nameof(Administrator);

    /// <summary>Reserved for future use. Group leadership is determined by Group.LeaderUserId, not Identity roles.</summary>
    public const string GroupLeader = nameof(GroupLeader);

    /// <summary>Reserved for future use. Group membership is determined by GroupMember table, not Identity roles.</summary>
    public const string GroupMember = nameof(GroupMember);
}

/// <summary>
/// Role values returned to the frontend (e.g. from GET /api/Users/me).
/// Only Admin and User; group leader/member are per-group and come from group APIs.
/// </summary>
public static class ApiRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}