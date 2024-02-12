#!/bin/bash

# Cross-OS native compilation is not supported
#dotnet publish -c Release -r win-x64 --self-contained --nologo -o publish/windows
#dotnet publish -c Release -r linux-x64 --self-contained --nologo -o publish/linux

#Requires: The latest Command Line Tools for XCode. Supported on .NET 8 and later versions.
dotnet publish -c Release -r osx-arm64 --self-contained --nologo -o publish/mac