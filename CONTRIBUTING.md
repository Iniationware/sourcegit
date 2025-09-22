# Contributing to SourceGit

Thank you for your interest in contributing to SourceGit! This guide will help you get started with contributing to the project.

## 🤝 Ways to Contribute

### 1. Report Bugs
- Check if the issue already exists
- Create a detailed bug report with steps to reproduce
- Include system information and error messages

### 2. Suggest Features
- Describe the feature and its use case
- Explain how it would benefit users
- Consider implementation complexity

### 3. Submit Code
- Fix bugs or implement features
- Improve performance or code quality
- Add tests for new functionality

### 4. Improve Documentation
- Fix typos and grammar
- Add examples and clarifications
- Translate to other languages

### 5. Help with Translations
- See [Translation Status](../TRANSLATION.md)
- Contribute to existing translations
- Add support for new languages

## 🚀 Development Setup

### Prerequisites
- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/) 2.25.1 or higher
- Your favorite IDE (VS Code, Visual Studio, Rider)

### Building from Source

1. **Clone the repository**
   ```bash
   git clone https://github.com/sourcegit-scm/sourcegit.git
   cd sourcegit
   ```

2. **Checkout develop branch**
   ```bash
   git checkout develop
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the project**
   ```bash
   dotnet build -c Debug
   ```

5. **Run SourceGit**
   ```bash
   dotnet run --project src/SourceGit.csproj
   ```

### Project Structure

```
sourcegit/
├── src/                    # Source code
│   ├── Commands/          # Git command wrappers
│   ├── Models/            # Data models
│   ├── ViewModels/        # MVVM view models
│   ├── Views/             # Avalonia UI views
│   └── Native/            # Platform-specific code
├── build/                  # Build resources
├── docs/                   # Documentation
└── screenshots/            # Screenshots for README
```

## 📝 Coding Guidelines

### C# Style
- Follow standard C# naming conventions
- Use meaningful variable and method names
- Keep methods small and focused
- Add XML documentation for public APIs

### Git Commits
- Use clear, descriptive commit messages
- Follow conventional commits format:
  - `feat:` New features
  - `fix:` Bug fixes
  - `docs:` Documentation changes
  - `style:` Code style changes
  - `refactor:` Code refactoring
  - `perf:` Performance improvements
  - `test:` Test additions/changes
  - `chore:` Build process/auxiliary changes

### Code Quality
- Ensure no compiler warnings
- Add unit tests for new functionality
- Maintain or improve code coverage
- Follow SOLID principles

## 🔄 Pull Request Process

### Before Submitting

1. **Base your work on `develop` branch**
   ```bash
   git checkout develop
   git pull upstream develop
   git checkout -b feature/your-feature
   ```

2. **Follow coding standards**
   - Run code formatting
   - Fix any linting issues
   - Ensure builds pass

3. **Test thoroughly**
   - Test on your platform
   - Consider cross-platform impacts
   - Verify no regressions

4. **Update documentation**
   - Update README if needed
   - Add/update code comments
   - Document new features

### Submitting PR

1. **Create Pull Request**
   - Target the `develop` branch
   - Use clear, descriptive title
   - Reference related issues

2. **PR Description Should Include**
   - What changes were made
   - Why changes were necessary
   - How to test the changes
   - Screenshots if UI changes

3. **Address Review Feedback**
   - Respond to all comments
   - Make requested changes
   - Re-request review when ready

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SourceGit.Tests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests
- Place tests in appropriate test project
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

## 🌍 Localization

### Adding Translations

1. **Locate language files**
   ```
   src/Resources/Locales/
   ```

2. **Copy English template**
   - Copy `en_US.axaml`
   - Rename to your locale (e.g., `fr_FR.axaml`)

3. **Translate strings**
   - Keep XML structure intact
   - Translate values, not keys
   - Test special characters

4. **Register locale**
   - Add to `Models/Locale.cs`
   - Update supported languages list

## 🐛 Debugging

### Debug Build
```bash
dotnet build -c Debug
dotnet run --project src/SourceGit.csproj --configuration Debug
```

### Logging
- Logs are stored in the data directory
- Enable verbose logging in preferences
- Check `logs/` folder for crash reports

### Common Issues

**Build Failures**
- Ensure .NET SDK 9.0+ is installed
- Run `dotnet restore` to update packages
- Clear build cache: `dotnet clean`

**Runtime Issues**
- Check Git version compatibility
- Verify file permissions
- Review application logs

## 📚 Resources

### Documentation
- [Avalonia Documentation](https://docs.avaloniaui.net/)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Git Documentation](https://git-scm.com/doc)

### Tools
- [Visual Studio Code](https://code.visualstudio.com/)
- [Visual Studio](https://visualstudio.microsoft.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/)

### Community
- [GitHub Discussions](https://github.com/sourcegit-scm/sourcegit/discussions)
- [Issue Tracker](https://github.com/sourcegit-scm/sourcegit/issues)

## ⚖️ License

By contributing to SourceGit, you agree that your contributions will be licensed under the [MIT License](../LICENSE).

## 🙏 Thank You!

Your contributions make SourceGit better for everyone. We appreciate your time and effort!

---

<div align="center">
  <b>Happy Contributing! 🎉</b>
</div>