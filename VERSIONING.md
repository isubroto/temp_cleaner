# DeepCleaner Versioning Strategy

## Overview

This document explains how 4-component versioning is implemented across the DeepCleaner project to ensure compatibility with Windows and WinGet package manager.

## Version Format

DeepCleaner uses **4-component versioning**: `MAJOR.MINOR.PATCH.REVISION`

Example: `0.1.38.3`

- MAJOR: 0 (pre-release)
- MINOR: 1
- PATCH: 36
- REVISION: 3

## Why 4-Component Versioning?

### Windows File Properties

Windows displays file version information with up to 4 components. If only 3 components are used (e.g., `0.1.36`), Windows may display it as `0.1.36` while internally treating it as `0.1.36.0`, causing confusion.

### WinGet Version Comparison

WinGet compares versions component-by-component. If your installed version shows `0.1.36` (3 components) but the manifest specifies `0.1.38.3` (4 components), WinGet treats them as different versions and triggers an unnecessary update.

## Version Sources

### 1. TempCleaner.csproj

```xml
<Version>0.1.38.3</Version>
<AssemblyVersion>0.1.38.3</AssemblyVersion>
<FileVersion>0.1.38.3</FileVersion>
<InformationalVersion>0.1.38.3</InformationalVersion>
```

- **Version**: NuGet package version
- **AssemblyVersion**: .NET assembly version
- **FileVersion**: Windows file version (displayed in Properties)
- **InformationalVersion**: Product version (displayed in Properties)

### 2. app.manifest

```xml
<assemblyIdentity version="0.1.38.3" name="DeepCleaner.app"/>
```

This is what Windows uses to display the version in Task Manager and file properties. **CRITICAL**: This must match the .csproj versions.

### 3. WiX Installer (Product.wxs)

```xml
<Package Version="$(var.ProductVersion)" ...>
```

MSI installers only support 3-component versions. The GitHub workflow handles this by:

- Using 4-component version for the .exe and WinGet manifest
- Using 3-component version for the MSI ProductVersion (e.g., `0.1.36`)

## GitHub Workflow Automation

### Version Computation

```powershell
# Ensure 4-component version
$parts = $safe -split '\.'
if ($parts.Length -eq 3) {
    $safe = "$safe.0"  # Add .0 if only 3 components
}
```

### Automatic Updates

The workflow automatically updates:

1. ? All version tags in `TempCleaner.csproj`
2. ? Version in `app.manifest`
3. ? Commits and pushes changes back to the repository

### WinGet Manifest

The WinGet manifest uses the full 4-component version:

```yaml
PackageVersion: 0.1.38.3
```

## Manual Version Updates

When updating the version manually, you **MUST** update these 2 files:

### 1. TempCleaner.csproj

Update all four version properties:

```xml
<Version>0.1.38.3</Version>
<AssemblyVersion>0.1.38.3</AssemblyVersion>
<FileVersion>0.1.38.3</FileVersion>
<InformationalVersion>0.1.38.3</InformationalVersion>
```

### 2. app.manifest

Update the assemblyIdentity version:

```xml
<assemblyIdentity version="0.1.38.3" name="DeepCleaner.app"/>
```

## Verification Checklist

After building, verify the version is correct:

### Windows File Properties

1. Right-click `DeepCleaner.exe`
2. Select "Properties" ? "Details" tab
3. Check **File version** shows all 4 components: `0.1.38.3`
4. Check **Product version** shows all 4 components: `0.1.38.3`

### Task Manager

1. Run the application
2. Open Task Manager
3. Right-click "DeepCleaner" ? "Properties"
4. Verify version shows all 4 components

### WinGet

```powershell
# Check installed version
winget list --id SubrotoSaha.DeepCleaner

# Should show: 0.1.38.3 (all 4 components)
```

## Common Issues

### Issue: WinGet always shows updates available

**Cause**: Installed version shows 3 components, manifest has 4 components
**Solution**: Ensure `app.manifest` has the correct 4-component version

### Issue: Windows shows only 3 components

**Cause**: `app.manifest` has incorrect or missing version
**Solution**: Update `app.manifest` assemblyIdentity version

### Issue: MSI build fails with 4-component version

**Cause**: MSI only supports 3-component versions
**Solution**: This is handled automatically in the workflow - it uses 3-component for MSI, 4-component for everything else

## GitVersion Integration

The project uses GitVersion for automatic semantic versioning:

- **MajorMinorPatch**: Used for MSI (3 components)
- **FullSemVer**: Converted to 4-component for Windows compatibility

Example:

- GitVersion output: `0.1.36`
- Converted to: `0.1.36.0` (automatically adds 4th component)
- Used in WinGet manifest and file properties

## Best Practices

1. ? Always use 4-component versions
2. ? Keep `TempCleaner.csproj` and `app.manifest` in sync
3. ? Let the GitHub workflow handle version updates
4. ? Verify version in Windows Properties after each build
5. ? Test WinGet upgrades before releasing

## References

- [Microsoft Assembly Versioning](https://docs.microsoft.com/en-us/dotnet/standard/assembly/versioning)
- [WinGet Manifest Schema](https://github.com/microsoft/winget-pkgs/tree/master/doc/manifest)
- [WiX Toolset Documentation](https://wixtoolset.org/docs/)
