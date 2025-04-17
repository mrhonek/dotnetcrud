#!/bin/bash

# Script to reset demo data - intended to be run as a cron job in Railway
# Updated for better async handling and logging

# Get the API URL from environment or use default
API_URL=${API_URL:-"https://aspnetcrud-production.up.railway.app"}
API_KEY=${DEMO_RESET_API_KEY:-"demo-reset-key-2024"}

echo "Starting demo data reset process..."
echo "API URL: $API_URL"

# Call the asynchronous reset endpoint instead of the synchronous one
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
  "$API_URL/api/Admin/cron-reset")

if [ "$RESPONSE" -eq 200 ]; then
  echo "Demo data reset initiated successfully!"
  exit 0
else
  echo "Error initiating demo data reset. HTTP status: $RESPONSE"
  exit 1
fi 