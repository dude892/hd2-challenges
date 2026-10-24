---
description: "Use when creating or modifying C# tests, fixtures, or test helpers under tests. Covers test design, catalog-driven expectations, isolation, and class layout."
applyTo: "tests/**/*.cs"
---

# Testing Guidelines

## Test Design

- Each test must verify one behavioral statement only. Split availability, initial state, selection, clearing, persistence, and other independent behaviors into separate named tests; do not combine them into one long scenario. Multiple assertions are appropriate only when they jointly prove that single statement. Use theories to cover the same statement across inputs.

## Catalog-Driven Expectations

- For tests of catalog-driven behavior, resolve saved IDs and operation indexes through `CatalogService` or `RedemptionCatalogService` and derive expectations from the resolved models (for example, `CurrentOperation.MaxStars`).
- Do not duplicate catalog values such as maximum stars, mission counts, loadouts, or display names in theory data or assertions, where they can drift out of sync.
- Keep fixture references and behavioral inputs explicit; use fixed expected catalog values only when testing the catalog contract itself, and do not compute expected behavior by calling the rule being tested.

## Isolation and Parallelism

- Local E2E runs use `tests/hd2-challenges-e2e/xunit.runner.json` (`parallelMode: all`, `maxParallelThreads: 8`, conservative scheduling); CI uses `xunit.runner.ci.json` with parallelism off.
- E2E tests share one app fixture and use Playwright's isolated browser contexts. Keep shared catalog objects read-only and use per-test state for mutations.
- The CI workflow runs E2E tests separately in Chromium, Firefox, and WebKit.

## Standard Test Class Layout

Organize test classes from top to bottom in this order:

1. **Fields & Constants:** Private dependencies such as mocks, loggers, or fixtures.
2. **Constructor:** Per-test setup.
3. **Properties & Methods for Data:** Static test data, including `[MemberData]` sources.
4. **Private Test Helper Methods:** Methods used internally by the test class to support test execution.
5. **Test Methods (`[Fact]` / `[Theory]`):** The actual tests.
6. **Dispose Method:** Per-test cleanup, including `DisposeAsync` when applicable.

Omit sections that are not needed; do not add empty constructors or cleanup methods solely to satisfy this layout.
