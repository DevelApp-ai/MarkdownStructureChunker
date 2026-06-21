# Release and Changelog Process

## Versioning model

This project follows Semantic Versioning (`MAJOR.MINOR.PATCH`):

- `PATCH`: bug fixes and internal changes with no public API breaks
- `MINOR`: backwards-compatible API additions
- `MAJOR`: breaking API changes

## Release flow

1. Ensure all required checks pass:
   - `dotnet build --configuration Release --no-restore`
   - `dotnet format --verify-no-changes`
   - `dotnet test --configuration Release --no-build`
2. Confirm any public API changes are documented in PR notes.
3. Merge the release-ready PR to `main`.
4. CI packs and publishes NuGet artifacts and creates a GitHub release for stable versions.

## Changelog guidance

- Keep release notes focused on user-visible changes.
- Group entries by:
  - Added
  - Changed
  - Fixed
  - Removed
- For breaking changes, include:
  - What changed
  - Why it changed
  - Migration guidance

## Hotfix process

1. Create a patch branch from `main`.
2. Apply minimal fix and add or update tests.
3. Run required quality gate.
4. Merge after review and release as a patch version.
