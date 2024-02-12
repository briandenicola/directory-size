#!/bin/bash

# Cross-OS native compilation is not supported
#dotnet publish -c Release -r osx-arm64 --self-contained --nologo -o publish/mac
#dotnet publish -c Release -r win-x64 --self-contained --nologo -o publish/windows

#Requires: sudo apt-get install clang zlib1g-dev
dotnet publish -c Release -r linux-x64 --self-contained --nologo -o publish/linux