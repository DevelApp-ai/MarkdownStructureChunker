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

## Pull request workflow

1. Create a feature branch from `main`.
2. Make changes and include/adjust tests near the implementation.
3. Run the required quality gate locally.
4. Open a pull request and request review.
5. Merge only after required checks and approvals pass.
