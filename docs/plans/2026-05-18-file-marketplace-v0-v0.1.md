# File Marketplace v0 → v0.1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a localhost-only file marketplace where a seller uploads a file, a buyer "pays" (fake) and downloads it 5 times, structured so cloud storage + real payments drop in at v1 via adapters + config only.

**Architecture:** Ports & adapters. .NET 10 layered solution `src/server/SharePrint.slnx` with projects `SharePrint.Domain`/`SharePrint.Application`/`SharePrint.Infrastructure`/`SharePrint.Api` behind `IFileStorage` + `IPaymentProcessor` ports, selected by environment config via DI. SvelteKit SPA client at `src/client/SharePrint/` with a class-based `ApiService` (union-return) + injected resource services. SQLite for v0.1 (EF Core provider swaps to Postgres at v1).

**Tech Stack:** .NET 10 (ASP.NET Core, EF Core 10, ASP.NET Core Identity, SQLite, xUnit), Svelte 5 + SvelteKit + TypeScript (Vite), house API convention from Grantigo (`apiService.ts`/`uploadService.ts`).

**Namespaces:** `SharePrint.Domain`, `SharePrint.Application` (+ `.Abstractions`), `SharePrint.Infrastructure` (+ `.Persistence`/`.Storage`/`.Payments`), `SharePrint.Api` (+ `.Contracts`/`.Endpoints`). `Program` stays in the global namespace (so `WebApplicationFactory<Program>` works).

**Spec:** `docs/superpowers/specs/2026-05-18-file-marketplace-phased-design.md`

---

## Prerequisites (manual, once — done by Marcus)

- Git repo already exists at `C:\Users\Marcu\Repo\ExamenArbete\ExamensArbete` (note: inner folder, spelled *ExamensArbete*). A repo-root `.gitignore` is in place. Marcus handles all commits manually — the `git commit` lines in tasks are the intended commit points, not auto-run.
- Installed: .NET 10 SDK (`dotnet --version` ≥ 10), Node ≥ 20 (`node -v`), `dotnet tool install --global dotnet-ef`.
- All `dotnet` commands run from `src/server/`. All `npm`/`npx` commands run from `src/client/SharePrint/`. Task 1 (scaffold) and Task 7 (client SPA reconfig) are **already done** — start at Task 2.

---

## File Structure

Repo root = git repo `ExamensArbete/`. Projects sit **directly** under
`src/server/` (no `src/server/src`, no `server/src`). One solution
`SharePrint.slnx`. Specs/plans live in the OUTER `ExamenArbete/docs/` dir.

```
ExamensArbete/
  src/
    server/
      SharePrint.slnx                       # .NET 10 solution (one)
      Domain/                               # SharePrint.Domain — no infra deps
        User.cs                             # AppUser : IdentityUser
        Listing.cs
        Order.cs  OrderItem.cs  DownloadGrant.cs
        Roles.cs                            # role-name constants
      Application/                          # SharePrint.Application
        Abstractions/IFileStorage.cs
        Abstractions/IPaymentProcessor.cs
        Abstractions/PaymentResult.cs
        Abstractions/StoredFile.cs
      Infrastructure/                       # SharePrint.Infrastructure
        Persistence/AppDbContext.cs
        Persistence/Migrations/*            # EF-generated
        Storage/LocalDiskStorage.cs
        Payments/FakePaymentProcessor.cs
        DependencyInjection.cs              # config-driven adapter wiring
      Api/                                  # SharePrint.Api
        Program.cs
        DownloadToken.cs
        Endpoints/AuthEndpoints.cs
        Endpoints/SellerEndpoints.cs
        Endpoints/ListingEndpoints.cs
        Endpoints/CheckoutEndpoints.cs
        Endpoints/DownloadEndpoints.cs
        Contracts/*.cs                      # request/response DTOs
        appsettings.json  appsettings.Development.json
      tests/
        Domain.Tests/                       # SharePrint.Domain.Tests
        Application.Tests/                  # SharePrint.Application.Tests
        Api.IntegrationTests/               # SharePrint.Api.IntegrationTests
    client/
      SharePrint/                           # SvelteKit SPA
        vite.config.ts                      # /api dev proxy → :5136
        .env                                # VITE_API_URL=/api
        src/routes/...                      # +page.ts loaders
        src/lib/services/...                # ApiService + resource services
        src/lib/stores/auth.svelte.ts
  .gitignore
```

Each file has one responsibility. Endpoint files group by feature (files that change together live together). Adapters are isolated behind ports so v1 swaps them without touching `SharePrint.Application`/endpoints.

---

# Phase v0 — Scaffold (walking skeleton)

## Task 1: Solution + projects — ✅ ALREADY DONE

Scaffolded, references wired, packages added, builds 0/0. Recorded here for
reference. **Skip to Task 2.** Commands actually run (cwd `src/server/`):

- [x] **Step 1: Create solution and projects**

```bash
dotnet new sln -n SharePrint                                              # → SharePrint.slnx (.NET 10)
dotnet new classlib -n SharePrint.Domain         -o Domain         -f net10.0
dotnet new classlib -n SharePrint.Application    -o Application    -f net10.0
dotnet new classlib -n SharePrint.Infrastructure -o Infrastructure -f net10.0
dotnet new web      -n SharePrint.Api            -o Api            -f net10.0
dotnet new xunit    -n SharePrint.Domain.Tests       -o tests/Domain.Tests       -f net10.0
dotnet new xunit    -n SharePrint.Application.Tests  -o tests/Application.Tests  -f net10.0
dotnet new xunit    -n SharePrint.Api.IntegrationTests -o tests/Api.IntegrationTests -f net10.0
dotnet sln SharePrint.slnx add Domain Application Infrastructure Api tests/Domain.Tests tests/Application.Tests tests/Api.IntegrationTests
```

- [x] **Step 2: Wire project references**

```bash
dotnet add Application reference Domain
dotnet add Infrastructure reference Application Domain
dotnet add Api reference Infrastructure Application Domain
dotnet add tests/Domain.Tests reference Domain
dotnet add tests/Application.Tests reference Application Domain
dotnet add tests/Api.IntegrationTests reference Api Infrastructure Application Domain
```

- [x] **Step 3: Add NuGet packages** (`10.*`)

```bash
dotnet add Domain         package Microsoft.Extensions.Identity.Stores            -v 10.*
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Sqlite            -v 10.*
dotnet add Infrastructure package Microsoft.EntityFrameworkCore.Design            -v 10.*
dotnet add Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore -v 10.*
dotnet add Api            package Microsoft.EntityFrameworkCore.Design            -v 10.*
dotnet add tests/Api.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing    -v 10.*
dotnet add tests/Api.IntegrationTests package Microsoft.EntityFrameworkCore.Sqlite -v 10.*
```

- [x] **Step 4: Verify build** — `dotnet build SharePrint.slnx` → Build succeeded, 0/0.

- [x] **Step 5: Commit** — Marcus commits manually (repo-root `.gitignore` in place).

---

## Task 2: Domain entities + roles

**Files:**
- Create: `src/server/Domain/Roles.cs`, `User.cs`, `Listing.cs`, `Order.cs`, `OrderItem.cs`, `DownloadGrant.cs`
- Test: `src/server/tests/Domain.Tests/DownloadGrantTests.cs`

- [ ] **Step 1: Write the failing test**

`src/server/tests/Domain.Tests/DownloadGrantTests.cs`:

```csharp
using SharePrint.Domain;
using Xunit;

public class DownloadGrantTests
{
    [Fact]
    public void IssueDownload_decrements_and_stamps()
    {
    }

    [Fact]
    public void IssueDownload_throws_when_exhausted()
    {
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Domain.Tests` (from `src/server/`)
Expected: FAIL — `DownloadGrant` / `IssueDownload` not defined.

- [ ] **Step 3: Add Identity package to Domain and write entities**

Run from `src/server/`: `dotnet add Domain package Microsoft.Extensions.Identity.Stores -v 10.*`

`src/server/Domain/Roles.cs`:

```csharp
namespace SharePrint.Domain;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Seller = "Seller";
    public const string Admin = "Admin";
}
```

`src/server/Domain/User.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace SharePrint.Domain;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

`src/server/Domain/Listing.cs`:

```csharp
namespace SharePrint.Domain;

public enum ListingStatus { Active, Unlisted }

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SellerId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "SEK";
    public string StorageKey { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

`src/server/Domain/Order.cs`:

```csharp
namespace SharePrint.Domain;

public enum OrderStatus { Paid }

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BuyerId { get; set; } = "";
    public decimal Total { get; set; }
    public string Currency { get; set; } = "SEK";
    public OrderStatus Status { get; set; } = OrderStatus.Paid;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}
```

`src/server/Domain/OrderItem.cs`:

```csharp
namespace SharePrint.Domain;

public enum OrderItemType { Download }

public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ListingId { get; set; }
    public OrderItemType Type { get; set; } = OrderItemType.Download;
    public decimal UnitPrice { get; set; }
    public DownloadGrant? Grant { get; set; }
}
```

`src/server/Domain/DownloadGrant.cs`:

```csharp
namespace SharePrint.Domain;

public class DownloadGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderItemId { get; set; }
    public int DownloadsRemaining { get; set; } = 5;
    public DateTimeOffset? LastDownloadedAt { get; set; }

    public void IssueDownload()
    {
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Domain.Tests`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/server/
git commit -m "feat: domain entities and download-grant invariant"
```

---

## Task 3: Application ports

**Files:**
- Create: `src/server/Application/Abstractions/IFileStorage.cs`, `StoredFile.cs`, `IPaymentProcessor.cs`, `PaymentResult.cs`

- [ ] **Step 1: Write the port interfaces**

`src/server/Application/Abstractions/StoredFile.cs`:

```csharp
namespace SharePrint.Application.Abstractions;

public sealed record StoredFile(Stream Content, string ContentType, string OriginalFileName);
```

`src/server/Application/Abstractions/IFileStorage.cs`:

```csharp
namespace SharePrint.Application.Abstractions;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken ct = default);
    Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
```

`src/server/Application/Abstractions/PaymentResult.cs`:

```csharp
namespace SharePrint.Application.Abstractions;

public sealed record PaymentResult(bool Succeeded, string TransactionId);
```

`src/server/Application/Abstractions/IPaymentProcessor.cs`:

```csharp
namespace SharePrint.Application.Abstractions;

public interface IPaymentProcessor
{
    Task<PaymentResult> ChargeAsync(decimal amount, string currency, string buyerId, CancellationToken ct = default);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build` (from `src/server/`)
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/server/
git commit -m "feat: file-storage and payment ports"
```

---

## Task 4: EF DbContext + SQLite + initial migration

**Files:**
- Create: `src/server/Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/server/Api/appsettings.json`, `src/server/Api/appsettings.Development.json`

> **DB notes — dev vs prod setup:**
> - v0.1 dev = SQLite file `marketplace.db` next to API binary. Zero install, one file, easy reset (`rm marketplace.db`).
> - Prod target = Postgres. Swap = change provider call `UseSqlite` → `UseNpgsql`, add NuGet `Npgsql.EntityFrameworkCore.PostgreSQL`, set env var `ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...`. No domain/application change.
> - `ConnectionStrings:Default` resolved from `appsettings.{Environment}.json` then overridden by env vars (double-underscore = nested key). Never commit prod connection string — use env or User Secrets in dev, secret manager in prod (Azure Key Vault, AWS Secrets Manager).
> - Migrations replay on app startup via `Database.MigrateAsync()` in `Program.cs`. For prod, prefer `dotnet ef database update` as a release-step over startup migration (lock + rollback control).
>
> Refs:
> - [EF Core SQLite provider](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/)
> - [Npgsql EF Core (Postgres)](https://www.npgsql.org/efcore/)
> - [ASP.NET Core configuration + secrets](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

- [ ] **Step 1: Write the DbContext**

`src/server/Infrastructure/Persistence/AppDbContext.cs`:

```csharp
using SharePrint.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SharePrint.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DownloadGrant> DownloadGrants => Set<DownloadGrant>();

    protected override void OnModelCreating(ModelBuilder b)
    {
    }
}
```

- [ ] **Step 2: Add config keys**

`src/server/Api/appsettings.json` (replace whole file):

```json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "ConnectionStrings": { "Default": "Data Source=marketplace.db" },
  "Storage": { "Provider": "LocalDisk", "LocalDisk": { "RootPath": "./storage" } },
  "Payment": { "Provider": "Fake" },
  "Download": { "TokenSecret": "dev-only-change-me", "TokenTtlMinutes": 10 }
}
```

`src/server/Api/appsettings.Development.json` (replace whole file):

```json
{
  "Logging": { "LogLevel": { "Default": "Debug" } },
  "Storage": { "Provider": "LocalDisk", "LocalDisk": { "RootPath": "./storage" } },
  "Payment": { "Provider": "Fake" }
}
```

- [ ] **Step 3: Create the EF migration**

Run from `src/server/`:

```bash
dotnet ef migrations add Initial -p Infrastructure -s Api -o Persistence/Migrations
```

Expected: `Done. To undo this action, use 'ef migrations remove'`.

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add src/server/
git commit -m "feat: EF DbContext, SQLite config, initial migration"
```

---

## Task 5: Adapters (LocalDiskStorage, FakePaymentProcessor) + storage contract test

**Files:**
- Create: `src/server/Infrastructure/Storage/LocalDiskStorage.cs`, `Payments/FakePaymentProcessor.cs`
- Test: `src/server/tests/Application.Tests/FileStorageContractTests.cs`, `LocalDiskStorageTests.cs`

> **Adapter notes — files + payment:**
> - `LocalDiskStorage` writes raw bytes under `Storage:LocalDisk:RootPath` (default `./storage`). Sidecar metadata (content-type, original filename) at `.meta/<key>`. Storage key = random GUID — never derived from filename so leaks no PII.
> - For images / pictures specifically: same byte-stream path. v1 add thumbnail generation in a `ThumbnailingStorage` decorator (`IFileStorage` wrap). Keep `LocalDiskStorage` dumb.
> - v1 cloud swap = `S3Storage : IFileStorage` adapter using AWS SDK `PutObjectAsync` / presigned `GetObject`. Same port → no endpoint change. Bucket per env (`marketplace-dev`, `marketplace-prod`), credentials via IAM role / env vars.
> - `FakePaymentProcessor` returns success unconditionally + fake transaction id. Used in dev + integration tests.
> - v1 payment swap = `StripePaymentProcessor` calls Stripe `PaymentIntents.CreateAsync` then confirms; or Klarna for SEK / EU split-pay. Charge happens server-side, never client-side trust.
>
> Refs:
> - [ASP.NET file uploads + streaming](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
> - [AWS S3 .NET — upload + presigned URL](https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/s3-apis-intro.html)
> - [Stripe PaymentIntents](https://stripe.com/docs/payments/payment-intents)

- [ ] **Step 1: Write the failing contract test**

`src/server/tests/Application.Tests/FileStorageContractTests.cs`:

```csharp
using System.Text;
using SharePrint.Application.Abstractions;
using Xunit;

public abstract class FileStorageContractTests
{
    protected abstract IFileStorage CreateStorage();

    [Fact]
    public async Task Save_then_OpenRead_round_trips()
    {
    }

    [Fact]
    public async Task StorageKey_does_not_contain_original_filename()
    {
    }
}
```

`src/server/tests/Application.Tests/LocalDiskStorageTests.cs`:

```csharp
using SharePrint.Application.Abstractions;
using SharePrint.Infrastructure.Storage;

public class LocalDiskStorageTests : FileStorageContractTests
{
    protected override IFileStorage CreateStorage()
    {
    }
}
```

Add reference: run from `src/server/`: `dotnet add tests/Application.Tests reference Infrastructure`

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Application.Tests`
Expected: FAIL — `LocalDiskStorage` not defined.

- [ ] **Step 3: Implement adapters**

`src/server/Infrastructure/Storage/LocalDiskStorage.cs`:

```csharp
using SharePrint.Application.Abstractions;

namespace SharePrint.Infrastructure.Storage;

public class LocalDiskStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _metaDir;

    public LocalDiskStorage(string rootPath)
    {
    }

    public async Task<string> SaveAsync(Stream content, string contentType, string originalFileName, CancellationToken ct = default)
    {
    }

    public async Task<StoredFile> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
    }
}
```

`src/server/Infrastructure/Payments/FakePaymentProcessor.cs`:

```csharp
using SharePrint.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace SharePrint.Infrastructure.Payments;

public class FakePaymentProcessor : IPaymentProcessor
{
    private readonly ILogger<FakePaymentProcessor> _log;
    public FakePaymentProcessor(ILogger<FakePaymentProcessor> log) => _log = log;

    public Task<PaymentResult> ChargeAsync(decimal amount, string currency, string buyerId, CancellationToken ct = default)
    {
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Application.Tests`
Expected: PASS (2 tests via `LocalDiskStorageTests`).

- [ ] **Step 5: Commit**

```bash
git add src/server/
git commit -m "feat: local-disk storage + fake payment adapters with contract tests"
```

---

## Task 6: Config-driven DI + Program.cs + health endpoint

**Files:**
- Create: `src/server/Infrastructure/DependencyInjection.cs`
- Modify: `src/server/Api/Program.cs`

> **Wiring notes — env-driven adapter selection:**
> - `Storage:Provider` + `Payment:Provider` config keys pick concrete impl at DI registration. v0.1 values = `LocalDisk` / `Fake`. v1 values = `S3` / `Stripe`. Unknown provider throws on startup — fail fast, no silent fallback.
> - Dev/prod split = `appsettings.Development.json` (committed) + env vars override (uncommitted secrets). Run with `ASPNETCORE_ENVIRONMENT=Production` to pick prod config.
> - Secrets (Stripe API key, S3 access key, `Download:TokenSecret`) come from env vars or secret manager, never from `appsettings.json`. v0.1 has placeholder `dev-only-change-me` — must be replaced before any deploy.
> - Identity password rules + cookie auth set here. For prod, set `o.Cookie.Secure = CookieSecurePolicy.Always` behind HTTPS.
>
> Refs:
> - [.NET Configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
> - [ASP.NET environment-based config](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments)
> - [Options pattern + validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)

- [ ] **Step 1: Write the composition root**

`src/server/Infrastructure/DependencyInjection.cs`:

```csharp
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using Infrastructure.Payments;
using SharePrint.Infrastructure.Persistence;
using SharePrint.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharePrint.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
    }
}
```

- [ ] **Step 2: Write Program.cs**

`src/server/Api/Program.cs` (replace whole file):

```csharp
using SharePrint.Domain;
using SharePrint.Infrastructure;
using SharePrint.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Write the failing health integration test**

`src/server/tests/Api.IntegrationTests/HealthTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public HealthTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task Health_returns_ok()
    {
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/server/
git commit -m "feat: config-driven DI, cookie auth, role seeding, health endpoint"
```

---

## Task 7: SvelteKit client — ApiService + health round-trip

**Files:**
- Create: `src/client/SharePrint/src/lib/services/apiService.ts`, `unwrap.ts`, `src/client/SharePrint/src/lib/stores/auth.svelte.ts`, `src/client/SharePrint/src/routes/+page.ts`/`+page.svelte`

> **Steps 1–2 (scaffold + SPA reconfig) are ✅ ALREADY DONE.** The SvelteKit app
> exists at `src/client/SharePrint/` (Svelte 5, Kit 2, Vite). `adapter-static`
> installed; `svelte.config.js` uses `adapter({ fallback: 'index.html' })`;
> `src/routes/+layout.ts` has `ssr=false`/`prerender=false`; `vite.config.ts`
> proxies `/api → http://localhost:5136`; `.env` has `VITE_API_URL=/api`;
> empty `src/lib/services/` + `src/lib/stores/` exist. **Start at Step 3.**

- [x] **Step 1: Scaffold SvelteKit** — done (app pre-existed as `SharePrint`).
- [x] **Step 2: Configure SPA mode + dev proxy** — done (adapter-static, `+layout.ts` SPA flags, Vite `/api`→`:5136`, `.env`).

- [ ] **Step 3: Write ApiService (house convention)**

`src/client/SharePrint/src/lib/stores/auth.svelte.ts` (create):

```ts
export const auth = $state<{ isAuthenticated: boolean; user: { id: string; email: string; displayName: string; roles: string[] } | null }>({
  isAuthenticated: false,
  user: null
});
```

`src/client/SharePrint/src/lib/services/apiService.ts` (create — mirrors Grantigo `apiService.ts`):

```ts
import { auth } from '$lib/stores/auth.svelte';

const apiUrl = import.meta.env.VITE_API_URL;

export interface AppError {
  code: 'AUTH_ERROR' | 'API_ERROR' | 'NETWORK_ERROR' | 'NOT_FOUND';
  message: string;
  status?: number;
  details?: unknown;
}

function authError(res: Response): AppError | null {
}

export class ApiService {
  private fetch: typeof fetch;
  constructor(fetchFn: typeof fetch = fetch) {}

  private async parse<T>(res: Response): Promise<T | null> {
  }

  async get<T>(endpoint: string, params?: Record<string, string | number | undefined>): Promise<T | null | AppError> {
  }

  async post<T, D>(endpoint: string, data: D): Promise<T | null | AppError> {
  }
  async patch<T, D>(endpoint: string, data: D): Promise<T | null | AppError> {
  }
  async put<T, D>(endpoint: string, data: D): Promise<T | null | AppError> {
  }

  private async body<T, D>(method: string, endpoint: string, data: D): Promise<T | null | AppError> {
  }

  async delete<T>(endpoint: string): Promise<T | null | AppError> {
  }

  async postFormData<T>(endpoint: string, form: FormData): Promise<T | null | AppError> {
  }
}
```

`src/client/SharePrint/src/lib/services/unwrap.ts` (create):

```ts
import type { AppError } from './apiService';

export function isAppError(x: unknown): x is AppError {
}

export function unwrap<T>(res: T | null | AppError): T {
}
```

- [ ] **Step 4: Health round-trip page**

`src/client/SharePrint/src/routes/+page.ts` (create):

```ts
import { ApiService } from '$lib/services/apiService';
import { unwrap } from '$lib/services/unwrap';

export async function load({ fetch }) {
}
```

`src/client/SharePrint/src/routes/+page.svelte` (replace whole file):

```svelte
<script lang="ts">
  let { data } = $props();
</script>

<h1>File Marketplace</h1>
<p>API health: {data.health.status}</p>
```

- [ ] **Step 5: Manual verify the round-trip**

Run (two terminals): `dotnet run --project Api` (from `src/server/`, note the http port; set the Vite proxy target to it if not 5136) and `npm run dev` (from `src/client/SharePrint/`).
Open the Vite URL. Expected: page shows `API health: ok`.

- [ ] **Step 6: Commit**

```bash
git add src/client/SharePrint/
git commit -m "feat: SvelteKit SPA scaffold, ApiService, health round-trip"
```

**v0 exit criteria met:** both apps run; frontend round-trips one API call; health integration test passes.

---

# Phase v0.1 — Core happy path

## Task 8: Auth endpoints (register/login/logout/me)

**Files:**
- Create: `src/server/Api/Contracts/AuthContracts.cs`, `src/server/Api/Endpoints/AuthEndpoints.cs`
- Modify: `src/server/Api/Program.cs` (map endpoints)
- Test: `src/server/tests/Api.IntegrationTests/AuthTests.cs`

- [ ] **Step 1: Write the failing test**

`src/server/tests/Api.IntegrationTests/AuthTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public AuthTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task Register_then_me_returns_user_with_customer_role()
    {
    }
    public record MeDto(string id, string email, string displayName, string[] roles);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Api.IntegrationTests --filter AuthTests`
Expected: FAIL — 404 on `/api/auth/register`.

- [ ] **Step 3: Write contracts + endpoints**

`src/server/Api/Contracts/AuthContracts.cs`:

```csharp
namespace SharePrint.Api.Contracts;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record MeResponse(string Id, string Email, string DisplayName, string[] Roles);
```

`src/server/Api/Endpoints/AuthEndpoints.cs`:

```csharp
using SharePrint.Api.Contracts;
using SharePrint.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SharePrint.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth");

        g.MapPost("/register", async (RegisterRequest r, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
        {
        });

        g.MapPost("/login", async (LoginRequest r, SignInManager<AppUser> signIn) =>
        {
        });

        g.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
        }).RequireAuthorization();

        g.MapGet("/me", async (HttpContext ctx, UserManager<AppUser> users) =>
        {
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 4: Map endpoints in Program.cs**

In `src/server/Api/Program.cs`, replace the line `app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));` with:

```csharp
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAuth();
```

Add `using SharePrint.Api.Endpoints;` at the top.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests --filter AuthTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/server/
git commit -m "feat: auth endpoints with Customer role + auto-confirmed email"
```

---

## Task 9: Seller apply endpoint

**Files:**
- Create: `src/server/Api/Endpoints/SellerEndpoints.cs`
- Modify: `src/server/Api/Program.cs`
- Test: `src/server/tests/Api.IntegrationTests/SellerTests.cs`

- [ ] **Step 1: Write the failing test**

`src/server/tests/Api.IntegrationTests/SellerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class SellerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public SellerTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task Apply_adds_seller_role()
    {
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Api.IntegrationTests --filter SellerTests`
Expected: FAIL — 404 on `/api/seller/apply`.

- [ ] **Step 3: Write the endpoint**

`src/server/Api/Endpoints/SellerEndpoints.cs`:

```csharp
using SharePrint.Domain;
using Microsoft.AspNetCore.Identity;

namespace SharePrint.Api.Endpoints;

public static class SellerEndpoints
{
    public static void MapSeller(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/seller/apply", async (HttpContext ctx, UserManager<AppUser> users) =>
        {
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 4: Map it**

In `src/server/Api/Program.cs`, add after `app.MapAuth();`:

```csharp
app.MapSeller();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests --filter SellerTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/server/
git commit -m "feat: seller-apply endpoint grants Seller role"
```

---

## Task 10: Listing create (upload) + catalog + detail

**Files:**
- Create: `src/server/Api/Contracts/ListingContracts.cs`, `src/server/Api/Endpoints/ListingEndpoints.cs`
- Modify: `src/server/Api/Program.cs`
- Test: `src/server/tests/Api.IntegrationTests/ListingTests.cs`

> **Upload notes — saving picture / file bytes:**
> - Multipart form field `file` streams via `IFormFile.OpenReadStream()` directly into `IFileStorage.SaveAsync`. No `ReadAllBytesAsync` — keep memory flat for big files.
> - Server validates: title non-empty, price > 0, file present + non-empty. v1 add: MIME allowlist (`image/png`, `image/jpeg`, `application/pdf`, `model/stl`), max size (`Kestrel:Limits:MaxRequestBodySize`), antivirus scan hook (`IFileScanner`).
> - Persisted on `Listing`: `StorageKey` (opaque GUID), `OriginalFileName` (for download `Content-Disposition`), `ContentType`, `SizeBytes`. Never expose `StorageKey` over wire — DTO (`ListingSummary`/`ListingDetail`) hides it.
> - Pictures specifically: v1 add image-only endpoint variant that runs through a thumbnailing decorator (`ThumbnailingStorage` wraps `IFileStorage`, writes `<key>_thumb.webp` alongside original).
>
> Refs:
> - [File uploads in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads)
> - [Kestrel request limits](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options)
> - [File upload security guidance](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads#security-considerations)

- [ ] **Step 1: Write the failing test**

`src/server/tests/Api.IntegrationTests/ListingTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class ListingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public ListingTests(WebApplicationFactory<Program> f) => _f = f;

    private async Task<HttpClient> Seller()
    {
    }

    [Fact]
    public async Task Create_lists_in_catalog_without_exposing_key()
    {
    }

    [Fact]
    public async Task Customer_cannot_create_listing()
    {
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Api.IntegrationTests --filter ListingTests`
Expected: FAIL — 404 on `/api/listings`.

- [ ] **Step 3: Write contracts + endpoints**

`src/server/Api/Contracts/ListingContracts.cs`:

```csharp
namespace SharePrint.Api.Contracts;

public record ListingSummary(Guid Id, string Title, decimal Price, string Currency, string SellerDisplayName);
public record ListingDetail(Guid Id, string Title, string Description, decimal Price, string Currency, string SellerDisplayName, string Status);
public record UpdateListingRequest(string Title, string Description, decimal Price);
```

`src/server/Api/Endpoints/ListingEndpoints.cs`:

```csharp
using SharePrint.Api.Contracts;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SharePrint.Api.Endpoints;

public static class ListingEndpoints
{
    public static void MapListings(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/listings");

        g.MapPost("", async (HttpRequest req, HttpContext ctx, IFileStorage storage,
            UserManager<AppUser> users, AppDbContext db) =>
        {
        }).RequireAuthorization(p => p.RequireRole(Roles.Seller));

        g.MapGet("", async (AppDbContext db, UserManager<AppUser> users, int page = 1, int pageSize = 20) =>
        {
        });

        g.MapGet("/{id:guid}", async (Guid id, AppDbContext db, UserManager<AppUser> users) =>
        {
        });

        g.MapPatch("/{id:guid}", async (Guid id, UpdateListingRequest r, HttpContext ctx,
            UserManager<AppUser> users, AppDbContext db) =>
        {
        }).RequireAuthorization(p => p.RequireRole(Roles.Seller));

        g.MapPost("/{id:guid}/unlist", async (Guid id, HttpContext ctx,
            UserManager<AppUser> users, AppDbContext db) =>
        {
        }).RequireAuthorization(p => p.RequireRole(Roles.Seller));
    }
}
```

- [ ] **Step 4: Map endpoints**

In `src/server/Api/Program.cs`, add after `app.MapSeller();`:

```csharp
app.MapListings();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests --filter ListingTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/server/
git commit -m "feat: listing create/catalog/detail/edit/unlist with role authz"
```

---

## Task 11: Checkout → Order + DownloadGrant(5)

**Files:**
- Create: `src/server/Api/Contracts/CheckoutContracts.cs`, `src/server/Api/Endpoints/CheckoutEndpoints.cs`
- Modify: `src/server/Api/Program.cs`
- Test: `src/server/tests/Api.IntegrationTests/CheckoutTests.cs`

> **Payment notes:**
> - `IPaymentProcessor.ChargeAsync` runs before any `Order` insert. Charge fails → 402, no order row, no grant. Charge succeeds → order + items + grants persisted in same `SaveChangesAsync` (single transaction).
> - v0.1 `FakePaymentProcessor` returns success unconditionally. Logs amount + buyer for traceability.
> - v1 swap to `StripePaymentProcessor`: create + confirm `PaymentIntent`, store `PaymentIntent.Id` on `Order` (column `PaymentReference`). Use idempotency key = `Order.Id` so client retries do not double-charge. Webhook `payment_intent.succeeded` reconciles edge case where client crashes after charge.
> - For SEK / Swedish market specifically, consider Klarna Payments (split-pay, invoice). Same port, different adapter (`KlarnaPaymentProcessor`).
> - Never trust client-sent total — server recomputes from `Listing.Price` lookup. v1 adds price-lock (timestamp + price hash on the cart) so price-change race does not under-charge.
>
> Refs:
> - [Stripe PaymentIntents API](https://stripe.com/docs/api/payment_intents)
> - [Stripe idempotent requests](https://stripe.com/docs/api/idempotent_requests)
> - [Klarna Payments docs](https://docs.klarna.com/klarna-payments/)

- [ ] **Step 1: Write the failing test**

`src/server/tests/Api.IntegrationTests/CheckoutTests.cs`:

```csharp
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class CheckoutTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public CheckoutTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task Checkout_creates_order_with_grant_of_five()
    {
    }
    public record IdDto(Guid id);
    public record OrderDto(Guid id, decimal total, List<ItemDto> items);
    public record ItemDto(Guid orderItemId, Guid listingId, string title, int downloadsRemaining);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Api.IntegrationTests --filter CheckoutTests`
Expected: FAIL — 404 on `/api/checkout`.

- [ ] **Step 3: Write contracts + endpoints**

`src/server/Api/Contracts/CheckoutContracts.cs`:

```csharp
namespace SharePrint.Api.Contracts;

public record CheckoutRequest(Guid[] ListingIds);
public record OrderItemResponse(Guid OrderItemId, Guid ListingId, string Title, int DownloadsRemaining);
public record OrderResponse(Guid Id, decimal Total, string Currency, List<OrderItemResponse> Items);
```

`src/server/Api/Endpoints/CheckoutEndpoints.cs`:

```csharp
using SharePrint.Api.Contracts;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SharePrint.Api.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckout(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/checkout", async (CheckoutRequest r, HttpContext ctx,
            UserManager<AppUser> users, AppDbContext db, IPaymentProcessor pay) =>
        {
        }).RequireAuthorization();

        app.MapGet("/api/orders", async (HttpContext ctx, UserManager<AppUser> users, AppDbContext db) =>
        {
        }).RequireAuthorization();
    }
}
```

- [ ] **Step 4: Map endpoints**

In `src/server/Api/Program.cs`, add after `app.MapListings();`:

```csharp
app.MapCheckout();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests --filter CheckoutTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/server/
git commit -m "feat: checkout via fake payment creates order + 5-download grant"
```

---

## Task 12: Download token (decrement-on-issuance) + file stream

**Files:**
- Create: `src/server/Api/Endpoints/DownloadEndpoints.cs`, `src/server/Api/DownloadToken.cs`
- Modify: `src/server/Api/Program.cs`
- Test: `src/server/tests/Api.IntegrationTests/DownloadTests.cs`

> **Download notes — file stream + signed token:**
> - HMAC-SHA256 token = base64url(`{orderItemId}:{expiryUnix}:{hexSig}`). Validates signature + expiry server-side; no DB lookup to validate token (stateless).
> - Decrement-on-issuance: counter drops when `/api/downloads/{id}` is called, NOT when bytes flow. Trade-off = network failure mid-download still burns a credit. v1 alt = decrement-on-stream-complete (track via byte range checkpoints).
> - Token TTL = `Download:TokenTtlMinutes` (default 10 min). Shorter = safer if URL leaks, longer = friendlier on slow networks.
> - File bytes streamed via `IFileStorage.OpenReadAsync` → `Results.File(stream, contentType, originalFileName)`. ASP.NET handles range requests + `Content-Disposition`.
> - v1 swap = generate S3 presigned URL instead of HMAC token + local stream. Client downloads from S3 directly — bytes never touch the API box. Saves egress cost + CPU.
>
> Refs:
> - [`HMACSHA256` in .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256)
> - [S3 presigned URLs (.NET)](https://docs.aws.amazon.com/AmazonS3/latest/userguide/ShareObjectPreSignedURL.html)
> - [ASP.NET large-file streaming](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads#upload-large-files-with-streaming)

- [ ] **Step 1: Write the failing test**

`src/server/tests/Api.IntegrationTests/DownloadTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class DownloadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public DownloadTests(WebApplicationFactory<Program> f) => _f = f;

    private async Task<(HttpClient c, Guid orderItemId)> BoughtItem()
    {
    }

    [Fact]
    public async Task Token_then_file_streams_and_counter_decrements()
    {
    }

    [Fact]
    public async Task Sixth_token_request_is_rejected()
    {
    }
    public record TokenDto(string token);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/Api.IntegrationTests --filter DownloadTests`
Expected: FAIL — 404 on `/api/downloads/...`.

- [ ] **Step 3: Write token helper + endpoints**

`src/server/Api/DownloadToken.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace SharePrint.Api;

public static class DownloadToken
{
    public static string Create(Guid orderItemId, DateTimeOffset expiry, string secret)
    {
    }

    public static bool TryValidate(string token, string secret, out Guid orderItemId)
    {
    }

    private static string Sign(string data, string secret)
    {
    }
    private static string Base64Url(string s)
    {
    }
    private static byte[] Base64UrlDecode(string s)
    {
    }
}
```

`src/server/Api/Endpoints/DownloadEndpoints.cs`:

```csharp
using SharePrint.Api;
using SharePrint.Application.Abstractions;
using SharePrint.Domain;
using SharePrint.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SharePrint.Api.Endpoints;

public static class DownloadEndpoints
{
    public static void MapDownloads(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/downloads/{orderItemId:guid}", async (Guid orderItemId, HttpContext ctx,
            UserManager<AppUser> users, AppDbContext db, IConfiguration cfg) =>
        {
        }).RequireAuthorization();

        app.MapGet("/api/files/{token}", async (string token, AppDbContext db,
            IFileStorage storage, IConfiguration cfg) =>
        {
        });
    }
}
```

- [ ] **Step 4: Map endpoints**

In `src/server/Api/Program.cs`, add after `app.MapCheckout();`:

```csharp
app.MapDownloads();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/Api.IntegrationTests --filter DownloadTests`
Expected: PASS (2 tests). Then run full suite: `dotnet test` — all green.

- [ ] **Step 6: Commit**

```bash
git add src/server/
git commit -m "feat: simulated signed-URL download with decrement-on-issuance"
```

---

## Task 13: Frontend resource services

**Files:**
- Create: `src/client/SharePrint/src/lib/services/authService.ts`, `listingService.ts`, `orderService.ts`, `downloadService.ts`, `types.ts`

- [ ] **Step 1: Write shared DTO types**

`src/client/SharePrint/src/lib/services/types.ts`:

```ts
export interface MeResponse { id: string; email: string; displayName: string; roles: string[]; }
export interface ListingSummary { id: string; title: string; price: number; currency: string; sellerDisplayName: string; }
export interface ListingDetail { id: string; title: string; description: string; price: number; currency: string; sellerDisplayName: string; status: string; }
export interface OrderItemResponse { orderItemId: string; listingId: string; title: string; downloadsRemaining: number; }
export interface OrderResponse { id: string; total: number; currency: string; items: OrderItemResponse[]; }
```

- [ ] **Step 2: Write the service classes**

`src/client/SharePrint/src/lib/services/authService.ts`:

```ts
import { ApiService } from './apiService';
import { unwrap } from './unwrap';
import { auth } from '$lib/stores/auth.svelte';
import type { MeResponse } from './types';

export class AuthService {
  constructor(private api: ApiService) {}

  async register(email: string, password: string, displayName: string) {
  }
  async login(email: string, password: string) {
  }
  async logout() {
  }
  async me(): Promise<MeResponse | null> {
  }
}
```

`src/client/SharePrint/src/lib/services/listingService.ts`:

```ts
import { ApiService } from './apiService';
import { unwrap } from './unwrap';
import type { ListingSummary, ListingDetail } from './types';

export class ListingService {
  constructor(private api: ApiService) {}

  list(page = 1, pageSize = 20): Promise<ListingSummary[]> {
  }
  get(id: string): Promise<ListingDetail> {
  }
  async createFileProduct(input: { title: string; description: string; price: number; file: File }): Promise<{ id: string }> {
  }
  async updateFileProduct(id: string, input: { title: string; description: string; price: number }) {
  }
  async unlistFileProduct(id: string) {
  }
}
```

`src/client/SharePrint/src/lib/services/orderService.ts`:

```ts
import { ApiService } from './apiService';
import { unwrap } from './unwrap';
import type { OrderResponse } from './types';

export class OrderService {
  constructor(private api: ApiService) {}

  async checkout(listingIds: string[]): Promise<{ id: string }> {
  }
  listOrders(): Promise<OrderResponse[]> {
  }
}
```

`src/client/SharePrint/src/lib/services/downloadService.ts`:

```ts
import { ApiService } from './apiService';
import { unwrap } from './unwrap';

export class DownloadService {
  constructor(private api: ApiService) {}

  async requestToken(orderItemId: string): Promise<string> {
  }
  fileUrl(token: string): string {
  }
}
```

- [ ] **Step 3: Verify type-check**

Run: `npm run check` (from `src/client/SharePrint/`)
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/client/SharePrint/
git commit -m "feat: frontend resource services (auth/listing/order/download)"
```

---

## Task 14: Frontend routes (catalog, detail, auth, sell, library)

**Files:**
- Create: `src/client/SharePrint/src/routes/+layout.ts`, `+page.ts`, `+page.svelte`, `listings/[id]/+page.ts` + `.svelte`, `login/+page.svelte`, `register/+page.svelte`, `sell/+page.svelte`, `library/+page.ts` + `.svelte`

- [ ] **Step 1: Layout loads session**

`src/client/SharePrint/src/routes/+layout.ts` (replace whole file):

```ts
import { ApiService } from '$lib/services/apiService';
import { AuthService } from '$lib/services/authService';

export const ssr = false;
export const prerender = false;

export async function load({ fetch }) {
}
```

- [ ] **Step 2: Catalog page**

`src/client/SharePrint/src/routes/+page.ts` (replace whole file):

```ts
import { ApiService } from '$lib/services/apiService';
import { ListingService } from '$lib/services/listingService';

export async function load({ fetch, url }) {
}
```

`src/client/SharePrint/src/routes/+page.svelte` (replace whole file):

```svelte
<script lang="ts">
  let { data } = $props();
</script>

<h1>Catalog</h1>
<nav><a href="/sell">Sell a file</a> | <a href="/library">My library</a> | <a href="/login">Login</a></nav>
<ul>
  {#each data.listings as l}
    <li><a href={`/listings/${l.id}`}>{l.title}</a> — {l.price} {l.currency} (by {l.sellerDisplayName})</li>
  {/each}
</ul>
```

- [ ] **Step 3: Listing detail + buy**

`src/client/SharePrint/src/routes/listings/[id]/+page.ts` (create):

```ts
import { ApiService } from '$lib/services/apiService';
import { ListingService } from '$lib/services/listingService';
import { error } from '@sveltejs/kit';

export async function load({ params, fetch }) {
}
```

`src/client/SharePrint/src/routes/listings/[id]/+page.svelte` (create):

```svelte
<script lang="ts">
  import { ApiService } from '$lib/services/apiService';
  import { OrderService } from '$lib/services/orderService';
  import { goto } from '$app/navigation';
  let { data } = $props();
  let msg = $state('');
  async function buy() {
  }
</script>

<h1>{data.listing.title}</h1>
<p>{data.listing.description}</p>
<p><b>{data.listing.price} {data.listing.currency}</b></p>
<button onclick={buy}>Buy (fake payment)</button>
{#if msg}<p style="color:red">{msg}</p>{/if}
```

- [ ] **Step 4: Login + register + sell pages**

`src/client/SharePrint/src/routes/register/+page.svelte` (create):

```svelte
<script lang="ts">
  import { ApiService } from '$lib/services/apiService';
  import { AuthService } from '$lib/services/authService';
  import { goto } from '$app/navigation';
  let email = $state(''), password = $state(''), displayName = $state(''), msg = $state('');
  async function submit() {
  }
</script>

<h1>Register</h1>
<input placeholder="email" bind:value={email} />
<input placeholder="display name" bind:value={displayName} />
<input type="password" placeholder="password" bind:value={password} />
<button onclick={submit}>Register</button>
{#if msg}<p style="color:red">{msg}</p>{/if}
```

`src/client/SharePrint/src/routes/login/+page.svelte` (create):

```svelte
<script lang="ts">
  import { ApiService } from '$lib/services/apiService';
  import { AuthService } from '$lib/services/authService';
  import { goto } from '$app/navigation';
  let email = $state(''), password = $state(''), msg = $state('');
  async function submit() {
  }
</script>

<h1>Login</h1>
<input placeholder="email" bind:value={email} />
<input type="password" placeholder="password" bind:value={password} />
<button onclick={submit}>Login</button>
{#if msg}<p style="color:red">{msg}</p>{/if}
```

`src/client/SharePrint/src/routes/sell/+page.svelte` (create):

```svelte
<script lang="ts">
  import { ApiService } from '$lib/services/apiService';
  import { AuthService } from '$lib/services/authService';
  import { ListingService } from '$lib/services/listingService';
  import { goto } from '$app/navigation';
  let title = $state(''), description = $state(''), price = $state(0);
  let files = $state<FileList | null>(null), msg = $state('');
  async function submit() {
  }
</script>

<h1>Sell a file</h1>
<input placeholder="title" bind:value={title} />
<input placeholder="description" bind:value={description} />
<input type="number" placeholder="price (SEK)" bind:value={price} />
<input type="file" bind:files />
<button onclick={submit}>Create listing</button>
{#if msg}<p style="color:red">{msg}</p>{/if}
```

- [ ] **Step 5: Library page (download)**

`src/client/SharePrint/src/routes/library/+page.ts` (create):

```ts
import { ApiService } from '$lib/services/apiService';
import { OrderService } from '$lib/services/orderService';
import { redirect } from '@sveltejs/kit';

export async function load({ fetch }) {
}
```

`src/client/SharePrint/src/routes/library/+page.svelte` (create):

```svelte
<script lang="ts">
  import { ApiService } from '$lib/services/apiService';
  import { DownloadService } from '$lib/services/downloadService';
  import { invalidateAll } from '$app/navigation';
  let { data } = $props();
  let msg = $state('');
  async function download(orderItemId: string) {
  }
</script>

<h1>My library</h1>
{#each data.orders as o}
  {#each o.items as i}
    <div>{i.title} — {i.downloadsRemaining} left
      <button disabled={i.downloadsRemaining === 0} onclick={() => download(i.orderItemId)}>Download</button>
    </div>
  {/each}
{/each}
{#if msg}<p style="color:red">{msg}</p>{/if}
```

- [ ] **Step 6: Manual end-to-end verify**

Run `dotnet run --project Api` (from `src/server/`) and `npm run dev` (from `src/client/SharePrint/`). In the browser: register → go to `/sell`, upload a file → open the listing → Buy → land on `/library` → Download (file downloads, count drops 5→4). Download 4 more times → button disables and a 6th attempt shows the exhausted error.

- [ ] **Step 7: Commit**

```bash
git add src/client/SharePrint/
git commit -m "feat: SvelteKit routes — catalog, detail, auth, sell, library"
```

---

## Task 15: End-to-end happy-path integration test

**Files:**
- Test: `src/server/tests/Api.IntegrationTests/HappyPathTests.cs`

- [ ] **Step 1: Write the full-flow test**

`src/server/tests/Api.IntegrationTests/HappyPathTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class HappyPathTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public HappyPathTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task Register_sell_buy_download_decrements()
    {
    }
}
```

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test` (from `src/server/`)
Expected: ALL tests PASS (Domain.Tests, Application.Tests, Api.IntegrationTests including this one).

- [ ] **Step 3: Commit**

```bash
git add src/server/
git commit -m "test: end-to-end happy-path coverage"
```

**v0.1 exit criteria met:** full happy path works end to end on localhost; integration test covers it; role authz tested (Customer cannot create a listing); grant exhaustion enforced.

---

## Self-Review

**Spec coverage:**
- Solution structure §5 → Task 1. Ports §6 → Task 3. Adapters + env routing §6 → Tasks 5–6. Roles/data model §7 → Tasks 2, 4. API surface §8 → Tasks 8–12. Frontend data loading §9 → Tasks 7, 14. Frontend API layering §10 (ApiService/unwrap/services/stores) → Tasks 7, 13. Testing §11 → tests in every task + contract test (Task 5) + e2e (Task 15). Milestone exit criteria §4 → end of Task 7 (v0) and Task 15 (v0.1). SEK currency §7 → Domain (Task 2) + checkout (Task 11). Auto-confirm email §4 → Task 8 (`EmailConfirmed = true`). Simulated signed URL §6 → Task 12. No-search/no-prints/etc. correctly omitted (v1).
- v1-only items (S3/Stripe/POD/refunds/reviews/moderation/email/OAuth/2FA/Postgres) intentionally absent — out of v0.1 scope per spec §4.

**Placeholder scan:** No TBD/TODO; every code step contains complete, runnable code; commands have expected output. No "similar to Task N".

**Type consistency:** `IFileStorage.SaveAsync(stream, contentType, originalFileName)` defined Task 3, used identically Tasks 5/10. `DownloadGrant.IssueDownload()` defined Task 2, used Task 12. `unwrap`/`AppError`/`ApiService` method names consistent across Tasks 7/13/14. DTO field names (`downloadsRemaining`, `orderItemId`, `sellerDisplayName`) match between server contracts (Tasks 10–11) and client `types.ts` (Task 13) and test DTOs. Endpoint paths consistent (`/api/...` server, `VITE_API_URL=/api` + endpoint without leading slash client).

No issues found.
