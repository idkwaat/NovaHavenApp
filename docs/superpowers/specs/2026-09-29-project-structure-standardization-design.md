# Nova Haven — Project structure and naming standardization

**Date:** 2026-09-29  
**Status:** Approved in conversation on 2026-09-29; Backend implementation plan drafted and awaiting review: `../plans/2026-09-29-api-controller-bll-dal-standardization.md`
**Scope:** Internal code organization and engineering conventions for Backend, Web and Mobile.

## Goal

Make the existing Nova Haven monorepo understandable and maintainable as a substantial student project: conventional API layers should be explicit, feature boundaries should be visible, files should have one clear responsibility, and names should follow the conventions of their language. A reviewer should be able to trace a feature from its controller, through an application service and repository, to its domain entity and persistence without guessing where code belongs.

The owner specifically asked to use Wynncraft as a reference. Only Wynncraft's public surface is observable: its official documentation describes a versioned public API organized into resource routes, with documented caching and rate limits. The website's private frontend framework, database, and internal folder layout are not established by those public sources. Nova Haven will therefore borrow the verifiable principles (feature-oriented boundaries and an explicit API contract), not claim to reproduce hidden implementation details.

## Current evidence and constraints

- Nova Haven is already a modular monolith: `backend/NovaHaven.Api`, `.Application`, `.Domain`, `.Infrastructure`; Next.js/React in `apps/web`; Flutter in `apps/mobile`; OpenAPI in `contracts/openapi`.
- `apps/web/next.config.ts` rewrites same-origin `/api/*` to the configured backend origin. Public Wiki server code calls the REST API; Admin browser requests use the same-origin route. `backend/NovaHaven.Api/Program.cs` configures EF Core with SQL Server and maps versioned API endpoint groups. The web does not connect directly to SQL Server.
- `apps/mobile/lib/main.dart` currently combines app startup/navigation with feature screens (about 1,643 lines); `apps/mobile/lib/wiki_api.dart` combines DTOs, Markdown TOC parsing and API requests (about 471 lines).
- API endpoint files include large feature modules (for example `KnowledgeEndpoints.cs` and `AdminWikiEndpoints.cs`). The existing four-project backend split should be preserved.
- There are no ASP.NET Core MVC controllers. Business routes are Minimal API endpoint maps and many inject `NovaDbContext` directly, combining HTTP binding, application rules, persistence queries/mutations, response projection and transaction handling.
- Domain entities already exist, grouped by feature under `NovaHaven.Domain`; they are not absent, but there is no explicit `Entities` subfolder convention. Application currently contains validators/policies, not feature application services. No repository abstractions or implementations were found.
- Web routes live under `apps/web/app`; feature clients/models are mixed under `apps/web/src/lib`, while some UI is colocated in route directories.
- There is no `.git` directory in this checkpoint, so the historical branch and commits cannot be used or recovered. A branch/worktree is not currently available.
- The current OpenSpec directory contains feature change packages rather than an `openspec/specs/` baseline. This refactor changes no observable product behavior and does not add a behavior spec or alter the OpenAPI contract.

## Decisions

### 1. Preserve runtime architecture and contracts

Keep Next.js/React, Flutter, ASP.NET Core, EF Core, SQL Server and the existing modular-monolith boundaries. Keep the database local. Do not introduce a framework, state-management library, generic CRUD-repository boilerplate, microservice, message broker, or a second API client-generation system as part of a folder cleanup.

The following remain invariant during the refactor:

- Public and Admin URL paths, REST methods, JSON field names, authorization, CSRF and ETag behavior.
- SQL table names, entity relationships, migration history and runtime database behavior.
- Published-only public content, the mobile reader's API behavior, and existing page compositions/styles.
- `contracts/openapi/wiki-v1.json` as the shared client-facing API contract.

Any discovered need to change an invariant is a separate design/spec decision, not an incidental rename.

### 2. Make the Backend's conventional layers explicit, including BLL

Preserve the four-project dependency direction: `Api` is the Presentation/composition-root layer and references `Application` plus `Infrastructure`; `Application` references `Domain`; `Infrastructure` references `Application` to implement its persistence abstractions and references `Domain`; `Domain` has no project dependencies. **`NovaHaven.Application` is the BLL (Business Logic Layer)**: it owns use-case services, validators/policies, repository contracts and transaction abstractions. Do not create a second parallel BLL project; that would split business logic between two assemblies. **`NovaHaven.Infrastructure` is the DAL implementation** for EF Core, SQL Server repositories, unit of work and migrations. Organize each project by feature (`Wiki`, `Catalog`, `Knowledge`, `Community`, `Commerce`, etc.) and reserve root-level folders for cross-cutting concerns.

- **API Controllers:** Replace feature Minimal API maps with thin ASP.NET Core `[ApiController]` controllers, using explicit route attributes, typed HTTP request/response DTOs, authorization metadata, cancellation tokens and consistent Problem Details. Controllers map HTTP DTOs to Application commands/queries and map Application results to response DTOs; they do not inject `NovaDbContext` or contain business workflows.
- **Application Services:** Add focused use-case services (for example `WikiArticleService`, `WikiCategoryService`, `CommerceCheckoutService`) that accept Application-owned commands/queries and return Application result models. They call existing validators/policies, coordinate domain behavior, transactions and repositories. Avoid a service-per-entity with no behavior; split by use case where a service becomes too broad. Application must not depend on API transport DTOs.
- **Domain Entities:** Keep the existing domain entities and make their location explicit under each feature's `Entities` folder. Entities own domain state/invariants and are not API request/response models.
- **Repositories:** Add feature/aggregate-specific repository interfaces in Application and EF Core implementations in Infrastructure. Repositories own persistence queries and writes; they return domain entities or Application read models. Do not add a generic `IRepository<T>` CRUD wrapper for every DbSet. Do not expose `NovaDbContext` outside Infrastructure. API HTTP DTOs live in `Api/Contracts/<Feature>`; Application commands, queries and result models live in Application and are mapped explicitly at the controller boundary.
- **Transactions:** Preserve current SQL Server transaction boundaries, isolation levels, rowversion/ETag behavior and atomic publication/checkout semantics through an Application-owned unit-of-work/transaction abstraction implemented by Infrastructure. Refactor only with relational integration tests.
- **Persistence:** Keep `NovaDbContext`, migrations and EF configuration in Infrastructure. Split the 558-line context/mapping into feature configurations only if the generated EF model is equivalent and no schema migration is produced.

HTTP routes and response schemas stay unchanged during the conversion, including Admin authorization and CSRF requirements. Existing request/response JSON casing and OpenAPI operations are acceptance criteria.

### 3. Organize Web and Mobile by feature without changing their behavior

Web keeps Next.js App Router route files under `apps/web/app`; `page.tsx`, `layout.tsx`, metadata and URL segments remain the routing source of truth. Move domain clients, models, hooks and reusable feature components into `apps/web/src/features/<feature>/...`; keep transport primitives and genuinely cross-feature UI in `src/shared/...`. Small route-only components may remain colocated with their route. Imports should name their destination explicitly; do not add barrel files solely to shorten paths.

Mobile keeps a thin `main.dart` entrypoint and separates app/bootstrap/navigation, shared core concerns, and feature code under `apps/mobile/lib/app`, `core`, and `features/<feature>`. Feature folders separate API models/data access from presentation screens/widgets where that boundary already exists. Theme and bookmark storage are shared/core concerns. No new state-management framework is required.

### 4. Apply language-native naming rules

- C#: PascalCase for types/methods/properties, descriptive feature namespaces aligned with folders, `*Request`/`*Response` for HTTP payload types, and `*Tests.cs` for tests.
- ASP.NET Core: controller classes end in `Controller`; application use cases end in `Service` only when they coordinate application behavior; repository interfaces/implementations have feature/aggregate names; EF configurations end in `Configuration`.
- TypeScript/React: kebab-case utility/API/model filenames, PascalCase component filenames and exports, `use-*.ts` for hooks, camelCase values, and framework-reserved App Router filenames unchanged.
- Dart/Flutter: snake_case filenames, PascalCase types/widgets, lowerCamelCase members, and descriptive screen names ending in `Screen` where they represent navigable screens; `*_test.dart` for tests.
- Preserve wire-format casing, route naming and persisted names even when internal type/file names are clarified.

Use repository-level formatting/editor configuration and shared .NET build properties where supported; do not impose one language's casing or file layout on another language.

## Target shape (illustrative)

```text
backend/
  NovaHaven.Api/Controllers/<Feature>Controller.cs
  NovaHaven.Api/Contracts/<Feature>/
  NovaHaven.Application/                 # BLL: services, policies, repository contracts
  NovaHaven.Application/Features/<Feature>/{Commands,Queries,Results,Repositories,Services}/
  NovaHaven.Domain/<Feature>/{Entities,ValueObjects}/
  NovaHaven.Infrastructure/Persistence/{Configurations,Repositories}/<Feature>/
  NovaHaven.Infrastructure/Persistence/UnitOfWork/  # DAL implementations
  NovaHaven.Infrastructure/Data/Migrations/   # ordered EF history stays intact
apps/
  web/app/                                     # URL route entrypoints stay stable
  web/src/features/<feature>/{api,model,hooks,components}/
  web/src/shared/{api,ui,lib}/
  mobile/lib/app/
  mobile/lib/core/{network,storage,theme}/
  mobile/lib/features/<feature>/{data,domain,presentation}/
contracts/openapi/                             # single API contract
tests/
```

This is a placement guide, not a requirement to create empty directories or move every file. Files remain together when splitting them would add ceremony without clarifying responsibility.

## Delivery sequence

1. Record baseline gates and add the minimal repository-wide conventions/solution metadata justified by the current projects.
2. Convert one representative Wiki backend slice end-to-end: Controller → Application service → repository abstraction/EF implementation → existing Domain entities. Prove unchanged routes and API schemas before migrating other features.
3. Migrate the remaining API features in small groups, with authorization/CSRF/ETag/transaction regression tests; keep migrations and local database schema unchanged.
4. Refactor the Web and Mobile Wiki slices into feature-oriented folders, then apply the same proven naming/boundary rules to remaining features.
5. Update README architecture/tree and local developer commands to match the actual result.

The implementation plan will name exact files and command gates after this design is approved. Do not perform bulk renames across the whole tree in one unverified operation.

## Verification and acceptance

- Backend: restore/build and run all Domain and Integration tests available in the environment; check EF model against the existing snapshot (`has-pending-model-changes`) and create no migration for a pure organization change.
- API architecture: business route methods are controllers; controllers contain no EF access; `NovaHaven.Application` is the explicit BLL and owns use-case services/contracts/repository abstractions; `NovaHaven.Infrastructure` is the DAL and implements persistence/unit-of-work; Domain entity behavior remains covered by unit tests.
- Web: `npm test`, TypeScript typecheck and production build; exercise key public and Admin routes against the local API when that runtime is available.
- Mobile: Flutter tests and analyzer; if Flutter is unavailable, record that limitation and do not claim mobile verification.
- Contract: OpenAPI paths and schemas remain unchanged; existing contract guards pass.
- Database: use only the configured local database; no schema/data mutation for a pure refactor. Do not claim live SQL integration unless it is actually run.
- Source review: no stale imports, duplicate helpers, unexplained abbreviations, giant generic `Utils`/`Services` buckets, secrets, or generated migration churn.

## Out of scope

Visual redesign, new features, endpoint or database changes, production deployment, Minecraft plugin/database integration, payment processing, and reconstructing missing Git history.

## Review notes

The current workspace has no `.git`, so this proposal cannot be committed and feature-branch/worktree isolation cannot be created from the supplied checkpoint. Implementation must preserve a clean audit trail through small, verifiable file changes and must not fabricate Git history. Wynncraft's public API documentation is evidence for its public API contract only, not for its private website implementation.
