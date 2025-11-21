# Log Policy for DJMixMaster

## Overview
This policy governs logging and error handling across all components in DJMixMaster. The goal is to provide clear, non-flooding logs for debugging while ensuring robust error handling. All new components and functions must adhere to this policy.

## Core Principles
- **Separation**: Console for critical events; files for detailed debugging.
- **Performance**: Avoid per-frame or high-frequency logs.
- **Consistency**: Use prefixed logs (e.g., "[Component] Message") with timestamps for critical.
- **Error Handling**: Try-catch on every function, logging exceptions with context.

## Console Logs (Critical)
- **Purpose**: Real-time monitoring of key events.
- **What to Log**:
  - Initialization success/failure.
  - Major state changes (play, pause, load).
  - Errors that halt operation.
- **Format**: "[Component] Message" (e.g., "[ShredEngine] File loaded successfully").
- **When**: On key events, not in loops.
- **Examples**:
  - "[Selekta] PortAudio initialized"
  - "[ShredEngine] Failed to load file on deck 1"

## File Logs (Detailed)
- **Purpose**: In-depth debugging without cluttering console.
- **What to Log**:
  - Internal processing, parameters, states (e.g., device enumeration, parsing).
- **Format**: Plain text to component-specific files (e.g., "shredengine_debug.log").
- **When**: On detailed operations, sampled if frequent.
- **Examples**:
  - "Device 5: ASUS (out: 2)"
  - "Fmt size: 28, Chunk: data, size: 6289384"

## Error Handling and Try-Catch
- **Requirement**: Every component and function must wrap operations in try-catch.
- **Logging**: Log exceptions with context (function, parameters, error type).
- **Format**: "Exception in [Function]: [Message] at [Context]"
- **Recovery**: Log and continue if possible; throw for critical errors.
- **Examples**:
  - C++: `try { ... } catch (std::exception& e) { std::cout << "[Component] Exception in " << __FUNCTION__ << ": " << e.what() << std::endl; }`
  - C#: `try { ... } catch (Exception e) { _logger.LogError(e, "Exception in {Method}", nameof(Method)); }`

## Implementation Rules
- **C++**: Use std::exception, log critical to cout, detailed to ofstream.
- **C#**: Use try-catch, log to ILogger (console) and files.
- **Files**: One per component (e.g., ShredEngine.log).
- **Testing**: Verify logs show full error context.
- **Updates**: Modify existing code to comply; new code must follow from start.

## Compliance Checklist
- [ ] Try-catch on all functions.
- [ ] Critical logs to console.
- [ ] Detailed logs to files.
- [ ] No per-frame console logs.
- [ ] Error context in logs.

## References
- AGENTS.md: High-level SOP.
- Update AGENTS.md to reference this doc.