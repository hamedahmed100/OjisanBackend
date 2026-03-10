# Admin Review Logic – Implementation Plan

## Overview

Enable admins to review all submissions under review (single orders and groups), **accept** them (move to **pending payment**) or **reject** with comments. Single orders and groups have slightly different flows.

---

## Business Rules Summary

| Scenario | Accept | Reject |
|----------|--------|--------|
| **Single buyer** | Status → **Accepted** (pending payment). User can pay. | Status → **Rejected**. Admin must provide comment. User must resubmit to address feedback; status stays Rejected until resubmit. |
| **Group** | Per submission: **Accept** → submission status **Accepted**. When **all** submissions accepted → group status **Accepted** (pending payment). | Per submission: **Reject** with **required comment**. Rejected members get status **Rejected** and can resubmit. When **all** members rejected → group status **Rejected**. |

---

## Part 1: Domain Changes

### 1.1 GroupStatus enum

Add a new value for “all members rejected”:

| File | Change |
|------|--------|
| `Domain/Enums/GroupStatus.cs` | Add `Rejected = 5` |

```csharp
public enum GroupStatus
{
    Recruiting = 0,
    ReadyForReview = 1,
    Accepted = 2,
    Finalized = 3,
    Cancelled = 4,
    Rejected = 5   // All members rejected; group stays in this state until members resubmit
}
```

### 1.2 Group entity

- **EvaluateGroupStatus** (existing): when group is `ReadyForReview`, if **all** submissions are **Accepted** → set group status to **Accepted** (already implemented).
- **New logic in same method**: when group is `ReadyForReview`, if **all** submissions are **Rejected** → set group status to **Rejected** and (optional) raise `GroupRejectedEvent` for notifications.

```csharp
// Inside EvaluateGroupStatus(), add after the "all accepted" block:
if (_submissions.Count > 0 && _submissions.All(s => s.Status == SubmissionStatus.Rejected))
{
    Status = GroupStatus.Rejected;
    AddDomainEvent(new GroupRejectedEvent(Id));  // optional
}
```

- **Recruiting / Rejected → ReadyForReview**: when a previously rejected member resubmits via `UpdateSubmission`, their submission goes back to `Submitted`. If the group was `Rejected`, you may want to move it back to `ReadyForReview` when at least one submission is no longer Rejected (e.g. one resubmitted). So in `UpdateSubmissionCommand` after `submission.UpdateDesign(...)` and `group.EvaluateGroupStatus()`, ensure that when group is `Rejected` and we now have at least one non-Rejected submission, set `Group.Status = GroupStatus.ReadyForReview`. Alternatively, add a method `Group.EvaluateBackToReadyForReview()` that sets status to `ReadyForReview` when not all submissions are Rejected.

Recommendation: add `EvaluateGroupStatus()` logic for “all rejected” → `GroupStatus.Rejected`. For “group was Rejected and someone resubmitted”, either:
- in `EvaluateGroupStatus()`: if status is `Rejected` and not all submissions are `Rejected`, set status to `ReadyForReview`; or  
- call a small helper from `UpdateSubmissionCommand` that does the same.

### 1.3 OrderSubmission (existing)

- **Accept()**: sets status to **Accepted**, clears `AdminFeedback`. No change needed.
- **Reject(string feedback)**: sets status to **Rejected**, sets `AdminFeedback`, unlocks edit, raises **SubmissionRejectedEvent**. No change needed.

### 1.4 Single-order resubmit after rejection

- Single orders use **ReadyForReview** (not **Submitted**). After rejection, user must be able to resubmit and set status back to **ReadyForReview**.
- **Option A**: Add `OrderSubmission.ResubmitAfterRejection()` that sets status to `ReadyForReview` and clears `AdminFeedback` (and optionally locks edit again until next review). Call it from a new **ResubmitSingleOrderCommand** that validates the submission is single (`GroupId == null`), is `Rejected`, and updates design/badges/add-ons as needed.
- **Option B**: Reuse/expand a single-order “update” endpoint so it: allows updates when status is `Rejected` and `GroupId == null`, updates design/badges/etc., then sets status to `ReadyForReview` and clears `AdminFeedback`.

Plan assumes **Option A**: add domain method and **ResubmitSingleOrder** (or **UpdateSingleOrderAfterRejection**) command used only for single orders.

---

## Part 2: Admin – List All Submissions Under Review

Admin needs one place to see **all** items waiting for review: single orders and groups.

### 2.1 Option A – Single unified endpoint (recommended)

**Query**: `GetAllSubmissionsUnderReviewQuery`

Returns:

- **Single orders**: submissions where `GroupId == null` and `Status == SubmissionStatus.ReadyForReview`.
- **Groups**: groups where `Status == GroupStatus.ReadyForReview`, with their submissions (each submission status can be Submitted/Accepted/Rejected).

Response shape:

- `SingleSubmissions`: list of single submission DTOs (e.g. `SubmissionId`, `UserId`, `Price`, `Status`, `CreatedAt`, `BadgeImageUrl`, etc.).
- `GroupsInReview`: list of group DTOs (e.g. `GroupId`, `Name`, `InviteCode`, `Submissions` with same submission summary).

This reuses the idea of existing `GetGroupsInReview` but adds single submissions and keeps one admin “review queue” API.

### 2.2 Option B – Keep existing + add single

- Keep **GET** `/api/admin/pipeline/review` as “groups in review” (existing **GetGroupsInReviewQuery**).
- Add **GET** `/api/admin/pipeline/review/single` → **GetSingleSubmissionsInReviewQuery** (submissions with `GroupId == null`, `Status == ReadyForReview`).

Plan assumes **Option A** for a single “review queue” endpoint; implementation can be one query that returns both lists.

---

## Part 3: Admin – Accept / Reject Single Order

Single order = one submission with `GroupId == null`, currently `ReadyForReview`.

### 3.1 Accept single submission

- **Endpoint**: e.g. **PUT** `/api/admin/pipeline/submissions/{submissionId:guid}/accept`
- **Handler**: Load submission by `PublicId`, ensure `GroupId == null` and `Status == ReadyForReview`, then call `submission.Accept()` and save.
- **Response**: **204 No Content** (or 200 with minimal confirmation).

No request body required.

### 3.2 Reject single submission

- **Endpoint**: e.g. **PUT** `/api/admin/pipeline/submissions/{submissionId:guid}/reject`
- **Body**: `{ "feedback": "string" }` — required.
- **Handler**: Load submission by `PublicId`, ensure `GroupId == null` and `Status == ReadyForReview`, then call `submission.Reject(request.Feedback)` and save. Existing **SubmissionRejectedEvent** will fire for notifications.
- **Response**: **204 No Content**.

---

## Part 4: Admin – Accept / Reject Group Submissions (existing + extensions)

### 4.1 Current behaviour (keep)

- **PUT** `/api/admin/pipeline/groups/{groupId:guid}/submissions/{submissionId:guid}/review`
- **Body**: `{ "isApproved": true | false, "feedback": "string (required when isApproved is false)" }`
- **Handler**: **ReviewSubmissionCommand** — accepts or rejects **one** submission in the group, then calls `group.EvaluateGroupStatus()`.

This already supports:
- Accept one → that submission becomes Accepted; when all accepted, group becomes Accepted.
- Reject one → that submission Rejected with feedback; feedback required.

### 4.2 Required domain change for “all rejected”

- In **Group.EvaluateGroupStatus()**, add: if all submissions are **Rejected**, set `Group.Status = GroupStatus.Rejected`.
- Then **ReviewSubmissionCommand** needs no signature change: after each reject, `EvaluateGroupStatus()` will set group to Rejected when the last submission is rejected.

### 4.3 Optional: Batch review for a group

- **Endpoint**: e.g. **PUT** `/api/admin/pipeline/groups/{groupId:guid}/review-batch`
- **Body**: list of `{ "submissionId": "guid", "isApproved": bool, "feedback": "string | null" }` — one entry per submission; feedback required when `isApproved === false`.
- **Handler**: For each item, load submission, call Accept() or Reject(feedback), then call `group.EvaluateGroupStatus()` once at the end. Validates that every submission in the group is reviewed exactly once and feedback is provided for every rejection.

This is optional; the existing one-by-one review endpoint is enough if you prefer simpler API.

---

## Part 5: User – Resubmit After Rejection

### 5.1 Group member (existing)

- **UpdateSubmission** (existing) already allows a rejected group member to update design and resubmit; `UpdateDesign()` sets status back to **Submitted** and clears feedback. After that, `EvaluateGroupStatus()` (and any new “back to ReadyForReview” logic when group was Rejected) keeps group state consistent.

### 5.2 Single order

- New command: **ResubmitSingleOrderCommand** (or **UpdateSingleOrderAfterRejectionCommand**).
- **Endpoint**: e.g. **PUT** `/api/users/submissions/{submissionId:guid}/resubmit` (or under orders/single).
- **Body**: same as submit (e.g. design, badges, add-ons, name behind) so the user can fix what was rejected.
- **Handler**: Load submission by `PublicId`, ensure `UserId == current user`, `GroupId == null`, `Status == Rejected`. Update design/badges/add-ons/name, then call new `OrderSubmission.ResubmitAfterRejection()` (or equivalent) that sets status to **ReadyForReview** and clears `AdminFeedback`. Save.
- **Response**: **204 No Content** or **200** with updated submission summary.

---

## Part 6: API Summary

### 6.1 New or changed admin endpoints

| Method | Path | Description | Body | Response |
|--------|------|-------------|------|----------|
| **GET** | `/api/admin/pipeline/review` | **(Replace or extend)** Return all submissions under review: single + groups. | — | See § 6.2 |
| **PUT** | `/api/admin/pipeline/submissions/{submissionId}/accept` | Accept a **single** submission → status **Accepted** (pending payment). | None | 204 No Content |
| **PUT** | `/api/admin/pipeline/submissions/{submissionId}/reject` | Reject a **single** submission with comment → status **Rejected**. | `{ "feedback": "string" }` | 204 No Content |
| **PUT** | `/api/admin/pipeline/groups/{groupId}/submissions/{submissionId}/review` | **(Existing)** Accept or reject **one** group submission. | `{ "isApproved": bool, "feedback": "string?" }` | 204 No Content |
| **PUT** | `/api/admin/pipeline/groups/{groupId}/review-batch` | **(Optional)** Accept/reject multiple group submissions in one call. | See § 6.3 | 204 No Content |

### 6.2 GET `/api/admin/pipeline/review` – Response body

**Unified “under review” response (Option A):**

```json
{
  "singleSubmissions": [
    {
      "submissionId": "guid",
      "userId": "string",
      "price": 0,
      "status": "ReadyForReview",
      "createdAt": "2025-03-08T12:00:00Z",
      "badgeImageUrl": "string | null",
      "productId": 0
    }
  ],
  "groupsInReview": [
    {
      "groupId": "guid",
      "name": "string",
      "leaderUserId": "string",
      "productId": 0,
      "maxMembers": 0,
      "currentSubmissions": 0,
      "inviteCode": "string",
      "submissions": [
        {
          "submissionId": "guid",
          "userId": "string",
          "price": 0,
          "status": "Submitted | Accepted | Rejected",
          "badgeImageUrl": "string | null"
        }
      ]
    }
  ]
}
```

If you keep the current **GET review** as groups-only, then add **GET** `/api/admin/pipeline/review/single` returning only `singleSubmissions` array.

### 6.3 PUT single reject – Request body

```json
{
  "feedback": "Please correct the badge text and re-upload image."
}
```

- **feedback** (string, required): comment shown to the user for the rejection.

### 6.4 PUT group review (existing) – Request body

```json
{
  "isApproved": true
}
```

or

```json
{
  "isApproved": false,
  "feedback": "Badge 2 image is blurry; please upload a higher resolution."
}
```

- **isApproved** (boolean, required): `true` = accept, `false` = reject.
- **feedback** (string, required when `isApproved` is `false`): comment for the rejected member.

### 6.5 PUT group review-batch (optional) – Request body

```json
{
  "decisions": [
    { "submissionId": "guid-1", "isApproved": true },
    { "submissionId": "guid-2", "isApproved": false, "feedback": "Please fix badge order." },
    { "submissionId": "guid-3", "isApproved": false, "feedback": "Name behind has a typo." }
  ]
}
```

- **decisions** (array, required): one entry per group submission.
- Each entry: **submissionId** (guid), **isApproved** (bool), **feedback** (string, required when `isApproved` is false).

**Response**: 204 No Content. On validation error (e.g. missing feedback for a rejection, or submission not in group), return 400 with error details.

### 6.6 User resubmit single order – Request body (example)

**PUT** `/api/users/submissions/{submissionId}/resubmit` (or equivalent)

```json
{
  "customDesignJson": "string",
  "nameBehind": "string | null",
  "badges": [ { "imageUrl": "string", "comment": "string" } ],
  "addOnIds": [ "guid" ]
}
```

Same structure as submit single order; all fields optional except those required by business rules (e.g. at least design or badges).  
**Response**: 204 No Content or 200 with updated submission summary.

---

## Part 7: Implementation Checklist

### Implementation Status (Completed)

- [x] **Domain**
  - [ ] Add `GroupStatus.Rejected = 5`.
  - [ ] In `Group.EvaluateGroupStatus()`, when all submissions are Rejected, set `Status = GroupStatus.Rejected` (and optional `GroupRejectedEvent`).
  - [ ] When group is Rejected and a member resubmits, set group back to `ReadyForReview` (in `EvaluateGroupStatus` or in `UpdateSubmissionCommand`).
  - [ ] Add `OrderSubmission.ResubmitAfterRejection()` (or equivalent) for single orders; use in single-order resubmit command.
- [ ] **Admin – List under review**
  - [ ] Implement **GetAllSubmissionsUnderReviewQuery** (single + groups) and change **GET** `/api/admin/pipeline/review` to return the unified response, **or** add **GetSingleSubmissionsInReviewQuery** and **GET** `/api/admin/pipeline/review/single`.
- [ ] **Admin – Single order**
  - [ ] **AcceptSingleSubmissionCommand** + **PUT** `.../submissions/{id}/accept`.
  - [ ] **RejectSingleSubmissionCommand** + **PUT** `.../submissions/{id}/reject` with body `{ "feedback": "..." }`.
- [ ] **Admin – Group**
  - [ ] Ensure **ReviewSubmissionCommand** still calls `group.EvaluateGroupStatus()` after each decision (already does).
  - [ ] (Optional) **ReviewGroupBatchCommand** + **PUT** `.../groups/{id}/review-batch`.
- [ ] **User – Resubmit single**
  - [ ] **ResubmitSingleOrderCommand** (or **UpdateSingleOrderAfterRejectionCommand**) + **PUT** user endpoint for resubmit with body as in § 6.6.
- [ ] **Mapping**
  - [ ] In **GetMyOrders** / display status, map `GroupStatus.Rejected` to the same display as “Rejected” (e.g. `OrderDisplayStatus.Rejected`).
- [ ] **Spec / docs**
  - [ ] Update OpenAPI (e.g. `wwwroot/api/specification.json`) and any Postman/docs for new and changed endpoints and request/response bodies.

---

## Part 8: Status Flows (recap)

**Single order**

- ReadyForReview → **(Admin accept)** → Accepted (pending payment).
- ReadyForReview → **(Admin reject + comment)** → Rejected → **(User resubmit)** → ReadyForReview.

**Group**

- ReadyForReview → **(Admin accept all)** → each submission Accepted → group **Accepted** (pending payment).
- ReadyForReview → **(Admin reject one or more, with comment per rejection)** → those submissions Rejected; when **all** rejected → group **Rejected**.
- Rejected group → **(Member resubmits)** → submission Submitted (and optionally group back to ReadyForReview) → admin can review again.

This plan keeps existing domain behaviour where possible, adds only the minimal enum and evaluation logic, and documents the new APIs and request/response shapes for implementation and client integration.

---

## Summary of Newly Created / Modified APIs

| # | Method | Path | Purpose |
|---|--------|------|---------|
| 1 | GET | `/api/admin/pipeline/review` | **Modified or extended.** Return all items under review: single submissions + groups with their submissions. |
| 2 | PUT | `/api/admin/pipeline/submissions/{submissionId}/accept` | **New.** Accept a single order → status **Accepted** (pending payment). No body. |
| 3 | PUT | `/api/admin/pipeline/submissions/{submissionId}/reject` | **New.** Reject a single order. **Body:** `{ "feedback": "string" }` (required). |
| 4 | PUT | `/api/admin/pipeline/groups/{groupId}/submissions/{submissionId}/review` | **Existing.** Accept or reject one group submission. **Body:** `{ "isApproved": bool, "feedback": "string?" }` — feedback required when rejecting. |
| 5 | PUT | `/api/admin/pipeline/groups/{groupId}/review-batch` | **New (optional).** Accept/reject multiple group submissions in one call. **Body:** `{ "decisions": [ { "submissionId": "guid", "isApproved": bool, "feedback": "string?" } ] }`. |
| 6 | PUT | `/api/users/submissions/{submissionId}/resubmit` | **New.** User resubmits a rejected **single** order (design, badges, add-ons). **Body:** same shape as submit single order. |

**Responses**

- **GET review:** `200 OK` with JSON: `{ "singleSubmissions": [ ... ], "groupsInReview": [ ... ] }`.
- **PUT accept (single):** `204 No Content`.
- **PUT reject (single):** `204 No Content`.
- **PUT group review (single/batch):** `204 No Content`.
- **PUT resubmit (single):** `204 No Content` or `200 OK` with updated submission.

**Errors**

- `404` if submission or group not found.
- `400` if feedback missing on reject, or invalid body.
- `403` for non-admin on admin endpoints, or non-owner on resubmit.
