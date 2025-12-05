@echo off
title Stopping Docker Environment...

echo Stopping docker-compose stack...
docker compose down

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =====================================
    echo   Docker Stack Stopped Successfully  
    echo =====================================
    echo.
) else (
    echo ERROR: docker-compose down failed!
)

echo Showing remaining running containers:
docker ps

echo.
pause