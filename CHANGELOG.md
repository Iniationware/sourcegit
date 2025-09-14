# Changelog

All notable changes to the Iniationware Edition of SourceGit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v2025.34.10] - 2025-09-14

### Added
- macOS DMG packaging with code signing and notarization support
- Comprehensive documentation (README, CHANGELOG, RELEASE_NOTES)
- Documentation hub in docs/README.md
- macOS signing setup guide
- Linux-compatible versioning format (removed hyphens)
- Pre-tag validation script for quality assurance
- Code formatting standards enforcement

### Changed
- Version format from v{YEAR}.{WEEK}-IW.{X} to v{YEAR}.{WEEK}.{X}
- Restored Ubuntu 20.04 containers for working ARM64 cross-compilation
- Fixed GitHub Actions workflows for reliable builds
- Updated README with Iniationware Edition branding

### Fixed
- Linux package compatibility issues (AppImage, DEB, RPM)
- Git permission issues in CI/CD containers
- Release asset creation failures

## [v2025.34-IW.9] - 2025-09-14

### Added
- Code formatting fixes across entire codebase

### Fixed
- Whitespace and indentation issues
- Missing final newlines in source files

## [v2025.34-IW.8] - 2025-09-14

### Added
- Repository metrics dashboard
- Branch counter functionality
- Commit statistics visualization

### Changed
- Enhanced refresh operations with cache invalidation

## [v2025.34-IW.7] - 2025-09-09

### Added
- Smart memory management system
- Performance monitoring capabilities
- Resource optimization features

### Fixed
- Memory leaks in long-running operations
- Resource exhaustion issues

## [v2025.34-IW.6] - 2025-09-09

### Added
- GitCommandCache for 60-70% performance improvement
- GitProcessPool for efficient process management
- BatchQueryExecutor for parallel operations

### Changed
- Optimized branch and commit operations
- Implemented lazy loading strategies

## [v2025.34-IW.5] - 2025-09-03

### Added
- Comprehensive shutdown improvements
- Enhanced error handling and recovery

### Fixed
- Application hang on shutdown (30+ seconds)
- Background operation cleanup issues

## [v2025.34-IW.4] - 2025-08-16

### Added
- Git-Flow optimization features
- Enhanced credential management
- Secure storage for authentication

### Fixed
- Missing GitFlow.FinishBranch locale keys
- UI update issues after Git-Flow operations

## [v2025.34-IW.3] - 2025-08-10

### Added
- Initial performance optimizations
- Basic caching implementation

## [v2025.34-IW.2] - 2025-08-05

### Added
- Iniationware branding
- Custom version display

### Changed
- Updated application metadata
- Modified update check URLs

## [v2025.34-IW.1] - 2025-08-01

### Added
- Initial Iniationware Edition fork
- Basic enterprise enhancements

### Changed
- Forked from SourceGit v2025.34

---

## Version Format

Starting from v2025.34.10, we use a three-part version number:
- **Major**: Year (2025)
- **Minor**: Week number (34)
- **Patch**: Iniationware increment (10, 11, 12...)

Previous versions used the format v{YEAR}.{WEEK}-IW.{INCREMENT} which caused issues with Linux packaging systems.