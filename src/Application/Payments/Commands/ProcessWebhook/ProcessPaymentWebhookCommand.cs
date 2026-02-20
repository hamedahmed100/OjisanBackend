using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Events;
using System.Text.Json;

namespace OjisanBackend.Application.Payments.Commands.ProcessWebhook;

public record ProcessPaymentWebhookCommand : IRequest
{
    public string Payload { get; init; } = string.Empty;

    public string Signature { get; init; } = string.Empty;
}

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;

    public ProcessPaymentWebhookCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Payload, nameof(request.Payload));
        Guard.Against.NullOrWhiteSpace(request.Signature, nameof(request.Signature));

        // Validate webhook signature first - CRITICAL SECURITY STEP
        if (!_paymentService.ValidateWebhookSignature(request.Payload, request.Signature))
        {
            throw new UnauthorizedAccessException("Invalid webhook signature. Request rejected.");
        }

        // Parse the Fatorah webhook payload
        var webhookData = JsonDocument.Parse(request.Payload);
        var root = webhookData.RootElement;

        // Extract merchant_resource_id (which is our Payment PublicId)
        var merchantResourceId = root.GetProperty("merchant_resource_id").GetString();
        Guard.Against.NullOrWhiteSpace(merchantResourceId, nameof(merchantResourceId));

        // Extract transaction status
        var transactionStatus = root.GetProperty("status").GetString();
        Guard.Against.NullOrWhiteSpace(transactionStatus, nameof(transactionStatus));

        // Extract transaction ID if available
        var transactionId = root.TryGetProperty("transaction_id", out var txIdElement)
            ? txIdElement.GetString()
            : string.Empty;

        // Find the payment by PublicId
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PublicId.ToString() == merchantResourceId, cancellationToken);

        if (payment is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Payment), merchantResourceId!);
        }

        // Update payment status based on webhook
        if (transactionStatus.Equals("success", StringComparison.OrdinalIgnoreCase) ||
            transactionStatus.Equals("completed", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Completed;
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                payment.TransactionId = transactionId;
            }

            // Raise domain event
            payment.AddDomainEvent(new PaymentCompletedEvent(payment.GroupId, payment.PublicId));
        }
        else if (transactionStatus.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                 transactionStatus.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Failed;
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                payment.TransactionId = transactionId;
            }
        }
        else
        {
            // Unknown status - log but don't update
            throw new InvalidOperationException($"Unknown payment status received: {transactionStatus}");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
