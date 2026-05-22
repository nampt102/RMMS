# 05 — API Conventions

## Base URL & Versioning

- Production: `https://api.rmms.example.com`
- Staging: `https://api-staging.rmms.example.com`
- All endpoints prefixed with `/api/v1/`
- API version in URL path (not header) for clarity
- Breaking changes → new version (`/api/v2/`)

## Authentication

### Header
```
Authorization: Bearer {access_token}
```

### Access token (JWT)
- Algorithm: HS256 (or RS256 if we add cert later)
- Lifetime: 15 minutes
- Payload:
  ```json
  {
    "sub": "<user_id>",
    "email": "<email>",
    "role": "pg|leader|admin|buh",
    "device_id": "<device_id>",
    "iat": 1700000000,
    "exp": 1700000900
  }
  ```

### Refresh token
- Random 256-bit, stored hashed in DB
- Lifetime: 30 days
- Rotated on each refresh (old token invalidated)

### Endpoints
- `POST /api/v1/auth/register` — PG self-registration via email
- `POST /api/v1/auth/login` — returns access + refresh
- `POST /api/v1/auth/refresh` — rotate tokens
- `POST /api/v1/auth/logout` — invalidate refresh token
- `POST /api/v1/auth/forgot-password` — send reset email
- `POST /api/v1/auth/reset-password` — with reset token
- `GET /api/v1/auth/me` — current user info

### Email-link Approval (special case)
- Endpoint `GET /api/v1/approvals/email-action` accepts token in query string
- No JWT required
- Token: HMAC-signed, single-use, 24h expiry
- See M9 module doc for details

## Request Format

### Headers
| Header | Required | Notes |
|---|---|---|
| `Content-Type` | Yes (POST/PUT) | `application/json` or `multipart/form-data` |
| `Authorization` | Yes (except auth endpoints) | `Bearer {token}` |
| `Accept-Language` | No | `vi` (default) or `en` for error messages |
| `X-Idempotency-Key` | For mutations | Optional but recommended for write ops |
| `X-Device-Id` | For mobile | Device identifier |
| `X-App-Version` | For mobile | App version (e.g., `1.0.3+12`) |

### Body
- JSON with camelCase keys (consistent with TS/Dart conventions)
- Dates: ISO 8601 with timezone (e.g., `2026-05-22T10:30:00Z`)
- Numbers: numbers, not strings
- Money: numbers (use integer minor units later if currency added)
- Enums: lowercase string values

### Example
```json
POST /api/v1/attendance/check-in
{
  "storeId": "0193..",
  "shiftId": "0193..",
  "gps": {
    "latitude": 10.776,
    "longitude": 106.700,
    "accuracy": 12.5,
    "isMocked": false
  },
  "selfieFile": "<multipart>",
  "storePhotoFile": "<multipart>",
  "note": "Optional note"
}
```

## Response Format

### Success
```json
{
  "data": { ... },
  "meta": { ... }   // pagination etc., optional
}
```

For lists with pagination:
```json
{
  "data": [ ... ],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "total": 153,
    "totalPages": 8
  }
}
```

### Error
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Localized error message",
    "details": [
      {
        "field": "email",
        "code": "INVALID_FORMAT",
        "message": "Email không hợp lệ"
      }
    ],
    "traceId": "0HMP..."
  }
}
```

## HTTP Status Codes

| Code | Meaning | When |
|---|---|---|
| 200 | OK | Successful GET/PUT/DELETE |
| 201 | Created | Successful POST creating resource |
| 204 | No Content | Successful DELETE without body |
| 400 | Bad Request | Invalid syntax, malformed JSON |
| 401 | Unauthorized | Missing/invalid token |
| 403 | Forbidden | Authenticated but no permission |
| 404 | Not Found | Resource doesn't exist (or no permission to see it) |
| 409 | Conflict | Business rule violation (e.g., duplicate, state mismatch) |
| 422 | Unprocessable Entity | Validation errors |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Unhandled exception |
| 502 / 503 | Bad Gateway / Unavailable | Upstream API down (Face, FCM) |

## Error Codes (Domain-specific)

### Auth
- `INVALID_CREDENTIALS` — wrong email/password
- `EMAIL_NOT_VERIFIED` — try to login before verifying
- `ACCOUNT_INACTIVE` — admin disabled account
- `TOKEN_EXPIRED` — JWT expired
- `TOKEN_INVALID` — malformed or wrong signature
- `REFRESH_TOKEN_REVOKED` — rotated already
- `DEVICE_NOT_AUTHORIZED` — PG using new device, needs approval
- `PASSWORD_TOO_WEAK` — failed strength check

### Attendance
- `STORE_NOT_ASSIGNED` — user can't check-in at this store
- `FAKE_GPS_DETECTED` — mock location flag set
- `FACE_VERIFICATION_FAILED` — vendor returned no match
- `FACE_NOT_ENROLLED` — user has no face template yet
- `ALREADY_CHECKED_IN` — open attendance exists
- `NO_OPEN_ATTENDANCE` — trying to check-out without check-in
- `CHECK_IN_TOO_EARLY` — more than 60 min before shift
- `SHIFT_NOT_FOUND` — invalid shift id

### Approval
- `APPROVAL_NOT_PENDING` — already decided
- `NOT_APPROVER` — user is not the designated approver
- `REJECT_REASON_REQUIRED` — must provide reason
- `EMAIL_TOKEN_EXPIRED`
- `EMAIL_TOKEN_USED`

### Form Engine
- `FORM_NOT_ASSIGNED` — user is not in targeting rules
- `FORM_EXPIRED` — past valid_to
- `FORM_DEADLINE_PASSED`
- `EDIT_NOT_ALLOWED`
- `IDEMPOTENCY_KEY_REUSED` — duplicate submission

## Pagination

Query params:
- `page` (1-indexed, default 1)
- `pageSize` (default 20, max 100)
- `sort` (e.g., `createdAt:desc` or `createdAt:asc,name:asc`)

Response includes `meta` (see above).

## Filtering

Use query params:
- `filter[status]=approved`
- `filter[startDate]=2026-01-01&filter[endDate]=2026-01-31`
- `search=keyword` — generic full-text on appropriate fields

## File Upload

### Multipart endpoint
```
POST /api/v1/attendance/check-in
Content-Type: multipart/form-data

selfie: <file>
storePhoto: <file>
data: { ... JSON ... }
```

### Or pre-signed URL (for large files / forms)
```
POST /api/v1/files/presign
{ "fileName":"...","mimeType":"...","fileSize":... }
→ { "uploadUrl":"https://minio.../...","fileKey":"..." }

# Client PUT directly to MinIO
# Then submit with fileKey reference
```

### Validation
- Selfie: max 5MB, jpeg/png
- Store photo: max 10MB, jpeg/png
- Documents: max 25MB, pdf/jpg/png/txt/docx
- Form attachments: max 10MB, jpg/png/pdf

## Rate Limiting

| Endpoint group | Limit |
|---|---|
| Auth (login, register) | 5 / minute / IP |
| Forgot password | 3 / hour / email |
| Attendance check-in | 10 / hour / user |
| Form submission | 60 / hour / user |
| General | 100 / minute / user |

Headers in response:
- `X-RateLimit-Limit`
- `X-RateLimit-Remaining`
- `X-RateLimit-Reset` (epoch)

## Idempotency

For POSTs that create things, accept `X-Idempotency-Key` header.
Server stores key → response for 24h. Re-request with same key returns cached response.

Critical for:
- Form submission (mobile flaky network)
- Check-in / Check-out
- Approval decisions

## Real-time (SignalR)

### Hub: `/hubs/team-monitoring`
Auth: JWT in query string (`?access_token=...`) due to WebSocket limitation.

Events server → client:
- `pgStatusChanged` — `{ userId, status, lastUpdate }`
- `attendanceCreated` — `{ userId, status, storeId }`
- `pendingReviewCountChanged` — `{ count }`

## CORS

Allowed origins (production):
- `https://admin.rmms.example.com`
- `https://app.rmms.example.com` (if Flutter Web build)

Methods: `GET, POST, PUT, PATCH, DELETE, OPTIONS`
Allowed headers: `Authorization, Content-Type, Accept-Language, X-Idempotency-Key, X-Device-Id, X-App-Version`

## Versioning Policy

- Backward-compatible additions: minor version bump in OpenAPI spec, no URL change
- Breaking changes: new URL prefix (`/api/v2/`)
- Deprecated endpoints: respond with `Deprecation: true` and `Sunset: <date>` headers for 6 months minimum

## OpenAPI / Swagger

Available at:
- `/swagger` — interactive docs (auth required in production)
- `/swagger/v1/swagger.json` — OpenAPI spec

Generated by Swashbuckle, kept in sync with code.

## Localization

`Accept-Language: vi` (default) or `en`
Applies to:
- Error messages
- Email content
- Notification text

NOT applied to user-generated content (forms, news) — those have explicit `_vi` / `_en` fields.
