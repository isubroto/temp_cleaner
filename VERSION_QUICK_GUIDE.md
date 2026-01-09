# ?? Quick Version Update Guide

## When You Want to Release a New Version

### Option 1: Automatic (Recommended - Via GitHub Workflow)
Just push to main or create a tag:
```bash
git tag v0.1.38.3
git push origin v0.1.38.3
```

The GitHub workflow will:
- ? Automatically compute the version
- ? Update `TempCleaner.csproj` (all 4 version tags)
- ? Update `app.manifest`
- ? Build the application
- ? Create the MSI installer
- ? Upload to GitHub Releases
- ? Submit to WinGet
- ? Commit version changes back to the repository

### Option 2: Manual Update
If you need to manually update the version:

#### Step 1: Update TempCleaner.csproj
```xml
<Version>0.1.38.3</Version>
<AssemblyVersion>0.1.38.3</AssemblyVersion>
<FileVersion>0.1.38.3</FileVersion>
<InformationalVersion>0.1.38.3</InformationalVersion>
```

#### Step 2: Update app.manifest
```xml
<assemblyIdentity version="0.1.38.3" name="DeepCleaner.app"/>
```

#### Step 3: Build and Verify
```powershell
dotnet build -c Release
Get-Item bin\Release\net8.0-windows\win-x64\DeepCleaner.exe | Select-Object -ExpandProperty VersionInfo
```

## Verification Checklist

After building, check:

### 1. PowerShell Verification
```powershell
$exe = "bin\Release\net8.0-windows\win-x64\DeepCleaner.exe"
$version = (Get-Item $exe).VersionInfo
Write-Host "File Version: $($version.FileVersion)"
Write-Host "Product Version: $($version.ProductVersion)"
```

**Expected output:**
```
File Version: 0.1.38.3
Product Version: 0.1.38.3
```

### 2. Windows Properties Check
1. Navigate to: `bin\Release\net8.0-windows\win-x64\DeepCleaner.exe`
2. Right-click ? Properties ? Details
3. Verify:
   - **File version**: `0.1.38.3`
   - **Product version**: `0.1.38.3`

### 3. WinGet Check (After Release)
```powershell
winget list --id SubrotoSaha.DeepCleaner
```

Should show the full 4-component version: `0.1.38.3`

## Important Notes

### ?? ALWAYS Use 4 Components
- ? Correct: `0.1.38.3`
- ? Wrong: `0.1.38` (only 3 components)

### ?? Keep Files in Sync
Both files must have the same version:
- `TempCleaner.csproj` (4 version properties)
- `app.manifest` (1 version attribute)

### ?? GitHub Workflow Handles It
The workflow automatically:
- Ensures 4-component versioning
- Updates both files
- Commits changes back

## Troubleshooting

### Problem: WinGet keeps showing updates available
**Solution:** Check that installed version shows 4 components
```powershell
winget show --id SubrotoSaha.DeepCleaner
```

### Problem: Windows shows only 3 components
**Solution:** Update `app.manifest` and rebuild:
```xml
<assemblyIdentity version="0.1.38.3" name="DeepCleaner.app"/>
```

### Problem: GitHub workflow fails on version update
**Solution:** Check that both files have valid 4-component versions

## Version Format
```
MAJOR.MINOR.PATCH.REVISION
  ?     ?      ?       ?
  ?     ?      ?       ?? Build/revision number (e.g., 3)
  ?     ?      ?????????? Bug fixes (e.g., 38)
  ?     ????????????????? Minor features (e.g., 1)
  ??????????????????????? Major breaking changes (e.g., 0)

Example: 0.1.38.3
```

## Files That Need Version Updates

| File | Location | What to Update |
|------|----------|---------------|
| `TempCleaner.csproj` | Root | `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<InformationalVersion>` |
| `app.manifest` | Root | `<assemblyIdentity version="...">` |

## Automated by GitHub Workflow

The following files are automatically updated by the workflow:
- ? `TempCleaner.csproj` (all version tags)
- ? `app.manifest` (assemblyIdentity version)
- ? WinGet manifests (PackageVersion)

You don't need to manually update these if using the GitHub workflow!

## Quick Commands

### Build Release
```powershell
dotnet build TempCleaner.csproj -c Release
```

### Publish Single-File
```powershell
dotnet publish TempCleaner.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Check Version
```powershell
(Get-Item "bin\Release\net8.0-windows\win-x64\DeepCleaner.exe").VersionInfo | Format-List FileVersion, ProductVersion
```

### Create and Push Tag
```bash
git tag v0.1.38.3
git push origin v0.1.38.3
```

## Need Help?

See the detailed documentation in `VERSIONING.md`
