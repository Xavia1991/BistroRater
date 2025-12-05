@echo off
title Starting Docker Environment...

echo Checking if Docker Desktop is running...
tasklist /FI "IMAGENAME eq Docker Desktop.exe" | find /I "Docker Desktop.exe" >NUL

if %ERRORLEVEL% NEQ 0 (
    echo Docker Desktop is not running. Launching it now...
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
) else (
    echo Docker Desktop is already running.
)

echo.
echo Waiting for Docker Engine to become available...

:waitloop
docker info >NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Docker is still starting...
    timeout /t 3 >NUL
    goto waitloop
)

echo Docker Engine is running!
echo.

echo Starting docker-compose stack...
docker compose up --build -d

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =====================================
    echo   Docker Stack Started Successfully  
    echo =====================================
    echo.
) else (
    echo ERROR: docker-compose failed!
)

echo Showing running containers:
docker ps

echo.
pause
