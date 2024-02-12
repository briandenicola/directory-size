@echo off

REM Cross-OS native compilation is not supported
REM dotnet publish -c Release -r osx-arm64 --self-contained --nologo -o publish/mac
REM dotnet publish -c Release -r linux-x64 --self-contained --nologo -o publish/linux

REM Requires: Visual Studio 2022, including the Desktop development with C++ workload with all default components.
dotnet publish -c Release -r win-x64 --self-contained --nologo -o publish/windows