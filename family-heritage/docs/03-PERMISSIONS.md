# 3. Roles and Permissions

The executable matrix lives in `contracts/src/permissions.ts` and is imported by both the backend (enforcement) and the frontend (hiding controls). **Only the backend decides.**

## 3.1 Roles

| Role | Scope | Stored on |
|---|---|---|
| Super Administrator | Platform | `users.platformRole = 'super_admin'` |
| User | Platform (default) | `users.platformRole = 'user'` |
| Family Administrator | One family | `family_memberships.role = 'family_admin'` |
| Family Moderator | One family, optionally limited to branches | `family_memberships.role = 'family_moderator'` + `moderatedBranchIds` |
| Family Member | One family | `family_memberships.role = 'family_member'` |
| Guest | — | no active membership (anonymous or signed-in non-member) |

A user can hold a different role in each family. Platform role and family role are independent: a super admin with no membership is a **guest** in that family.

## 3.2 Family permission matrix

✅ allowed · 🌿 allowed within assigned branches (all branches if none assigned) · 📝 only by submitting a contribution for review · 🌐 public data only · — denied

| Permission | Admin | Moderator | Member | Guest |
|---|:-:|:-:|:-:|:-:|
| `family.view` | ✅ | ✅ | ✅ | 🌐 (if family is public) |
| `family.update` (name, description, origin, emblem) | ✅ | — | — | — |
| `family.manage_privacy` | ✅ | — | — | — |
| `family.set_founder` (+ recalculation) | ✅ | — | — | — |
| `member.view` | ✅ | ✅ | ✅ (redacted per privacy) | 🌐 deceased & public only |
| `member.view_private` (contact, exact living DOB) | ✅ | 🌿 | own claimed profile | — |
| `member.create` | ✅ | 🌿 | 📝 | — |
| `member.update` | ✅ | 🌿 | 📝 (own profile: direct for non-genealogical fields) | — |
| `member.delete` | ✅ | 📝 | 📝 | — |
| `relationship.manage` | ✅ | 🌿 | 📝 | — |
| `contribution.submit` | ✅ | ✅ | ✅ | — (may request invitation) |
| `contribution.review` | ✅ | 🌿 | — | — |
| `invitation.manage` | ✅ | — | — | — |
| `invitation.request` | — | — | — | ✅ |
| `membership.manage` (roles, moderators) | ✅ | — | — | — |
| `branch.manage` | ✅ | — | — | — |
| `media.view` | ✅ | ✅ | ✅ | 🌐 |
| `media.upload` | ✅ | ✅ | ✅ if `privacy.allowMemberUploads` (published after approval) | — |
| `media.delete` | ✅ | 🌿 | own uploads | — |
| `audit.view` | ✅ | — | — | — |
| `record.restore` | ✅ | — | — | — |
| `generation.recalculate` | ✅ | — | — | — |

Additional invariants enforced in services:

* A family must always have at least one active `family_admin` (the last admin cannot leave or be demoted).
* Admins cannot be removed by moderators; moderators cannot grant roles.
* Changes to a moderator's own claimed profile's genealogical facts (parents, generation) still go through review by another reviewer.

## 3.3 Platform permission matrix

| Permission | Super Admin | User |
|---|:-:|:-:|
| `platform.settings` | ✅ | — |
| `platform.users.view` / `platform.users.manage` (suspend, reactivate, role) | ✅ | — |
| `platform.families.view` (registration metadata & counts only) | ✅ | — |
| `platform.families.manage` (suspend/archive a family registration) | ✅ | — |
| `platform.stats` | ✅ | — |
| `platform.reports.manage` (abuse reports) | ✅ | — |
| `family.create` | ✅ | ✅ |

**Super administrators never receive `member.view` by virtue of their platform role.** Platform endpoints return family names, status, owner account and aggregate counts, never people.

## 3.4 Enforcement

* `requireAuth` — validates the access token, loads the user, rejects suspended accounts and revoked sessions.
* `requirePlatformPermission(p)` — checks the platform matrix.
* `loadFamilyAccess` — resolves `:familyId`, loads the caller's active membership (if any) and attaches a `FamilyAccess` object `{ family, membership, role, can(permission, branchIds?) }` to the request. Non-existent and inaccessible private families both return **404** so family existence is not disclosed.
* `requireFamilyPermission(p)` — route-level check; branch-scoped checks that depend on the target record are done in the service with `access.can(p, targetBranchIds)`.
* Field-level redaction is applied by the member serializer using the same `FamilyAccess`.
