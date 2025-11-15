# MAIN HIGH LEVEL PRIORITY: Standard Operating Procedure (SOP)

Note: The Primary LOOP is I send you either Feedback or console errors or output. At this point you trouble shoot or dev then when you are finished the loop starts over. If feedback is given that the code is building with no errors and minimal warnings and a milestone is reached an update will be recommended where documentation is updated and a GIT is recommended. Do not commit without final approval. Then the loop continues after you recommend 3 options to proceed next.

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

### Testing Checklist
- [ ] 44100 Hz files (no resampling)
- [ ] 48000 Hz files (resampling)
- [ ] Mono files (channel conversion)
- [ ] Corrupted files (error handling)
- [ ] ASIO vs WaveOut output
- [ ] Pause/play state transitions

</content>
<parameter name="filePath">ref/JuceReferenceOutline.md