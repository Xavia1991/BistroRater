@echo off
echo Starting PostgreSQL container...
docker compose -f docker-compose-postgres.yml up -d
echo PostgreSQL is now running on port 5432.
pause