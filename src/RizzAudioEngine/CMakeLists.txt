cmake_minimum_required(VERSION 3.10)
project(RizzAudioEngine VERSION 1.0.0 LANGUAGES CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find PortAudio
set(PORTAUDIO_INCLUDE_DIRS "/mnt/c/users/rogue/code/djmixmaster/ref/portaudio-src/include")
set(PORTAUDIO_LIBRARIES "/mnt/c/users/rogue/code/djmixmaster/ref/portaudio-src/build/libportaudio.dll")

# Include directories
include_directories(${PORTAUDIO_INCLUDE_DIRS})

# Source files
set(SOURCES
    ShredEngine.cpp
    ScratchBuffer.cpp
    ClubMixer.cpp
    Selekta.cpp
)

# Header files
set(HEADERS
    ShredEngine.h
    ScratchBuffer.h
    ClubMixer.h
    Selekta.h
)

# Create DLL
add_library(ShredEngine SHARED ${SOURCES})

# Link libraries
target_link_libraries(ShredEngine ${PORTAUDIO_LIBRARIES})

# Compiler flags
if(MSVC)
    target_compile_options(ShredEngine PRIVATE /W4)
else()
    target_compile_options(ShredEngine PRIVATE -Wall -Wextra)
    target_link_options(ShredEngine PRIVATE -static-libgcc -static-libstdc++)
endif()

# Install
install(TARGETS ShredEngine
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)