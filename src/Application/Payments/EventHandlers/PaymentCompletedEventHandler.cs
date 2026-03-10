using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Application.Payments.EventHandlers;

/// <summary>
/// Event handler for PaymentCompletedEvent.
/// Automatically creates Trello card and OTO shipping label when payment succeeds.
/// </summary>
public class PaymentCompletedEventHandler : INotificationHandler<PaymentCompletedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly ITrelloService _trelloService;
    private readonly IShippingService _shippingService;
    private readonly IUserLookupService _userLookupService;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        IApplicationDbContext context,
        ITrelloService trelloService,
        IShippingService shippingService,
        IUserLookupService userLookupService,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        IConfiguration configuration,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _context = context;
        _trelloService = trelloService;
        _shippingService = shippingService;
        _userLookupService = userLookupService;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.GroupId.HasValue)
        {
            await HandleGroupPaymentAsync(notification, cancellationToken);
        }
        else if (notification.OrderSubmissionId.HasValue)
        {
            await HandleSingleOrderPaymentAsync(notification, cancellationToken);
        }
        else
        {
            _logger.LogWarning("PaymentCompletedEvent {PaymentId} has neither GroupId nor OrderSubmissionId",
                notification.PaymentPublicId);
        }
    }

    private async Task HandleGroupPaymentAsync(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .Include(g => g.Submissions)
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == notification.GroupId!.Value, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for PaymentCompletedEvent {PaymentId}",
                notification.GroupId, notification.PaymentPublicId);
            return;
        }

        // Fetch product details
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == group.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for Group {GroupId}",
                group.ProductId, group.Id);
            return;
        }

        // Fetch leader user for shipping address
        // Note: In a real scenario, you'd have a Users table or call Identity API
        // For now, we'll use LeaderUserId and assume address is stored elsewhere
        // This is a placeholder - adjust based on your actual user/address storage

        // Extract badge image URLs from submissions
        var memberSubmissions = group.Submissions.Select(s => new MemberSubmissionDto
        {
            UserId = s.UserId,
            BadgeImageUrl = ExtractBadgeImageUrl(s.CustomDesignJson),
            CustomDesignJson = s.CustomDesignJson,
            Price = s.Price
        }).ToList();

        // Create Trello card
        try
        {
            var orderDetails = new OrderDetailsDto
            {
                GroupId = group.PublicId,
                LeaderUserId = group.LeaderUserId,
                ProductId = product.Id,
                ProductName = product.Name,
                MaxMembers = group.MaxMembers,
                MemberSubmissions = memberSubmissions
            };

            var trelloCardId = await _trelloService.CreateCardAsync(orderDetails, cancellationToken);
            group.TrelloCardId = trelloCardId;

            _logger.LogInformation("Trello card created successfully for Group {GroupId}. Card ID: {CardId}",
                group.Id, trelloCardId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Trello card for Group {GroupId}. Payment succeeded but Trello integration failed.",
                group.Id);
            // Don't throw - payment succeeded, Trello failure shouldn't block the process
        }

        // Generate OTO shipping label
        try
        {
            var leaderForShipping = await _userLookupService.GetUserDetailsAsync(group.LeaderUserId, cancellationToken);

            var address = await _context.UserAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == group.LeaderUserId, cancellationToken);

            var recipientName = leaderForShipping?.UserName ?? "Group Leader";
            var phoneNumber = address?.PhoneNumber ?? leaderForShipping?.PhoneNumber ?? "+966500000000";
            var addressLine1 = address?.Street ?? "123 Main Street";
            var city = address?.City ?? "Riyadh";
            var district = address?.District ?? "Al Olaya";
            var postalCode = address?.PostalCode ?? "12345";

            var shippingDetails = new ShippingDetailsDto
            {
                GroupId = group.PublicId,
                OrderSubmissionId = null,
                RecipientName = recipientName,
                PhoneNumber = phoneNumber,
                AddressLine1 = addressLine1,
                City = city,
                District = district,
                PostalCode = postalCode,
                ItemCount = group.Submissions.Count,
                TotalValue = group.Submissions.Sum(s => s.Price)
            };

            var shippingResult = await _shippingService.GenerateLabelAsync(shippingDetails, cancellationToken);
            group.TrackingNumber = shippingResult.TrackingNumber;
            group.ShippingLabelUrl = shippingResult.ShippingLabelUrl;

            _logger.LogInformation("OTO shipping label generated successfully for Group {GroupId}. Tracking: {TrackingNumber}",
                group.Id, shippingResult.TrackingNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OTO shipping label for Group {GroupId}. Payment succeeded but OTO integration failed.",
                group.Id);
            // Don't throw - payment succeeded, OTO failure shouldn't block the process
        }

        // Mark the group as paid (transition to Finalized status)
        // This should happen after successful payment processing
        try
        {
            group.MarkAsPaid();
            _logger.LogInformation("Group {GroupId} marked as paid and transitioned to Finalized status", group.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark group {GroupId} as paid. Payment succeeded but status transition failed.", group.Id);
            // Don't throw - payment succeeded, status transition failure shouldn't block the process
            // However, this should be investigated as it indicates a state machine issue
        }

        // Send notification to leader about payment completion
        var leader = await _userLookupService.GetUserDetailsAsync(group.LeaderUserId, cancellationToken);
        if (leader != null)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == notification.PaymentPublicId, cancellationToken);

            // Send email notification
            if (!string.IsNullOrWhiteSpace(leader.Email))
            {
                try
                {
                    var subject = "تم تأكيد الدفع - Payment Confirmed";
                    var body = $@"
                        <html>
                        <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                            <h2>مرحباً {leader.UserName},</h2>
                            <p>تم تأكيد الدفع بنجاح! طلبك الآن في مرحلة الإنتاج.</p>
                            <p>Hello {leader.UserName},</p>
                            <p>Payment has been confirmed successfully! Your order is now in production.</p>
                            <div style=""background-color: #f5f5f5; padding: 15px; margin: 20px 0;"">
                                <h3>تفاصيل الدفع - Payment Details:</h3>
                                <p>Transaction ID: <strong>{payment?.TransactionId ?? "N/A"}</strong></p>
                                <p>Amount: <strong>{payment?.Amount:C}</strong></p>
                                <p>Group ID: <strong>{group.PublicId}</strong></p>
                            </div>
                            {(string.IsNullOrWhiteSpace(group.TrackingNumber) ? "" : $@"
                            <div style=""background-color: #e8f5e9; padding: 15px; margin: 20px 0;"">
                                <h3>معلومات الشحن - Shipping Information:</h3>
                                <p>Tracking Number: <strong>{group.TrackingNumber}</strong></p>
                                {(string.IsNullOrWhiteSpace(group.ShippingLabelUrl) ? "" : $@"<p><a href=""{group.ShippingLabelUrl}"">View Shipping Label</a></p>")}
                            </div>")}
                            <p>سيتم إشعارك عند بدء الشحن.</p>
                            <p>You will be notified when shipping begins.</p>
                            <br/>
                            <p>شكراً لك - Thank you</p>
                            <p>فريق Ojisan Store</p>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(leader.Email, subject, body, cancellationToken);
                    _logger.LogInformation("Payment confirmation email sent to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send payment confirmation email to leader {Email} for Group {GroupId}", leader.Email, notification.GroupId);
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
                        { "transaction_id", payment?.TransactionId ?? "N/A" },
                        { "amount", payment?.Amount.ToString("C") ?? "N/A" }
                    };

                    if (!string.IsNullOrWhiteSpace(group.TrackingNumber))
                    {
                        parameters.Add("tracking_number", group.TrackingNumber);
                    }

                    await _whatsAppService.SendMessageAsync(leader.PhoneNumber, "payment_confirmed", parameters, cancellationToken);
                    _logger.LogInformation("Payment confirmation WhatsApp sent to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send payment confirmation WhatsApp to leader {Phone} for Group {GroupId}", leader.PhoneNumber, notification.GroupId);
                    // Don't throw - notification failure shouldn't break the process
                }
            }
        }

        // Save all updates (including status change to Finalized)
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSingleOrderPaymentAsync(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        var submission = await _context.OrderSubmissions
            .Include(s => s.Badges)
            .FirstOrDefaultAsync(s => s.Id == notification.OrderSubmissionId!.Value, cancellationToken);

        if (submission == null)
        {
            _logger.LogWarning("OrderSubmission {OrderSubmissionId} not found for PaymentCompletedEvent {PaymentId}",
                notification.OrderSubmissionId, notification.PaymentPublicId);
            return;
        }

        if (submission.GroupId != null)
        {
            _logger.LogWarning("OrderSubmission {Id} has GroupId; use group payment flow", submission.Id);
            return;
        }

        var product = submission.ProductId.HasValue
            ? await _context.Products.FirstOrDefaultAsync(p => p.Id == submission.ProductId.Value, cancellationToken)
            : null;

        // Create Trello card for single order
        if (product != null)
        {
            try
            {
                var orderDetails = new OrderDetailsDto
                {
                    GroupId = submission.PublicId,
                    LeaderUserId = submission.UserId,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    MaxMembers = 1,
                    MemberSubmissions = new List<MemberSubmissionDto>
                    {
                        new()
                        {
                            UserId = submission.UserId,
                            BadgeImageUrl = ExtractBadgeImageUrl(submission.CustomDesignJson),
                            CustomDesignJson = submission.CustomDesignJson,
                            Price = submission.Price
                        }
                    }
                };

                var trelloCardId = await _trelloService.CreateCardAsync(orderDetails, cancellationToken);
                _logger.LogInformation("Trello card created for single order {SubmissionId}. Card ID: {CardId}",
                    submission.PublicId, trelloCardId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Trello card for single order {SubmissionId}", submission.PublicId);
            }
        }

        // Generate OTO shipping label
        try
        {
            var userForShipping = await _userLookupService.GetUserDetailsAsync(submission.UserId, cancellationToken);
            var address = await _context.UserAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == submission.UserId, cancellationToken);

            var recipientName = userForShipping?.UserName ?? "Customer";
            var phoneNumber = address?.PhoneNumber ?? userForShipping?.PhoneNumber ?? "+966500000000";
            var addressLine1 = address?.Street ?? "123 Main Street";
            var city = address?.City ?? "Riyadh";
            var district = address?.District ?? "Al Olaya";
            var postalCode = address?.PostalCode ?? "12345";

            var shippingDetails = new ShippingDetailsDto
            {
                GroupId = null,
                OrderSubmissionId = submission.PublicId,
                RecipientName = recipientName,
                PhoneNumber = phoneNumber,
                AddressLine1 = addressLine1,
                City = city,
                District = district,
                PostalCode = postalCode,
                ItemCount = 1,
                TotalValue = submission.Price
            };

            var shippingResult = await _shippingService.GenerateLabelAsync(shippingDetails, cancellationToken);
            submission.TrackingNumber = shippingResult.TrackingNumber;
            submission.ShippingLabelUrl = shippingResult.ShippingLabelUrl;

            _logger.LogInformation("OTO shipping label generated for single order {SubmissionId}. Tracking: {TrackingNumber}",
                submission.PublicId, shippingResult.TrackingNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OTO shipping label for single order {SubmissionId}", submission.PublicId);
        }

        submission.IsPaid = true;

        // Send notification
        var user = await _userLookupService.GetUserDetailsAsync(submission.UserId, cancellationToken);
        if (user != null)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PublicId == notification.PaymentPublicId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var subject = "تم تأكيد الدفع - Payment Confirmed";
                    var body = $@"
                        <html>
                        <body dir=""rtl"" style=""font-family: Arial, sans-serif;"">
                            <h2>مرحباً {user.UserName},</h2>
                            <p>تم تأكيد الدفع بنجاح! طلبك الآن في مرحلة الإنتاج.</p>
                            <p>Hello {user.UserName},</p>
                            <p>Payment has been confirmed successfully! Your order is now in production.</p>
                            <div style=""background-color: #f5f5f5; padding: 15px; margin: 20px 0;"">
                                <h3>تفاصيل الدفع - Payment Details:</h3>
                                <p>Transaction ID: <strong>{payment?.TransactionId ?? "N/A"}</strong></p>
                                <p>Amount: <strong>{payment?.Amount:C}</strong></p>
                            </div>
                            {(string.IsNullOrWhiteSpace(submission.TrackingNumber) ? "" : $@"
                            <div style=""background-color: #e8f5e9; padding: 15px; margin: 20px 0;"">
                                <h3>معلومات الشحن - Shipping Information:</h3>
                                <p>Tracking Number: <strong>{submission.TrackingNumber}</strong></p>
                            </div>")}
                            <br/>
                            <p>شكراً لك - Thank you</p>
                            <p>فريق Ojisan Store</p>
                        </body>
                        </html>";

                    await _emailService.SendEmailAsync(user.Email, subject, body, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send payment confirmation email for single order {SubmissionId}", submission.PublicId);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string ExtractBadgeImageUrl(string customDesignJson)
    {
        if (string.IsNullOrWhiteSpace(customDesignJson))
        {
            return string.Empty;
        }

        // Extract badge image URL from JSON
        // This is a simplified version - you may want to use System.Text.Json to parse properly
        var urlMatch = System.Text.RegularExpressions.Regex.Match(
            customDesignJson,
            @"""badgeImageUrl""\s*:\s*""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return urlMatch.Success ? urlMatch.Groups[1].Value : string.Empty;
    }
}
