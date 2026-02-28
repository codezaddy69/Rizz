cmake_minimum_required(VERSION 3.10)
project(RizzAudioEngine VERSION 1.0.0 LANGUAGES CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# PortAudio configuration (local build)
set(PORTAUDIO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../external/portaudio/build")
set(PORTAUDIO_INCLUDE_DIRS "${PORTAUDIO_ROOT}/include")
set(PORTAUDIO_LIBRARIES "${PORTAUDIO_ROOT}/lib/libportaudio.so")

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

# Create shared library
add_library(ShredEngine SHARED ${SOURCES})

# Link libraries
target_link_libraries(ShredEngine ${PORTAUDIO_LIBRARIES} -lpthread -lrt -lm)

# Compiler flags
if(MSVC)
    target_compile_options(ShredEngine PRIVATE /W4)
else()
    target_compile_options(ShredEngine PRIVATE -Wall -Wextra -fPIC)
    target_link_options(ShredEngine PRIVATE -static-libgcc -static-libstdc++)
endif()

# Install
install(TARGETS ShredEngine
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)
