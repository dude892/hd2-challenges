# HD2 Challenges Repository Instructions

## Project Context

- This is a Blazor WebAssembly application targeting .NET 10 for Helldivers 2 challenge tracking.
- The website has not been deployed yet. Optimize for the current local application and the current data shape.
- Do not spend implementation effort on preserving previous save states, migrating old data, or supporting legacy export formats yet. Add migration/version compatibility only when deployment or an explicit requirement makes it necessary.
- Keep changes focused and consistent with the existing C# and Razor patterns. Avoid introducing a server, database, API, or new persistence layer unless explicitly requested.

## Application Structure

- `Program.cs` configures the WebAssembly host, the root components, an `HttpClient` rooted at the app base URL, and scoped application services.
- `Pages/` contains routable Razor pages. `Components/` contains reusable UI components. `Layout/` contains the application shell and navigation.
- `wwwroot/data/` contains the static JSON catalog and challenge definitions. `wwwroot/images/` contains item and warbond assets. `wwwroot/js/app.js` contains the JavaScript interop functions used by the app.
- `Models/` contains both JSON-facing definitions and runtime domain classes. `Services/` contains catalog loading, browser persistence, export, and Penitent Crusade rules.

## Service Architecture

Services are registered as scoped services in `Program.cs` and should remain small, feature-oriented, and testable.

- `CatalogService` is the catalog boundary. It lazily loads the static JSON files through `HttpClient`, post-processes `GameItem` instances, builds `AllItemsSet`, resolves starter loadouts, and converts difficulty definitions into runtime `PenitentDifficulty` objects. Use it for item, warbond, operation, difficulty, and loadout lookup rather than loading catalog JSON from components.
- `PenitentMissionService` owns mission progression rules: advancing missions and operations, applying failures, setting progress, and detecting the final mission. It mutates a supplied `PenitentState`.
- `PenitentRewardService` owns reward and punishment rules: calculating pools, rolling rewards, creating pending choices, claiming rewards, banning rewards, and removing punishments. It mutates a supplied `PenitentState`.
- `BrowserStorageService` is the local-storage adapter. It serializes values as JSON and calls `hd2App.getLocalStorage`, `setLocalStorage`, and `removeLocalStorage` through `IJSRuntime`.
- `FileExportService` is the download adapter. It uses `hd2App.downloadFile` through `IJSRuntime` and provides JSON export helpers.

Keep UI event handlers focused on orchestration, state display, and persistence. Put catalog resolution in `CatalogService`, game rules in the relevant Penitent service, and browser/JavaScript concerns in the storage or export adapter.

## Data, Definition, and Runtime Class Architecture

### Static data and definitions

- Static JSON under `wwwroot/data/` is the source catalog for items, warbonds, operations, Penitent difficulties, and the starter loadout.
- `*Definition` classes are JSON-friendly DTOs. They store stable IDs, display values, numeric settings, and references as strings or lists of internal names. Examples include `OperationDefinition`, `PenitentDifficultyDefinition`, `PenitentStarterLoadoutDefinition`, and `PenitentStateDefinition`.
- Item identity is `GameItem.Id`, and item collections should preserve the existing case-insensitive equality behavior. Use IDs for serialized references, lookups, and save data rather than display names.

### Runtime domain objects

- `GameItem` is the runtime item model. `CatalogService` assigns its `ItemKind` and resolves its optional `Warbond` after deserialization; these derived properties are not the catalog JSON contract.
- `ItemSet` is the categorized runtime collection for stratagems, primaries, secondaries, throwables, armor passives, and boosters. Use its set operations and category collections instead of duplicating item-set logic in components.
- `Operation` is the runtime form of `OperationDefinition` and owns clamped mission-number behavior and mission labels.
- `PenitentDifficulty` is the runtime form of `PenitentDifficultyDefinition`; it resolves its start operation and loadout overrides/additions to `ItemSet` instances.
- `PenitentState` is the live Penitent Crusade aggregate. It holds resolved runtime objects for difficulty, operation, starter items, acquired items, banned items, pending rewards, pending punishments, and selected warbonds. Its `ToDefinition()` method is the serialization boundary back to stable IDs.

### Save and persistence boundary

- Browser saves use `SaveLibrary<TState>`, `SaveSlot<TState>`, and `SaveExport<TState>` around `PenitentStateDefinition`, not around the live `PenitentState` object graph.
- `PenitentCrusade.razor` loads the catalog first, resolves a `PenitentStateDefinition` into a `PenitentState`, and persists definitions back to browser local storage after state changes.
- Imported and exported save files use the same definition-based representation. Keep runtime-only properties out of save DTOs and resolve IDs through `CatalogService` when loading.
- The current local-storage key and save format are allowed to evolve without backward migration support until the site is deployed. Do not add compatibility shims or migration code speculatively.

## Change Guidelines

- Prefer existing services, models, JSON files, and JS interop APIs over new abstractions.
- When adding catalog data, update the corresponding definition/model and the relevant JSON file together.
- When adding persisted state, update both the definition representation and the runtime construction/`ToDefinition()` path.
- Keep random reward selection and mission progression in the domain services, not in Razor markup.
- Validate changes with `dotnet build`; for behavior changes, add or update focused tests when a test project is introduced.
