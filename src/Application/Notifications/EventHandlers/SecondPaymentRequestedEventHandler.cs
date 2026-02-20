using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Application.Notifications.EventHandlers;

/// <summary>
/// Event handler for SecondPaymentRequestedEvent.
/// Sends the final payment link to the group leader.
/// </summary>
public class SecondPaymentRequestedEventHandler : INotificationHandler<SecondPaymentRequestedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookupService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<SecondPaymentRequestedEventHandler> _logger;

    public SecondPaymentRequestedEventHandler(
        IApplicationDbContext context,
        IUserLookupService userLookupService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ILogger<SecondPaymentRequestedEventHandler> logger)
    {
        _context = context;
        _userLookupService = userLookupService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task Handle(SecondPaymentRequestedEvent notification, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == notification.GroupId, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for SecondPaymentRequestedEvent", notification.GroupId);
            return;
        }

        var leader = await _userLookupService.GetUserDetailsAsync(group.LeaderUserId, cancellationToken);
        if (leader == null)
        {
            _logger.LogWarning("Leader {LeaderUserId} not found for SecondPaymentRequestedEvent", group.LeaderUserId);
            return;
        }

        // Email
        if (!string.IsNullOrWhiteSpace(leader.Email))
        {
            try
            {
                var subject = "الدفعة النهائية مطلوبة - Final Payment Required";
                var body = $@"
                    <html>
                    <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                        <h2>مرحباً {leader.UserName},</h2>
                        <p>نرجو منك إكمال الدفعة النهائية لطلب مجموعتك.</p>
                        <p>Hello {leader.UserName},</p>
                        <p>Please complete the final payment for your group order.</p>
                        <p>
                            <a href=""{notification.CheckoutUrl}"">اضغط هنا لإكمال الدفع - Click here to complete payment</a>
                        </p>
                        <br/>
                        <p>شكراً لك - Thank you</p>
                        <p>فريق Ojisan Store</p>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(leader.Email, subject, body, cancellationToken);
                _logger.LogInformation("Second payment email sent to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send second payment email to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
            }
        }

        // WhatsApp
        if (!string.IsNullOrWhiteSpace(leader.PhoneNumber))
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "group_id", group.PublicId.ToString() },
                    { "payment_url", notification.CheckoutUrl }
                };

                await _whatsAppService.SendMessageAsync(leader.PhoneNumber, "second_payment_requested", parameters, cancellationToken);
                _logger.LogInformation("Second payment WhatsApp sent to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send second payment WhatsApp to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
            }
        }
    }
}

