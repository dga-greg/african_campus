# 5. REST API Specification

Base path: `/api/v1`. JSON only (except uploads: `multipart/form-data`). Status column: **✅ implemented (Phase 1)**, otherwise the phase that delivers it.

## 5.1 Conventions

**Success**
```json
{ "data": { ... } }
{ "data": [ ... ], "meta": { "page": 1, "pageSize": 25, "total": 312, "totalPages": 13 } }
```
**Error**
```json
{ "error": { "code": "VALIDATION_FAILED", "message": "Request validation failed", "details": [{ "path": "email", "message": "Invalid email" }], "requestId": "9f1c…" } }
```
Error codes (`contracts/src/api.ts`): `VALIDATION_FAILED` 400 · `UNAUTHENTICATED` 401 · `INVALID_CREDENTIALS` 401 · `SESSION_EXPIRED` 401 · `CSRF_INVALID` 403 · `FORBIDDEN` 403 · `ACCOUNT_SUSPENDED` 403 · `NOT_FOUND` 404 · `CONFLICT` 409 · `EMAIL_TAKEN` 409 · `VERSION_CONFLICT` 409 · `ACCOUNT_LOCKED` 423 · `RATE_LIMITED` 429 · `INTERNAL` 500.

* **Pagination:** `?page=1&pageSize=25` (max 100). Lists return `meta`.
* **Filtering/sorting:** explicit, whitelisted query parameters per endpoint (`?generation=3&branchId=…&sort=surname`).
* **Concurrency:** updatable resources return `version`; `PATCH` bodies include it and get `409 VERSION_CONFLICT` if stale.
* **Auth:** cookies `fh_access` (JWT, 15 min, path `/api`) and `fh_refresh` (opaque, 30 days, path `/api/v1/auth`). All `POST/PUT/PATCH/DELETE` require header `X-XSRF-TOKEN` equal to cookie `XSRF-TOKEN` (obtain with `GET /auth/csrf`).
* **Request id:** every response has `X-Request-Id`.
* **Rate limits:** global 300 req/min/IP; auth endpoints 10 req/15 min/IP for login, register, forgot/reset password.

## 5.2 Authentication

| Method & path | Auth | Body → Response | Status |
|---|---|---|---|
| `GET /auth/csrf` | — | → `{ token }` and sets `XSRF-TOKEN` cookie | ✅ |
| `POST /auth/register` | — | `{ email, password, displayName }` → `201 { user }` + session cookies | ✅ |
| `POST /auth/login` | — | `{ email, password }` → `{ user }` + cookies | ✅ |
| `POST /auth/logout` | cookie | → `204`, revokes session, clears cookies | ✅ |
| `POST /auth/refresh` | refresh cookie | → `{ user }`, rotates refresh token | ✅ |
| `POST /auth/forgot-password` | — | `{ email }` → `202` always (no account enumeration) | ✅ |
| `POST /auth/reset-password` | — | `{ token, password }` → `204`, revokes all sessions | ✅ |
| `GET /auth/me` | ✅ | → `{ user }` | ✅ |
| `PATCH /auth/me` | ✅ | `{ displayName?, preferences? }` → `{ user }` | ✅ |
| `POST /auth/change-password` | ✅ | `{ currentPassword, newPassword }` → `204`, revokes other sessions | ✅ |
| `GET /auth/sessions` / `DELETE /auth/sessions/:id` | ✅ | list / revoke own sessions | ✅ |
| `POST /auth/verify-email` | — | `{ token }` | Phase 6 |

## 5.3 Platform administration (super admin)

| Method & path | Permission | Status |
|---|---|---|
| `GET /admin/stats` → `{ users: {total, active, suspended}, families: {total, active} }` | `platform.stats` | ✅ |
| `GET /admin/users?q=&status=&page=` | `platform.users.view` | ✅ |
| `PATCH /admin/users/:userId` `{ status?, platformRole? }` (cannot change self) | `platform.users.manage` | ✅ |
| `GET /admin/families?q=&status=` (registration metadata only) | `platform.families.view` | Phase 2 |
| `PATCH /admin/families/:familyId` `{ status }` | `platform.families.manage` | Phase 2 |
| `GET/PATCH /admin/reports` | `platform.reports.manage` | Phase 8 |

## 5.4 Families

| Method & path | Permission | Status |
|---|---|---|
| `GET /families` — families the caller belongs to, with role | auth | ✅ |
| `GET /families/:familyId` | `family.view` (guests: public families, public fields) | ✅ |
| `POST /families` `{ name, description, origin, privacy }` — caller becomes `family_admin` | `family.create` | Phase 2 |
| `PATCH /families/:familyId` | `family.update` / `family.manage_privacy` | Phase 2 |
| `PUT /families/:familyId/founder` `{ memberId, reason }` → `202` + recalculation | `family.set_founder` | Phase 3 |
| `GET /families/:familyId/memberships` · `PATCH …/memberships/:id` `{ role, moderatedBranchIds }` | `membership.manage` | Phase 6 |

## 5.5 Members (people)

| Method & path | Permission | Status |
|---|---|---|
| `POST /families/:familyId/members` `{ person, links?: { parents?: [{id, subtype}], partners?: [...] } }` | `member.create` (or contribution) | Phase 2 |
| `GET /families/:familyId/members?q=&generation=&branchId=&living=&page=` | `member.view` | Phase 2 |
| `GET /families/:familyId/members/search?q=` (partial, phonetic) | `member.view` | Phase 2 |
| `GET /families/:familyId/members/:memberId` (redacted per viewer) | `member.view` | Phase 2 |
| `PATCH /families/:familyId/members/:memberId` | `member.update` | Phase 2 |
| `POST /families/:familyId/members/:memberId/deletion-proposals` | `member.delete` / contribution | Phase 2 / 6 |
| `GET /families/:familyId/members/:memberId/history` · `POST …/restore/:auditId` | `audit.view` / `record.restore` | Phase 6 |

## 5.6 Relationships

| Method & path | Permission | Status |
|---|---|---|
| `POST /families/:familyId/relationships` `{ type, personA, personB, subtype, status?, startDate?, verification }` | `relationship.manage` | Phase 2 |
| `PATCH /families/:familyId/relationships/:id` | `relationship.manage` | Phase 2 |
| `DELETE /families/:familyId/relationships/:id` | `relationship.manage` | Phase 2 |
| `GET /families/:familyId/relationships/between?a=&b=` → kinship + path + confidence | `member.view` | Phase 5 |

## 5.7 Generations

| Method & path | Permission | Status |
|---|---|---|
| `GET /families/:familyId/generations` → summary per generation | `member.view` | Phase 3 |
| `GET /families/:familyId/generations/:n/members?page=` | `member.view` | Phase 3 |
| `GET /families/:familyId/generations/stats` → trends from snapshots | `member.view` | Phase 3 |
| `POST /families/:familyId/generations/recalculate` → `202 { jobId }` | `generation.recalculate` | Phase 3 |
| `PUT /families/:familyId/members/:memberId/generation-override` `{ number, reason }` | `generation.recalculate` | Phase 3 |
| `GET /families/:familyId/generations/conflicts` | `member.view` | Phase 3 |

## 5.8 Family tree (graph windows)

All return `{ nodes: PersonCard[], edges: Edge[], cursor? }` — never the whole family.

| Method & path | Status |
|---|---|
| `GET /families/:familyId/tree/root?depth=3` — founder + N generations | Phase 4 |
| `GET /families/:familyId/tree/nodes/:memberId/children` — expand one node (children + their partners) | Phase 4 |
| `GET /families/:familyId/tree/nodes/:memberId/ancestors?depth=` | Phase 4 |
| `GET /families/:familyId/tree/nodes/:memberId/descendants?depth=` | Phase 4 |
| `GET /families/:familyId/tree/branches/:branchId?depth=` | Phase 4 |
| `GET /families/:familyId/tree/path?from=&to=` — nodes on the relationship path | Phase 5 |

## 5.9 Branches, collaboration, invitations, media, history

| Method & path | Status |
|---|---|
| `GET/POST /families/:familyId/branches`, `GET/PATCH/DELETE …/branches/:id`, `GET …/branches/:id/stats` | Phase 4 |
| `POST /families/:familyId/contributions` · `GET …/contributions?status=` · `GET …/contributions/:id` | Phase 6 |
| `POST …/contributions/:id/approve` · `…/reject` `{ note }` · `…/request-clarification` `{ message }` · `…/withdraw` | Phase 6 |
| `GET /families/:familyId/audit?targetType=&targetId=` | Phase 6 |
| `POST /families/:familyId/invitations` `{ email, role, suggestedProfileId? }` · `GET …` · `DELETE …/:id` | Phase 6 |
| `GET /invitations/:token` (preview) · `POST /invitations/:token/accept` | Phase 6 |
| `POST /families/:familyId/claims` `{ memberId, evidence }` (reviewed by admin) | Phase 6 |
| `POST /families/:familyId/media` (multipart) · `GET …/media?memberId=&kind=` · `GET …/media/:id/url` (signed, short-lived) · `DELETE …/media/:id` | Phase 7 |
| `GET/POST/PATCH/DELETE /families/:familyId/events` | Phase 7 |
| `GET /families/:familyId/dashboard` | Phase 3 (stats) / 6 (pending contributions) |
| `POST /families/:familyId/export` (GEDCOM + JSON) · account export/deletion | Phase 8 |
