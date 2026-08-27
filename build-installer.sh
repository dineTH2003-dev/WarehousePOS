#!/usr/bin/env bash
set -e

echo "=========================================================="
echo "   WarehousePOS — Standalone Release Build & Publish"
echo "=========================================================="

DOTNET_CMD="$HOME/.dotnet/dotnet"
if ! command -v "$DOTNET_CMD" &> /dev/null; then
    DOTNET_CMD="dotnet"
fi

echo "1. Running full test suite..."
"$DOTNET_CMD" test --configuration Release

echo ""
echo "2. Cleaning previous output distribution folder..."
rm -rf dist/win-x64

echo ""
echo "3. Publishing self-contained win-x64 release bundle..."
"$DOTNET_CMD" publish src/WarehousePOS.Desktop/WarehousePOS.Desktop.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o dist/win-x64

echo ""
echo "=========================================================="
echo " SUCCESS: Self-Contained Release Published!"
echo " Location: $(pwd)/dist/win-x64/"
echo " Main EXE: $(pwd)/dist/win-x64/WarehousePOS.Desktop.exe"
echo ""
echo " To build the Windows Setup Installer (.exe):"
echo " Open Inno Setup Compiler on Windows and compile:"
echo "   $(pwd)/installer/WarehousePOS_Setup.iss"
echo "=========================================================="
