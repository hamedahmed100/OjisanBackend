# Order Fulfillment Status – Implementation Plan

## Overview

Extend the order status flow to support **Processing**, **Shipped** for group orders, and **Paid/Shipping/Shipped** for single orders.

---

## Part 1: Processing and Shipped for Group Orders

### 1.1 Domain Changes

| Entity | Change |
|--------|--------|
| `Group` | Add `ShippedAt` (DateTime?) – when admin marks order as delivered |
| `Group` | Add `MarkAsShipped()` method |

### 1.2 Status Mapping (Group)

| Group.Status | TrackingNumber | ShippedAt | Display Status |
|--------------|----------------|-----------|----------------|
| Finalized | null | null | Processing (in production) |
| Finalized | set | null | Shipping (in transit) |
| Finalized | set | set | Shipped (delivered) |
| Finalized | null | set | Shipped (edge case) |

### 1.3 Admin Endpoint

- **PUT** `/api/admin/pipeline/groups/{id}/mark-shipped` – sets `Group.ShippedAt = DateTime.UtcNow`

---

## Part 2: Single Orders – Paid/Shipping/Shipped

### 2.1 Domain Changes

| Entity | Change |
|--------|--------|
| `OrderSubmission` | Add `TrackingNumber` (string?), `ShippingLabelUrl` (string?), `ShippedAt` (DateTime?) - for single orders |
| `OrderSubmission` | Add `IsPaid` (bool) – set when payment completes |
| `OrderSubmission` | Add `MarkAsShipped()` method |
| `Payment` | Make `GroupId` nullable (int?) |
| `Payment` | Add `OrderSubmissionId` (int?) – one of GroupId or OrderSubmissionId required |

### 2.2 Payment Flow for Single Orders

1. User submits single order → OrderSubmission (Status = ReadyForReview)
2. Admin accepts → Status = Accepted
3. User calls CreatePaymentSession with OrderSubmissionId
4. Payment webhook completes → PaymentCompletedEvent (OrderSubmissionId)
5. Handler: create Trello card, generate OTO label, set OrderSubmission.IsPaid, TrackingNumber, ShippingLabelUrl
6. Admin marks shipped → OrderSubmission.ShippedAt

### 2.3 API Changes

- **CreatePaymentSession**: Add optional `OrderSubmissionId` – when set, create payment for single order
- **PaymentCompletedEvent**: Add `OrderSubmissionId?` – handler branches on GroupId vs OrderSubmissionId
- **IShippingService**: Extend `ShippingDetailsDto` to support single order (OrderSubmissionId, UserId for address)

### 2.4 Admin Endpoint

- **PUT** `/api/admin/pipeline/submissions/{id}/mark-shipped` – sets `OrderSubmission.ShippedAt` for single orders

### 2.5 Status Mapping (Single Order)

| OrderSubmission | IsPaid | TrackingNumber | ShippedAt | Display Status |
|-----------------|--------|----------------|----------|----------------|
| Accepted | false | - | - | Accepted |
| Accepted | true | null | null | Paid |
| Accepted | true | set | null | Shipping |
| Accepted | true | set | set | Shipped |

---

## Migration

- Add `ShippedAt` to Groups
- Add `ProductId`, `TrackingNumber`, `ShippingLabelUrl`, `ShippedAt`, `IsPaid` to OrderSubmissions
- Add `OrderSubmissionId` to Payments, make `GroupId` nullable
- Add check constraint or validation: Payment must have GroupId XOR OrderSubmissionId

---

## Implementation Status (Completed)

### Admin Endpoints

| Endpoint | Description |
|----------|-------------|
| **PUT** `/api/admin/pipeline/groups/{id}/mark-shipped` | Marks a group order as shipped (sets `Group.ShippedAt`). Requires group status = Finalized. |
| **PUT** `/api/admin/pipeline/submissions/{id}/mark-shipped` | Marks a single order as shipped (sets `OrderSubmission.ShippedAt`). Requires order to be paid. |

### CreatePaymentSession

- Accepts either `groupId` or `orderSubmissionId` in the request body.
- For single orders: order must be Accepted, not yet paid.

### Migration

- Run: `dotnet ef database update -p src/Infrastructure -s src/Web`
- Migration: `AddOrderFulfillmentAndSingleOrderPayment`
