#!/usr/bin/env bash
set -e

# Package Tepi for Linux Debian ARM64
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

if [ -d "$ROOT_DIR/linux/publish-arm64" ]; then
    BUILD_DIR="$ROOT_DIR/linux/publish-arm64"
else
    BUILD_DIR="$ROOT_DIR/linux/bin/Release/net8.0/linux-arm64/publish"
fi

PKG_ROOT="$ROOT_DIR/linux/packaging/tepi_arm64"
OUT_DEB="$ROOT_DIR/tepi_2.6.3_arm64.deb"

echo "Building Debian package for arm64..."
echo "Using build output from: $BUILD_DIR"

if [ ! -d "$BUILD_DIR" ]; then
    echo "Error: Build directory not found: $BUILD_DIR"
    exit 1
fi

# Clean previous package directory
rm -rf "$PKG_ROOT"
mkdir -p "$PKG_ROOT/DEBIAN"
mkdir -p "$PKG_ROOT/usr/bin"
mkdir -p "$PKG_ROOT/opt/tepi"
mkdir -p "$PKG_ROOT/usr/share/applications"
mkdir -p "$PKG_ROOT/usr/share/icons/hicolor/256x256/apps"

# 1. Copy control file
cp "$ROOT_DIR/linux/packaging/DEBIAN/control" "$PKG_ROOT/DEBIAN/control"

# 2. Copy entire publish output to /opt/tepi
echo "Copying published files to /opt/tepi..."
cp -r "$BUILD_DIR"/* "$PKG_ROOT/opt/tepi/"
chmod 0755 "$PKG_ROOT/opt/tepi/Tepi.Linux"

# 3. Create wrapper script in /usr/bin/tepi with PRoot CoreCLR memory guards
cat << 'EOF' > "$PKG_ROOT/usr/bin/tepi"
#!/bin/sh
export DOTNET_GCHeapHardLimit=20000000
export DOTNET_gcServer=0
export DOTNET_EnableWriteXorExecute=0
export LD_LIBRARY_PATH="/opt/tepi:${LD_LIBRARY_PATH}"
cd /opt/tepi
exec /opt/tepi/Tepi.Linux "$@"
EOF
chmod 0755 "$PKG_ROOT/usr/bin/tepi"

# 4. Copy desktop entry
cp "$ROOT_DIR/linux/packaging/tepi.desktop" "$PKG_ROOT/usr/share/applications/tepi.desktop"

# 5. Icon placeholder / assets
touch "$PKG_ROOT/usr/share/icons/hicolor/256x256/apps/tepi.png"

# 6. Set correct permissions for Debian package directories
chmod -R 0755 "$PKG_ROOT/DEBIAN"

# 7. Build the .deb
dpkg-deb --build --root-owner-group "$PKG_ROOT" "$OUT_DEB"

echo "Debian package successfully created: $OUT_DEB"
