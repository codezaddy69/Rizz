# DJ Mix Master - Contributing Guide

## Getting Started

### Development Environment Setup
1. **Prerequisites**:
   - Visual Studio 2022 (Community edition is fine)
   - .NET 9.0 SDK
   - CMake 3.28+
   - Git

2. **Clone and Setup**:
   ```bash
   git clone https://github.com/your-org/djmixmaster.git
   cd djmixmaster
   dotnet restore
   ```

3. **Build**:
   ```bash
   # Build C# components
   dotnet build

   # Build C++ engine
   cd RizzAudioEngine
   cmake -B build -S .
   cmake --build build --config Release
   ```

### Project Structure
```
djmixmaster/
├── src/                    # C# source code
├── RizzAudioEngine/       # C++ audio engine
├── docs/                  # Documentation
├── tests/                 # Unit and integration tests
├── scripts/               # Build and utility scripts
└── assets/                # Sample files and resources
```

## Development Workflow

### 1. Choose an Issue
- Check GitHub Issues for open tasks
- Look for "good first issue" or "help wanted" labels
- Create an issue if you have a new idea

### 2. Create a Branch
```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-number-description
```

### 3. Make Changes
- Follow the coding standards below
- Write tests for new functionality
- Update documentation as needed
- Test your changes thoroughly

### 4. Commit Changes
```bash
git add .
git commit -m "feat: add new feature description

- What was changed
- Why it was changed
- Any breaking changes
"
```

### 5. Create Pull Request
- Push your branch to GitHub
- Create a PR with a clear description
- Reference any related issues
- Request review from maintainers

## Coding Standards

### C# Code Style
- Use C# 12 features where appropriate
- Follow .NET naming conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs

```csharp
/// <summary>
/// Loads an audio track into the specified deck
/// </summary>
/// <param name="deck">Deck number (0 or 1)</param>
/// <param name="filePath">Path to audio file</param>
public void LoadTrack(int deck, string filePath)
{
    if (deck < 0 || deck > 1)
        throw new ArgumentOutOfRangeException(nameof(deck));

    // Implementation
}
```

### C++ Code Style
- Follow Google C++ Style Guide
- Use smart pointers for memory management
- Add Doxygen comments for interfaces

```cpp
/**
 * @brief Loads an audio file for playback
 * @param filePath Path to the audio file
 * @return true if successful, false otherwise
 */
bool LoadFile(const std::string& filePath) {
    // Implementation
    return true;
}
```

### WPF XAML Style
- Use meaningful names for controls
- Group related elements
- Use styles and templates for consistency

```xml
<!-- Good -->
<Button x:Name="btnPlay" Content="PLAY" Style="{StaticResource NeonButton}"/>

<!-- Avoid -->
<Button Content="PLAY" Background="Green"/>
```

## Testing

### Unit Tests
- Write tests for all new functionality
- Use xUnit for C# tests
- Mock external dependencies

```csharp
[Fact]
public void AudioEngine_LoadTrack_ValidFile_Success()
{
    // Arrange
    var engine = new AudioEngine();
    var testFile = "test.mp3";

    // Act
    engine.LoadTrack(0, testFile);

    // Assert
    Assert.Equal(testFile, engine.GetProperties(0).FilePath);
}
```

### Integration Tests
- Test complete workflows
- Verify audio pipeline functionality
- Check UI interactions

### Manual Testing Checklist
- [ ] Application starts without errors
- [ ] Audio files load correctly
- [ ] Playback works on both decks
- [ ] UI controls respond properly
- [ ] No crashes during normal use
- [ ] Performance is acceptable

## Documentation

### Code Documentation
- Add XML comments to public methods
- Document complex algorithms
- Explain non-obvious design decisions

### User Documentation
- Update user manual for new features
- Add screenshots for UI changes
- Update troubleshooting guides

### API Documentation
- Document all public APIs
- Provide usage examples
- Note breaking changes

## Pull Request Guidelines

### PR Title Format
```
type(scope): description

Types: feat, fix, docs, style, refactor, test, chore
Examples:
- feat(audio): add VST plugin support
- fix(ui): resolve waveform rendering bug
- docs(api): update AudioEngine documentation
```

### PR Description Template
```
## Description
Brief description of the changes

## Changes Made
- List of specific changes
- Files modified
- New dependencies (if any)

## Testing
- How the changes were tested
- Test cases added
- Manual testing performed

## Screenshots (if applicable)
- Before/after screenshots for UI changes

## Breaking Changes
- Any breaking changes for users/developers

## Related Issues
- Closes #123
- Related to #456
```

### Review Process
1. Automated checks pass (build, tests, linting)
2. Code review by at least one maintainer
3. All review comments addressed
4. Final approval and merge

## Issue Reporting

### Bug Reports
- Use the bug report template
- Include system information
- Provide steps to reproduce
- Attach log files and screenshots

### Feature Requests
- Describe the problem you're trying to solve
- Explain your proposed solution
- Consider alternative approaches
- Provide mockups if applicable

## Community Guidelines

### Code of Conduct
- Be respectful and inclusive
- Focus on constructive feedback
- Help newcomers learn
- Maintain professional communication

### Getting Help
- Check existing documentation first
- Search GitHub issues
- Ask in GitHub Discussions
- Join community chat

### Recognition
Contributors are recognized in:
- Release notes
- Contributors file
- GitHub project insights

## Advanced Topics

### Audio Engine Development
- Understand real-time audio processing
- Learn about ASIO and low-latency audio
- Study digital signal processing basics

### UI/UX Design
- Follow DJ software conventions
- Ensure accessibility compliance
- Test on different screen sizes

### Performance Optimization
- Profile code regularly
- Optimize audio callback paths
- Minimize memory allocations

### Cross-Platform Considerations
- Design with future platforms in mind
- Avoid platform-specific code
- Use abstraction layers

## Resources

### Learning Materials
- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [C++ Reference](https://en.cppreference.com/)
- [WPF Documentation](https://learn.microsoft.com/dotnet/desktop/wpf/)
- [Audio Programming Resources](https://www.musicdsp.org/)

### Tools and Utilities
- Visual Studio extensions
- ReSharper for code analysis
- GitHub Copilot for productivity
- Performance profiling tools

### Community
- GitHub Discussions
- Stack Overflow
- Reddit communities
- Professional networks

Thank you for contributing to DJ Mix Master! Your efforts help make professional DJ software accessible to everyone.