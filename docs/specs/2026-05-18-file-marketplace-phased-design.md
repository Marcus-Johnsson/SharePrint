# File Marketplace — Phased Implementation Design (v0 → v0.1 → v1)

**Date:** 2026-05-18
**Owner:** Marcus
**Source requirements:** `file-marketplace-requirements.md` (v0.1 draft)
**Status:** Approved for planning

---

## 1. Purpose

Deliver the file marketplace described in the requirements document incrementally
through three milestones. The earliest runnable milestone must work fully on
localhost with **no cloud services**, **no real payments**, and **no production
environment** — while being structured so that v1 can introduce cloud storage and
real payments by adding adapters and configuration only, with no rewrite of domain
or feature code.

## 2. Tech stack

- **Backend:** .NET 10 / C#, ASP.NET Core. Layered solution
  `src/server/SharePrint.slnx` (`.slnx` = .NET 10 XML solution format) with
  projects `SharePrint.Domain` / `SharePrint.Application` /
  `SharePrint.Infrastructure` / `SharePrint.Api`, all targeting `net10.0`,
  namespaces `SharePrint.*`.
- **Frontend:** Svelte 5 + TypeScript, SvelteKit in SPA/CSR mode (`ssr = false`,
  static adapter). Lives at `src/client/SharePrint/`.
- **Auth:** ASP.NET Core Identity with HttpOnly same-site cookie sessions.
- **Database:** SQLite for v0.1 (EF Core 10); EF Core provider swap to PostgreSQL for v1.
- **File storage:** backend local disk for v0.1 behind an `IFileStorage` port;
  S3/R2 adapter for v1.
- **Payments:** real **Stripe in v0.1** (test mode) — embedded Payment Intents +
  Stripe Elements (buyer stays in the shop), behind an `IPaymentProcessor` port.
  `FakePaymentProcessor` retained as a config-selectable option
  (`Payment:Provider = Fake | Stripe`) for offline/CI/tests. Buyer-charge only in
  v0.1; Stripe **Connect / split payouts / KYC / tax** stay v1. Server uses the
  `Stripe.net` SDK; client uses `@stripe/stripe-js`. Fulfillment is webhook-driven
  (idempotent), not client-trusted.

## 3. Strategy — Ports & adapters from day 0 (chosen)

Define port interfaces in v0. v0.1 ships local/fake adapters, selected by
environment configuration via DI. v1 adds cloud/real adapters as new classes plus
config — domain and feature code untouched.

Rejected alternatives:
- *Concrete now, refactor at v1* — fastest v0.1 but rewrites every call site at v1
  and fights the swappable-by-environment requirement.
- *Config if/else branching* — quick but leaks infrastructure into domain, hard to
  unit-test, does not scale to Stripe Connect / POD complexity.

## 4. Milestone model

### v0 — Scaffold (walking skeleton, no features)

- Solution + 4 backend projects compile. SvelteKit client (`src/client/SharePrint/`) runs.
- EF Core migration creates the SQLite database.
- Health endpoint. DI wired with port interfaces and v0.1 adapters registered.
- Frontend round-trips one real API call.

**Exit criteria:** `dotnet run` (server) and `npm run dev` (client) both start;
frontend successfully calls one API endpoint; one passing integration test.

### v0.1 — Core happy path (local disk, fake payment, localhost only)

In scope:
- Register / login / logout (Identity + cookie). Email auto-confirmed on register
  (no email provider yet — explicit v1 gap).
- Become a seller: adds `Seller` role to the current user (no KYC; payment is fake).
- Seller creates a listing: multipart upload → `IFileStorage.SaveAsync` → random
  storage key → `Listing` with `status = active` (no scan/review pipeline).
- Buyer browses active catalog (paged), views listing detail (file never exposed).
- Checkout → `IPaymentProcessor.CreatePaymentAsync` → a **pending** `Order`
  (with `PaymentRef`). Stripe path: returns a PaymentIntent `clientSecret`; the
  client confirms in-page via Stripe Elements (no redirect). Fake path: settles
  immediately (no client secret).
- Payment confirmation is **webhook-driven**: `POST /api/webhooks/stripe`
  (signature-verified, idempotent) on `payment_intent.succeeded` flips the
  `Order` to **paid** and creates one `OrderItem` (type `download`) +
  `DownloadGrant` (5 remaining) per listing; `payment_intent.payment_failed`
  marks it failed. (Fake path creates these synchronously at checkout.)
- Download: `POST /api/downloads/{orderItemId}` checks grant, decrements on
  issuance (FR-31), returns short-lived HMAC token (~10 min);
  `GET /api/files/{token}` validates and streams the file from disk.
- Buyer library lists orders with remaining download counts.

Out of scope for v0.1 (deferred to v1): prints / POD, refunds / disputes, reviews,
full-text search and filters, wishlist, OAuth, 2FA, transactional email, content
moderation / DMCA / reports, audit log, seller KYC / payouts, multi-currency / FX.

**Exit criteria:** full happy-path flow works end to end on localhost (register →
become seller → upload → browse → checkout (fake) → download, with the 5-download
counter decrementing); integration test covers the path; authorization tests pass.

### v1 — Full requirements document

Swap adapters via configuration:
- `S3Storage` with real presigned upload/download URLs (replaces `LocalDiskStorage`).
- Extend the existing v0.1 `StripePaymentProcessor` with Stripe **Connect**
  (seller onboarding), split payouts, KYC, and tax. (The basic buyer charge
  already exists from v0.1; `FakePaymentProcessor` remains for tests/CI.)

Add feature modules: print path + POD provider, refunds/disputes, reviews, FTS
search (Postgres FTS), trust & safety (reports, moderation queue, DMCA, account
suspension), audit log, notifications (transactional email + in-app), OAuth + 2FA,
GDPR export/delete, cookie consent. EF Core provider swapped to PostgreSQL. CDN in
front of public previews/assets.

No domain rewrite: new adapters + new feature modules + configuration.

## 5. Solution structure

Repo root is the git repo `ExamensArbete/` (note the spelling — distinct from the
outer `ExamenArbete/` working dir that holds `docs/`). Real layout:

```
ExamensArbete/                       # git repo
  src/
    server/
      SharePrint.slnx                # .NET 10 solution (one solution)
      Domain/                        # SharePrint.Domain — entities, no infra deps
      Application/                   # SharePrint.Application — ports (IFileStorage, IPaymentProcessor)
      Infrastructure/                # SharePrint.Infrastructure — EF Core, LocalDiskStorage, FakePaymentProcessor
      Api/                           # SharePrint.Api — ASP.NET Core host, DI, auth, endpoints
      tests/
        Domain.Tests/                # SharePrint.Domain.Tests
        Application.Tests/           # SharePrint.Application.Tests
        Api.IntegrationTests/        # SharePrint.Api.IntegrationTests
    client/
      SharePrint/                    # Svelte 5 + TS, SvelteKit (SPA/CSR)
  .gitignore
# specs/plans live in the OUTER dir: ../../docs/superpowers/ (ExamenArbete/docs)
```

Note: projects sit **directly** under `src/server/` — there is no `src/server/src`
and no `server/src`. There is exactly **one** solution (`SharePrint.slnx`)
containing the 4 src projects + 3 test projects.

Dev wiring: SvelteKit/Vite dev-proxy `/api` → the .NET server
(`http://localhost:5136`, the `SharePrint.Api` http launch profile) so the auth
cookie is same-origin in development (avoids CORS/cookie friction).

## 6. Ports & environment routing

**Ports** (in `SharePrint.Application.Abstractions`):
- `IFileStorage`: `SaveAsync(stream, contentType) -> storageKey`,
  `OpenReadAsync(key) -> stream`, `CreateReadTicketAsync(key, ttl) -> ticket`,
  `DeleteAsync(key)`.
- `IPaymentProcessor`: `CreatePaymentAsync(amount, currency, buyerId, listingIds,
  orderId) -> PaymentResult` where `PaymentResult { ProviderRef, ClientSecret?,
  SettledImmediately }`. `ClientSecret == null` / `SettledImmediately == true`
  means the provider settled in-process (Fake); a non-null `ClientSecret` means
  the client must confirm (Stripe), with fulfillment arriving via webhook.

**Adapters** (in `SharePrint.Infrastructure`):
- v0.1: `LocalDiskStorage` (root from config, random object key — never the
  original filename, NFR-4 shape); **both** `FakePaymentProcessor` (settles in
  process, logs) **and** `StripePaymentProcessor` (test-mode PaymentIntent via
  `Stripe.net`), chosen by `Payment:Provider`.
- v1: `S3Storage` (real presigned URLs); `StripePaymentProcessor` extended with
  Connect/payouts/KYC/tax.

**Environment routing:**
- `appsettings.json` + `appsettings.Development.json`.
- Keys: `Storage:Provider = LocalDisk | S3`, `Storage:LocalDisk:RootPath`,
  `Payment:Provider = Fake | Stripe`.
- Stripe keys (test mode, server-side via user-secrets/env): `Stripe:SecretKey`,
  `Stripe:WebhookSecret`. Client gets `VITE_STRIPE_PUBLISHABLE_KEY` in
  `src/client/SharePrint/.env`. None are committed; `appsettings.json` holds only
  empty placeholders.
- Local dev webhooks: run the Stripe CLI —
  `stripe listen --forward-to http://localhost:5136/api/webhooks/stripe` — and use
  the signing secret it prints as `Stripe:WebhookSecret`. The `Fake` provider
  needs none of this (offline).
- DI selects the implementation from the config value. Development (localhost) →
  LocalDisk + Fake. Production (v1) → S3 + Stripe. localhost may opt into cloud by
  flipping config (the requested nice-to-have, available for free).

**Simulated signed URL (v0.1):** v0.1 has no object storage, so the v1 signed-URL
semantics are modeled with a server endpoint: `POST /api/downloads/{orderItemId}`
verifies ownership and remaining count, decrements on issuance (FR-31), and returns
a short-lived HMAC token (~10 min TTL). `GET /api/files/{token}` validates the
signature and expiry and streams the file from `IFileStorage`. The raw storage key
is never exposed. v1 changes this to return an S3 presigned URL instead of a token —
no domain change.

## 7. Roles & data model (v0.1 subset of requirements §8)

**Roles** (ASP.NET Core Identity roles, not boolean flags):
- `Customer` — assigned on register; every authenticated user.
- `Seller` — added on become-seller (no KYC in v0.1).
- `Admin` — seeded; used in v1 for moderation.
- A single user can hold `Customer` + `Seller` simultaneously (requirements §4).

| Entity | v0.1 fields |
|---|---|
| User (Identity) | Id, Email, PasswordHash, DisplayName, EmailConfirmed (auto-true), CreatedAt |
| Roles | `AspNetRoles` / `AspNetUserRoles` (Identity-managed): Customer, Seller, Admin |
| Listing | Id, SellerId→User, Title, Description, Price, Currency, StorageKey, OriginalFileName, ContentType, SizeBytes, Status [active \| unlisted], CreatedAt |
| Order | Id, BuyerId→User, Total, Currency, Status [pending \| paid \| failed], PaymentRef (provider PaymentIntent id), CreatedAt |
| OrderItem | Id, OrderId, ListingId, Type [download], UnitPrice |
| DownloadGrant | Id, OrderItemId, DownloadsRemaining (init 5), LastDownloadedAt |

Relationships: User 1—* Listing (as seller); User 1—* Order (as buyer);
Order 1—* OrderItem; OrderItem 1—1 DownloadGrant; Listing 1—* OrderItem.

Single currency in v0.1: **SEK** (configurable; no FX).

Deferred to v1: SellerProfile / KYC, ListingPrintOption, PrintFulfillment, Review,
Report, AuditLog.

## 8. API surface & flows (v0.1)

**Auth**
- `POST /api/auth/register` — create user, assign `Customer`, EmailConfirmed=true, sign in (cookie).
- `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`.

**Seller**
- `POST /api/seller/apply` — add `Seller` role to current user (no KYC).

**Listings**
- `POST /api/listings` — `[Authorize(Roles="Seller")]`, multipart (file + meta) →
  `IFileStorage.SaveAsync` → random key → `status = active`.
- `GET /api/listings` — catalog, active only, paged (no search in v0.1).
- `GET /api/listings/{id}` — detail; never exposes storage key or file.
- `PATCH /api/listings/{id}` — owner only: title / description / price.
- `POST /api/listings/{id}/unlist` — owner only: hidden from catalog; prior grants
  still work (FR-20).

**Checkout / orders / payments**
- `POST /api/checkout { listingIds[] }` — validates, computes total, creates a
  **pending** `Order`, calls `IPaymentProcessor.CreatePaymentAsync`. Returns
  `{ orderId, clientSecret?, status }`. Fake → `status: "paid"` (items + grants
  created now, `clientSecret: null`). Stripe → `status: "pending"` +
  `clientSecret` for Elements.
- `POST /api/webhooks/stripe` — raw body, `Stripe-Signature` verified against
  `Stripe:WebhookSecret`; **idempotent** (keyed on PaymentIntent id / `Order.PaymentRef`).
  `payment_intent.succeeded` → `Order` → paid + create `OrderItem` (download) +
  `DownloadGrant` (5) per listing; `payment_intent.payment_failed` → failed.
  Always returns 200. (Only mapped when `Payment:Provider = Stripe`.)
- `GET /api/orders` — buyer library: orders with status + remaining download
  counts. Client polls this after `confirmPayment` until the order is `paid`
  (webhook may lag a beat).

**Download (simulated signed URL)**
- `POST /api/downloads/{orderItemId}` — authorize owner, remaining > 0, decrement
  (FR-31), issue HMAC token (~10 min).
- `GET /api/files/{token}` — validate signature + expiry, stream from disk; key
  never exposed.

**Flows**
- *Sell:* register → `seller/apply` → `POST /api/listings` (upload) → live in catalog.
- *Buy + download (Stripe):* `GET /api/listings/{id}` → `POST /api/checkout`
  (pending `Order` + `clientSecret`) → Stripe Elements `confirmPayment` in-page →
  `payment_intent.succeeded` webhook flips `Order` paid + creates `DownloadGrant`
  (5) → client polls `GET /api/orders` → `POST /api/downloads/{orderItemId}` →
  `GET /api/files/{token}` → stream; counter decrements.
- *Buy + download (Fake):* same, but `POST /api/checkout` settles immediately
  (paid `Order` + grant created at checkout, no Elements step).

**Error handling** (boundary only, real): RFC 7807 ProblemDetails responses.
Validation on register and listing creation (required fields, price > 0, file size
cap, basic MIME allowlist). 401/403 via Identity + roles. 404 for missing listing.
403/409 when a grant is exhausted or the caller is not the owner. 401 for an
invalid or expired download token. No virus scan or content moderation in v0.1
(v1).

## 9. Frontend data loading (`src/client/SharePrint/`, SvelteKit)

SPA/CSR mode (`ssr = false`, static adapter). Route data is loaded via
**`+page.ts` `load()`**, not ad-hoc fetches in components.

```
src/client/SharePrint/src/routes/
  +layout.ts                       # load() → authService.me(), hydrates auth.svelte store
  +page.svelte / +page.ts          # catalog: load() → listingService.list, reads url.searchParams (paging/filter)
  listings/[id]/+page.svelte / +page.ts   # load({params,fetch}) → listingService.get
  library/+page.svelte / +page.ts  # load() → orderService.listOrders; AUTH_ERROR → redirect(302,'/login')
  login/  register/  sell/  …
src/client/SharePrint/src/lib/services/   # see §10
src/client/SharePrint/src/lib/stores/     # auth.svelte.ts runes store
```

- `+page.ts` is a universal load, running in the browser (SPA), using the `fetch`
  provided to `load`.
- Filter / sort / paging logic lives in `+page.ts`, read from `url.searchParams`.
  SvelteKit re-runs `load` on query change via invalidation — no manual refetch.
- v0.1 filters = paging only (no search per scope), but the logic is seated in
  `+page.ts` so v1 search/filters slot in with no component rewrite.
- Auth-guarded routes redirect on 401 from inside `load`.

## 10. Frontend API layering (`src/client/SharePrint/src/lib/services/`)

Follows the established house convention (ref: Grantigo `apiService.ts` /
`uploadService.ts`).

**Layer 1 — `ApiService` class** (`lib/services/apiService.ts`):
- Methods: `get<T>(endpoint, params?)`, `post<T,D>(endpoint, data)`,
  `put<T,D>`, `patch<T,D>`, `delete<T>(endpoint)`,
  `postFormData<T>(endpoint, FormData)` (no manual `Content-Type` — browser sets
  the multipart boundary).
- Base URL from `import.meta.env.VITE_API_URL`; request URL is
  `${VITE_API_URL}/${endpoint}`. This is the frontend half of environment routing:
  localhost API in v0.1, production API URL in v1 — same swappable principle as the
  backend storage/payment adapters.
- `credentials: 'include'` on every request (Identity cookie).
- **Does not throw.** Returns a union `T | null | AppError`.
  `parseResponse` returns `null` for `204`/empty bodies and guards JSON parsing.
- `get` builds the query string from a params record, supporting array values and
  skipping `undefined`/empty.
- Error mapping: `401` → set `auth.isAuthenticated = false` in the auth runes store
  and return `{ code: 'AUTH_ERROR' }`; `403` is returned as `API_ERROR` for the
  caller to handle (e.g. role/entitlement); `404` → `NOT_FOUND`; other non-OK →
  `API_ERROR` (with `status` and response text in `details`); thrown fetch →
  `NETWORK_ERROR`.

```ts
interface AppError {
  code: 'AUTH_ERROR' | 'API_ERROR' | 'NETWORK_ERROR' | 'NOT_FOUND';
  message: string;
  status?: number;
  details?: unknown;
}
```

**Layer 2 — resource service classes** (`lib/services/*.ts`): each takes
`ApiService` via constructor injection, owns its route strings + request/response
DTO types, and narrows the union for callers (returns typed data or throws a typed
error). A shared `unwrap<T>(res): T` helper centralizes the `'code' in res`
narrowing (replacing the inline check seen in `uploadService`): returns `T` on
success, throws a typed error carrying the `AppError` on failure.

```
src/client/SharePrint/src/lib/services/
  apiService.ts      # Layer 1
  unwrap.ts          # shared union-narrowing helper
  authService.ts     # register / login / logout / me
  listingService.ts  # list / get / createFileProduct / updateFileProduct / unlistFileProduct
  orderService.ts    # checkout (→ {orderId, clientSecret?, status}) / listOrders / poll status
  paymentService.ts  # @stripe/stripe-js: loadStripe(VITE_STRIPE_PUBLISHABLE_KEY),
                     #   mount Payment Element, confirmPayment({redirect:'if_required'})
  downloadService.ts # requestToken (POST /downloads/{id}) then resolve file URL
  types.ts           # shared DTOs mirroring server contracts
```

("file product" is the frontend-facing term for a `Listing` entity.)

**Layer 3 — user-action facades** (`lib/actions/`, thin, optional): only for
multi-step orchestration (e.g. `requestDownload` = get token, then navigate to the
file URL; `createFileProduct` = build FormData, call service, then
`invalidate()`). Single-call actions may call Layer 2 directly from a component or
SvelteKit form action — no facade required. v0.1 includes only what the core happy
path needs (YAGNI).

**Client-state stores** (`lib/stores/`, Svelte 5 runes in `*.svelte.ts`):
`auth.svelte.ts` (`isAuthenticated`, current user — written by `ApiService` on
`401` and by `authService` on login/me), plus UI/ephemeral state. Pure runed state,
no `fetch`.

**Consumers & data-flow rules:**
- Reads: `+page.ts` `load()` calls **Layer 2 services** (never raw `fetch`),
  using the `fetch` passed to `load`. A returned `AUTH_ERROR` →
  `redirect(302,'/login')` (the auth store is already flipped); `NOT_FOUND` →
  SvelteKit `error(404)`.
- Writes: components / SvelteKit form actions call Layer 2 (or a Layer 3 facade for
  multi-step), then call `invalidate()` to refresh affected `load()` data.
- Server data is **not** mirrored into stores; stores hold only genuine client
  state (auth/session, UI). Prevents state-duplication bugs.

## 11. Testing

- **SharePrint.Domain.Tests** (xUnit): invariants — `DownloadGrant` cannot
  decrement below 0, exhaustion logic. Fast, pure.
- **SharePrint.Application.Tests**: use-case services against fake `IFileStorage` /
  `IPaymentProcessor`. Checkout → Order + Grant(5); download decrements on
  issuance; exhausted grant rejected; non-owner rejected.
- **SharePrint.Api.IntegrationTests**: `WebApplicationFactory` + SQLite, real Identity +
  cookie, **`Payment:Provider = Fake`** (offline, deterministic — the happy-path
  suite never calls Stripe). Full happy path register → apply → upload → browse →
  checkout (Fake settles now) → download, plus authorization tests (Customer
  cannot create a listing; non-owner cannot download).
- **Stripe path**: `StripePaymentProcessor` unit-tested with a mocked Stripe
  client; the webhook handler tested with a synthetic signed event (verify
  idempotency + grant creation). Real test-mode Stripe + Stripe CLI is a manual
  smoke step, not in the automated suite.
- **Storage contract tests**: one shared suite executed against any `IFileStorage`
  implementation. v0.1 runs it against `LocalDiskStorage`; v1 runs the same suite
  against `S3Storage` — guarantees swap safety (core of the chosen strategy).
- **Frontend**: light — Vitest for a store/component; optional Playwright
  happy-path (kept minimal for thesis scope).
- TDD is applied during implementation (enforced in the writing-plans / execution
  phase).

## 12. Deferred decisions (from requirements §10, resolved at v1)

These do not affect v0.1 and are recorded for v1 planning:

- Download counter on issuance vs confirmed delivery — v0.1 uses **issuance**
  (FR-31, abuse-resistant); revisit for user-friendliness at v1.
- Download-window expiry in addition to the 5-download cap — none in v0.1
  (perpetual until exhausted); decide at v1.
- Allowed file types — v0.1 uses a basic MIME allowlist; finalize per-type rules
  (images, vectors, 3D, PDF, audio) at v1 with print implications.
- Single vs multiple POD providers — not in v0.1; abstract behind an interface at v1.
- Lifetime of unlisted items for prior buyers — v0.1 keeps prior grants working
  indefinitely; confirm fixed window at v1.
- Discount codes — out of scope until v1+.
- Currency — single currency in v0.1; multi-currency / FX decision at v1.
- Payment (decided 2026-05-18): real Stripe **in v0.1**, test mode, embedded
  Payment Intents + Elements, **buyer-charge only**, webhook-driven fulfillment,
  `FakePaymentProcessor` kept as a `Payment:Provider` option. Stripe Connect /
  split payouts / KYC / tax deferred to v1. Refunds/disputes remain v1.
