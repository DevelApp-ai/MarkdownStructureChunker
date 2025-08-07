# MarkdownStructureChunker v1.0.0 Release Process

## Current Status
- ✅ **Code Complete**: All features implemented and tested
- ✅ **CI/CD Ready**: GitVersion and SourceLink configured
- ✅ **Quality Assured**: 66 tests passing, zero warnings
- 🔄 **Pending**: v1.0.0 release creation

## How to Create v1.0.0 Release

### Step 1: Merge Feature Branch to Main
```bash
# 1. Create and merge pull request
# Go to GitHub and create PR: feature/implement-markdown-structure-chunker → main

# 2. After PR approval and merge, the following happens automatically:
# - GitVersion calculates version as 1.0.0 (first release)
# - dotnet.yml workflow triggers on main branch
# - Packages published to NuGet.org
# - GitHub release created with tag v1.0.0
```

### Step 2: Verify Release Artifacts
After merge, check that these were created automatically:

**NuGet.org Packages:**
- `MarkdownStructureChunker.1.0.0.nupkg`
- `MarkdownStructureChunker.1.0.0.snupkg` (symbols)

**GitHub Release:**
- Tag: `v1.0.0`
- Release: `Release v1.0.0`
- Attached artifacts: `.nupkg` and `.snupkg` files

**GitHub Packages (Backup):**
- Same packages also published to GitHub Packages

## GitVersion Behavior

### Version Calculation:
- **Current Branch**: `feature/implement-markdown-structure-chunker`
- **Current Version**: `1.0.0-feature-implement-markdown-structure-chunker.X`
- **After Merge to Main**: `1.0.0` (stable release)

### Future Versions:
- **Patch Release**: `1.0.1` (bug fixes)
- **Minor Release**: `1.1.0` (new features)
- **Major Release**: `2.0.0` (breaking changes)

## Release Workflow Details

### Automatic Process (dotnet.yml):
1. **GitVersion**: Calculates semantic version from Git history
2. **Build & Test**: Ensures quality before release
3. **Package Creation**: Creates .nupkg and .snupkg files
4. **NuGet Publishing**: Publishes to NuGet.org (public)
5. **GitHub Packages**: Publishes to GitHub Packages (backup)
6. **GitHub Release**: Creates release with tag and artifacts
7. **SourceLink**: Enables source code debugging transparency

### Manual Override (if needed):
If automatic release fails, you can manually create a release:

```bash
# 1. Tag the commit manually
git tag v1.0.0
git push origin v1.0.0

# 2. Create GitHub release manually
# Go to GitHub → Releases → Create new release
# Tag: v1.0.0
# Title: Release v1.0.0
# Upload: .nupkg and .snupkg files
```

## Post-Release Actions

### 1. Verify NuGet Package
```bash
# Check package is available
dotnet add package MarkdownStructureChunker --version 1.0.0
```

### 2. Update Documentation
- Update README.md with installation instructions
- Update API documentation if needed
- Create release notes highlighting features

### 3. Announce Release
- GitHub Discussions/Issues
- NuGet package description
- Social media/blog posts

## Troubleshooting

### If GitVersion Shows Wrong Version:
```bash
# Check GitVersion calculation locally
dotnet tool install --global GitVersion.Tool
gitversion
```

### If NuGet Publishing Fails:
- Check `NUGET_API_KEY` secret is set in GitHub repository
- Verify API key has push permissions
- Check package name doesn't conflict

### If GitHub Release Fails:
- Check workflow has `contents: write` permission
- Verify `GITHUB_TOKEN` has release permissions
- Check tag doesn't already exist

## Next Release Process

For future releases after v1.0.0:

1. **Feature Development**: Create feature branches
2. **Version Control**: GitVersion handles versioning automatically
3. **Release**: Merge to main triggers automatic release
4. **Semantic Versioning**: 
   - Patch: Bug fixes (1.0.1)
   - Minor: New features (1.1.0) 
   - Major: Breaking changes (2.0.0)

## Summary

**To release v1.0.0 right now:**
1. ✅ Merge the current PR to main
2. ✅ Wait for automatic workflow completion (~5 minutes)
3. ✅ Verify release appears on GitHub and NuGet.org
4. ✅ Test installation: `dotnet add package MarkdownStructureChunker`

The entire release process is automated - just merge to main! 🚀

