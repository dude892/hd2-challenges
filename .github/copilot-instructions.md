# HD2 Challenges Repository Instructions

## Project Context

- This is a Blazor WebAssembly application targeting .NET 10 for Helldivers 2 challenge tracking.
- The website has now been deployed and is under active use, take care to maintain backward compatibility and avoid breaking changes.
- Keep changes focused and consistent with the existing C# and Razor patterns. Avoid introducing a server, database, API, or new persistence layer unless explicitly requested.
- While the main focus of the app right now is on the "Warpath: Redemption" challenge tracking, the architecture should remain flexible enough to accommodate future expansions or additional challenge types.
- Do not take the following instructions as exhaustive; they are meant to guide the development process and maintain consistency within the project, and should be adapted as needed for specific scenarios.

## Application Structure

- The repository-root `hd2-challenges.slnx` includes the Blazor app in `challenges/` and the Windows/WPF XAML converter in `xaml2Svg/`. Build both with `dotnet build` from the repository root.
- Application paths below are relative to `challenges/`. Run the app with `dotnet run --project .\challenges\hd2-challenges.csproj`, or use the VS Code "Warpath: Redemption" debug configuration.
- The app build runs `xaml2Svg/hd2Xaml2Svg.csproj` to generate SVGs and galleries from `challenges/imports/xaml/` into `$(OutputPath)generated/wwwroot/svgs/` under the app's build directory. Linked content registers these as `/svgs/` static assets and copies them into build and publish `wwwroot/svgs/`. The separate generated folder keeps converter outputs distinct from hand-authored assets such as `challenges/wwwroot/svgs/icon/shield-plus.svg`. This build step requires Windows and the .NET 10 SDK; XAML dictionaries produce multiple SVGs in category subfolders, so conversion runs on each normal build, not design-time builds.
- `Program.cs` configures the WebAssembly host, the root components, an `HttpClient` rooted at the app base URL, and scoped application services.
- `Pages/` contains routable Razor pages. `Components/` contains reusable UI components. `Layout/` contains the application shell and navigation.
- `wwwroot/data/` contains the static JSON catalog and challenge definitions. `wwwroot/images/` contains item and warbond assets. `wwwroot/js/app.js` contains the JavaScript interop functions used by the app.
- `Models/` contains both JSON-facing definitions and runtime domain classes. `Services/` contains catalog loading, browser persistence, export, and Warpath: Redemption rules.
- `imports/` contains files being imported from an external source not to be used directly in the app, do not modify them at all for any reason, as they will be overwritten or managed externally.

## Service Architecture

Services are registered as scoped services in `Program.cs` and should remain small, feature-oriented, and testable.

- `CatalogService` is the catalog boundary. It lazily loads the static JSON files through `HttpClient`, post-processes `GameItem` instances, and builds `AllItemsSet`. Use it for item, warbond, and operation lookup rather than loading catalog JSON from components.
- `RedemptionCatalogService` loads `redemption-difficulties.json` and `redemption-starter-loadout.json`, resolves loadouts and runtime `RedemptionDifficulty` objects, and constructs `RedemptionState` instances.
- `RedemptionSessionService` owns the active runtime state, run lifecycle, mission progression, rewards, punishments, and persistence coordination. Expose user-level workflow actions; keep reward selection and mission advancement helpers private and test their behavior through those actions.
- `BrowserStorageService` is the local-storage adapter. It serializes values as JSON and calls `hd2App.getLocalStorage`, `setLocalStorage`, and `removeLocalStorage` through `IJSRuntime`.
- `FileExportService` is the download adapter. It uses `hd2App.downloadFile` through `IJSRuntime` and provides JSON export helpers.

Keep UI event handlers focused on orchestration, state display, and persistence. Put catalog resolution in the relevant catalog service, game rules in the relevant Redemption service, and browser/JavaScript concerns in the storage or export adapter.

Keep service boundaries narrow: expose only the operations and data consumers need, and keep mutable state owned by the service. An interface such as `IReadOnlyList<T>` limits operations available through that interface, but does not make a mutable object immutable; when exposing collections, return a read-only wrapper or snapshot as appropriate, and use immutable elements or controlled update methods when callers must not mutate item state. Route state changes through the owning service so related notifications and invariants cannot be bypassed.

## Data, Definition, and Runtime Class Architecture

### Static data and definitions

- Static JSON under `wwwroot/data/` is the source catalog for items, warbonds, operations, Redemption difficulties, and the starter loadout.
- `*Definition` classes are JSON-friendly DTOs. They store stable IDs, display values, numeric settings, and references as strings or lists of internal names. Examples include `OperationDefinition`, `RedemptionDifficultyDefinition`, `RedemptionStarterLoadoutDefinition`, and `RedemptionStateDefinition`.
- Item identity is `GameItem.Id`, and item collections should preserve the existing case-insensitive equality behavior. Use IDs for serialized references, lookups, and save data rather than display names.

### Runtime domain objects

- `GameItem` is the runtime item model. `CatalogService` assigns its `ItemKind` and resolves its optional `Warbond` after deserialization; these derived properties are not the catalog JSON contract.
- `ItemSet` is the categorized runtime collection for stratagems, primaries, secondaries, throwables, armor passives, and boosters. Use its set operations and category collections instead of duplicating item-set logic in components.
- `Operation` is the runtime form of `OperationDefinition` and owns clamped mission-number behavior and mission labels.
- `RedemptionDifficulty` is the runtime form of `RedemptionDifficultyDefinition`; it resolves its start operation and loadout overrides/additions to `ItemSet` instances.
- `RedemptionState` is the live Warpath: Redemption aggregate. It holds resolved runtime objects for difficulty, operation, starter items, acquired items, banned items, pending rewards, pending punishments, and selected warbonds. Its `ToDefinition()` method is the serialization boundary back to stable IDs.

### Save and persistence boundary

- Browser saves use `SaveLibrary<TState>`, `SaveSlot<TState>`, and `SaveExport<TState>` around `RedemptionStateDefinition`, not around the live `RedemptionState` object graph.
- `WarpathRedemption.razor` loads the catalog first, resolves a `RedemptionStateDefinition` into a `RedemptionState`, and persists definitions back to browser local storage after state changes through `RedemptionSaveLibraryService`.
- `RedemptionSaveLibraryService` uses local-storage key `blazor-warpath-redemption` and export mode `warpath-redemption`.
- Imported and exported save files use the same definition-based representation. Keep runtime-only properties out of save DTOs and resolve IDs through `CatalogService` when loading.
- The site is deployed and browser saves and exported files are user data. Treat changes to the local-storage key or save format as compatibility-sensitive: preserve existing data or provide an explicit migration/import path when changing the schema. Do not change the key or format casually.

## Change Guidelines

- Prefer existing services, models, JSON files, and JS interop APIs over new abstractions.
- When adding catalog data, update the corresponding definition/model and the relevant JSON file together.
- When adding persisted state, update both the definition representation and the runtime construction/`ToDefinition()` path.
- Keep random reward selection and mission progression in the domain services, not in Razor markup.
- Validate code changes with the focused tests for the affected behavior, and build the solution when changes affect project integration, generated assets, or deployment. Add or update tests in the existing test projects; do not create another test project unless the scope requires it.
- Avoid unexplained numeric literals. Use named constants for values that have domain meaning or are shared; keep obvious one-off values local rather than extracting constants mechanically.

## Tools Available

### Querying Microsoft Documentation

You have access to MCP tools called `microsoft_docs_search`, `microsoft_docs_fetch`, and `microsoft_code_sample_search` - these tools allow you to search through and fetch Microsoft's latest official documentation and code samples, and that information might be more detailed or newer than what's in your training data set.

When handling questions around how to work with native Microsoft technologies, such as C#, F#, ASP.NET Core, Microsoft.Extensions, NuGet, Entity Framework, the `dotnet` runtime - please use these tools for research purposes when dealing with specific / narrowly defined questions that may occur.

### Glider MCP Usage

Use Glider for compiler-backed C#/.NET navigation, diagnostics, dependencies, impact analysis, edits, and refactors. Start navigation with find_code and preview supported changes. Before semantic work, call server_status and confirm the loaded solution or project belongs to the current repository or worktree. Changing directories does not retarget Glider. Call load with the intended .sln, .slnx, or .csproj if the loaded workspace differs.

### Other Tools
PowerShell - for running scripts and automating tasks within the development environment.
Python is not installed and should not be used in scripts for processing.
This is a Windows system and does not have access to any unix tools or environments.

## Design Concerns

Do not just put something in a service because it seems convenient; consider the proper separation of concerns and whether it truly belongs in the domain layer.
Prefer keeping UI logic in Razor components and domain logic in services and models.
When in doubt, ask whether a piece of logic affects the state of the application or the presentation; if it affects state, it likely belongs in a service or model, if it affects presentation, it likely belongs in a Razor component.
Consider also the testability and maintainability of the code when deciding where it should reside.
If something is not relying on the capabilities of a service, it is likely better suited to a model. Any logic that primarily manipulates or represents data without side effects should generally reside in a model rather than a service.
Do not put application state management logic directly in Razor components; delegate it to services instead.
Do not put random wrappers around service calls in Razor components; call the service methods directly instead.
Use razor.css files to define component-specific styles instead of putting them in global CSS files.
DO NOT JUST RANDOMLY CHANGE THE FORMATTING OF A DOCUMENT FOR STUFF THAT IS NOT BEING MODIFIED OR UPDATED.

## Testing
- When creating or modifying C# tests, fixtures, or test helpers, follow [Testing Guidelines](instructions/testing.instructions.md) for test design, catalog-driven expectations, isolation, and class layout.
- The solution contains three test projects, all using xUnit v3 4.x with Microsoft Testing Platform (MTP) v2 on .NET 10. The repository-root `global.json` selects MTP. `tests/hd2-challenges` contains model, service, persistence/adapter, and bUnit component tests; `tests/hd2Xaml2Svg` covers XAML-to-SVG conversion; `tests/hd2-challenges-e2e` uses Playwright to exercise the running Blazor app, including catalog and Redemption flows, save/import/export, and reload persistence.
- Run the full suite with `dotnet test --solution .\hd2-challenges.slnx`. For a focused project run, use `dotnet test --project .\tests\hd2-challenges\hd2-challenges.tests.csproj`, `dotnet test --project .\tests\hd2Xaml2Svg\hd2Xaml2Svg.tests.csproj`, or `dotnet test --project .\tests\hd2-challenges-e2e\hd2-challenges.e2e.tests.csproj`.
- Use MTP options directly, without a `--` separator: `--report-trx` for TRX results and `--filter-class`, `--filter-method`, or `--filter-trait` to select tests. VSTest's `--logger trx` and `--filter` syntax do not apply.
- The app build runs the WPF converter, so builds and tests require Windows and the .NET 10 SDK. Before running E2E tests with `--no-build`, build the solution in the matching configuration. Install a browser with `pwsh .\tests\hd2-challenges-e2e\bin\Debug\net10.0\playwright.ps1 install chromium` (substitute `firefox` or `webkit` as needed). `BROWSER` selects the browser; Chromium is the default. The E2E fixture starts the already-built app itself.
- Add or update focused tests for behavior changes and cover affected user-visible flows when practical. Run the relevant test project after edits; use the full suite when changing shared behavior or project integration. Do not rely on a build alone for behavioral changes.