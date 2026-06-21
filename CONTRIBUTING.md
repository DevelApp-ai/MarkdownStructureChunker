# Contributing

## Branch protection expectations

The `main` branch should enforce:
- At least one pull request review approval
- Required status checks before merge
- No direct pushes to `main`
- Up-to-date branch before merge

## Required quality gate

Every pull request should pass:
- `dotnet build --configuration Release --no-restore`
- `dotnet format --verify-no-changes`
- `dotnet test --configuration Release --no-build`

## API compatibility and versioning policy

- NuGet package versions follow Semantic Versioning (`MAJOR.MINOR.PATCH`).
- `PATCH`: bug fixes and internal changes with no public API changes.
- `MINOR`: backwards-compatible API additions.
- `MAJOR`: breaking API changes.
- Any public API removal or signature change must be documented in PR notes and release notes.

## Dependency and vulnerability monitoring policy

- Dependabot is configured for NuGet and GitHub Actions updates on a weekly cadence.
- Security updates should be triaged promptly and merged as soon as validation passes.
- Routine dependency updates should be reviewed at least weekly.

## Pull request workflow

1. Create a feature branch from `main`.
2. Make changes and include/adjust tests near the implementation.
3. Run the required quality gate locally.
4. Open a pull request and request review.
5. Merge only after required checks and approvals pass.
