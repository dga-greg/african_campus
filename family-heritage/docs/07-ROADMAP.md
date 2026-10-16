# 7. Implementation Roadmap

| Phase | Scope | Key deliverables | Exit criteria |
|---|---|---|---|
| **1 — Foundation** ✅ | Monorepo, shared contracts, Express API skeleton, MongoDB + Redis wiring, all collection schemas, authentication, RBAC, platform admin (accounts), Angular shell + auth pages | See [PHASE1.md](PHASE1.md) | Auth + RBAC integration tests green against a real MongoDB replica set; frontend builds and unit tests pass |
| 2 — Family registry | Create/update family, founding ancestor, person CRUD with fuzzy dates and privacy redaction, relationship CRUD with integrity rules (cycle check, duplicate edges, 2 biological parents), search tokens + phonetic search | Families, Members, Relationships pages; member profile | Integration tests for every integrity rule; redaction tests per role |
| 3 — Generation engine | Engine (§4), recalculation job, conflict review + override, snapshots, generation dashboard + filters, family dashboard charts | Generations page, dashboard stats | Unit tests incl. cousin-marriage conflicts, unknown parents, adoptive policy, founder change |
| 4 — Interactive tree | Graph-window endpoints, family-unit layout, D3 canvas, expand/collapse, search & focus, ancestor/descendant highlight, filters, full screen, export, branches | Family Tree, Branches pages | Layout unit tests (blended families, multiple partners), Playwright smoke on 5 000-person fixture |
| 5 — Relationship finder | LCA, kinship classification, affinal/step paths, path visualisation | Relationship Finder page | Kinship unit tests table-driven over ≥ 40 cases |
| 6 — Collaboration | Invitations (e-mail, expiring tokens), account linking, profile claims, contributions + review workflow, audit log viewer, restore, e-mail verification, membership/role management | Invitations, Administration pages | Integration tests for approve → apply → recalc → audit |
| 7 — Archive | Media upload (S3-compatible, sniffing, image re-encoding, signed URLs), stories, oral histories, events timeline | History, Photos & Documents pages | Upload security tests |
| 8 — Hardening & deployment | Performance tests, security review, data export (JSON + GEDCOM) and deletion workflows, abuse reports, responsive polish, Playwright E2E suite (§19), Dockerfiles, CI/CD | | E2E suite green in CI |
