cmake_minimum_required(VERSION 3.10)
project(RizzAudioEngine VERSION 1.0.0 LANGUAGES CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find PortAudio
find_package(PkgConfig REQUIRED)
pkg_check_modules(PORTAUDIO REQUIRED portaudio-2.0)

# Include directories
include_directories(${PORTAUDIO_INCLUDE_DIRS})

# Source files
set(SOURCES
    ShredEngine.cpp
    ScratchBuffer.cpp
    BeatSource.cpp
    ClubMixer.cpp
    Selekta.cpp
)

# Header files
set(HEADERS
    ShredEngine.h
    ScratchBuffer.h
    BeatSource.h
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
endif()

# Install
install(TARGETS ShredEngine
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)