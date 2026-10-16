# 6. UI Component Architecture

## 6.1 Principles

* **Standalone components only**, `ChangeDetectionStrategy.OnPush`, zoneless change detection, state in **signals** (`signal`, `computed`, `effect`), RxJS only at the HTTP boundary.
* **Feature-based directories**, each feature lazily loaded through `loadComponent` / `loadChildren`.
* **Smart vs. presentational:** route components ("pages") inject stores and services; `shared/ui` components take inputs and emit outputs only.
* **Typed API layer:** every request and response type comes from `@fh/contracts`.
* **Accessibility:** semantic landmarks, visible focus rings, labelled form controls with error text linked by `aria-describedby`, `prefers-reduced-motion` respected, colour never the only signal (generation colours are paired with a "G3" label). CDK provides focus traps (drawers, dialogs), live announcer and a11y helpers.
* **Design tokens** in Tailwind v4 `@theme`: neutral surfaces, one brand accent (deep green, kente-inspired gold as secondary), and an 8-step generation accent scale with WCAG-checked contrast on white.

## 6.2 Application shell

```
AppComponent
└── RouterOutlet
    ├── AuthLayout                          (/auth/** — centred card, brand panel)
    │   ├── LoginPage · RegisterPage · ForgotPasswordPage · ResetPasswordPage
    └── ShellLayout                         (authenticated area, authGuard)
        ├── SidebarNav  (desktop: persistent; mobile: CDK-trapped drawer)
        ├── TopBar      (family switcher, search entry, user menu)
        └── RouterOutlet
            ├── DashboardPage
            ├── FamilyTreePage              (Phase 4)
            ├── MembersPage / MemberProfilePage (Phase 2)
            ├── GenerationsPage             (Phase 3)
            ├── BranchesPage                (Phase 4)
            ├── RelationshipFinderPage      (Phase 5)
            ├── HistoryPage · MediaPage     (Phase 7)
            ├── InvitationsPage             (Phase 6)
            ├── AdminPage                   (platform: superAdminGuard; family admin: Phase 6)
            └── SettingsPage
```

Navigation items for later phases are shown but **disabled with a "Coming in Phase N" label** and routed to an honest placeholder; nothing pretends to work.

## 6.3 State

| Store (signal-based service, `providedIn: 'root'`) | Holds |
|---|---|
| `AuthStore` | `user`, `status: 'unknown' \| 'authenticated' \| 'anonymous'`, `isSuperAdmin`, login/logout/register/refresh |
| `FamilyContextStore` | the families list, `activeFamilyId` (persisted per user), `activeRole`, `can(permission)` using the shared matrix |
| `TreeStore` (Phase 4) | loaded node/edge window, expanded set, selection, filters, viewport transform |
| `RelationshipStore` (Phase 5) | selected pair, result, path |

## 6.4 HTTP layer

* `provideHttpClient(withFetch(), withXsrfConfiguration({cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN'}), withInterceptors([apiBaseUrl, credentials, authRefresh]))`.
* `authRefresh` interceptor: on `401` with code `UNAUTHENTICATED`/`SESSION_EXPIRED` from a non-auth endpoint, performs **one** shared refresh (concurrent 401s wait for the same refresh) and retries once; on failure the store becomes anonymous and the router navigates to `/auth/login?returnUrl=…`.
* `ApiError` normalises the server error envelope; forms map `details[].path` onto controls.

## 6.5 Family tree visualisation (Phase 4)

* `FamilyTreePage` hosts three regions: `TreeSidebar` (filters, branches, display settings), `TreeCanvas` (D3), `MemberPanel` (selected person). Below `lg` the sidebars become CDK drawers / a bottom sheet.
* `TreeCanvas` is a thin Angular wrapper; layout is a **pure function** `layoutFamilyGraph(nodes, edges, options) → positioned nodes, family units, routed links`, so it can be unit-tested without a DOM.
* Layout model: **family units** (a couple or single parent) instead of a binary tree. Each generation is a horizontal band (tinted with the generation accent). Children hang from the midpoint of their parents' union line; additional partners are placed beside the shared person with separate union connectors; multiple parental sets (birth + adoptive) are drawn as separate connectors with distinct line styles. Married-in partners are drawn in the band of their partner.
* Line styles (legend): biological = solid, adoptive = dashed, step/foster/guardian = dotted, partnership = double line, ended partnership = faded double line, unverified = lighter with "?" marker.
* Rendering: SVG for ≤ ~1 500 visible nodes with off-viewport culling; D3 zoom/pan; incremental expansion via the "expand node" endpoint; export via serialising the SVG (SVG), drawing to canvas (PNG) and jsPDF (PDF).
