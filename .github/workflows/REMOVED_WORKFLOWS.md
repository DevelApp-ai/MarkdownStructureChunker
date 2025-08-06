# Removed Workflow Files

The following workflow files have been removed to eliminate conflicts and consolidate CI/CD operations:

## Removed Files:
- `ci-cd.yml` - Replaced by `dotnet.yml` with GitVersion support
- `build-test.yml` - Functionality merged into `dotnet.yml` and `pr-validation.yml`
- `release.yml` - Replaced by automatic releases in `dotnet.yml`

## Current Active Workflows:
- `dotnet.yml` - Primary CI/CD pipeline (pushes to main)
- `pr-validation.yml` - Pull request validation

## Reason for Removal:
These files were causing conflicts because:
1. Multiple workflows triggering on the same events
2. Duplicate functionality between workflows
3. Permission conflicts between different workflow approaches
4. GitVersion integration conflicts

## Migration:
All functionality has been consolidated into the two remaining workflows with proper separation of concerns.

