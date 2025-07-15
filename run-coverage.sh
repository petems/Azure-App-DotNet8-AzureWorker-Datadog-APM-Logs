#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Running tests with code coverage...${NC}"

# Clean previous test results
rm -rf AzureAppServiceSample.Tests/TestResults/
rm -rf coverage-report/

# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Check if tests passed
if [ $? -eq 0 ]; then
    echo -e "${GREEN}Tests passed! Generating coverage report...${NC}"
    
    # Find the latest coverage file
    COVERAGE_FILE=$(find ./TestResults -name "coverage.cobertura.xml" -type f | sort -r | head -1)
    
    if [ -f "$COVERAGE_FILE" ]; then
        echo -e "${YELLOW}Coverage file found: $COVERAGE_FILE${NC}"
        
        # Generate HTML report
        reportgenerator -reports:"$COVERAGE_FILE" -targetdir:"coverage-report" -reporttypes:"Html;HtmlSummary"
        
        echo -e "${GREEN}Coverage report generated in coverage-report/index.html${NC}"
        echo -e "${YELLOW}You can open it with: open coverage-report/index.html${NC}"
        
        # Display summary
        echo -e "${GREEN}Coverage Summary:${NC}"
        reportgenerator -reports:"$COVERAGE_FILE" -targetdir:"coverage-report" -reporttypes:"TextSummary"
        cat coverage-report/Summary.txt
        
    else
        echo -e "${RED}Coverage file not found!${NC}"
        exit 1
    fi
else
    echo -e "${RED}Tests failed!${NC}"
    exit 1
fi 