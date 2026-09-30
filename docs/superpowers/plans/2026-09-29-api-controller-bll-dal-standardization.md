# Nova Haven API Controller–BLL–DAL Standardization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert every ASP.NET Core business route to a thin MVC Controller → BLL (`NovaHaven.Application`) → feature repository → DAL (`NovaHaven.Infrastructure`) flow while keeping the existing API and database behavior unchanged.

**Architecture:** Keep the four existing projects and make their roles explicit: Api is Presentation/composition root; Application is BLL and owns use cases, repository contracts and transaction abstraction; Domain owns entities/invariants; Infrastructure is DAL and implements repositories/UoW with EF Core. Convert one feature at a time, retaining the current route and JSON contract and proving each slice through the existing SQL integration tests.

**Tech Stack:** ASP.NET Core .NET 10 MVC, EF Core 10.0.12, PostgreSQL/Npgsql, xUnit, existing OpenAPI JSON contract.

**Spec:** `docs/superpowers/specs/2026-09-29-project-structure-standardization-design.md`

## Global Constraints

- `NovaHaven.Application` is the BLL; do not create a second BLL assembly.
- `NovaHaven.Infrastructure` is the DAL; controllers and Application services never inject or reference `NovaDbContext`.
- Repository interfaces are feature/aggregate-specific; do not add generic `IRepository<T>` CRUD wrappers.
- Keep .NET target `net10.0`, nullable enabled, implicit usings enabled and warnings as errors.
- Preserve every current route, HTTP method, JSON property, status code, authorization rule, CSRF check, ETag and transaction boundary.
- Preserve current PostgreSQL tables, migration baseline, relationships and local-only database usage; this API refactor is not a schema migration. Historical SQL Server migration files remain archived and excluded from the active migration assembly.
- Keep public Wiki reads anonymous and published-only; Admin writes remain authorized and antiforgery-protected.
- Do not add mediator, AutoMapper, a new persistence framework, remote service, or speculative abstraction package.
- Do not claim relational verification unless the PostgreSQL integration suite actually runs against a disposable database.
- The current working copy has Git metadata on `delivery/source-install-guide`, but its large existing working-tree changes are user-owned. Preserve them; do not claim the unavailable pre-checkpoint commit history was recovered.
- Integration fixtures may create/drop only their exact GUID-named `NovaHaven_Integration_<32 hex>` databases. Never target the configured local content database.

## Review Focus

1. **Draft/private content leakage:** anonymous Wiki/API reads expose only the current published revision. Pin with `PublicWikiOnlyExposesPublishedRevision` and public detail/list assertions in `WikiApiIntegrationTests.cs`.
2. **Auth/CSRF regression:** anonymous and non-Admin requests cannot read or mutate Admin routes; missing/invalid CSRF is rejected. Pin with `AnonymousAdminReadRequiresAuthentication`, `AuthenticatedNonAdminCannotMutateAdminWiki` and the existing mutation helpers in `WikiApiIntegrationTests.cs`.
3. **Missing/stale ETag and concurrent writes:** missing precondition remains 428, stale remains 412, and same-ETag races have one winner. Pin with `CategoryRequiresIfMatchAndRejectsDuplicateSlug` and `ConcurrentArticleEditsWithSameEtagHaveOneWinnerPerRound`.
4. **Transaction/publication atomicity:** publish/restore/checkout either persist the complete result or nothing; audit/revision state remains consistent. Pin with revision/restore integration tests and commerce checkout integration tests.
5. **Schema/contract drift:** moving entity/configuration files creates no pending EF model change, and clients see no path/schema change. Pin with `dotnet ef migrations has-pending-model-changes` and `npm test` contract guards.

---

### Task 1: Establish a backend solution and repeatable baseline

**Files:**
- Create: `NovaHaven.sln`
- Create: `Directory.Build.props`
- Create: `.editorconfig`
- Modify: `backend/NovaHaven.Api/NovaHaven.Api.csproj`
- Modify: `backend/NovaHaven.Application/NovaHaven.Application.csproj`
- Modify: `backend/NovaHaven.Domain/NovaHaven.Domain.csproj`
- Modify: `backend/NovaHaven.Infrastructure/NovaHaven.Infrastructure.csproj`
- Modify: `tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj`
- Modify: `tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj`

**Interfaces:**
- Consumes: all six existing backend/test projects.
- Produces: one solution containing those projects and shared .NET properties; project-specific package references remain in their existing project files.

- [ ] **Step 1: Record the pre-change baseline**

Run:

```powershell
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj
dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj
```

Record exact pass/fail and whether the second command can connect to its configured local SQL Server. Do not repair unrelated baseline failures in this task.

- [x] **Step 2: Create `NovaHaven.sln` and add the six existing projects**

Use `dotnet new solution --format sln -n NovaHaven`, then `dotnet sln NovaHaven.sln add` for the four backend projects and two backend test projects.

- [x] **Step 3: Centralize the repeated build properties**

Set `TargetFramework` to `net10.0`, `ImplicitUsings` to `enable`, `Nullable` to `enable`, and `TreatWarningsAsErrors` to `true` in `Directory.Build.props`; remove only those duplicated properties from the six project files. Do not centralize package versions in this change.

- [x] **Step 4: Add C# conventions to `.editorconfig`**

Set UTF-8, four-space indentation and a final newline for C#; do not set a repository-wide line-ending rule or mass-format existing files. Keep compiler warnings-as-errors in `Directory.Build.props` rather than duplicating that policy as editor diagnostics.

- [ ] **Step 5: Verify solution restore/build/tests**

Run `dotnet test NovaHaven.sln`. Expected: all currently runnable Domain and Integration tests pass; report a SQL/environment limitation separately if the relational suite cannot connect.

### Task 2: Put domain entities and EF mappings in explicit feature folders

**Files:**
- Move: entity classes under `backend/NovaHaven.Domain/<Feature>/` to `backend/NovaHaven.Domain/<Feature>/Entities/`
- Modify: namespaces/usings in Application, Infrastructure, Api, migrations/snapshots and backend tests that reference those entities
- Create: feature `IEntityTypeConfiguration<TEntity>` files under `backend/NovaHaven.Infrastructure/Persistence/Configurations/<Feature>/`
- Modify: `backend/NovaHaven.Infrastructure/Data/NovaDbContext.cs`
- Test: `tests/backend/NovaHaven.Domain.Tests/*.cs`
- Test: `tests/backend/NovaHaven.Integration.Tests/*.cs`

**Interfaces:**
- Consumes: existing Domain entity classes and `NovaDbContext.OnModelCreating` mappings.
- Produces: entities in `NovaHaven.Domain.<Feature>.Entities`; EF mappings applied from the Infrastructure assembly; migration filenames/order unchanged.

- [ ] **Step 1: Capture model invariants before moving types**

Run the current domain tests and `dotnet ef migrations has-pending-model-changes --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext`. Record the result before modifying mappings.

- [x] **Step 2: Move one feature's entities and update its namespace references**

Start with Wiki entities and update imports in API, Application, Infrastructure, migration designer/snapshot files and tests. Do not rename entity types or properties.

- [x] **Step 3: Extract the Wiki EF mappings without changing model configuration**

Move each Wiki-specific `Entity<T>` configuration from `NovaDbContext.OnModelCreating` to a focused `Wiki*Configuration : IEntityTypeConfiguration<TEntity>` class; call `ApplyConfigurationsFromAssembly` while retaining Identity setup and any genuinely cross-feature configuration.

- [ ] **Step 4: Run Wiki and Domain regression tests**

Run `dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj` and the Wiki integration tests. Expected: unchanged behavior and no migration generated by the refactor.

- [x] **Step 5: Repeat the move/configuration extraction per remaining Domain feature**

Apply the same sequence to Catalog, News, Knowledge, Community, Integration, Rewards and Commerce. Keep migrations under `Infrastructure/Data/Migrations` and preserve their timestamps and ordering.

- [x] **Step 6: Verify EF model equivalence**

Run `dotnet ef migrations has-pending-model-changes --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext`. Expected: no pending model changes. If the model differs, stop and fix the extraction; do not generate a migration to mask it.

### Task 3: Add the shared BLL result and transaction boundaries

**Files:**
- Create: `backend/NovaHaven.Application/Common/Results/ApplicationError.cs`
- Create: `backend/NovaHaven.Application/Common/Results/ApplicationResult.cs`
- Create: `backend/NovaHaven.Application/Common/Transactions/IUnitOfWork.cs`
- Create: `backend/NovaHaven.Application/Common/Transactions/TransactionIsolation.cs`
- Create: `backend/NovaHaven.Infrastructure/Persistence/UnitOfWork/EfUnitOfWork.cs`
- Create: `backend/NovaHaven.Api/Errors/ApplicationProblemDetailsMapper.cs`
- Modify: `backend/NovaHaven.Infrastructure/NovaHaven.Infrastructure.csproj`
- Create: `tests/backend/NovaHaven.Application.Tests/NovaHaven.Application.Tests.csproj`
- Create: `tests/backend/NovaHaven.Application.Tests/ApplicationResultTests.cs`
- Create: `tests/backend/NovaHaven.Integration.Tests/CommerceCheckoutIntegrationTests.cs`

**Interfaces:**
- Produces: `Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken)` and `Task<T> IUnitOfWork.ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, Func<T, bool> shouldCommit, TransactionIsolation isolation, CancellationToken cancellationToken)`. A typed unsuccessful result rolls back without being converted into an exception.
- `Infrastructure` references `Application` to implement its ports; `Application` continues to reference `Domain` only.

- [x] **Step 1: Create focused tests for BLL results and Infrastructure transaction rollback**

Create the Application unit-test project using the repository's current xUnit/Test SDK versions, reference `NovaHaven.Application`, and add it to `NovaHaven.sln`. Assert BLL success/failure result behavior. Also create the rollback integration test: force an exception after a write inside the unit-of-work callback and assert SQL Server contains no partial state.

- [x] **Step 2: Run the new tests and confirm they fail at the missing contracts**

Run the focused Application result filter and rollback integration-test filter. Expected: build/test failure naming the missing result and unit-of-work contracts, not a baseline test regression.

- [x] **Step 3: Implement the BLL result and unit-of-work contracts**

Use a stable application error code plus a safe message and optional field-validation dictionary. Keep HTTP status codes and `ProblemDetails` types out of Application; add a single API mapper from typed errors to the current RFC 9457 response/status contract.

- [x] **Step 4: Implement `EfUnitOfWork` with the scoped `NovaDbContext`**

Commit only when the operation returns and `shouldCommit(result)` is true; rollback and return typed failures otherwise. Roll back and propagate the original exception on thrown failures. Preserve explicit isolation levels used by Wiki publication and Commerce checkout.

- [ ] **Step 5: Run the new tests and existing transaction integration tests**

Run the result tests and rollback integration test alongside existing Commerce transaction/replay tests. If local PostgreSQL is unavailable, preserve the test and report it as unverified rather than bypassing relational coverage.

### Task 4: Convert public Wiki reads to Controller → BLL → repository

**Files:**
- Create: `backend/NovaHaven.Api/Controllers/WikiController.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiArticleSummaryResponse.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiArticleResponse.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiTagResponse.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiCategoryResponse.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Queries/WikiArticleListQuery.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Results/WikiArticlePageResult.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Services/WikiReadService.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Repositories/IWikiReadRepository.cs`
- Create: `backend/NovaHaven.Infrastructure/Persistence/Repositories/Wiki/EfWikiReadRepository.cs`
- Create: `tests/backend/NovaHaven.Architecture.Tests/NovaHaven.Architecture.Tests.csproj`
- Test: `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`
- Test: `tests/backend/NovaHaven.Architecture.Tests/WikiControllerArchitectureTests.cs`

**Interfaces:**
- Controller base route: `/api/v1/wiki`.
- `WikiReadService.ListArticlesAsync(WikiArticleListQuery, CancellationToken)`, `GetArticleAsync(string slug, CancellationToken)`, `ListTagsAsync(CancellationToken)`, `ListCategoriesAsync(CancellationToken)`.
- `IWikiReadRepository` implements those four read operations and returns typed Application results; it owns all EF query/projection code.

- [x] **Step 1: Add a controller architecture test and route characterization assertions**

Assert `WikiController` is an `[ApiController]`, has the exact four existing public routes, depends on `WikiReadService` and does not accept `NovaDbContext`. Add JSON assertions for existing response property names and types.

Create the architecture test project with the existing xUnit/Test SDK versions (`xunit` 2.9.2, `Microsoft.NET.Test.Sdk` 17.14.1, runner 3.1.1), reference `NovaHaven.Api`, and add it to `NovaHaven.sln`.

- [x] **Step 2: Run the focused tests and confirm the architecture test fails**

Run the `WikiControllerArchitectureTests` filter and existing public Wiki integration tests. Expected: the new architecture test fails because the controller does not exist; current route behavior remains characterized.

- [x] **Step 3: Implement the typed public Wiki DTOs, query and service**

Preserve the exact current JSON fields, pagination defaults/limits, search/category/tag behavior, ordering and published-revision-only rule.

- [x] **Step 4: Implement `IWikiReadRepository` in Infrastructure**

Move the EF reads from `PublicWikiEndpoints` into the repository; do not expose `IQueryable` or `NovaDbContext` to Application.

- [x] **Step 5: Implement `WikiController` and map results to HTTP responses**

Use attribute routes for `/articles`, `/articles/{slug}`, `/tags`, `/categories`; allow anonymous read and return the same status/payload behavior as before.

- [ ] **Step 6: Run architecture and public-read integration tests**

Expected: all public endpoints keep their contract and anonymous callers cannot see drafts or unpublished articles.

### Task 5: Convert Wiki categories and tags to Admin Controllers and BLL services

**Files:**
- Create: `backend/NovaHaven.Api/Controllers/AdminWikiCategoriesController.cs`
- Create: `backend/NovaHaven.Api/Controllers/AdminWikiTagsController.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiCategoryRequest.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiTagRequest.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Services/WikiCategoryService.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Services/WikiTagService.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Repositories/IWikiCategoryRepository.cs`
- Create: `backend/NovaHaven.Application/Features/Wiki/Repositories/IWikiTagRepository.cs`
- Create: corresponding `EfWikiCategoryRepository.cs` and `EfWikiTagRepository.cs` under Infrastructure
- Modify: `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`
- Test: `tests/backend/NovaHaven.Architecture.Tests/AdminWikiControllerArchitectureTests.cs`

**Interfaces:**
- Base route for both controllers: `/api/v1/admin/wiki`; every action requires the existing `AdminOnly` role policy and existing CSRF protection on writes.
- Services expose list/get/create/update/delete-or-deactivate use cases matching the current category/tag endpoints; repository methods own EF queries and writes.

- [x] **Step 1: Add characterization/architecture assertions for category and tag route methods, auth, CSRF and ETags**
- [ ] **Step 2: Run focused tests and confirm the new controller architecture assertions fail**
- [x] **Step 3: Implement category/tag request DTOs and Application services using the existing validators/policies**
- [x] **Step 4: Implement feature repositories and unit-of-work persistence**
- [x] **Step 5: Implement the two thin Admin controllers with exact existing route templates and precondition behavior**
- [ ] **Step 6: Run category/tag integration and Domain policy tests; expect identical 401/403/409/412/428 semantics**

### Task 6: Convert Wiki article editorial lifecycle to Controller and BLL

**Files:**
- Create: `backend/NovaHaven.Api/Controllers/AdminWikiArticlesController.cs`
- Create: `backend/NovaHaven.Api/Contracts/Wiki/WikiDraftRequest.cs`
- Create: typed Admin draft, revision-history and mutation response DTOs under `backend/NovaHaven.Api/Contracts/Wiki/`
- Create: Application commands/queries/results and `WikiArticleService.cs` under `backend/NovaHaven.Application/Features/Wiki/`
- Create: `IWikiArticleRepository.cs` and `EfWikiArticleRepository.cs`
- Modify: `backend/NovaHaven.Api/Infrastructure/SqlServerConflict.cs` only if SQL conflict translation must move to the shared API error mapper
- Test: `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`
- Test: `tests/backend/NovaHaven.Domain.Tests/WikiRulesTests.cs`

**Interfaces:**
- Controller route: `/api/v1/admin/wiki/articles`; service operations mirror existing list, create, get, patch, publish, unpublish, list revisions and restore routes.
- Article mutation results carry the article data and the rowversion ETag value; HTTP header generation remains in the API layer.

- [x] **Step 1: Add/update tests for draft isolation, validation, missing/stale ETag, publish, unpublish, restore and simultaneous edits**
- [x] **Step 2: Run focused Wiki tests and confirm newly added coverage catches an intentionally absent BLL operation/controller**
- [x] **Step 3: Implement Application commands/queries/results and `WikiArticleService` using `WikiDraftValidator` and `IUnitOfWork`**
- [x] **Step 4: Implement repository queries/mutations without moving HTTP concerns into Infrastructure**
- [x] **Step 5: Implement the thin controller and map typed application errors to the existing safe Problem Details/status codes**
- [ ] **Step 6: Run all Wiki lifecycle tests, including the same-ETag race; expect one winner and one stale rejection**

### Task 7: Convert Wiki media to Controller → BLL → storage port

**Files:**
- Create: `backend/NovaHaven.Api/Controllers/MediaController.cs` and typed contracts under `Api/Contracts/Media/`
- Create: Application media service and storage port under `Application/Features/Media/`
- Move: `IWikiMediaStorage` from Infrastructure to the Application port; keep `LocalWikiMediaStorage` in Infrastructure
- Modify: `backend/NovaHaven.Api/Endpoints/MediaEndpoints.cs` (remove only after parity)
- Test: media coverage in `tests/backend/NovaHaven.Integration.Tests/WikiApiIntegrationTests.cs`

**Interfaces:** Preserve upload signature/size checks, Admin authorization and CSRF, public visibility rules, response fields, and local filesystem storage behavior.

- [x] **Step 1: Add/confirm regression tests for size, content signature, authorization, CSRF and unpublished-media privacy**
- [ ] **Step 2: Run the focused media tests against the configured local test database/storage**
- [x] **Step 3: Move storage abstraction to Application and implement the BLL use cases plus Infrastructure adapter**
- [ ] **Step 4: Add the thin media controller, retain route/headers/statuses, then run media tests**

### Task 8: Convert authentication, audit and operations endpoints

**Files:**
- Create: `AuthController.cs`, `AuditController.cs` and `OperationsController.cs` under `backend/NovaHaven.Api/Controllers/`
- Create: typed HTTP contracts under `Api/Contracts/{Auth,Audit,Operations}/`
- Create: application use cases and narrowly scoped ports under `Application/Features/{Auth,Audit,Operations}/`
- Add Infrastructure adapters where needed; keep Identity and EF types out of Application
- Modify/remove: `AuthEndpoints.cs`, `AuditEndpoints.cs`, `OperationsEndpoints.cs` after parity
- Test: auth coverage, `WikiApiIntegrationTests.cs` and `OperationsApiIntegrationTests.cs`

**Interfaces:** Preserve cookie/session settings, CSRF behavior, Admin audit authorization, diagnostic payload and health behavior. Do not return secrets or connection strings.

- [x] **Step 1: Add characterization tests for login/logout/token, anonymous/Admin audit access and diagnostics**
- [x] **Step 2: Implement the Application boundaries and thin controllers without changing cookie or status semantics**
- [x] **Step 3: Run auth, audit and operations integration tests; retain `/health` as a simple platform endpoint if desired**

### Task 9: Convert News API

**Files:** Create `NewsController.cs`, News HTTP contracts, `NewsService`, `INewsRepository`, and `EfNewsRepository`; remove `NewsEndpoints.cs` only after parity; update News integration tests.

**Interfaces:** Preserve public published-only listing/detail, Admin editorial lifecycle, current paths, JSON fields, status codes, authorization, CSRF and ETag rules.

- [x] **Step 1: Add route/JSON/auth/ETag characterization assertions and run current News tests**
- [x] **Step 2: Implement News BLL use cases and feature repository**
- [x] **Step 3: Implement thin controllers and run the full PostgreSQL integration suite**

### Task 10: Convert Catalog items and Recipes API

**Files:** Create `CatalogController.cs`, `CatalogRecipesController.cs`, their HTTP contracts/services/repository ports and Infrastructure adapters; remove `CatalogEndpoints.cs` and `CatalogRecipeEndpoints.cs` only after parity; update `CatalogApiIntegrationTests.cs`.

**Interfaces:** Preserve published-only reads, recipe-to-item relations, current price/definition semantics, Admin lifecycle, routes and concurrency rules. Share a repository only where the current persistence aggregate/query boundary supports it.

- [x] **Step 1: Characterize routes, response JSON, authorization, validation and ETags; run Catalog tests** — architecture assertions cover both route sets, auth/CSRF and controller dependency boundaries; PostgreSQL integration asserts public-only lifecycle, revision snapshots, filters and paging.
- [x] **Step 2: Implement Catalog and Recipe BLL use cases and focused repositories**
- [x] **Step 3: Implement both controllers and run Catalog/Recipe integration** — 2/2 Catalog API integration tests passed; full PostgreSQL suite passed after conversion.

### Task 11: Convert Rewards API

**Files:** Create `RewardsController.cs`, typed contracts, `RewardService`, `IRewardRepository` and its EF adapter; remove `RewardEndpoints.cs` after parity; update `RewardsCommerceApiIntegrationTests.cs`.

**Interfaces:** Preserve Admin authorization/CSRF, reward definition behavior, existing public status/read contract and all JSON/status semantics.

- [x] **Step 1: Characterize reward routes, authorization and response shape; run reward tests** — architecture assertions cover route, auth/CSRF and layer boundaries; integration verifies published revision privacy after a draft edit.
- [x] **Step 2: Implement BLL service, repository and thin controller**
- [x] **Step 3: Run reward integration and related Domain tests** — reward publication/privacy/unpublish integration passed; full PostgreSQL suite passed.

### Task 12: Convert Knowledge API

**Files:** Create `KnowledgeController.cs`, typed contracts, `KnowledgeService`, `IKnowledgeRepository` and its EF adapter; remove `KnowledgeEndpoints.cs` after parity; update `KnowledgeApiIntegrationTests.cs`.

**Interfaces:** Preserve published revision graphs, safe cross-link validation, publication lifecycle, routes, JSON and ETag behavior.

- [x] **Step 1: Characterize public/Admin routes, revision behavior, relation validation and ETags; run current tests** — API route/layer assertions and PostgreSQL Knowledge lifecycle tests cover published snapshots, validation, admin guards and ETags.
- [x] **Step 2: Move use cases to BLL and EF queries/writes to the feature repository**
- [x] **Step 3: Add the thin controller and run Knowledge integration plus Domain validator tests** — focused PostgreSQL Knowledge/architecture tests passed; the full integration suite passed after the EF query translation fix.

### Task 13: Convert Community API

**Files:** Create `CommunityController.cs`, typed contracts, `CommunityService`, `ICommunityRepository` and its EF adapter; remove `CommunityEndpoints.cs` after parity; update `CommunityApiIntegrationTests.cs`.

**Interfaces:** Preserve registrations, leaderboard behavior, route and payload contracts, Admin authorization/CSRF, concurrency and existing coordinate/data meanings.

- [x] **Step 1: Characterize route/payload, registration, leaderboard, authorization and ETag behavior; run current tests**
- [x] **Step 2: Implement Community BLL service/repository and thin controller**
- [x] **Step 3: Run Community integration tests and Domain validators** — focused Community integration and architecture tests passed; full PostgreSQL integration passed.

### Task 14: Convert Integration API

**Files:** Create `IntegrationController.cs`, typed contracts, `IntegrationService`, `IIntegrationRepository` and its EF adapter; remove `IntegrationEndpoints.cs` after parity; update `IntegrationApiIntegrationTests.cs`.

**Interfaces:** Preserve public capability status, Admin mutations, route/payload meanings and authorization/CSRF. Do not invent Minecraft plugin endpoints or database access.

- [x] **Step 1: Characterize public/Admin routes, status payload and authorization; run current tests**
- [x] **Step 2: Implement the Integration BLL service/repository and thin controller**
- [x] **Step 3: Run Integration API integration tests** — focused integration/architecture tests and full PostgreSQL suite passed.

### Task 15: Convert Commerce offers and checkout/order APIs

**Files:**
- Create: `CommerceOffersController.cs` and `CommerceOrdersController.cs`
- Create: request/response DTOs under `backend/NovaHaven.Api/Contracts/Commerce/`
- Create: `CommerceOfferService.cs`, `CommerceCheckoutService.cs`, `ICommerceRepository.cs` and Application checkout/result models
- Create: `EfCommerceRepository.cs` under Infrastructure and reuse `EfUnitOfWork`
- Modify/remove: `CommerceEndpoints.cs` and `CommerceOrderEndpoints.cs`
- Test: `tests/backend/NovaHaven.Integration.Tests/LocalDemoCommerceApiIntegrationTests.cs`, `RewardsCommerceApiIntegrationTests.cs`, and `tests/backend/NovaHaven.Domain.Tests/CommerceCheckoutTests.cs`

**Interfaces:**
- Checkout remains explicitly local-demo only; no gateway, charge, game reward delivery or remote payment provider is introduced.
- Idempotency key, order lines, request fingerprint, transaction isolation, rollback, and Admin order-history authorization remain unchanged.

- [x] **Step 1: Add/retain tests for empty cart rejection, request replay, invalid totals, rollback and Admin order-history access**
- [x] **Step 2: Move checkout orchestration to the BLL service and persistence behind focused commerce repositories/`IUnitOfWork`**
- [x] **Step 3: Add typed controller contracts and preserve current status/error responses**
- [x] **Step 4: Run Commerce unit and PostgreSQL integration tests, including replay and transaction rollback cases** — focused Commerce tests passed 9/9; full PostgreSQL integration passed.

### Task 16: Remove business Minimal API maps and enforce layer boundaries

**Files:**
- Modify: `backend/NovaHaven.Api/Program.cs`
- Remove after parity: `backend/NovaHaven.Api/Endpoints/*.cs`
- Create: `tests/backend/NovaHaven.Architecture.Tests/ApiLayeringTests.cs`
- Modify: `README.md`
- Modify: `docs/roadmap/NOVA-HAVEN-ROADMAP.md`

**Interfaces:**
- `Program.cs` registers controllers, BLL services, repository implementations, unit of work and existing auth/media adapters; `/health` may remain a simple health mapping.
- No controller or Application service directly accepts `NovaDbContext`; Application has no reference to API or Infrastructure.

- [x] **Step 1: Add architecture tests** — API contract/layer boundaries and per-feature controller routing are covered.

Assert every business route is discovered through an MVC controller, controller constructors do not inject `NovaDbContext`, Application has no project reference to Api/Infrastructure, and EF repository/UoW implementations are in Infrastructure.

- [x] **Step 2: Run architecture tests and check for remaining Minimal API feature maps or layer leaks** — only `/health` remains as a Minimal API route.
- [x] **Step 3: Register all controllers and feature dependencies in `Program.cs`; remove obsolete business endpoint maps**
- [x] **Step 4: Run the solution build/domain/PostgreSQL integration tests and `npm test`** — build 0 warnings/errors; Domain 80/80; Integration 78/78; Node 161 passed/5 skipped.
- [x] **Step 5: Validate the maintained OpenAPI path/method inventory against every current controller action while preserving characterized behavior** — 106 controller operations matched 106 contract operations; integration tests verify the API behavior.
- [x] **Step 6: Run EF pending-model-change check and the complete local PostgreSQL integration suite** — no pending changes; 78/78 passed on an isolated cluster.
- [x] **Step 7: Update README/roadmap with folder roles, commands run and blocked verification**

## Delivery Boundary

This plan standardizes the Backend API. It does not reorganize Next.js or Flutter source folders; those are independent subprojects and need their own implementation plans under the approved parent spec. No code is considered complete until the entire plan passes its gates.

## Execution Notes

Use native sequential execution: the contracts and transaction boundaries are shared across many API features, so parallel edits would risk inconsistent responses and conflicting EF changes. The working tree is dirty; preserve all existing changes and do not stage/commit the tree wholesale. Keep the configured local content database untouched; relational integration tests may use only exact GUID-named disposable databases.

**Final execution update (2026-09-30):** Tasks 12–16 are complete. Knowledge, Community, Integrations, Commerce/offers/orders and Notifications now use thin MVC controllers, typed API contracts, Application BLL services and focused Infrastructure repositories. Existing PostgreSQL schema/migration was not changed. All 106 controller route/method pairs match the maintained OpenAPI contract. Latest build/test evidence and the Flutter APK limitation are in `docs/verification/2026-09-30-api-mobile-completion.md`. No commit/deployment was made because the checkout was already dirty and neither was requested.
