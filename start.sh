#!/bin/bash
# start.sh - Simple startup script for the .NET application

set -e

echo "Starting Screen Supplier API..."

# Get database connection details from AWS Secrets Manager
DB_SECRET=$(aws secretsmanager get-secret-value --region af-south-1 --secret-id prod/postgres-screen-supplier --query SecretString --output text)
DB_HOST=$(aws secretsmanager get-secret-value --region af-south-1 --secret-id screen-supplier-rds-host --query SecretString --output text)

# Parse the database credentials
DB_USERNAME=$(echo $DB_SECRET | jq -r '.db_username')
DB_PASSWORD=$(echo $DB_SECRET | jq -r '.db_password')

# Set environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://localhost:5000
export ConnectionStrings__DefaultConnection="Host=$DB_HOST;Port=5432;Database=ScreenProducerDb;User Id=$DB_USERNAME;Password=$DB_PASSWORD;"

# External service URLs - these can be hardcoded since they're public
export ExternalServices__CommercialBank__BaseUrl="https://commercial-bank-api.projects.bbdgrad.com"
export ExternalServices__BulkLogistics__BaseUrl="https://bulk-logistics-api.projects.bbdgrad.com"
export ExternalServices__Hand__BaseUrl="https://hand-api.projects.bbdgrad.com"
export ExternalServices__Recycler__BaseUrl="https://recycler-api.projects.bbdgrad.com"
export ExternalServices__Suppliers__HandBaseUrl="https://hand-api.projects.bbdgrad.com"
export ExternalServices__Suppliers__RecyclerBaseUrl="https://recycler-api.projects.bbdgrad.com"
export BankSettings__NotificationUrl="https://screen-supplier-api.projects.bbdgrad.com/payment"

# Kill any existing process
pkill -f ScreenProducerAPI || true

# Wait a moment for cleanup
sleep 2

# Start the application in the background
cd $HOME/build
echo "Starting application..."
nohup ./ScreenProducerAPI > app.log 2>&1 &

# Save the process ID
echo $! > app.pid

echo "Application started with PID $(cat app.pid)"