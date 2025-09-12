# 🌊 TempCleaner

<div align="center">

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/isubroto/temp_cleaner?style=for-the-badge&logo=github&color=00D4AA)](https://github.com/isubroto/temp_cleaner/releases/latest)
[![License](https://img.shields.io/github/license/isubroto/temp_cleaner?style=for-the-badge&color=0099FF)](LICENSE)
[![Downloads](https://img.shields.io/github/downloads/isubroto/temp_cleaner/total?style=for-the-badge&logo=download&color=00E5FF)](https://github.com/isubroto/temp_cleaner/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=for-the-badge&logo=windows)](https://www.microsoft.com/windows/)

**A powerful Windows utility that cleans temporary files, system cache, and frees up disk space with an immersive deep-sea themed interface.**

[🚀 Download Latest Release](https://github.com/isubroto/temp_cleaner/releases/latest) • [📖 Documentation](#-features) • [🐛 Report Bug](https://github.com/isubroto/temp_cleaner/issues) • [💡 Request Feature](https://github.com/isubroto/temp_cleaner/issues)

</div>

---

## 📸 Screenshots

<div align="center">
<img src="Images/Designer.png" alt="TempCleaner Interface" width="600"/>
</div>

## ✨ Features

### 🧹 **Comprehensive Cleaning**

- **System Temporary Files**: Windows temp directories, prefetch files, and system caches
- **User Profile Cleanup**: User-specific temp files, browser caches, and recent file lists
- **Windows Update Cache**: Software distribution downloads and update residuals
- **Application Caches**: Browser caches, Adobe temp files, Java deployment cache
- **System Logs**: Event logs, setup logs, and debug files
- **Recycle Bin**: Automatic emptying after cleanup completion

### 🎨 **Immersive Interface**

- **Deep-Sea Theme**: Beautiful abyss-inspired UI with bioluminescent accents
- **Real-time Progress**: Live scanning progress with detailed file discovery
- **Interactive Logging**: Comprehensive cleanup logs with oceanic messaging
- **Storage Visualization**: Dynamic storage usage cards and statistics
- **Smooth Animations**: Fluid transitions and hover effects

### 🛡️ **Safety & Security**

- **Safe Cleaning Algorithms**: Intelligent file filtering to avoid system-critical files
- **Permission Handling**: Proper access control with administrator privilege detection
- **Error Recovery**: Graceful handling of locked or inaccessible files
- **Pre-scan Validation**: Files are analyzed before deletion
- **Detailed Reporting**: Complete logs of all operations performed

### 🔄 **Auto-Update System**

- **GitHub Integration**: Automatic update checking from releases
- **Smart Downloads**: Progress tracking with speed and ETA calculations
- **MSI Installation**: Professional installer packages with admin privileges
- **Seamless Updates**: One-click update process with automatic restart

### ⚡ **Performance**

- **Asynchronous Operations**: Non-blocking UI during scanning and cleaning
- **Multi-threaded Scanning**: Parallel directory traversal for faster results
- **Memory Efficient**: Optimized for large file collections
- **Progress Tracking**: Real-time progress updates and statistics

## 🚀 Quick Start

### Installation Options

#### Option 1: WinGet (Recommended)

```powershell
winget install SubrotoSaha.TempCleaner
```

#### Option 2: MSI Installer

1. Download the latest `.msi` installer from [Releases](https://github.com/isubroto/temp_cleaner/releases/latest)
2. Run as Administrator
3. Follow the installation wizard
4. Launch from Start Menu or Desktop shortcut

#### Option 3: Portable Version

1. Download the `.exe` file from [Releases](https://github.com/isubroto/temp_cleaner/releases/latest)
2. Run directly - no installation required
3. Perfect for USB drives or temporary usage

## 🎯 Usage

### Basic Cleanup Process

1. **Launch TempCleaner** with administrator privileges (recommended)
2. **Click "🔍 Deep Scan"** to start scanning for temporary files
3. **Review the results** in the real-time log viewer
4. **Click "🧽 Purify System"** to begin the cleanup process
5. **Monitor progress** through the beautiful progress indicators
6. **Review completion summary** with space recovered statistics

### Scanned Locations

TempCleaner intelligently scans the following locations:

#### System Directories

- `C:\Windows\Temp` - Windows system temporary files
- `C:\Windows\Prefetch` - Application prefetch data
- `C:\Windows\SoftwareDistribution\Download` - Windows Update cache
- `C:\Windows\Logs` - System log files
- `C:\Windows\Debug` - Debug information files
- `C:\Windows\Minidump` - System crash dumps

#### User Profile Areas

- `%LocalAppData%\Temp` - User temporary files
- `%LocalAppData%\Microsoft\Windows\INetCache` - Internet Explorer cache
- `%LocalAppData%\Microsoft\Windows\Explorer` - Explorer thumbnails
- `%LocalAppData%\CrashDumps` - Application crash dumps
- `%AppData%\Microsoft\Windows\Recent` - Recent files and jump lists

#### Application Caches

- Browser caches (Chrome, Firefox, Edge)
- Adobe Common Media Cache Files
- Java deployment cache
- DirectX Shader Cache
- Windows Store app cache

## 🔧 System Requirements

| Requirement          | Specification                    |
| -------------------- | -------------------------------- |
| **Operating System** | Windows 10 (1809+) or Windows 11 |
| **Architecture**     | x64 (64-bit)                     |
| **Runtime**          | .NET 8.0 (self-contained)        |
| **Memory**           | 512 MB RAM minimum               |
| **Storage**          | 50 MB available space            |
| **Privileges**       | Administrator recommended        |

## 🛠️ Development

### Built With

- **Framework**: .NET 8.0 with WPF
- **Language**: C# with modern language features
- **UI Framework**: Windows Presentation Foundation (WPF)
- **Packaging**: MSI installer with WiX Toolset
- **Architecture**: Single-file, self-contained deployment

### Project Structure

```
TempCleaner/
├── 📁 Images/                  # Application icons and resources
├── 📁 Installer/              # WiX installer project files
├── 📄 App.xaml               # Application entry point
├── 📄 MainWindow.xaml        # Main UI layout
├── 📄 MainWindow.xaml.cs     # Main application logic
├── 📄 DirFinder.cs           # File system scanning engine
├── 📄 GitHubUpdater.cs       # Auto-update functionality
├── 📄 DownloadProgressWindow.xaml    # Update download UI
├── 📄 FileStatusReport.cs    # File operation reporting
└── 📄 TempCleaner.csproj     # Project configuration
```

### Key Classes

- **`MainWindow`**: Core application window with scanning and cleaning logic
- **`DirFinder`**: High-performance file system traversal and discovery
- **`GitHubUpdater`**: Automatic update checking and download management
- **`DownloadProgressWindow`**: Update download progress with real-time statistics

### Building from Source

#### Prerequisites

- Visual Studio 2022+ or .NET 8.0 SDK
- Windows 10/11 development environment
- WiX Toolset v4+ (for installer builds)

#### Build Steps

```powershell
# Clone the repository
git clone https://github.com/isubroto/temp_cleaner.git
cd temp_cleaner

# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Release

# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

#### Installer Build

```powershell
# Build MSI installer (requires WiX)
dotnet build Installer/Installer.wixproj --configuration Release
```

## 🔒 Privacy & Security

### Data Privacy

- **No Data Collection**: TempCleaner does not collect or transmit personal data
- **Local Operation**: All scanning and cleaning operations are performed locally
- **No Telemetry**: No usage statistics or analytics are collected
- **Open Source**: Complete source code available for security review

### Security Features

- **Code Signing**: Releases are digitally signed for authenticity
- **Virus Scanning**: All releases are scanned by multiple antivirus engines
- **Safe Defaults**: Conservative file filtering to protect system integrity
- **Permission Respect**: Proper Windows security model compliance

## 🤝 Contributing

We welcome contributions from the community! Here's how you can help:

### Ways to Contribute

1. **🐛 Bug Reports**: Found an issue? [Create a bug report](https://github.com/isubroto/temp_cleaner/issues/new?template=bug_report.md)
2. **💡 Feature Requests**: Have an idea? [Suggest a feature](https://github.com/isubroto/temp_cleaner/issues/new?template=feature_request.md)
3. **📝 Documentation**: Help improve our documentation
4. **🔧 Code**: Submit pull requests with improvements
5. **🌐 Translations**: Help translate the interface

### Development Guidelines

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Code Standards

- Follow C# coding conventions
- Add XML documentation for public methods
- Include unit tests for new functionality
- Maintain the existing UI/UX design language

## 📋 Changelog

### Version 0.1.28.3 (Latest)

- ✨ Enhanced deep-sea UI theme with improved animations
- 🚀 Optimized scanning performance for large file sets
- 🔄 Improved auto-update system with better error handling
- 🛡️ Enhanced safety checks for system-critical files
- 📊 Better progress tracking and storage visualization
- 🐛 Fixed various minor bugs and stability issues

### Previous Releases

For complete changelog, see [Releases](https://github.com/isubroto/temp_cleaner/releases).

## 🆘 Support

### Getting Help

- **📖 Documentation**: Check this README and inline help
- **🐛 Issues**: [Search existing issues](https://github.com/isubroto/temp_cleaner/issues) or create a new one
- **💬 Discussions**: Join [GitHub Discussions](https://github.com/isubroto/temp_cleaner/discussions)
- **📧 Contact**: Reach out via GitHub issues for support

### Troubleshooting

#### Common Issues

**"Access Denied" Errors**

- Run TempCleaner as Administrator
- Ensure no antivirus is blocking the application
- Check Windows User Account Control settings

**Slow Scanning Performance**

- Close other disk-intensive applications
- Ensure adequate free RAM (512MB+)
- Consider excluding large directories if needed

**Update Download Failures**

- Check internet connection
- Verify firewall/antivirus isn't blocking downloads
- Try manual download from Releases page

## 📜 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 Subroto Saha

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

## 🙏 Acknowledgments

- **Microsoft** - For the excellent .NET and WPF frameworks
- **WiX Toolset** - For professional Windows installer creation
- **GitHub** - For hosting and CI/CD infrastructure
- **Open Source Community** - For inspiration and best practices
- **Beta Testers** - For valuable feedback and bug reports

---

<div align="center">

**Made with ❤️ by [Subroto Saha](https://github.com/isubroto)**

**🌊 Dive deep, clean deeper. 🌊**

[![GitHub stars](https://img.shields.io/github/stars/isubroto/temp_cleaner?style=social)](https://github.com/isubroto/temp_cleaner/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/isubroto/temp_cleaner?style=social)](https://github.com/isubroto/temp_cleaner/network/members)
[![GitHub watchers](https://img.shields.io/github/watchers/isubroto/temp_cleaner?style=social)](https://github.com/isubroto/temp_cleaner/watchers)

</div>
