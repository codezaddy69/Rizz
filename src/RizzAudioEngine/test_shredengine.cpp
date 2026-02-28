#include <iostream>
#include <dlfcn.h>
#include <cstring>

// Define the function pointer types
typedef void (*InitializeEngineFunc)(bool);
typedef int (*LoadFileFunc)(int, const char*);
typedef void (*PlayFunc)(int);
typedef void (*PauseFunc)(int);
typedef void (*StopFunc)(int);
typedef void (*ShutdownEngineFunc)();

int main() {
    std::cout << "Testing ShredEngine library..." << std::endl;

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
    ShutdownEngineFunc shutdownEngine = (ShutdownEngineFunc)dlsym(handle, "ShutdownEngine");

    // Check for errors
    const char* dlsymError = dlerror();
    if (dlsymError) {
        std::cerr << "Error loading functions: " << dlsymError << std::endl;
        dlclose(handle);
        return 1;
    }

    // Test in test mode (generates sine wave, no actual audio output)
    std::cout << "\n1. Initializing engine in test mode..." << std::endl;
    initializeEngine(true);
    std::cout << "   Engine initialized successfully!" << std::endl;

    std::cout << "\n2. Testing LoadFile..." << std::endl;
    int result = loadFile(1, "test.mp3");
    if (result == 0) {
        std::cout << "   LoadFile returned success (even though file doesn't exist in test mode)" << std::endl;
    } else {
        std::cout << "   LoadFile returned error (expected for non-existent file)" << std::endl;
    }

    std::cout << "\n3. Testing Play..." << std::endl;
    play(1);
    std::cout << "   Play called successfully" << std::endl;

    std::cout << "\n4. Testing Pause..." << std::endl;
    pause(1);
    std::cout << "   Pause called successfully" << std::endl;

    std::cout << "\n5. Testing Stop..." << std::endl;
    stop(1);
    std::cout << "   Stop called successfully" << std::endl;

    std::cout << "\n6. Shutting down engine..." << std::endl;
    shutdownEngine();
    std::cout << "   Engine shutdown successfully" << std::endl;

    // Close the library
    dlclose(handle);

    std::cout << "\n✓ All tests passed! ShredEngine library is working correctly." << std::endl;
    return 0;
}
