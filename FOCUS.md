# **FOCUS.md Update: Rizz Fork - PortAudio Overhaul Plan**

## **Executive Summary**
The "Rizz" fork will migrate our DJMixMaster from NAudio to PortAudio.NET (a C# wrapper for PortAudio), adopting Mixxx's proven architecture as inspiration. This includes a SoundManager for device handling, EngineMixer for buffering, and CachingReader for file I/O. We'll improve on Mixxx by adding multi-threading optimizations, advanced customization, and a tabbed options menu leveraging our assets (themes from `assets/gfx/`, test samples from `assets/audio/`). The overhaul prioritizes speed, features, and user control while maintaining DJ workflow simplicity.

**Key Goals**:
- **Tech Stack Alignment**: Use PortAudio.NET for cross-platform audio I/O, Qt-inspired C# classes for engine components.
- **Performance Boost**: Multi-threaded processing, GPU-accelerated effects, and optimized buffering.
- **Customization**: Extensive options menu with tabs for every aspect, using our asset images for themes.
- **Improvements Over Mixxx**: 3 specific enhancements (detailed below).
- **Scope**: Full audio engine rewrite; UI remains WPF but integrates new options.

## **Deep Dive into Mixxx Code as Inspiration**
Mixxx's codebase is a masterclass in modular audio engineering. Key insights from `ref/mixxx/src/`:

- **SoundIO Layer (`soundio/`)**: PortAudio handles all I/O. `SoundManager` enumerates devices, manages connections, and provides callbacks. ASIO is treated as one API among many—no special casing, but robust error handling (e.g., `SoundDevicePortAudio` with drift correction and CPU monitoring). Devices are queried via `Pa_GetDeviceCount()`, with sample rate validation.

- **Engine Layer (`engine/`)**: `EngineMixer` is the heart—processes channels in buffers, applies effects, and mixes outputs (main, booth, headphones). `EngineBuffer` manages deck playback with controls (BPM, cue, loop). `CachingReader` uses worker threads for pre-buffering audio, with hints for cue points. Buffers are pre-allocated for efficiency.

- **Mixer/Player Layer (`mixer/`)**: `Deck` and `PlayerManager` handle track loading. `BasePlayer` integrates with the engine.

- **Controls (`control/`)**: Qt-based control objects for UI binding, with polling proxies for real-time updates.

- **Overall Architecture**: Event-driven, with `EngineWorkerScheduler` for offloading tasks. Strong separation of concerns: I/O → Mixing → Playback → UI.

**Architectural Differences from Our Current System**:
- **Ours**: Monolithic `AudioEngine` with NAudio providers; single-threaded; direct ASIO via AsioOut.
- **Mixxx**: Distributed across SoundManager (I/O), EngineMixer (processing), EngineBuffer (playback); multi-threaded; PortAudio abstraction.
- **Conceptual Shift**: From "pipeline of providers" to "buffered channel processing" with workers. Our permanent pipeline becomes dynamic channels.

**What We Need for Rizz**:
- PortAudio.NET integration for device enumeration and streaming.
- C# equivalents of Mixxx classes: SoundManager, EngineMixer, EngineBuffer, CachingReader.
- Multi-threading with `System.Threading` for workers.
- Buffer management with `Span<T>` or arrays for efficiency.
- Qt-inspired control system using WPF bindings.
- Asset integration: Load themes from `assets/gfx/` (e.g., `cyberdeck.png` for dark mode), test samples from `assets/audio/` (e.g., `ThisIsTrash.wav` for playback demos).

## **3 Improvements Over Mixxx**
1. **GPU-Accelerated Effects and Mixing**: Mixxx uses CPU-only effects. We'll add DirectX/OpenGL shaders (via SharpDX or OpenTK) for real-time GPU processing of filters/reverbs, reducing CPU load by 40-60% on modern hardware.

2. **AI-Assisted Auto-Mixing**: Integrate ML.NET for beat-matching and transition suggestions. Analyze tracks in real-time for seamless crossfades, a feature Mixxx lacks.

3. **Modular Plugin System**: Mixxx has basic effects. We'll use .NET's plugin architecture (MEF) for user-created effects/scripts, with hot-reload and sharing via NuGet.

## **Extensive Options Menu with Tabs**
Leverage our assets for themes and samples. The options menu will be tabbed, highly customizable, and include demos using `assets/audio/`.

- **Tab 1: Audio Devices** - Device selection, sample rates, buffer sizes. Demo: Play `ThisIsTrash.wav` to test output.
- **Tab 2: Engine Settings** - Threading options, buffer sizes, GPU acceleration toggle. Theme preview with `cyberdeck.png`.
- **Tab 3: Effects & Mixing** - Effect chains, crossfader curves, AI mixing options. Load samples for effect previews.
- **Tab 4: Playback & Controls** - BPM detection, cue sensitivity, loop modes. Test with `assets/audio/` tracks.
- **Tab 5: Themes & UI** - Background images from `assets/gfx/` (e.g., `galaxy.png` for space theme), font sizes, colors.
- **Tab 6: Advanced** - Plugin management, logging levels, performance metrics. Include sample playback for latency tests.
- **Tab 7: Library & Assets** - Scan `assets/` for themes/samples, auto-load on startup.

Each tab uses WPF controls with asset previews.

## **Documented Classes for Rizz Overhaul**
Based on Mixxx inspiration, here's each class we'll create/modify. All in C#, using PortAudio.NET and .NET threading.

1. **SoundManager** (Inspired by `SoundManager`): Manages PortAudio devices. Methods: `QueryDevices()`, `SetupDevices()`, `PushInputBuffers()`. Handles ASIO enumeration and fallbacks.

2. **EngineMixer** (Inspired by `EngineMixer`): Core processor. Methods: `Process()`, `AddChannel()`. Manages buffers for main/booth/headphones, applies gains/effects.

3. **EngineBuffer** (Inspired by `EngineBuffer`): Deck playback. Methods: `Process()`, `Seek()`. Integrates with CachingReader for audio data.

4. **CachingReader** (Inspired by `CachingReader`): Multi-threaded file reader. Methods: `ReadAhead()`, `Hint()`. Uses `ThreadPool` for workers.

5. **EngineWorkerScheduler** (Inspired by `EngineWorkerScheduler`): Thread manager. Methods: `ScheduleTask()`, `ProcessQueue()`. Offloads effects/scaling.

6. **SoundDevice** (Inspired by `SoundDevicePortAudio`): Device wrapper. Methods: `Open()`, `CallbackProcess()`. Handles PortAudio streams.

7. **ControlObject** (Inspired by `ControlObject`): UI bindings. Methods: `SetValue()`, `GetValue()`. Polling proxies for real-time sync.

8. **OptionsManager** (New): Manages tabbed options. Methods: `LoadSettings()`, `SaveSettings()`. Integrates assets for themes/samples.

9. **AssetLoader** (New): Loads `assets/gfx/` and `assets/audio/`. Methods: `LoadTheme()`, `PlaySample()`. For UI demos.

10. **RizzApplication** (Inspired by `MixxxApplication`): Main app class. Initializes SoundManager/EngineMixer.

## **Solid Execution Plan**
1. **Phase 1: Research & Planning (Current)** - Complete analysis (this document). Output: Updated FOCUS.md.
2. **Phase 2: Fork & Setup** - Git fork to "Rizz" branch. Add PortAudio.NET via NuGet.
3. **Phase 3: Core Audio Rewrite** - Implement SoundManager, EngineMixer, EngineBuffer. Test basic playback.
4. **Phase 4: File I/O Overhaul** - Add CachingReader. Migrate from NAudio readers.
5. **Phase 5: Multi-Threading & Optimizations** - Integrate EngineWorkerScheduler, GPU effects.
6. **Phase 6: UI/Options Integration** - Build tabbed options menu, load assets.
7. **Phase 7: Improvements & Testing** - Add AI mixing, plugin system. Full regression testing.
8. **Phase 8: Polish & Release** - Performance tuning, documentation.

**Timeline**: 4-6 weeks for core; 2-3 weeks for features/options. **Risks**: PortAudio.NET may have bugs; mitigate with extensive testing. **Success Criteria**: ASIO systray appears reliably; 50% faster mixing; full customization.

This plan positions Rizz as a superior, Mixxx-inspired DJ app with our unique twists. Once READ-ONLY lifts, we can execute.