#!/bin/bash

# Check if this is a cron job run
if [ "$RAILWAY_CRON_JOB" = "true" ]; then
  # Execute the database reset command when running as a cron job
  echo "Starting database reset (cron job)"
  dotnet ASPNETCRUD.API.dll --reset-database
  exit $?
else
  # Normal startup for the web application
  echo "Starting web application"
  dotnet ASPNETCRUD.API.dll
fi 