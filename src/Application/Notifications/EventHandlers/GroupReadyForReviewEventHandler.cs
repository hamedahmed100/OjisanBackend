using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Application.Notifications.EventHandlers;

/// <summary>
/// Event handler for GroupReadyForReviewEvent.
/// Sends notification to the group leader when all members have submitted their designs.
/// </summary>
public class GroupReadyForReviewEventHandler : INotificationHandler<GroupReadyForReviewEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookupService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<GroupReadyForReviewEventHandler> _logger;

    public GroupReadyForReviewEventHandler(
        IApplicationDbContext context,
        IUserLookupService userLookupService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ILogger<GroupReadyForReviewEventHandler> logger)
    {
        _context = context;
        _userLookupService = userLookupService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task Handle(GroupReadyForReviewEvent notification, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == notification.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for GroupReadyForReviewEvent", notification.GroupId);
            return;
        }

        // Fetch the leader's user details
        var leader = await _userLookupService.GetUserDetailsAsync(group.LeaderUserId, cancellationToken);
        if (leader == null)
        {
            _logger.LogWarning("Leader user {UserId} not found for Group {GroupId}", group.LeaderUserId, notification.GroupId);
            return;
        }

        // Send email notification
        if (!string.IsNullOrWhiteSpace(leader.Email))
        {
            try
            {
                var subject = "طلبك جاهز للمراجعة - Your Order is Ready for Review";
                var body = $@"
                    <html>
                    <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                        <h2>مرحباً {leader.UserName},</h2>
                        <p>تم إرسال جميع التصاميم من أعضاء المجموعة. طلبك الآن جاهز للمراجعة من قبل فريقنا.</p>
                        <p>Hello {leader.UserName},</p>
                        <p>All group members have submitted their designs. Your order is now ready for review by our team.</p>
                        <p>Group ID: <strong>{group.PublicId}</strong></p>
                        <p>Invite Code: <strong>{group.InviteCode}</strong></p>
                        <br/>
                        <p>شكراً لك - Thank you</p>
                        <p>فريق Ojisan Store</p>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(leader.Email, subject, body, cancellationToken);
                _logger.LogInformation("Email notification sent to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
                // Don't throw - notification failure shouldn't break the process
            }
        }

        // Send WhatsApp notification
        if (!string.IsNullOrWhiteSpace(leader.PhoneNumber))
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "group_id", group.PublicId.ToString() },
                    { "invite_code", group.InviteCode }
                };

                await _whatsAppService.SendMessageAsync(leader.PhoneNumber, "order_ready_for_review", parameters, cancellationToken);
                _logger.LogInformation("WhatsApp notification sent to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp notification to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
                // Don't throw - notification failure shouldn't break the process
            }
        }
    }
}
