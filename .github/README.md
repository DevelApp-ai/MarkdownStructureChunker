# GitHub Actions Workflows

This directory contains the CI/CD workflows for the MarkdownStructureChunker project.

## Active Workflows

### 1. `.NET CI/CD` (`dotnet.yml`)
**Primary CI/CD Pipeline** - Handles all build, test, and deployment operations.

**Triggers:**
- Push to `main` branch
- Pull requests to `main` branch

**Features:**
- GitVersion automatic semantic versioning
- SourceLink integration for debugging transparency
- Automatic package publishing to NuGet.org (main branch) and GitHub Packages (PRs)
- Automatic GitHub release creation for stable versions
- Comprehensive testing and validation

**Permissions:**
- `contents: write` - For creating releases and tags
- `packages: write` - For publishing to GitHub Packages
- `pull-requests: write` - For PR interactions
- `issues: write` - For issue comments
- `actions: read` - For workflow access

### 2. `Validate Pull Request` (`pr-validation.yml`)
**PR Validation** - Provides detailed validation feedback on pull requests.

**Triggers:**
- Pull request events (opened, synchronize, reopened) to `main` branch

**Features:**
- GitVersion-based version calculation for PRs
- Build and test validation
- Package creation verification
- Automated PR comments with validation results

**Permissions:**
- `contents: read` - For repository access
- `issues: write` - For commenting on PRs
- `pull-requests: write` - For PR interactions

## Disabled Workflows

The following workflows have been disabled to prevent conflicts:

### `build-test.yml.disabled`
- **Reason**: Replaced by the comprehensive `dotnet.yml` workflow
- **Previous Function**: Basic build and test validation
- **Migration**: All functionality moved to `dotnet.yml`

### `release.yml.disabled`
- **Reason**: Replaced by automatic GitVersion-based releases in `dotnet.yml`
- **Previous Function**: Manual release creation with version input
- **Migration**: Automatic releases now handled by GitVersion

### `ci-cd-legacy.yml.disabled`
- **Reason**: Replaced by modern GitVersion-based `dotnet.yml` workflow
- **Previous Function**: Legacy CI/CD with manual versioning
- **Migration**: Enhanced with GitVersion and SourceLink support

### `pr-validation-old.yml.disabled`
- **Reason**: Updated to support GitVersion and avoid conflicts
- **Previous Function**: Basic PR validation without versioning
- **Migration**: Enhanced with GitVersion integration

## Workflow Strategy

### Version Management
- **Automatic**: GitVersion calculates versions from Git history
- **Branch-based**: Different versioning strategies per branch type
- **Semantic**: Follows semantic versioning (SemVer) conventions

### Publishing Strategy
- **Pull Requests**: Pre-release packages to GitHub Packages
- **Main Branch**: Stable packages to NuGet.org + GitHub Packages
- **Releases**: Automatic GitHub releases for stable versions

### Quality Assurance
- **Build Validation**: All code must compile successfully
- **Test Coverage**: All tests must pass (66 test cases)
- **Package Verification**: Packages must be created successfully
- **SourceLink**: Source code transparency enabled

## Configuration Files

### `GitVersion.yml`
Controls automatic version calculation based on Git history and branch patterns.

### Project Files
- **SourceLink**: Enabled for debugging transparency
- **Symbol Packages**: Generated for all releases (.snupkg)
- **Package Metadata**: Comprehensive NuGet package information

