# 2. Data Model — MongoDB collections and the genealogy graph

The executable definitions are the Mongoose schemas under `backend/src/modules/*/*.model.ts`. This document explains them. Every collection except `users`, `auth_sessions` and `password_reset_tokens` carries a `familyId`, and **every query on those collections is scoped by `familyId`** (repositories take it as a required first argument).

Common conventions:

* `_id` is an `ObjectId`; references are `ObjectId`s named `<thing>Id`.
* `createdAt` / `updatedAt` via Mongoose timestamps.
* Soft deletion: `deletedAt: Date | null`, `deletedBy: ObjectId | null`. Partial indexes use `{ deletedAt: null }` so deleted records do not collide with live ones on unique keys.
* Optimistic concurrency (`version` key, `optimisticConcurrency: true`) on records people edit concurrently: families, members, relationships, contributions.

## 2.1 The genealogy graph

```
           partnership (marriage, current)
   Kofi ◄──────────────────────────────► Akosua          (Akosua married in: no generation)
    │  \                                    /
    │   parent_child (biological, verified)
    ▼                                     ▼
  Kwame (G2)                            Ama (G2)
    │
    ├── parent_child (biological)  ──► Yaw (G3)
    └── parent_child (adoptive)    ──► Esi (G3, adoptive descent counted by policy)
```

* **Vertices** — `family_members` documents (people). A person belongs to exactly one family tree. (The same human in two families' trees is two records; linking across families is out of scope and must never be automatic.)
* **Edges** — `relationships` documents:
  * `parent_child`: `personA` = parent, `personB` = child. Directed.
  * `partnership`: `personA` < `personB` (ids sorted), undirected.
* **Derived** (never stored): siblings (share ≥ 1 parent; full if they share 2 biological parents, half if 1), grandparents, cousins, in-laws, step-relations, branches' membership, generation numbers (stored as a cache only, recomputed by the engine).

Graph traversals use MongoDB `$graphLookup` for bounded queries (ancestors of X, descendants of X up to depth N) and an in-memory adjacency list for whole-family computations (generation engine), which is loaded with a covered index scan `{familyId, type, deletedAt}` projecting only `personA, personB, subtype, verification`.

### Integrity rules (enforced in the relationship service, inside a transaction)

1. Both endpoints exist, are not deleted, and belong to the same family.
2. No self-edges.
3. No duplicate edges — unique partial index on `{familyId, type, personA, personB}`.
4. **No cycles** — before inserting `parent → child`, the service checks (with `$graphLookup` upward from the parent) that the child is not already an ancestor of the parent.
5. At most **two biological parents** per child. Other parental subtypes are unbounded.
6. A person cannot be both parent and partner of the same person.
7. Removing an edge is a soft delete, audited, and triggers generation recalculation if it was a descent edge.

## 2.2 Collections

### `users` — authentication accounts
| Field | Type | Notes |
|---|---|---|
| email | string | lower-cased, **unique** |
| passwordHash | string | Argon2id encoded hash; `select: false` |
| displayName | string | |
| platformRole | `'super_admin' \| 'user'` | |
| status | `'active' \| 'suspended' \| 'deleted'` | |
| emailVerifiedAt | Date \| null | |
| passwordChangedAt | Date | |
| failedLoginCount, lockedUntil | number, Date \| null | account lockout |
| lastLoginAt | Date \| null | |
| preferences | `{ locale, theme: 'system'\|'light'\|'dark', defaultFamilyId }` | |

Indexes: `{email: 1}` unique; `{platformRole: 1, status: 1}`.

### `auth_sessions` — refresh-token sessions
`userId`, `tokenHash` (SHA-256 of current refresh token, unique), `previousTokens: [{hash, rotatedAt}]` (last 10, for reuse detection), `expiresAt` (**TTL index**), `revokedAt`, `revokedReason`, `userAgent`, `ip`, `lastUsedAt`.

### `password_reset_tokens`
`userId`, `tokenHash` (unique), `expiresAt` (TTL), `usedAt`, `requestedIp`.

### `families`
| Field | Type | Notes |
|---|---|---|
| name | string | e.g. "Mensah Family" |
| slug | string | unique, URL-safe |
| description, history | string | |
| emblemMediaId | ObjectId \| null | → `family_media` |
| foundingMemberId | ObjectId \| null | → `family_members` (Generation 1) |
| origin | `{ hometown, region, country }` | |
| privacy | `{ visibility: 'private' \| 'unlisted' \| 'public', showLivingToMembers: boolean, allowMemberUploads: boolean, requireApprovalForMemberEdits: boolean }` | defaults: private / true / true / true |
| generationPolicy | `{ countAdoptive: boolean }` | default true |
| status | `'active' \| 'suspended' \| 'archived'` | platform-managed |
| stats | `{ memberCount, generationCount, branchCount, livingCount, deceasedCount, recalculatedAt }` | cache written by jobs |
| createdBy | ObjectId → users | |

Indexes: `{slug: 1}` unique; `{'privacy.visibility': 1, status: 1}`.

### `family_memberships` — user ↔ family (+ claimed profile)
`familyId`, `userId`, `role: 'family_admin' | 'family_moderator' | 'family_member'`, `moderatedBranchIds: ObjectId[]` (empty = all branches, only meaningful for moderators), `memberProfileId` (claimed person, set only after verification), `status: 'active' | 'removed'`, `invitedBy`, `joinedAt`.

Indexes: `{familyId, userId}` **unique**; `{userId, status}`; `{familyId, role}`.

### `family_members` — people
| Group | Fields |
|---|---|
| Names | `firstName`, `middleNames`, `surname`, `maidenName`, `previousNames[]`, `preferredName`, `title` |
| Identity | `gender?: 'female' \| 'male' \| 'non_binary' \| 'other' \| 'undisclosed'` (optional, voluntary) |
| Life | `vitalStatus: 'living' \| 'deceased' \| 'unknown'`, `birthDate: FuzzyDate`, `birthPlace`, `deathDate: FuzzyDate`, `deathPlace`, `nationality`, `hometown` |
| Story | `biography`, `education[]`, `occupations[]`, `achievements[]`, `stories[]`, `sources[]` (`{title, citation, url, mediaId}`) |
| Media | `profilePhotoMediaId` |
| Private | `contact: { email, phone, address }` — `select: false`, visible only to admins and the person's own claimed account |
| Privacy | `privacy: { level: 'family' \| 'public' \| 'restricted', hideDates: boolean }`; living people default `family` and are never `public` unless explicitly set |
| Generation (cache) | `generation: { number: number \| null, status: 'founder' \| 'computed' \| 'conflict' \| 'married_in' \| 'unconnected' \| 'unverified_path' \| 'pending', candidates: number[], override: { number, reason, by, at } \| null, computedAt }` |
| Search | `searchTokens[]` (normalised, diacritics removed, all name parts + prefixes), `phoneticKeys[]` |
| Lifecycle | `createdBy`, `updatedBy`, `deletedAt`, `deletedBy`, `version` |

`FuzzyDate = { year?, month?, day?, qualifier: 'exact'|'about'|'before'|'after'|'between'|'estimated', endYear?, text?, sortKey: number|null }`, where `sortKey = year*10000 + month*100 + day` with unknown parts as 0, used for ordering and range queries only.

Indexes: `{familyId, deletedAt, 'generation.number'}`; `{familyId, searchTokens}`; `{familyId, phoneticKeys}`; `{familyId, surname, firstName}`; `{familyId, 'birthDate.year'}`; `{familyId, hometown}`.

### `relationships` — edges
`familyId`, `type: 'parent_child' | 'partnership'`, `personA`, `personB`, `subtype` (`parent_child`: biological / adoptive / step / foster / guardian / unknown; `partnership`: marriage / customary_marriage / civil_union / partnership / engagement / unknown), `status` (partnership only: current / divorced / separated / widowed / annulled / unknown), `startDate`, `endDate` (FuzzyDate), `verification: 'verified' | 'unverified' | 'disputed'`, `sources[]`, `notes`, `createdBy`, `deletedAt`, `version`.

Indexes: `{familyId, type, personA, personB}` **unique, partial on `deletedAt: null`**; `{familyId, personB, type}` (parents of X); `{familyId, personA, type}` (children / partners of X).

### `family_branches`
`familyId`, `name`, `foundingMemberId` (the branch is "all descendants of this person"), `description`, `history`, `administrators: ObjectId[]` (users), `stats: { descendantCount, generationSpan, livingCount, recalculatedAt }`. Membership of a branch is **derived** by descendant traversal — people are never copied into branches, and a person can belong to several branches.
Indexes: `{familyId, foundingMemberId}` unique (partial, live); `{familyId, name}`.

### `family_invitations`
`familyId`, `email` (lower-cased), `tokenHash` (unique), `role`, `suggestedProfileId` (a person they may claim — claim still requires admin verification), `invitedBy`, `expiresAt` (TTL after expiry + 30 days for audit), `status: 'pending' | 'accepted' | 'revoked' | 'expired'`, `acceptedBy`, `acceptedAt`.
Indexes: `{tokenHash}` unique; `{familyId, email, status}`.

### `family_contributions` — proposed changes
`familyId`, `kind: 'member.create' | 'member.update' | 'member.delete' | 'relationship.create' | 'relationship.update' | 'relationship.delete' | 'media.attach' | 'story.create' | 'claim.profile'`, `targetType`, `targetId`, `branchIds[]` (for moderator scoping), `payload` (the proposed values), `previousValues` (snapshot at proposal time, for conflict detection), `status: 'pending' | 'needs_clarification' | 'approved' | 'rejected' | 'withdrawn'`, `proposedBy`, `reviewedBy`, `reviewedAt`, `reviewNote`, `discussion: [{by, message, at}]`, `appliedChangeId` (→ audit log).
Indexes: `{familyId, status, createdAt: -1}`; `{familyId, proposedBy}`; `{familyId, targetType, targetId}`.

### `family_media`
`familyId`, `kind: 'photo' | 'document' | 'audio' | 'video'`, `title`, `description`, `storageKey`, `mimeType`, `byteSize`, `sha256`, `width?`, `height?`, `durationSec?`, `memberIds[]`, `eventId?`, `date: FuzzyDate`, `privacy: 'family' | 'admins' | 'public'`, `provenance: { source, citation, originalHolder }`, `uploadedBy`, `status: 'processing' | 'pending_approval' | 'published' | 'rejected'`, `deletedAt`.
Indexes: `{familyId, memberIds}`; `{familyId, kind, createdAt: -1}`; `{familyId, status}`.

### `family_events`
`familyId`, `kind: 'birth' | 'death' | 'marriage' | 'reunion' | 'migration' | 'tradition' | 'historical' | 'other'`, `title`, `description`, `date: FuzzyDate`, `place`, `memberIds[]`, `mediaIds[]`, `privacy`, `createdBy`, `deletedAt`.
Indexes: `{familyId, 'date.sortKey'}`; `{familyId, memberIds}`.

### `audit_logs` — append-only
`familyId` (null for platform-level actions), `actorId`, `actorRole`, `action` (e.g. `member.update`, `relationship.delete`, `admin.user.suspend`), `targetType`, `targetId`, `before`, `after` (redacted of secrets), `contributionId?`, `requestId`, `ip`, `at`.
Indexes: `{familyId, at: -1}`; `{familyId, targetType, targetId, at: -1}`; `{actorId, at: -1}`. The application never updates or deletes these documents; "restore previous version" writes a new change whose `after` equals an earlier `before`.

### `generation_snapshots`
`familyId`, `founderId`, `computedAt`, `durationMs`, `totals: { members, generations, conflicts, unconnected, marriedIn }`, `byGeneration: [{ number, total, living, deceased, unknown, earliestBirthYear, latestBirthYear }]`. One document per recalculation; the dashboard reads the latest. Index `{familyId, computedAt: -1}`; old snapshots kept for trend charts.

## 2.3 Transactions

Used wherever more than one document must change together, e.g.:

* add person + parent edge + audit entry;
* approve contribution → apply change → write audit log → mark contribution approved;
* change founding ancestor → update family → enqueue recalculation (the job is enqueued only after commit).
