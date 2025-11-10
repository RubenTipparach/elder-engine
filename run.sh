#!/bin/bash

# Elder Engine - Run Script
# Simple script to build and run the Elder Engine project

set -e

echo "Building Elder Engine..."
dotnet build

echo ""
echo "Running Elder Engine..."
dotnet run
