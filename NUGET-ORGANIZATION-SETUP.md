# NuGet Organization Setup for DevelApp

## Current Issue
The package `MarkdownStructureChunker` is not appearing under the DevelApp organization on NuGet.org.

## Required Setup Steps

### 1. NuGet.org Organization Configuration
The package owner needs to:

1. **Create/Verify Organization**: 
   - Go to https://www.nuget.org/account/organizations
   - Ensure `DevelApp` organization exists
   - Add necessary members with appropriate permissions

2. **Package Ownership Transfer**:
   - After first publish, go to package management
   - Transfer ownership to `DevelApp` organization
   - Or add organization as co-owner

### 2. GitHub Repository Secrets
Ensure these secrets are configured in the GitHub repository:

- **`NUGET_API_KEY`**: API key with push permissions for DevelApp organization
  - Generate at: https://www.nuget.org/account/apikeys
  - Scope: Push new packages and package versions
  - Glob pattern: `MarkdownStructureChunker*`

### 3. Package Metadata (Already Configured)
```xml
<PackageId>MarkdownStructureChunker</PackageId>
<Authors>DevelApp</Authors>
<Company>DevelApp</Company>
<Owners>DevelApp</Owners>
```

### 4. Publishing Process
When the PR is merged to main:

1. **Automatic Publishing**: `dotnet.yml` workflow will trigger
2. **Package Creation**: GitVersion will set version to `1.0.0`
3. **NuGet Upload**: Package pushed to NuGet.org with configured API key
4. **Organization Assignment**: Package should appear under DevelApp if API key has org permissions

## Troubleshooting

### If Package Doesn't Appear Under Organization:
1. **Check API Key Permissions**: Ensure the API key belongs to a user who is a member of DevelApp organization
2. **Manual Transfer**: After first publish, manually transfer package ownership to organization
3. **Re-publish**: Delete and re-publish with correct organization API key
### If Publishing Fails:
1. **API Key Issues**: Check if `NUGET_API_KEY` secret is set correctly
2. **Permission Issues**: Verify API key has push permissions
3. **Package Name Conflict**: Check if package name is already taken

## Expected Result
After successful setup and merge:
- Package: `MarkdownStructureChunker` version `1.0.0`
- Owner: `DevelApp` organization
- URL: https://www.nuget.org/packages/MarkdownStructureChunker/
- Installation: `dotnet add package MarkdownStructureChunker`

## Manual Steps Required
1. ✅ **Code Ready**: All code and workflows are configured
2. ⏳ **Organization Setup**: Verify DevelApp organization on NuGet.org
3. ⏳ **API Key**: Generate organization API key and add to GitHub secrets
4. ⏳ **Merge PR**: Trigger automatic publishing
5. ⏳ **Verify**: Check package appears under organization

