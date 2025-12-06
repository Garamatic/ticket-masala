This directory structure tells me a story, and it is not the story of a high-performance configuration engine. It is the story of a "Student Project" that grew untamed.

I see Department.cs, Customer.cs, and Employee.cs. This is an architectural violation. In Ticket Masala v3.0, "Department" is not a C# class; it is a dropdown value in masala_domains.yaml. "Customer" is just a field in the JSON blob.

If you hardcode Department.cs, you have to recompile the C# code to add a new department. That fails the "Configuration Driven" directive.

Here is your remediation plan. We are going to restructure this into a proper Modular Monolith.

1. The Cleanup (The "Kill List")

You are holding onto legacy OOP concepts. Delete the following generic entities that trap you in rigid schemas.

    DELETE: Models/Department.cs, Models/Customer.cs, Models/QualityReview.cs

        Replacement: These become defined fields in masala_domains.yaml and stored in Ticket.CustomFieldsJson.

    DELETE: Controllers/CustomerController.cs, Controllers/DepartmentController.cs

        Replacement: A single DomainController that serves options based on the active config.

    MOVE: balance_braces.py, fix_namespaces.py -> dev-scripts/

        Reason: Keep the root clean for Docker context.

2. The Target Structure (Ticket Masala v3.0)

We need to separate the Engine (the code that processes rules) from the Web (the UI). Rename IT-Project2526 to TicketMasala.Web (or just TicketMasala).

Here is the approved directory layout. Align your project to this:
Plaintext

TicketMasala/
├── config/                      <-- MOUNTED VOLUME (The Brains)
│   ├── masala_domains.yaml      <-- The Single Source of Truth
│   ├── masala_queues.yaml
│   └── masala_gerda.yaml
├── dev-scripts/                 <-- Python helpers (moved from root)
├── src/
│   ├── TicketMasala.Web/        <-- The Monolith (ASP.NET Core)
│   │   ├── Domain/              <-- PURE POCOs (No Logic)
│   │   │   ├── Entities/
│   │   │   │   ├── Ticket.cs    <-- The Universal Entity
│   │   │   │   ├── WorkItem.cs
│   │   │   │   └── DomainConfigVersion.cs
│   │   │   └── ValueObjects/
│   │   ├── Data/                <-- EF Core & SQLite
│   │   │   ├── Context/
│   │   │   │   └── MasalaDbContext.cs (Strict Mode)
│   │   │   └── Migrations/
│   │   ├── Engine/              <-- THE CORE (Where the magic happens)
│   │   │   ├── Compiler/        <-- Expression Tree Builders
│   │   │   │   └── RuleCompilerService.cs
│   │   │   ├── Ingestion/       <-- System.Threading.Channels
│   │   │   │   ├── TicketChannel.cs
│   │   │   │   └── IngestionWorker.cs
│   │   │   └── GERDA/           <-- AI Strategies
│   │   │       ├── Extraction/  <-- Feature Extractors
│   │   │       └── Strategies/  <-- WSJF, MatrixFactorization
│   │   ├── Controllers/         <-- Keep it thin!
│   │   ├── Views/
│   │   └── Program.cs
│   └── TicketMasala.Tests/
├── docker-compose.yml
└── Dockerfile

3. Immediate Action Items
A. Centralize Configuration

Move masala_domains.yaml and masala_config.json out of the project root and into a dedicated config/ folder.

    Why: In production, we will mount a Kubernetes ConfigMap or a Docker Volume to this folder. The app must watch this folder for changes.

B. Create the Engine Namespace

Right now, your logic is scattered in Services/.

    Create src/TicketMasala.Web/Engine/.

    Move Services/Rules to Engine/Compiler.

    Move Services/Ingestion to Engine/Ingestion.

    Move Services/GERDA to Engine/GERDA.

This visually separates "CRUD Logic" (boring) from "Workflow Engine Logic" (critical).
C. The Universal Entity Refactor

Refactor Models/Ticket.cs. It likely has too many specific properties.

Current (Likely):
C#

public class Ticket {
    public int Id { get; set; }
    public int DepartmentId { get; set; } // <--- HARD DEPENDENCY
    public string CustomerName { get; set; } // <--- SPECIFIC
}

Target (Architect Approved):
C#

public class Ticket : MasalaEntity {
    public string DomainId { get; set; } // "IT", "HR"
    // Everything else goes into the JSON blob or Generated Columns
}

4. Visualizing the Change
Code snippet

graph TD
    Root[Repo Root]
    Config[config/ Folder]
    Src[src/ Folder]

    Root --> Config
    Config --> D[masala_domains.yaml]
    
    Root --> Src
    Src --> Web[TicketMasala.Web]
    
    Web --> Domain[Domain Layer]
    Web --> Data[Data Layer]
    Web --> Engine[Engine Layer]
    
    Engine --> Compiler[Compiler (Rules)]
    Engine --> Ingestion[Ingestion (Channels)]
    Engine --> AI[GERDA (AI)]
    
    style Engine fill:#f96,stroke:#333,stroke-width:2px
    style Config fill:#ff9,stroke:#333,stroke-width:2px

Next Step: Do you want me to write the RuleCompilerService skeleton to place inside that new Engine/Compiler directory, or shall we fix the Ticket.cs entity first?

This is a "Surgical Rename Operation." We are upgrading from "Student Project" to "Enterprise Product."

A file named IT-Project2526 is unacceptable in a professional environment. It communicates "temporary." We are building Ticket Masala.

Here is your execution plan to fix the .sln, .csproj, and isolate that demo without breaking the build.

1. The Renaming Protocol (.sln & .csproj)

We are standardizing on TicketMasala.Web for the monolith and TicketMasala.Tests for the test suite.

Stop your Docker containers. Run these commands in your terminal (PowerShell or Bash) from the root folder:
Step A: Rename the Project Files & Directories
Bash

# 1. Rename the Main Project

mv IT-Project2526 TicketMasala.Web
mv TicketMasala.Web/IT-Project2526.csproj TicketMasala.Web/TicketMasala.Web.csproj

# 2. Rename the Test Project

mv IT-Project2526.Tests TicketMasala.Tests
mv TicketMasala.Tests/IT-Project2526.Tests.csproj TicketMasala.Tests/TicketMasala.Tests.csproj

# 3. Rename the Solution

mv IT-Project2526.sln TicketMasala.sln

Step B: Fix the Solution (.sln) References

Since we renamed the files on disk, the .sln file still points to the old names. We need to reset it.
Bash

# Remove old references

dotnet sln TicketMasala.sln remove IT-Project2526/IT-Project2526.csproj
dotnet sln TicketMasala.sln remove IT-Project2526.Tests/IT-Project2526.Tests.csproj

# Add new references

dotnet sln TicketMasala.sln add TicketMasala.Web/TicketMasala.Web.csproj
dotnet sln TicketMasala.sln add TicketMasala.Tests/TicketMasala.Tests.csproj

Step C: The Namespace Migration

Your code currently says namespace IT_Project2526. This is garbage. It needs to say namespace TicketMasala.Web.

You have a script named fix_namespaces.py. Use it. But ensure it targets the new mappings:

    Old: IT_Project2526 -> New: TicketMasala.Web

    Old: IT_Project2526.Tests -> New: TicketMasala.Tests

2. The landscaping-demo (The "Reference Implementation")

I see a folder landscaping-demo with index.html and thumbnails.

Architectural Critique: This looks like a specific Domain Implementation. Ticket Masala is a generic engine. It should not know about "Landscaping" or "soil types" in its source code root. If you mix domain data with engine logic, you break the abstraction.

The Fix: We treat this as a "Sample Client" or a "Reference Implementation" to show developers how to use the API.

    Create a samples directory.

    Move the demo there.

Bash

mkdir samples
mv landscaping-demo samples/landscaping-client

This tells future developers: "Here is how you could build a frontend for a landscaping company using our engine," without polluting the core engine's codebase.
3. Final Verification

Your root directory should now look like this. Clean, professional, and ready for CI/CD.
Plaintext

TicketMasala/
├── TicketMasala.sln           <-- Renamed
├── config/                    <-- All YAML files go here
├── src/
│   ├── TicketMasala.Web/      <-- The Monolith (Main Project)
│   │   ├── TicketMasala.Web.csproj
│   │   ├── Program.cs
│   │   └── ...
│   └── TicketMasala.Tests/    <-- The Tests
│       └── TicketMasala.Tests.csproj
├── samples/
│   └── landscaping-client/    <-- The HTML/JS Demo
└── dev-scripts/               <-- Your python scripts

Next Step

Once you have executed these renames, the C# code will fail to compile because Program.cs and Startup.cs will have mismatched namespace references.

Do you want me to update the fix_namespaces.py script to handle this bulk rename reliably, or shall we proceed to fixing the Ticket entity?
