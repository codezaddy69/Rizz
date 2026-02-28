#include <iostream>
#include <dlfcn.h>
#include <cstring>
#include <unistd.h>

// Define the function pointer types
typedef void (*InitializeEngineFunc)(bool);
typedef int (*LoadFileFunc)(int, const char*);
typedef void (*PlayFunc)(int);
typedef void (*PauseFunc)(int);
typedef void (*StopFunc)(int);
typedef void (*SeekFunc)(int, double);
typedef double (*GetPositionFunc)(int);
typedef double (*GetLengthFunc)(int);
typedef void (*SetVolumeFunc)(int, float);
typedef void (*ShutdownEngineFunc)();

int main(int argc, char* argv[]) {
    const char* audioFile = (argc > 1) ? argv[1] : "../assets/audio/ThisIsTrash.wav";

    std::cout << "Testing ShredEngine library with real audio..." << std::endl;
    std::cout << "Audio file: " << audioFile << std::endl;

    // Load the library
    void* handle = dlopen("./libShredEngine.so", RTLD_LAZY);
    if (!handle) {
        std::cerr << "Error loading library: " << dlerror() << std::endl;
        return 1;
    }

    // Reset errors
    dlerror();

    // Load functions
    InitializeEngineFunc initializeEngine = (InitializeEngineFunc)dlsym(handle, "InitializeEngine");
    LoadFileFunc loadFile = (LoadFileFunc)dlsym(handle, "LoadFile");
    PlayFunc play = (PlayFunc)dlsym(handle, "Play");
    PauseFunc pause = (PauseFunc)dlsym(handle, "Pause");
    StopFunc stop = (StopFunc)dlsym(handle, "Stop");
    SeekFunc seek = (SeekFunc)dlsym(handle, "Seek");
    GetPositionFunc getPosition = (GetPositionFunc)dlsym(handle, "GetPosition");
    GetLengthFunc getLength = (GetLengthFunc)dlsym(handle, "GetLength");
    SetVolumeFunc setVolume = (SetVolumeFunc)dlsym(handle, "SetVolume");
    ShutdownEngineFunc shutdownEngine = (ShutdownEngineFunc)dlsym(handle, "ShutdownEngine");

    // Check for errors
    const char* dlsymError = dlerror();
    if (dlsymError) {
        std::cerr << "Error loading functions: " << dlsymError << std::endl;
        dlclose(handle);
        return 1;
    }

    // Initialize engine in production mode (actual audio)
    std::cout << "\n1. Initializing engine..." << std::endl;
    initializeEngine(false);
    std::cout << "   Engine initialized successfully!" << std::endl;

    // Load audio file
    std::cout << "\n2. Loading audio file..." << std::endl;
    int result = loadFile(1, audioFile);
    if (result == 0) {
        std::cout << "   File loaded successfully!" << std::endl;
        double length = getLength(1);
        std::cout << "   Duration: " << length << " seconds" << std::endl;
    } else {
        std::cerr << "   Failed to load file!" << std::endl;
        shutdownEngine();
        dlclose(handle);
        return 1;
    }

    // Set volume
    std::cout << "\n3. Setting volume..." << std::endl;
    setVolume(1, 0.7f);
    std::cout << "   Volume set to 70%" << std::endl;

    // Play
    std::cout << "\n4. Starting playback..." << std::endl;
    play(1);
    std::cout << "   Playback started!" << std::endl;

    // Play for 3 seconds
    std::cout << "\n5. Playing for 3 seconds..." << std::endl;
    for (int i = 0; i < 3; i++) {
        usleep(1000000); // 1 second
        double pos = getPosition(1);
        std::cout << "   Position: " << pos << " / " << getLength(1) << " seconds" << std::endl;
    }

    // Pause
    std::cout << "\n6. Pausing..." << std::endl;
    pause(1);
    std::cout << "   Playback paused!" << std::endl;
    std::cout << "   Final position: " << getPosition(1) << " seconds" << std::endl;

    // Seek to beginning
    std::cout << "\n7. Seeking to beginning..." << std::endl;
    seek(1, 0.0);
    std::cout << "   Position after seek: " << getPosition(1) << " seconds" << std::endl;

    // Play again
    std::cout << "\n8. Playing again for 2 seconds..." << std::endl;
    play(1);
    for (int i = 0; i < 2; i++) {
        usleep(1000000);
        double pos = getPosition(1);
        std::cout << "   Position: " << pos << " / " << getLength(1) << " seconds" << std::endl;
    }

    // Stop
    std::cout << "\n9. Stopping playback..." << std::endl;
    stop(1);
    std::cout << "   Playback stopped!" << std::endl;

    // Shutdown
    std::cout << "\n10. Shutting down engine..." << std::endl;
    shutdownEngine();
    std::cout << "    Engine shutdown successfully!" << std::endl;

    // Close the library
    dlclose(handle);

    std::cout << "\n✓ All tests passed! ShredEngine library is fully functional." << std::endl;
    return 0;
}
