using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Application.Notifications.EventHandlers;

/// <summary>
/// Event handler for GroupAcceptedEvent.
/// Sends notification to the group leader with payment link when all submissions are approved.
/// </summary>
public class GroupAcceptedEventHandler : INotificationHandler<GroupAcceptedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookupService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroupAcceptedEventHandler> _logger;

    public GroupAcceptedEventHandler(
        IApplicationDbContext context,
        IUserLookupService userLookupService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        IConfiguration configuration,
        ILogger<GroupAcceptedEventHandler> logger)
    {
        _context = context;
        _userLookupService = userLookupService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(GroupAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == notification.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for GroupAcceptedEvent", notification.GroupId);
            return;
        }

        // Fetch the leader's user details
        var leader = await _userLookupService.GetUserDetailsAsync(group.LeaderUserId, cancellationToken);
        if (leader == null)
        {
            _logger.LogWarning("Leader user {UserId} not found for Group {GroupId}", group.LeaderUserId, notification.GroupId);
            return;
        }

        // Build payment link
        var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "https://ojisan-store.com";
        var paymentLink = $"{frontendBaseUrl.TrimEnd('/')}/groups/{group.PublicId}/payment";

        // Send email notification
        if (!string.IsNullOrWhiteSpace(leader.Email))
        {
            try
            {
                var subject = "تم قبول طلبك - Your Order Has Been Approved";
                var body = $@"
                    <html>
                    <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                        <h2>مرحباً {leader.UserName},</h2>
                        <p>تهانينا! تم قبول جميع التصاميم من فريقنا. طلبك جاهز الآن للمتابعة إلى الدفع.</p>
                        <p>Hello {leader.UserName},</p>
                        <p>Congratulations! All designs have been approved by our team. Your order is now ready to proceed to payment.</p>
                        <div style=""text-align: center; margin: 30px 0;"">
                            <a href=""{paymentLink}"" style=""background-color: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;"">
                                ادفع الآن - Pay Now
                            </a>
                        </div>
                        <p>Group ID: <strong>{group.PublicId}</strong></p>
                        <p>Invite Code: <strong>{group.InviteCode}</strong></p>
                        <br/>
                        <p>شكراً لك - Thank you</p>
                        <p>فريق Ojisan Store</p>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(leader.Email, subject, body, cancellationToken);
                _logger.LogInformation("Approval email sent to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval email to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
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
                    { "payment_link", paymentLink }
                };

                await _whatsAppService.SendMessageAsync(leader.PhoneNumber, "order_approved", parameters, cancellationToken);
                _logger.LogInformation("Approval WhatsApp sent to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send approval WhatsApp to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
                // Don't throw - notification failure shouldn't break the process
            }
        }
    }
}
