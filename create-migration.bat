@echo off
REM Script to create EF Core migration for CarbonFootprint entities
REM You need to run this after closing and reopening your terminal

cd tibg-sport-backend
dotnet ef migrations add AddCarbonFootprintEntities --context FytAiDbContext --project ..\TIBG.ENTITIES\TIBG.ENTITIES.csproj --startup-project .

echo.
echo Migration created! To apply it to the database, run:
echo dotnet ef database update
pause
