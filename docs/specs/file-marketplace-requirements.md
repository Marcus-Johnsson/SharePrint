# File Marketplace — Requirements Document

**Version:** 0.1 (draft)
**Owner:** Marcus
**Last updated:** 2026-05-14

---

## 1. Overview

A web-based marketplace where users can upload digital files to sell, and other users can buy them. On purchase, a buyer can either download the file directly or order a physical print of it, which is fulfilled by a third-party print-on-demand (POD) service and shipped to them.

The marketplace handles file hosting, payments, fulfillment routing, and trust & safety. Sellers receive payouts minus a platform fee.

## 2. Goals

- Allow any verified user to list a digital file for sale.
- Allow any user to purchase a file and receive it as a download or as a printed product.
- Keep the seller's file private and tamper-proof — only authorized buyers can ever obtain it.
- Pay sellers reliably and handle tax/VAT correctly.
- Protect the platform from malware, illegal content, and copyright infringement.

## 3. Non-goals (v1)

- Subscription pricing / recurring sales.
- Bundles or multi-file packs.
- In-app messaging beyond order-related communication.
- Mobile native apps (web only for v1).
- DRM beyond watermarked previews.
- Auctions or variable pricing.

## 4. Actors

| Actor | Description |
|---|---|
| Guest | Unauthenticated visitor. Can browse the catalog and view previews. |
| Buyer | Authenticated user who can purchase files. |
| Seller | Authenticated user who has completed seller verification and can list files. |
| Admin | Platform operator. Can moderate listings, handle disputes, suspend accounts. |
| POD service | Third-party print-on-demand provider (external system). |
| Payment processor | Stripe Connect or equivalent (external system). |

A single human can be both a buyer and a seller on one account.

## 5. Functional requirements

### 5.1 Accounts & authentication

- FR-1: Users can register with email and password.
- FR-2: Email address must be verified before any purchase or upload.
- FR-3: Users can sign in via OAuth (Google, GitHub) in addition to password.
- FR-4: Users can enable 2FA (TOTP).
- FR-5: Users can reset their password via a one-time email link.
- FR-6: Users can edit their profile (display name, avatar, bio).
- FR-7: Users can request account deletion and data export (GDPR).
- FR-8: Sessions expire after a configurable inactivity period.

### 5.2 Becoming a seller

- FR-9: Any authenticated user can apply to become a seller.
- FR-10: Seller verification requires connecting a payout account via the payment processor (which collects KYC and tax info).
- FR-11: Sellers can be suspended by admins; suspended sellers cannot list new items, but existing purchases remain valid.

### 5.3 Listings (seller actions)

- FR-12: Sellers can create a listing by uploading one file plus metadata: title, description, tags, category, license type, price, currency.
- FR-13: File upload uses presigned URLs and goes directly to object storage, not through the application server.
- FR-14: Uploaded files must pass: format/MIME validation, size limit, virus scan, content-moderation hash check.
- FR-15: A new listing enters a `pending_review` state until checks pass; failing files are rejected with a reason.
- FR-16: Sellers can upload or auto-generate a preview/thumbnail (watermarked).
- FR-17: Sellers can toggle "prints available" and select supported print options (size, paper/material).
- FR-18: Sellers can edit price, description, tags, and preview at any time.
- FR-19: Sellers can replace the underlying file; the new version must pass the full check pipeline before being served to new buyers. Existing buyers continue to receive the version they purchased.
- FR-20: Sellers can unlist an item. Unlisted items remain accessible to prior buyers for re-download.
- FR-21: Sellers see a dashboard with sales, revenue, reviews, and payout history.

### 5.4 Browsing & search (buyer actions)

- FR-22: Guests and buyers can browse the catalog, filter by category and tags, and sort by price, popularity, and recency.
- FR-23: Full-text search across title, description, and tags.
- FR-24: Listing detail page shows preview, description, license, price, reviews, and seller info — but never the underlying file.
- FR-25: Buyers can add items to a wishlist.

### 5.5 Purchase — download path

- FR-26: Buyer initiates checkout for one or more items.
- FR-27: Payment is processed via the integrated payment processor.
- FR-28: On successful payment, an `Order` and one `DownloadGrant` per item are created.
- FR-29: Each `DownloadGrant` starts with **5 remaining downloads** and no expiry on the grant itself.
- FR-30: Each individual download link is a signed URL with a short TTL (5–15 minutes).
- FR-31: The download counter is decremented server-side when a signed URL is issued, not on file delivery. (Decision can be revisited; see Open Questions.)
- FR-32: Buyer can view all past purchases and re-download from there until the counter is exhausted.
- FR-33: Buyer receives an emailed receipt with a link back to their library.

### 5.6 Purchase — print path

- FR-34: If the listing supports prints, buyer can select "ship a print" instead of (or in addition to) download.
- FR-35: Buyer enters a shipping address and selects print options.
- FR-36: Print fee is added to the purchase total; shipping cost is calculated at checkout via the POD service.
- FR-37: On successful payment, the server submits a print order to the POD service, passing the file (via signed URL) and shipping details.
- FR-38: Order status is updated from POD webhooks: `submitted → in_production → shipped → delivered`.
- FR-39: Buyer can view print order status and tracking number from their orders page.
- FR-40: The file is never delivered to the buyer on a print-only purchase.

### 5.7 Refunds & disputes

- FR-41: Buyers can request a refund within a configurable window (e.g., 14 days) for print orders that have not yet shipped.
- FR-42: Download-path purchases are non-refundable by default once any download has occurred; refundable before first download.
- FR-43: Disputes opened with the payment processor are surfaced to admins for handling.
- FR-44: Refunds reverse the platform fee and seller payout proportionally.

### 5.8 Reviews

- FR-45: Buyers can rate (1–5) and review an item only after a completed purchase.
- FR-46: Sellers can reply to reviews once.
- FR-47: Reviews can be reported and moderated.

### 5.9 Trust & safety

- FR-48: Any user can report a listing for copyright infringement, inappropriate content, or quality issues.
- FR-49: A DMCA takedown intake flow is available.
- FR-50: Reported items enter a moderation queue. Admins can hide, unlist, or remove items.
- FR-51: Admins can suspend or ban accounts.
- FR-52: All sensitive actions (payouts, takedowns, file replacements, refunds) are written to an immutable audit log.

### 5.10 Notifications

- FR-53: Transactional email for: signup verification, password reset, purchase receipt, download-ready, print order updates, refund processed, dispute opened.
- FR-54: In-app notifications mirror email events.
- FR-55: Users can mute non-essential notifications in settings.

## 6. Non-functional requirements

### 6.1 Security

- NFR-1: All traffic uses HTTPS.
- NFR-2: Passwords are hashed with a modern KDF (argon2id or bcrypt with adequate cost).
- NFR-3: Files at rest in object storage are encrypted.
- NFR-4: Signed download URLs use short TTLs and a randomized object key, never the original filename.
- NFR-5: All authorization checks happen server-side; the client is never trusted with download eligibility.
- NFR-6: Rate limiting on auth endpoints, signed-URL issuance, and search.
- NFR-7: CAPTCHA on signup and password reset.

### 6.2 Privacy & compliance

- NFR-8: Cookie consent banner for EU visitors.
- NFR-9: GDPR-compliant data export and deletion.
- NFR-10: Sales tax / VAT calculated and collected via the payment processor's tax service.
- NFR-11: Seller tax reporting (1099, EU equivalents) handled by the payment processor where possible.
- NFR-12: Privacy policy, terms of service, and seller agreement are public, versioned, and accepted at signup.

### 6.3 Performance

- NFR-13: Catalog pages render in under 1s p95 on a normal connection.
- NFR-14: Search returns results in under 300ms p95.
- NFR-15: Previews and thumbnails are served via CDN.
- NFR-16: The app server never serves the actual paid files — only signed URLs to object storage.

### 6.4 Reliability

- NFR-17: Database is backed up daily with point-in-time recovery.
- NFR-18: Object storage uses provider-default durability (e.g., 11 nines).
- NFR-19: Webhook handlers (payments, POD) are idempotent.
- NFR-20: Failed POD submissions are retried with exponential backoff and surfaced to admins after N failures.

### 6.5 Observability

- NFR-21: Centralized application logs.
- NFR-22: Error tracking (Sentry or similar).
- NFR-23: Metrics on signups, sales, downloads, refunds, POD failures.

## 7. System architecture (high-level)

- **Web frontend** — public catalog, buyer/seller dashboards, admin console.
- **Application server** — REST/GraphQL API. Stateless. Handles auth, listings, orders, signed-URL issuance.
- **Database** — Postgres for relational data (users, listings, orders, reviews, audit log).
- **Search index** — Postgres FTS for v1; upgradeable to Meilisearch/Algolia.
- **Object storage** — S3, Cloudflare R2, or GCS. Three logical zones:
  - `incoming/` — fresh seller uploads, untrusted.
  - `approved/` — for-sale files, private, served only via signed URLs.
  - `public/` — previews, thumbnails, served via CDN.
- **Background workers** — virus scan, hash computation, preview generation, POD submission, webhook processing.
- **Payment processor** — Stripe Connect (recommended) for split payouts, KYC, tax.
- **POD provider** — Printful, Gelato, Prodigi, or Lulu (TBD).
- **Email provider** — Postmark, SendGrid, or Resend.
- **CDN** — in front of `public/` bucket and frontend static assets.

## 8. Data model (key entities)

| Entity | Key fields |
|---|---|
| User | id, email, password_hash, display_name, is_seller, is_admin, created_at |
| SellerProfile | user_id, payout_account_id, kyc_status, payout_schedule |
| Listing | id, seller_id, title, description, price, currency, license, file_key, file_hash, status, print_enabled, created_at |
| ListingPrintOption | listing_id, size, material, base_cost |
| Order | id, buyer_id, total, currency, status, created_at |
| OrderItem | order_id, listing_id, type (`download` or `print`), unit_price, fulfillment_status |
| DownloadGrant | order_item_id, downloads_remaining, last_downloaded_at |
| PrintFulfillment | order_item_id, pod_order_id, shipping_address, tracking_number, status |
| Review | id, listing_id, buyer_id, rating, body, created_at |
| Report | id, target_type, target_id, reporter_id, reason, status |
| AuditLog | id, actor_id, action, target_type, target_id, payload, created_at |

## 9. Key user flows (summary)

**Sell a file.** Sign up → become a seller (connect payout) → upload file via presigned URL → file is scanned, hashed, moderated → set metadata and price → listing goes live.

**Buy + download.** Browse → view listing → checkout → pay → `DownloadGrant` (5 downloads) created → buyer clicks Download → server issues short-lived signed URL → buyer downloads from object storage directly → counter decrements.

**Buy + print.** Browse → view listing → checkout with print option → enter shipping → pay → server submits print job to POD with signed URL to file → POD prints and ships → status updates via webhook → buyer sees tracking.

## 10. Open questions

- Should `DownloadGrant` count toward the 5-download limit on signed-URL issuance or on confirmed delivery? (Issuance is simpler and abuse-resistant; delivery is friendlier to users on flaky networks.)
- Is there a download window expiry (e.g., 1 year) in addition to the 5-download limit, or is access perpetual until exhausted?
- Which file types are allowed in v1? (Images, vectors, 3D models, PDFs, audio? Each has different print implications.)
- Single POD provider for v1, or abstract behind an interface from day one?
- Do unlisted items remain available to prior buyers indefinitely, or for a fixed window?
- Will sellers be allowed to issue discount codes?
- Currency: single-currency at launch (USD or EUR) or multi-currency with FX?

## 11. Glossary

- **POD** — Print-on-demand. Third-party service that prints and ships physical products from a digital file.
- **Signed URL** — A time-limited URL to object storage that grants temporary read access to a specific object.
- **Presigned upload URL** — Same idea, but for `PUT` instead of `GET`; lets the client upload directly to storage.
- **KYC** — Know Your Customer. Identity/tax verification required before paying out earnings.
- **DMCA** — US copyright takedown process; the de facto template for similar laws elsewhere.
