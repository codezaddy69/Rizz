#!/bin/bash
# Build script for ShredEngine on Linux

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
EXTERNAL_DIR="$PROJECT_ROOT/external"

echo "=========================================="
echo "Building ShredEngine for Linux"
echo "=========================================="

# Step 1: Build PortAudio
echo ""
echo "[1/3] Building PortAudio..."
if [ ! -d "$EXTERNAL_DIR/portaudio/build" ]; then
    echo "PortAudio not found. Downloading and building..."
    mkdir -p "$EXTERNAL_DIR"
    cd "$EXTERNAL_DIR"

    if [ ! -f "pa_stable_v190700_20210406.tgz" ]; then
        echo "Downloading PortAudio..."
        wget -q http://files.portaudio.com/archives/pa_stable_v190700_20210406.tgz
    fi

    echo "Extracting PortAudio..."
    tar -xzf pa_stable_v190700_20210406.tgz
    cd portaudio

    echo "Configuring PortAudio..."
    ./configure --prefix="$(pwd)/build" --disable-static --enable-shared --quiet

    echo "Building PortAudio..."
    make -j4 --quiet

    echo "Installing PortAudio..."
    make install --quiet
else
    echo "PortAudio already built. Skipping."
fi

# Step 2: Build ShredEngine
echo ""
echo "[2/3] Building ShredEngine..."
cd "$SCRIPT_DIR"
make clean
make

# Step 3: Install
echo ""
echo "[3/3] Installing ShredEngine..."
make install

echo ""
echo "=========================================="
echo "Build complete!"
echo "=========================================="
echo ""
echo "Output files:"
echo "  - $PROJECT_ROOT/bin/libShredEngine.so"
echo "  - $PROJECT_ROOT/bin/libportaudio.so.2"
echo ""
echo "To test the library:"
echo "  cd $PROJECT_ROOT/bin"
echo "  ./test_shredengine"
echo ""
