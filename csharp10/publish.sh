#!/bin/bash

dotnet publish -c Release -r osx.11.0-arm64 --self-contained --nologo -o publish/mac -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -r linux-x64 --self-contained --nologo -o publish/linux -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish -c Release -r win10-x64 --self-contained --nologo -o publish/windows -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
