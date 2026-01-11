# DJ Mix Master - Troubleshooting Guide

## Audio Issues

### No Sound Output
**Symptoms**: Application starts but no audio plays
**Solutions**:
1. Check audio device selection in settings
2. Verify volume levels are not muted
3. Test with different output modes (ASIO → WASAPI → DirectSound)
4. Restart the application
5. Check Windows audio mixer

### High Latency/Delay
**Symptoms**: Noticeable delay between action and sound
**Solutions**:
1. Switch to ASIO output mode
2. Lower buffer size in audio settings (try 128-256 samples)
3. Close other audio applications
4. Use a dedicated audio interface
5. Check CPU usage (should be <50%)

### Audio Dropouts/Stuttering
**Symptoms**: Sound cuts out intermittently
**Solutions**:
1. Increase buffer size (try 512-1024 samples)
2. Close background applications
3. Disable Windows audio enhancements
4. Update audio drivers
5. Check for overheating

### Distortion/Artifacts
**Symptoms**: Sound is distorted or has artifacts
**Solutions**:
1. Verify sample rate compatibility (44.1kHz recommended)
2. Check bit depth settings
3. Disable audio enhancements
4. Try different output formats
5. Test with different audio files

## Application Issues

### Won't Start
**Symptoms**: Double-clicking exe does nothing or shows error
**Solutions**:
1. Check .NET 9.0 runtime installation
2. Run as administrator
3. Check Windows Event Viewer for error details
4. Verify all dependencies are present
5. Try running from command line: `DJMixMaster.exe`

### Crashes on Startup
**Symptoms**: Application starts then immediately closes
**Solutions**:
1. Check log files in `logs/` directory
2. Delete `settings/audio.json` to reset to defaults
3. Run with verbose logging: `DJMixMaster.exe --verbose`
4. Check for conflicting applications
5. Update graphics drivers

### UI Not Responding
**Symptoms**: Interface is frozen or unresponsive
**Solutions**:
1. Wait for audio processing to complete
2. Check CPU usage (may be overloaded)
3. Restart the application
4. Close and reopen the window
5. Check for Windows UI scaling issues

## File Loading Issues

### Tracks Won't Load
**Symptoms**: Clicking LOAD does nothing or shows error
**Solutions**:
1. Verify file format (MP3, WAV supported)
2. Check file permissions
3. Try different file locations
4. Check available disk space
5. Look for corrupted files

### Metadata Not Displayed
**Symptoms**: Track info shows "Unknown" or blank
**Solutions**:
1. Check file metadata/tags
2. Try re-encoding the file
3. Use files with proper ID3 tags
4. Check for special characters in filenames

### Large Files Cause Issues
**Symptoms**: Problems with big audio files
**Solutions**:
1. Ensure sufficient RAM (4GB+ recommended)
2. Use 44.1kHz sample rate files
3. Split large files if possible
4. Check disk I/O performance

## Performance Issues

### High CPU Usage
**Symptoms**: Application uses excessive CPU
**Solutions**:
1. Close other applications
2. Lower visual quality settings
3. Disable unnecessary effects
4. Update to latest version
5. Check for background processes

### Memory Leaks
**Symptoms**: Memory usage grows over time
**Solutions**:
1. Restart the application periodically
2. Monitor with Task Manager
3. Check for large waveform caches
4. Update to latest version

### Slow Waveform Rendering
**Symptoms**: Waveforms update slowly or not at all
**Solutions**:
1. Reduce waveform resolution
2. Close other GPU-intensive applications
3. Update graphics drivers
4. Lower display refresh rate

## Build and Development Issues

### Compilation Errors
**Symptoms**: `dotnet build` fails
**Solutions**:
1. Ensure .NET 9.0 SDK is installed
2. Run `dotnet restore` first
3. Check for missing NuGet packages
4. Verify C++ toolchain for RizzAudioEngine
5. Check CMake version (3.28+ required)

### C++ Build Failures
**Symptoms**: RizzAudioEngine won't compile
**Solutions**:
1. Install Visual Studio with C++ workload
2. Check CMake installation
3. Verify compiler toolset
4. Clean build directory: `rmdir /s build`
5. Re-run CMake configuration

### Runtime DLL Issues
**Symptoms**: Missing libShredEngine.dll errors
**Solutions**:
1. Build RizzAudioEngine first
2. Copy DLL to output directory
3. Check PATH environment variable
4. Use dependency walker to check dependencies

## Network and Cloud Issues

### Sync Failures (Future Feature)
**Symptoms**: Cloud sync doesn't work
**Solutions**:
1. Check internet connection
2. Verify account credentials
3. Check firewall settings
4. Clear cache and retry
5. Contact support for account issues

## Advanced Troubleshooting

### Log Analysis
```bash
# Check recent logs
tail -f logs/debug.log

# Search for errors
grep "ERROR" logs/*.log

# Check audio engine logs
cat logs/audio_engine.log
```

### System Information
```bash
# Get system specs
systeminfo | findstr /C:"OS" /C:"Processor" /C:"Memory"

# Check .NET version
dotnet --version

# List audio devices
# Use Windows Settings > Sound
```

### Diagnostic Commands
```bash
# Test audio output
dotnet run -- test-audio

# Benchmark performance
dotnet run -- benchmark

# Verbose startup
dotnet run -- verbose
```

### Recovery Procedures

#### Reset Application Settings
```bash
# Delete settings files
del settings\*.json

# Clear cache
rmdir /s cache
```

#### Clean Reinstall
```bash
# Remove all files
rmdir /s bin obj

# Rebuild from scratch
dotnet clean
dotnet restore
dotnet build
```

#### Emergency Mode
```bash
# Start with minimal features
dotnet run -- safe-mode

# Disable audio processing
dotnet run -- no-audio
```

## Getting Help

### Self-Help Resources
- Read the full documentation in `docs/`
- Check GitHub issues for similar problems
- Review changelog for recent fixes
- Test with sample files

### Community Support
- GitHub Discussions
- Reddit communities
- Discord server
- User forums

### Professional Support
- Priority email support
- Remote debugging sessions
- Custom build assistance
- Training sessions

### Bug Reporting
When reporting issues, include:
- System specifications
- Application version
- Steps to reproduce
- Log files
- Screenshot of error

## Prevention

### Best Practices
- Keep system updated
- Use stable audio drivers
- Close unnecessary applications
- Regular backups of settings
- Monitor system resources

### Maintenance
- Clean temporary files weekly
- Update application regularly
- Defragment drives
- Check disk health

### Monitoring
- Use Task Manager to monitor resources
- Check Event Viewer for system errors
- Monitor audio device status
- Keep log files for reference