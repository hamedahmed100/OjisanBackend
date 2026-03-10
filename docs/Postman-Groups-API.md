# Groups API – Postman request/response reference

Base URL: `https://localhost:5001` (or set `{{baseUrl}}` in Postman)

All endpoints under **Create Group**, **Join Group**, and **Submit Design** require authentication unless marked otherwise. Use the Bearer token from the Login response below.

---

## 0. Auth (get token for Group APIs)

**Login (get access token):**  
**POST** `{{baseUrl}}/api/Users/login`

**Headers:**  
`Content-Type: application/json`

**Request body:**

```json
{
  "email": "ojiuser_01@gmail.com",
  "password": "YourPassword123!"
}
```

**Response 200 OK:**

```json
{
  "tokenType": "Bearer",
  "accessToken": "CfDJ8...",
  "expiresIn": 604800
}
```

Use `accessToken` in subsequent requests:  
`Authorization: Bearer <accessToken>`

(If your app uses **signin** instead: POST `{{baseUrl}}/api/Users/signin` with the same body; it may use cookies. For Postman, prefer **login** to get a token.)

---

## 1. Create group (leader)

**POST** `{{baseUrl}}/api/Groups`

**Headers:**  
`Content-Type: application/json`  
`Authorization: Bearer <token>`

**Request body – same jacket colors (uniform):**

```json
{
  "memberCount": 4,
  "name": "فريق النجوم",
  "isUniformColorSelected": true,
  "productPublicId": "00000000-0000-0000-0000-000000000001",
  "baseDesign": {
    "color": "navy",
    "material": "polyester",
    "pattern": "solid"
  },
  "nameBehind": "اسم الفريق",
  "addOnIds": []
}
```

**Request body – different jacket colors (non-uniform):**

```json
{
  "memberCount": 4,
  "name": "فريق النجوم",
  "isUniformColorSelected": false,
  "productPublicId": "00000000-0000-0000-0000-000000000001",
  "addOnIds": []
}
```

**Response 201 Created:**

```json
{
  "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "priceBreakdown": {
    "originalProductPrice": 1200.00,
    "discountedProductPrice": 1020.00,
    "addonPrice": 0,
    "discountAmount": 180.00,
    "subtotal": 1200.00,
    "finalTotal": 1020.00,
    "promotionApplied": true,
    "appliedDiscountPercentage": 15
  }
}
```

**Response 400:** Invalid member count, product not found, etc.

---

## 2. Get my groups

**GET** `{{baseUrl}}/api/Groups/mine`

**Headers:**  
`Authorization: Bearer <token>`

**Response 200 OK:**

```json
[
  {
    "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
    "name": "فريق النجوم",
    "role": "Leader",
    "status": "Recruiting",
    "maxMembers": 4,
    "membersJoinedCount": 2,
    "membersSubmittedCount": 0,
    "isUniformColorSelected": false,
    "inviteCode": "TEAM-VQN2",
    "createdAt": "2026-03-08T11:55:15.958851Z"
  }
]
```

---

## 3. Get group details (by group id)

**GET** `{{baseUrl}}/api/Groups/00c1bde8-fbb0-4054-b9a9-e8997deabe54`

Replace `00c1bde8-fbb0-4054-b9a9-e8997deabe54` with your group’s `groupId`.

**Headers:**  
`Authorization: Bearer <token>`

**Response 200 OK:**

```json
{
  "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "name": "فريق النجوم",
  "status": 0,
  "maxMembers": 4,
  "membersJoinedCount": 2,
  "membersSubmittedCount": 0,
  "inviteCode": "TEAM-VQN2",
  "inviteLink": "http://localhost:4200/join/TEAM-VQN2",
  "isUniformColorSelected": false,
  "baseDesignJson": "{\"color\":\"\",\"material\":\"\",\"pattern\":\"\"}",
  "members": [
    {
      "userId": "fb14ba59-5d00-47cc-b65d-7b756c701bfd",
      "displayName": "ojiuser_01@gmail.com",
      "isLeader": true,
      "joinedAt": "2026-03-08T11:55:15.958851+00:00",
      "hasSubmitted": false
    },
    {
      "userId": "7784715d-bdb3-4cba-828f-8381bd94b114",
      "displayName": "ojiuser_02@gmail.com",
      "isLeader": false,
      "joinedAt": "2026-03-08T11:55:39.8202788+00:00",
      "hasSubmitted": false
    }
  ],
  "submissions": [
    {
      "submissionId": "a530a2bb-ca6b-4388-b138-5645463b2768",
      "userId": "fb14ba59-5d00-47cc-b65d-7b756c701bfd",
      "displayName": "ojiuser_01@gmail.com",
      "isLeader": true,
      "status": "Draft",
      "customDesignJson": "",
      "nameBehind": "",
      "price": 0.00,
      "badges": [],
      "addOns": []
    }
  ]
}
```

- `status`: 0 = Recruiting, 1 = ReadyForReview, 2 = Accepted, 3 = Finalized  
- `hasSubmitted`: true only when the member has submitted (status not Draft).  
- `membersSubmittedCount`: count of submissions that are not Draft.

**Response 403:** Not a member of this group.  
**Response 404:** Group not found.

---

## 4. Get group by invite code (anonymous, for join page)

**GET** `{{baseUrl}}/api/Groups/invite/TEAM-VQN2`

Code can be with or without `TEAM-` prefix (e.g. `VQN2` or `TEAM-VQN2`).

**Response 200 OK:**

```json
{
  "publicId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "name": "فريق النجوم",
  "leaderUserId": "fb14ba59-5d00-47cc-b65d-7b756c701bfd",
  "productId": 1,
  "maxMembers": 4,
  "currentMembers": 2,
  "status": 0,
  "inviteCode": "TEAM-VQN2"
}
```

**Response 404:** Invalid or expired code, or group not recruiting.

---

## 5. Join group

**POST** `{{baseUrl}}/api/Groups/invite/TEAM-VQN2/join`

Use the same invite code format as in “Get group by invite code”.

**Headers:**  
`Authorization: Bearer <token>`

**Request body:** none (or empty `{}`).

**Response 204 No Content**

**Response 400/404:** Invalid code, group full, or already a member.

---

## 6. Submit member design (leader or member)

**POST** `{{baseUrl}}/api/Groups/00c1bde8-fbb0-4054-b9a9-e8997deabe54/submissions`

Replace the GUID with your group id. Body must include the same `groupId`.

**Headers:**  
`Content-Type: application/json`  
`Authorization: Bearer <token>`

**Request body – group with different jacket colors (non-uniform):**

```json
{
  "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "customDesignJson": "{\"color\":\"navy\",\"material\":\"polyester\",\"pattern\":\"solid\"}",
  "badges": [
    { "imageUrl": "/uploads/badges/badge1.png", "comment": "Comment 1" },
    { "imageUrl": "/uploads/badges/badge2.png", "comment": "Comment 2" },
    { "imageUrl": "/uploads/badges/badge3.png", "comment": "Comment 3" }
  ],
  "addOnIds": [],
  "nameBehind": "اسمي"
}
```

**Request body – group with same jacket colors (uniform):**  
`customDesignJson` and `nameBehind` are ignored (taken from group). Still send at least 3 badges:

```json
{
  "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "customDesignJson": "",
  "badges": [
    { "imageUrl": "/uploads/badges/badge1.png", "comment": "Comment 1" },
    { "imageUrl": "/uploads/badges/badge2.png", "comment": "Comment 2" },
    { "imageUrl": "/uploads/badges/badge3.png", "comment": "Comment 3" }
  ],
  "addOnIds": [],
  "nameBehind": ""
}
```

- Badges: 3–12 items, each with non-empty `comment`.  
- For non-uniform groups, `customDesignJson` is required.

**Response 201 Created:**

```json
"a530a2bb-ca6b-4388-b138-5645463b2768"
```

(Submission public id as string.)

**Response 400:** Badge count/comment validation, or missing custom design for non-uniform group.

---

## 7. Update submission (e.g. after rejection)

**PUT** `{{baseUrl}}/api/Groups/00c1bde8-fbb0-4054-b9a9-e8997deabe54/submissions/a530a2bb-ca6b-4388-b138-5645463b2768`

Replace first GUID with `groupId`, second with `submissionId`.

**Headers:**  
`Content-Type: application/json`  
`Authorization: Bearer <token>`

**Request body:**

```json
{
  "groupId": "00c1bde8-fbb0-4054-b9a9-e8997deabe54",
  "submissionId": "a530a2bb-ca6b-4388-b138-5645463b2768",
  "newCustomDesignJson": "{\"color\":\"black\",\"material\":\"cotton\",\"pattern\":\"striped\"}",
  "badges": [
    { "imageUrl": "/uploads/badges/b1.png", "comment": "Updated 1" },
    { "imageUrl": "/uploads/badges/b2.png", "comment": "Updated 2" },
    { "imageUrl": "/uploads/badges/b3.png", "comment": "Updated 3" }
  ],
  "addOnIds": [],
  "nameBehind": "اسم محدث"
}
```

- `newCustomDesignJson` is required.  
- For uniform groups, design/nameBehind come from group; you can still send badges/addOns.  
- Submission must be Rejected and unlocked for editing.

**Response 204 No Content**

**Response 400/403/404:** Validation, not owner, or submission/group not found.

---

## 8. Get group invite link

**GET** `{{baseUrl}}/api/Groups/00c1bde8-fbb0-4054-b9a9-e8997deabe54/invite-link`

**Headers:**  
`Authorization: Bearer <token>`

**Response 200 OK:**

```json
"http://localhost:4200/join/TEAM-VQN2"
```

(Plain string URL.)

---

## 9. Validate promotion (anonymous)

**GET** `{{baseUrl}}/api/Groups/promotions/validate?memberCount=6`

**Response 200 OK:**

```json
{
  "isActive": true,
  "promotionName": "Uniform Colour 15% Off",
  "discountPercent": 15,
  "endDateUtc": "2026-12-31T23:59:59Z"
}
```

---

## 10. Get discount eligibility (anonymous)

**GET** `{{baseUrl}}/api/Groups/discount-eligibility?productPublicId=00000000-0000-0000-0000-000000000001&memberCount=6&isUniformColorSelected=true&addOnIds=`

Query params:

- `productPublicId` (guid, required)  
- `memberCount` (int, required)  
- `isUniformColorSelected` (bool, required)  
- `addOnIds` (optional) comma-separated guids

**Response 200 OK:**

```json
{
  "isEligibleForDiscount": true,
  "promotionName": "Uniform Colour 15% Off",
  "discountPercent": 15,
  "promotionEndDateUtc": "2026-12-31T23:59:59Z"
}
```

---

## Suggested Postman flow

1. **Sign in:** POST `/api/Users/signin` → copy token.  
2. **Create group:** POST `/api/Groups` (as leader) → copy `groupId`.  
3. **Get group details:** GET `/api/Groups/{{groupId}}` → confirm `membersSubmittedCount` 0, leader `hasSubmitted` false.  
4. **Get invite info (optional):** GET `/api/Groups/invite/TEAM-XXXX` (use invite code from step 2/3).  
5. **Join (as another user):** POST `/api/Groups/invite/TEAM-XXXX/join` with second user’s token.  
6. **Submit leader design:** POST `/api/Groups/{{groupId}}/submissions` with leader token and body containing `groupId`.  
7. **Submit member design:** POST `/api/Groups/{{groupId}}/submissions` with member token.  
8. **Get group details again:** GET `/api/Groups/{{groupId}}` → confirm `membersSubmittedCount` and `hasSubmitted` as expected.

For **update after rejection**, use PUT `/api/Groups/{{groupId}}/submissions/{{submissionId}}` with the submission owner’s token.
