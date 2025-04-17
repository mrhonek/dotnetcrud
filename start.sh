#!/bin/bash

# Added debug logging to help troubleshoot
echo "Starting script with environment: ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT"
echo "RAILWAY_CRON_JOB is set to: ${RAILWAY_CRON_JOB:-not set}"

# Check if this is a cron job run
if [ "$RAILWAY_CRON_JOB" = "true" ]; then
  # Execute the database reset by calling the API endpoint
  echo "Starting database reset via API endpoint (cron job)"
  
  # Start the API in the background
  dotnet ASPNETCRUD.API.dll &
  API_PID=$!
  
  # Wait for API to start
  echo "Waiting for API to start..."
  sleep 10
  
  # Call the endpoint
  echo "Calling reset endpoint..."
  curl -X POST http://localhost:8000/api/Admin/cron-reset
  
  # Get the result
  RESULT=$?
  
  # Kill the API process
  kill $API_PID
  
  # Return the result
  echo "Reset process completed with result: $RESULT"
  exit $RESULT
else
  # Normal startup for the web application
  echo "Starting web application normally"
  exec dotnet ASPNETCRUD.API.dll
fi 