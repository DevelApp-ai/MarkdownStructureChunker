# GitHub Actions Workflows

This directory contains the CI/CD workflows for the MarkdownStructureChunker project.

## Workflows

### 1. CI/CD Pipeline (`ci-cd.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main`
- Published releases

**Jobs:**
- **test**: Builds the solution and runs all tests
- **build-package**: Creates NuGet packages (.nupkg) and symbol packages (.snupkg)
- **publish-nuget**: Publishes both main and symbol packages to NuGet.org (on releases only)
- **publish-github-packages**: Publishes to GitHub Packages (on main branch)

### 2. Release Workflow (`release.yml`)

**Trigger:** Manual workflow dispatch

**Purpose:** Creates official releases with version management

**Steps:**
1. Updates version numbers in project files
2. Commits version changes
3. Builds and tests the solution
4. Creates NuGet packages
5. Creates GitHub release with release notes
6. Uploads packages as release assets

**Usage:**
1. Go to Actions tab in GitHub
2. Select "Release" workflow
3. Click "Run workflow"
4. Enter version number (e.g., "1.0.0")
5. Choose if it's a pre-release

### 3. PR Validation (`pr-validation.yml`)

**Triggers:**
- Pull request opened, synchronized, or reopened

**Purpose:** Validates pull requests before merge

**Checks:**
- Code formatting
- Build success
- Test execution with coverage
- Package build validation
- Automated PR comments with results

## Setup Requirements

### Secrets

The following secrets need to be configured in the GitHub repository:

1. **`NUGET_API_KEY`**: API key for publishing to NuGet.org
   - Get from: https://www.nuget.org/account/apikeys
   - Scope: Push new packages and package versions

### Repository Settings

1. **Branch Protection**: Enable branch protection for `main` branch
   - Require status checks to pass
   - Require pull request reviews
   - Include administrators

2. **GitHub Packages**: Enable GitHub Packages for the repository
   - Automatically configured for publishing

## Release Process

### For v1.0.0 Release:

1. **Prepare Release:**
   ```bash
   # Ensure all changes are merged to main
   git checkout main
   git pull origin main
   ```

2. **Create Release:**
   - Go to GitHub Actions
   - Run "Release" workflow
   - Enter version: `1.0.0`
   - Set prerelease: `false`

3. **Verify Release:**
   - Check GitHub release is created
   - Verify NuGet package is published
   - Test package installation

### For Future Releases:

1. Update version in workflow dispatch
2. Follow semantic versioning (MAJOR.MINOR.PATCH)
3. Update release notes as needed

## Package Publishing

### NuGet.org
- **Automatic**: On GitHub releases
- **Packages**: Both main (.nupkg) and symbol (.snupkg) packages
- **Symbol Server**: Symbols automatically published to NuGet.org symbol server
- **Debugging**: Enables source-level debugging for consumers
- **Manual**: Use `dotnet nuget push` with API key for both package types

### GitHub Packages
- **Automatic**: On pushes to main branch
- **Access**: Requires GitHub authentication
- **Packages**: Main packages only (symbols not supported by GitHub Packages)

## Monitoring

- **Build Status**: Check Actions tab for workflow status
- **Package Downloads**: Monitor on NuGet.org
- **Issues**: GitHub Issues for bug reports and feature requests

## Troubleshooting

### Common Issues:

1. **NuGet Push Fails:**
   - Check API key is valid and has push permissions
   - Verify package version doesn't already exist

2. **Tests Fail:**
   - Check test output in Actions logs
   - Verify all dependencies are restored

3. **Package Build Fails:**
   - Check project file syntax
   - Verify all referenced files exist

### Getting Help:

- Check workflow logs in GitHub Actions
- Review this documentation
- Open an issue for workflow problems

