# Test Infrastructure Improvement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the test infrastructure from basic xUnit setup to a comprehensive, fast, reliable testing ecosystem with 80%+ coverage, CI/CD integration, and excellent developer experience.

**Architecture:** 
- Multi-project test suite (Domain, Web, Integration) with clear boundaries
- Coverlet + ReportGenerator for coverage with visual reports
- Collection-based parallelization for speed without sacrificing isolation
- Test containers for database integration tests
- Fixture + Factory hybrid for test data

**Tech Stack:** xUnit, Coverlet, ReportGenerator, TestContainers, Bogus, Moq, FluentAssertions, NSubstitute (replacing Moq for better syntax)

---

## Current State Analysis

**Existing:**
- 278 tests (275 passing, 3 failing in LoginCreateVerifyFlowTests)
- xUnit with Moq, FluentAssertions, Bogus, FsCheck
- SQLite in-memory for database tests
- WebApplicationFactory for integration tests
- Basic architecture tests (NetArchTest)

**Gaps:**
- No coverage reporting or thresholds
- No TicketMasala.Domain.Tests project
- No CI test execution
- No test parallelization configured
- Test data scattered, no unified approach
- 3 flaky tests in LoginCreateVerifyFlowTests

---

## File Structure Changes

```
src/
├── TicketMasala.Domain.Tests/           # NEW - Domain layer tests
│   ├── Entities/                        # Domain entity tests
│   ├── ValueObjects/                    # Value object tests
│   ├── Specifications/                  # Business rule tests
│   └── TicketMasala.Domain.Tests.csproj
├── TicketMasala.Tests/                  # EXISTING - Web layer tests
│   ├── xunit.runner.json                # NEW - Parallelization config
│   ├── coverlet.runsettings             # NEW - Coverage settings
│   └── TicketMasala.Tests.csproj        # MODIFIED - Add coverage
├── TestResults/                         # NEW - Coverage reports (gitignored)
└── .github/workflows/
    └── ci.yml                           # NEW - CI with tests + coverage
```

---

## Task 1: Fix Flaky Integration Tests

**Files:**
- Modify: `src/TicketMasala.Tests/IntegrationTests/LoginCreateVerifyFlowTests.cs`

The 3 failing tests are due to form validation failures. The tests expect a redirect but get OK with HTML content indicating validation errors.

- [ ] **Step 1: Investigate the validation requirements**

Read the CreateTicketAsync helper method around line 549 and the CreateTicket form requirements in the web project to understand what fields are required.

- [ ] **Step 2: Fix the CreateTicketAsync helper**

The helper is likely missing required form fields. Update the form data submission to include all required fields:

```csharp
// In LoginCreateVerifyFlowTests.cs around line 540-550
// Find CreateTicketAsync and ensure all required fields are submitted:
var formData = new Dictionary<string, string>
{
    ["Title"] = $"Test Ticket {Guid.NewGuid()}",
    ["Description"] = description,
    ["DomainId"] = "IT",  // Required field that may be missing
    ["__RequestVerificationToken"] = token
};
```

- [ ] **Step 3: Run the failing tests to verify fix**

```bash
dotnet test --filter "FullyQualifiedName~LoginCreateVerifyFlowTests" --verbosity normal
```
Expected: All 3 tests pass

- [ ] **Step 4: Commit**

```bash
git add src/TicketMasala.Tests/IntegrationTests/LoginCreateVerifyFlowTests.cs
git commit -m "test: fix flaky LoginCreateVerifyFlow tests by adding missing form fields"
```

---

## Task 2: Add Coverage Configuration

**Files:**
- Create: `src/TicketMasala.Tests/coverlet.runsettings`
- Modify: `src/TicketMasala.Tests/TicketMasala.Tests.csproj`

- [ ] **Step 1: Create Coverlet runsettings file**

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,json</Format>
          <ExcludeByFile>**/Migrations/**/*.cs,**/obj/**/*.cs</ExcludeByFile>
          <ExcludeByAttribute>GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
          <SkipAutoProps>true</SkipAutoProps>
          <DeterministicReport>false</DeterministicReport>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

- [ ] **Step 2: Add ReportGenerator tool and coverage targets to test project**

Modify `src/TicketMasala.Tests/TicketMasala.Tests.csproj`:

```xml
<!-- Add to existing ItemGroup with PackageReferences -->
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.2">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="coverlet.msbuild" Version="6.0.2">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>

<!-- Add new target for coverage reporting -->
<Target Name="GenerateCoverageReport" AfterTargets="GenerateCoverageResultAfterTest">
  <Exec Command="dotnet tool run reportgenerator -reports:$(CoverletOutput)coverage.cobertura.xml -targetdir:./TestResults/CoverageReport -reporttypes:Html" 
        Condition="Exists('$(CoverletOutput)coverage.cobertura.xml')" />
</Target>
```

- [ ] **Step 3: Add ReportGenerator to dotnet-tools.json**

Modify `dotnet-tools.json`:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "10.0.0",
      "commands": ["dotnet-ef"],
      "rollForward": false
    },
    "dotnet-reportgenerator": {
      "version": "5.4.1",
      "commands": ["reportgenerator"],
      "rollForward": false
    }
  }
}
```

- [ ] **Step 4: Restore tools and test coverage**

```bash
dotnet tool restore
dotnet test src/TicketMasala.Tests/TicketMasala.Tests.csproj --collect:"XPlat Code Coverage" --settings src/TicketMasala.Tests/coverlet.runsettings
```

Expected: Test run completes, coverage file generated at `src/TicketMasala.Tests/TestResults/*/coverage.cobertura.xml`

- [ ] **Step 5: Commit**

```bash
git add src/TicketMasala.Tests/coverlet.runsettings src/TicketMasala.Tests/TicketMasala.Tests.csproj dotnet-tools.json
git commit -m "test: add coverlet coverage reporting with reportgenerator"
```

---

## Task 3: Configure Test Parallelization

**Files:**
- Create: `src/TicketMasala.Tests/xunit.runner.json`
- Modify: `src/TicketMasala.Tests/AssemblyInfo.cs` (create if not exists)

- [ ] **Step 1: Create xunit runner configuration**

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "testCaseOrder": "random",
  "stopOnFail": false
}
```

- [ ] **Step 2: Add assembly-level collection behavior**

Create `src/TicketMasala.Tests/AssemblyInfo.cs`:

```csharp
using Xunit;

// Enable parallelization at assembly level
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, MaxParallelThreads = 4)]

// Disable parallelization for specific test collections that need isolation
// (Database tests already use [Collection("Database")] which enforces sequential within)
```

- [ ] **Step 3: Verify parallelization doesn't break tests**

```bash
dotnet test src/TicketMasala.Tests/TicketMasala.Tests.csproj --verbosity minimal
```
Expected: All 278 tests pass (or 275 if the 3 flaky tests aren't fixed yet)

- [ ] **Step 4: Commit**

```bash
git add src/TicketMasala.Tests/xunit.runner.json src/TicketMasala.Tests/AssemblyInfo.cs
git commit -m "test: enable collection-based test parallelization"
```

---

## Task 4: Create Domain Test Project

**Files:**
- Create: `src/TicketMasala.Domain.Tests/TicketMasala.Domain.Tests.csproj`
- Create: `src/TicketMasala.Domain.Tests/Entities/TicketTests.cs`
- Create: `src/TicketMasala.Domain.Tests/ValueObjects/PriorityTests.cs`
- Modify: `TicketMasala.sln`

- [ ] **Step 1: Create Domain test project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>TicketMasala.Domain.Tests</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TicketMasala.Domain\TicketMasala.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create Ticket entity tests**

```csharp
using FluentAssertions;
using TicketMasala.Domain.Entities;

namespace TicketMasala.Domain.Tests.Entities;

public class TicketTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTicket()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var title = "Test Ticket";
        var description = "Test Description";

        // Act
        var ticket = new Ticket
        {
            Guid = guid,
            Title = title,
            Description = description,
            DomainId = "IT",
            Status = "New"
        };

        // Assert
        ticket.Guid.Should().Be(guid);
        ticket.Title.Should().Be(title);
        ticket.Description.Should().Be(description);
        ticket.DomainId.Should().Be("IT");
        ticket.Status.Should().Be("New");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_WhenNullOrEmpty_ThrowsArgumentException(string? invalidTitle)
    {
        // Arrange & Act
        var action = () => new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = invalidTitle!,
            DomainId = "IT"
        };

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetStatus_WithValidStatus_UpdatesStatus()
    {
        // Arrange
        var ticket = new Ticket
        {
            Guid = Guid.NewGuid(),
            Title = "Test",
            DomainId = "IT",
            Status = "New"
        };

        // Act
        ticket.Status = "InProgress";

        // Assert
        ticket.Status.Should().Be("InProgress");
    }
}
```

- [ ] **Step 3: Create ValueObject tests**

First, check if Priority is a value object or enum in the Domain project, then create appropriate tests:

```bash
grep -r "class.*Priority" src/TicketMasala.Domain/ || grep -r "enum.*Priority" src/TicketMasala.Domain/
```

Based on findings, create tests in `src/TicketMasala.Domain.Tests/ValueObjects/PriorityTests.cs`.

- [ ] **Step 4: Add Domain.Tests to solution**

```bash
dotnet sln add src/TicketMasala.Domain.Tests/TicketMasala.Domain.Tests.csproj
```

- [ ] **Step 5: Run Domain tests to verify**

```bash
dotnet test src/TicketMasala.Domain.Tests/TicketMasala.Domain.Tests.csproj
```
Expected: Tests pass

- [ ] **Step 6: Commit**

```bash
git add src/TicketMasala.Domain.Tests/ TicketMasala.sln
git commit -m "test: add TicketMasala.Domain.Tests project with entity tests"
```

---

## Task 5: Add CI Workflow with Tests and Coverage

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create CI workflow**

```yaml
name: CI - Build, Test, and Coverage

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
        
    - name: Restore tools
      run: dotnet tool restore
      
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --settings src/TicketMasala.Tests/coverlet.runsettings
      
    - name: Generate Coverage Report
      run: |
        dotnet tool run reportgenerator \
          -reports:"src/TicketMasala.Tests/TestResults/**/coverage.cobertura.xml" \
          -targetdir:"coveragereport" \
          -reporttypes:Html;MarkdownSummaryGithub
          
    - name: Upload Coverage Report
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report
        path: coveragereport
        
    - name: Comment Coverage Summary
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v7
      with:
        script: |
          const fs = require('fs');
          const summary = fs.readFileSync('coveragereport/SummaryGithub.md', 'utf8');
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: '## 📊 Code Coverage Report\n\n' + summary
          });
```

- [ ] **Step 2: Validate workflow syntax**

```bash
cat .github/workflows/ci.yml | head -20
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions workflow with test execution and coverage reporting"
```

---

## Task 6: Create Test Data Builders

**Files:**
- Modify: `src/TicketMasala.Tests/TestHelpers/TestDataBuilder.cs`
- Create: `src/TicketMasala.Tests/Fixtures/Builders/TicketBuilder.cs`
- Create: `src/TicketMasala.Tests/Fixtures/Builders/ProjectBuilder.cs`
- Create: `src/TicketMasala.Tests/Fixtures/Builders/UserBuilder.cs`

- [ ] **Step 1: Create TicketBuilder**

```csharp
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

public class TicketBuilder
{
    private Guid _guid = Guid.NewGuid();
    private string _title = "Test Ticket";
    private string _description = "Test Description";
    private string _domainId = "IT";
    private string _status = "New";
    private Status _ticketStatus = Status.Pending;
    private string? _customerId;
    private string? _responsibleId;
    private int _estimatedEffortPoints = 5;
    private double _priorityScore = 50.0;
    private DateTime _creationDate = DateTime.UtcNow;

    public TicketBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public TicketBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TicketBuilder WithDomain(string domainId)
    {
        _domainId = domainId;
        return this;
    }

    public TicketBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public TicketBuilder WithTicketStatus(Status status)
    {
        _ticketStatus = status;
        return this;
    }

    public TicketBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public TicketBuilder WithResponsible(string responsibleId)
    {
        _responsibleId = responsibleId;
        return this;
    }

    public TicketBuilder WithEffortPoints(int points)
    {
        _estimatedEffortPoints = points;
        return this;
    }

    public TicketBuilder WithPriorityScore(double score)
    {
        _priorityScore = score;
        return this;
    }

    public Ticket Build()
    {
        return new Ticket
        {
            Guid = _guid,
            Title = _title,
            Description = _description,
            DomainId = _domainId,
            Status = _status,
            TicketStatus = _ticketStatus,
            CustomerId = _customerId,
            ResponsibleId = _responsibleId,
            EstimatedEffortPoints = _estimatedEffortPoints,
            PriorityScore = _priorityScore,
            CreationDate = _creationDate,
            CustomFieldsJson = "{}"
        };
    }
}
```

- [ ] **Step 2: Create ProjectBuilder**

```csharp
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

public class ProjectBuilder
{
    private Guid _guid = Guid.NewGuid();
    private string _name = "Test Project";
    private string _description = "Test Project Description";
    private Status _status = Status.InProgress;
    private string? _customerId;
    private string? _projectManagerId;
    private DateTime _creationDate = DateTime.UtcNow;
    private DateTime? _completionTarget = DateTime.UtcNow.AddMonths(3);

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProjectBuilder WithStatus(Status status)
    {
        _status = status;
        return this;
    }

    public ProjectBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public ProjectBuilder WithProjectManager(string projectManagerId)
    {
        _projectManagerId = projectManagerId;
        return this;
    }

    public ProjectBuilder WithCompletionTarget(DateTime target)
    {
        _completionTarget = target;
        return this;
    }

    public Project Build()
    {
        return new Project
        {
            Guid = _guid,
            Name = _name,
            Description = _description,
            Status = _status,
            CustomerId = _customerId,
            ProjectManagerId = _projectManagerId,
            CreationDate = _creationDate,
            CompletionTarget = _completionTarget
        };
    }
}
```

- [ ] **Step 3: Create UserBuilder**

```csharp
using TicketMasala.Domain.Entities;

namespace TicketMasala.Tests.Fixtures.Builders;

public class UserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string _email = $"user_{Guid.NewGuid():N}@test.com";
    private string _userName = $"user_{Guid.NewGuid():N}@test.com";
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _phone = "555-1234";
    private bool _emailConfirmed = true;

    public UserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        _userName = email;
        return this;
    }

    public UserBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithPhone(string phone)
    {
        _phone = phone;
        return this;
    }

    public ApplicationUser BuildCustomer()
    {
        return new ApplicationUser
        {
            Id = _id,
            UserName = _userName,
            Email = _email,
            FirstName = _firstName,
            LastName = _lastName,
            Phone = _phone,
            NormalizedEmail = _email.ToUpperInvariant(),
            NormalizedUserName = _userName.ToUpperInvariant(),
            EmailConfirmed = _emailConfirmed
        };
    }

    public Employee BuildEmployee(EmployeeType level = EmployeeType.Support, string team = "Support")
    {
        return new Employee
        {
            Id = _id,
            UserName = _userName,
            Email = _email,
            FirstName = _firstName,
            LastName = _lastName,
            Phone = _phone,
            Team = team,
            Level = level,
            Language = "EN",
            MaxCapacityPoints = 40,
            NormalizedEmail = _email.ToUpperInvariant(),
            NormalizedUserName = _userName.ToUpperInvariant(),
            EmailConfirmed = _emailConfirmed
        };
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/TicketMasala.Tests/Fixtures/Builders/
git commit -m "test: add fluent test data builders for Ticket, Project, and User entities"
```

---

## Task 7: Create Test Scripts and Documentation

**Files:**
- Create: `scripts/test.sh`
- Create: `scripts/test-coverage.sh`
- Modify: `README.md` (add testing section)

- [ ] **Step 1: Create test script**

```bash
#!/bin/bash
set -e

echo "🧪 Running Ticket Masala Tests..."

# Run all tests with normal verbosity
dotnet test --verbosity normal --no-restore "$@"

echo "✅ All tests passed!"
```

Make it executable: `chmod +x scripts/test.sh`

- [ ] **Step 2: Create coverage test script**

```bash
#!/bin/bash
set -e

echo "📊 Running tests with coverage..."

# Restore tools
dotnet tool restore

# Run tests with coverage collection
dotnet test src/TicketMasala.Tests/TicketMasala.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings src/TicketMasala.Tests/coverlet.runsettings \
  --results-directory ./TestResults

# Find the coverage file
COVERAGE_FILE=$(find ./TestResults -name "coverage.cobertura.xml" | head -1)

if [ -f "$COVERAGE_FILE" ]; then
    echo "📈 Generating coverage report..."
    dotnet tool run reportgenerator \
        -reports:"$COVERAGE_FILE" \
        -targetdir:"./TestResults/CoverageReport" \
        -reporttypes:Html;MarkdownSummary
    
    echo "✅ Coverage report generated at: ./TestResults/CoverageReport/index.html"
else
    echo "❌ Coverage file not found"
    exit 1
fi
```

Make it executable: `chmod +x scripts/test-coverage.sh`

- [ ] **Step 3: Add testing section to README**

Add to `README.md` before the deployment section:

```markdown
## 🧪 Testing

### Running Tests

```bash
# Run all tests
./scripts/test.sh

# Run with coverage report
./scripts/test-coverage.sh

# Run specific test class
dotnet test --filter "FullyQualifiedName~TicketTests"
```

### Test Structure

- **Unit Tests**: Fast, isolated tests using in-memory database
- **Integration Tests**: Full stack tests with WebApplicationFactory
- **Architecture Tests**: Enforce code structure with NetArchTest
- **Domain Tests**: Pure domain logic tests (no infrastructure)

### Coverage

Coverage reports are generated in `TestResults/CoverageReport/`. Open `index.html` to view the detailed report.

```

- [ ] **Step 4: Commit**

```bash
git add scripts/test.sh scripts/test-coverage.sh README.md
git commit -m "docs: add testing scripts and documentation"
```

---

## Task 8: Add Coverage Thresholds and Quality Gates

**Files:**
- Modify: `src/TicketMasala.Tests/TicketMasala.Tests.csproj`
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Add coverage thresholds to test project**

Add to `src/TicketMasala.Tests/TicketMasala.Tests.csproj` inside the PropertyGroup:

```xml
<PropertyGroup>
  <!-- Existing properties... -->
  
  <!-- Coverage Thresholds -->
  <Threshold>80</Threshold>
  <ThresholdType>line,method,class</ThresholdType>
  <ThresholdStat>minimum</ThresholdStat>
</PropertyGroup>
```

Add a target to validate coverage:

```xml
<Target Name="ValidateCoverage" AfterTargets="GenerateCoverageResultAfterTest">
  <PropertyGroup>
    <CoverageFile>$(CoverletOutput)coverage.json</CoverageFile>
  </PropertyGroup>
  
  <!-- Coverage validation will fail the build if below threshold -->
  <Warning Text="Code coverage is below threshold ($(Threshold)%)" 
           Condition="Exists('$(CoverageFile)') AND $([System.IO.File]::ReadAllText('$(CoverageFile)').Contains('&quot;LineCoverage&quot;'))" />
</Target>
```

- [ ] **Step 2: Update CI to fail on low coverage**

Modify `.github/workflows/ci.yml` to add coverage check:

```yaml
    - name: Check Coverage Threshold
      run: |
        COVERAGE_PCT=$(grep -oP '(?<=line-rate=")[^"]*' src/TicketMasala.Tests/TestResults/*/coverage.cobertura.xml | head -1 | awk '{print $1 * 100}')
        echo "Coverage: $COVERAGE_PCT%"
        if (( $(echo "$COVERAGE_PCT < 80" | bc -l) )); then
          echo "❌ Coverage $COVERAGE_PCT% is below 80% threshold"
          exit 1
        fi
```

- [ ] **Step 3: Commit**

```bash
git add src/TicketMasala.Tests/TicketMasala.Tests.csproj .github/workflows/ci.yml
git commit -m "test: add 80% coverage threshold and quality gates"
```

---

## Task 9: Update .gitignore for Test Artifacts

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: Add test result entries**

Add to `.gitignore`:

```gitignore
# Test Results
**/TestResults/
**/coverage.*
coveragereport/
*.coverage
*.coveragexml
```

- [ ] **Step 2: Commit**

```bash
git add .gitignore
git commit -m "chore: update gitignore for test artifacts and coverage reports"
```

---

## Task 10: Verify All Tests Pass

**Files:**
- None (verification task)

- [ ] **Step 1: Run full test suite**

```bash
dotnet test --verbosity minimal
```
Expected: All tests pass

- [ ] **Step 2: Run coverage collection**

```bash
./scripts/test-coverage.sh
```
Expected: Coverage report generated, view index.html

- [ ] **Step 3: Final verification commit**

```bash
git log --oneline -10
```
Expected: All 10 commits from this plan present

---

## Summary of Changes

| Area | Before | After |
|------|--------|-------|
| **Test Projects** | 1 (Web.Tests) | 2 (+ Domain.Tests) |
| **Total Tests** | 278 | 278+ (Domain tests added) |
| **Coverage** | None | Coverlet + ReportGenerator |
| **Parallelization** | None | Collection-based |
| **CI/CD** | Deploy only | Build + Test + Coverage |
| **Test Data** | Fixture-based | Builder + Fixture hybrid |
| **Documentation** | None | README + scripts |

## Acceptance Criteria

- [ ] All 278 existing tests pass (3 flaky tests fixed)
- [ ] Coverage reports generate successfully
- [ ] CI workflow runs on PR with coverage comment
- [ ] Domain.Tests project exists with at least 5 tests
- [ ] Test scripts work: `./scripts/test.sh` and `./scripts/test-coverage.sh`
- [ ] 80% coverage threshold configured
- [ ] Collection-based parallelization enabled
