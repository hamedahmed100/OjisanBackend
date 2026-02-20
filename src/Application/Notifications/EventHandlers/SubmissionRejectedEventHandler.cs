using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Application.Notifications.EventHandlers;

/// <summary>
/// Event handler for SubmissionRejectedEvent.
/// Sends rejection notification to the specific member (exception to leader-only rule).
/// </summary>
public class SubmissionRejectedEventHandler : INotificationHandler<SubmissionRejectedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserLookupService _userLookupService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<SubmissionRejectedEventHandler> _logger;

    public SubmissionRejectedEventHandler(
        IApplicationDbContext context,
        IUserLookupService userLookupService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        ILogger<SubmissionRejectedEventHandler> logger)
    {
        _context = context;
        _userLookupService = userLookupService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task Handle(SubmissionRejectedEvent notification, CancellationToken cancellationToken)
    {
        // Fetch the submission to get group context
        var submission = await _context.OrderSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PublicId == notification.SubmissionId, cancellationToken);

        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found for SubmissionRejectedEvent", notification.SubmissionId);
            return;
        }

        // Fetch the user who submitted (this is the exception - notify the member, not the leader)
        var user = await _userLookupService.GetUserDetailsAsync(notification.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for SubmissionRejectedEvent", notification.UserId);
            return;
        }

        // Fetch group for context
        var group = await _context.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == submission.GroupId, cancellationToken);

        // Send email notification
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var subject = "تصميمك يحتاج تعديل - Your Design Needs Revision";
                var body = $@"
                    <html>
                    <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                        <h2>مرحباً {user.UserName},</h2>
                        <p>نأسف لإبلاغك أن تصميمك يحتاج إلى بعض التعديلات قبل الموافقة عليه.</p>
                        <p>Hello {user.UserName},</p>
                        <p>We're sorry to inform you that your design needs some revisions before it can be approved.</p>
                        <div style=""background-color: #f5f5f5; padding: 15px; margin: 20px 0; border-right: 4px solid #ff6b6b;"">
                            <h3>ملاحظات الفريق - Team Feedback:</h3>
                            <p>{notification.Feedback}</p>
                        </div>
                        <p>يرجى تعديل التصميم وإعادة إرساله في أقرب وقت ممكن.</p>
                        <p>Please revise your design and resubmit it as soon as possible.</p>
                        <br/>
                        <p>شكراً لك - Thank you</p>
                        <p>فريق Ojisan Store</p>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
                _logger.LogInformation("Rejection email sent to user {Email} for Submission {SubmissionId}", user.Email, notification.SubmissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email to user {Email} for Submission {SubmissionId}", user.Email, notification.SubmissionId);
                // Don't throw - notification failure shouldn't break the process
            }
        }

        // Send WhatsApp notification
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "feedback", notification.Feedback },
                    { "submission_id", notification.SubmissionId.ToString() }
                };

                await _whatsAppService.SendMessageAsync(user.PhoneNumber, "submission_rejected", parameters, cancellationToken);
                _logger.LogInformation("Rejection WhatsApp sent to user {Phone} for Submission {SubmissionId}", user.PhoneNumber, notification.SubmissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection WhatsApp to user {Phone} for Submission {SubmissionId}", user.PhoneNumber, notification.SubmissionId);
                // Don't throw - notification failure shouldn't break the process
            }
        }
    }
}
