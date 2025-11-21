#!/bin/bash
echo "🚀 Starting Remote Deployment on AWS Workspace..."

# 1. Pull latest changes
git pull

# 2. Clean & Rebuild
echo "🧹 Cleaning old containers..."
docker-compose down --remove-orphans

echo "🏗️  Building and Starting Stack..."
docker-compose up --build -d

# 3. Validation
echo "🔍 Checking Logs for Database Connection..."
sleep 10
docker-compose logs auth-server | grep -i "listening"

echo "✅ Stack is UP. Access WebApp at http://localhost:5002"
