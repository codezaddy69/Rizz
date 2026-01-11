<system-reminder>
Your operational mode has changed from plan to build.
You are no longer in read-only mode.
You are permitted to make file changes, run shell commands, and utilize your arsenal of tools as needed.
</system-reminder>

# MAUI UI Build Guide (Windows First)
- Use dotnet.exe to build and run the MAUI UI on Windows.
- A patch branch (feat/maui-windows-first-wired) should be used for wiring to CoreDjEngine.
- The patch includes SharedUI for models/viewmodels separation, and a Windows MAUI front-end mirroring the WPF form.
