# MAIN HIGH LEVEL PRIORITY: Standard Operating Procedure (SOP)

Note: The Primary LOOP is I send you either Feedback or console errors or output. At this point you trouble shoot or dev then when you are finished the loop starts over. If feedback is given that the code is building with no errors and minimal warnings and a milestone is reached an update will be recommended where documentation is updated and a GIT is recommended. Do not commit without final approval. Then the loop continues after you recommend 3 options to proceed next.

IMPORTANT: Do not commit changes unless explicitly told to by the user.

## Audio Pipeline Implementation Notes

### ASIO Requirements
- ASIO4ALL must be installed and configured for 44100 Hz sample rate
- Enable "Always Resample 44.1↔48 kHz" in advanced settings
- Set buffer size to 256-512 samples for DJ use
- Test with WaveOut fallback if ASIO issues occur

### Permanent Pipeline Architecture
- SampleProvider chain: Silent → Playing → Looping → Volume
- Mixer maintains permanent inputs to prevent stream disconnects
- All format conversion happens before pipeline integration
- Use WDL resampler for best quality (replaces MediaFoundation)

### Error Handling Patterns
- Wrap all audio operations in try-catch with detailed logging
- Log format details (rate, channels, bits) on load
- Validate file integrity before processing
- Graceful fallback for unsupported formats

### Development Guidelines
- Always maintain permanent pipeline when adding new providers
- Test with various sample rates and formats
- Include comprehensive logging for debugging
- Follow existing error handling patterns

### UI Theming
- Light theme: Light gray background (#D3D3D3) with black text (#000000)
- All controls (TextBox, ComboBox, CheckBox, etc.) use light gray theme
- ComboBox dropdown items styled with light gray background
- Selected items highlighted with cyan background

### ASIO Troubleshooting
- Check logs for driver capabilities: buffer size (256-512), latency, supported sample rates (44100/48000)
- If garbled playback: Verify ASIO4ALL buffer settings, ensure ChannelOffset is correct, check for sample rate mismatches
- Fallback to WaveOut logs detailed error reasons
- Use ShowAsioControlPanel() to adjust driver settings
- Monitor MixingSampleProvider logs for pre-sum levels >1.0 (indicates overdriving)

### Testing Checklist
- [ ] 44100 Hz files (no resampling)
- [ ] 48000 Hz files (resampling)
- [ ] Mono files (channel conversion)
- [ ] Corrupted files (error handling)
- [ ] ASIO vs WaveOut output
- [ ] Pause/play state transitions

## Autocompletion and Autodebug Section

### Build and Run Process for Debugging
1. **Locate dotnet.exe**: Use `/mnt/c/Program Files/dotnet/dotnet.exe` (default Windows dotnet installation path).
2. **Build Command**: Run `/mnt/c/Program\ Files/dotnet/dotnet.exe build DJMixMaster.csproj` to compile the project.
3. **Run Command**: Execute `/mnt/c/Program\ Files/dotnet/dotnet.exe run --project DJMixMaster.csproj` to launch the application.
4. **Debugging**: Monitor terminal output for logs, errors, and audio pipeline details during runtime. WPF GUI will open separately; focus on console for troubleshooting ASIO, resampling, and playback issues.
5. **Post-Run Review**: After testing, check logs (e.g., log20251116_*.txt) for anomalies, validate against Testing Checklist, and recommend fixes or next steps.

</content>
<parameter name="filePath">ref/JuceReferenceOutline.md