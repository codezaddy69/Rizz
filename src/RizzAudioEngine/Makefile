# Makefile for ShredEngine (Linux build)
# Build shared library (.so) for Linux

CXX = g++
CXXFLAGS = -std=c++17 -Wall -Wextra -fPIC -O2
LDFLAGS = -shared -Wl,-soname,libShredEngine.so

# PortAudio paths
PORTAUDIO_ROOT = ../../external/portaudio/build
PORTAUDIO_INCLUDE = $(PORTAUDIO_ROOT)/include
PORTAUDIO_LIB = $(PORTAUDIO_ROOT)/lib

# Source files
SOURCES = ShredEngine.cpp ScratchBuffer.cpp ClubMixer.cpp Selekta.cpp
OBJECTS = $(SOURCES:.cpp=.o)

# Output
TARGET = libShredEngine.so
INSTALL_DIR = ../../bin

# Default target
all: $(TARGET)

# Build shared library
$(TARGET): $(OBJECTS)
	$(CXX) $(LDFLAGS) -Wl,-rpath,'$$ORIGIN' -o $@ $(OBJECTS) -L$(PORTAUDIO_LIB) -lportaudio -lpthread -lrt -lm
	@echo "Built $(TARGET) successfully"

# Compile source files
%.o: %.cpp
	$(CXX) $(CXXFLAGS) -I$(PORTAUDIO_INCLUDE) -c $< -o $@

# Install
install: $(TARGET)
	mkdir -p $(INSTALL_DIR)
	cp $(TARGET) $(INSTALL_DIR)/
	@echo "Installed $(TARGET) to $(INSTALL_DIR)"

# Clean
clean:
	rm -f $(OBJECTS) $(TARGET)

# Phony targets
.PHONY: all install clean
