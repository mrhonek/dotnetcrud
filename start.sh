#!/bin/bash

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
  echo "Starting web application"
  dotnet ASPNETCRUD.API.dll
fi 