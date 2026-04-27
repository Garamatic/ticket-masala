#!/bin/bash

# Configuration
API_URL="http://localhost:5000/api/ingest"
API_KEY="masala-test-key" # This matches what we'll put in appsettings.json

echo "🚀 Starting GatekeeperApi Ingestion Test..."

# 1. Test missing API Key
echo -e "\n1. Testing Unauthorized access (missing key)..."
curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL \
  -H "Content-Type: application/json" \
  -d '{"Template": "test", "Data": {"subject": "Test Ticket"}}' | grep -q "401" && echo "✅ Correctly returned 401 Unauthorized" || echo "❌ Failed: Expected 401 Unauthorized"

# 2. Test invalid JSON
echo -e "\n2. Testing Malformed JSON..."
curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"Template": "test", "Data": {"subject": "Test Ticket"}' | grep -q "400" && echo "✅ Correctly returned 400 Bad Request" || echo "❌ Failed: Expected 400 Bad Request"

# 3. Test valid ingestion
echo -e "\n3. Testing Successful ingestion..."
curl -s -i -X POST $API_URL \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "Template": "github-webhook",
    "Data": {
      "email": "juan@example.com",
      "name": "Juan Test",
      "subject": "Bug found in production",
      "description": "The app crashes when clicking the button."
    }
  }' | grep -q "HTTP/1.1 202" && echo "✅ Correctly returned 202 Accepted" || echo "❌ Failed: Expected 202 Accepted"

# 4. Test missing required fields (validation)
echo -e "\n4. Testing Validation (missing data)..."
curl -s -o /dev/null -w "%{http_code}" -X POST $API_URL \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"Template": "test", "Data": {}}' | grep -q "400" && echo "✅ Correctly returned 400 Bad Request (empty data)" || echo "❌ Failed: Expected 400 Bad Request"

echo -e "\n🏁 Test finished."
