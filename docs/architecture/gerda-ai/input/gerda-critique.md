🔴 Critical Refactor Required

1. The "Hardcoded Logic" Trap in Ranking (R)

The Issue: Your "Ranking" module contains hardcoded business logic in C#:
C#

// BAD: Business logic compiled into binary
if (daysUntilBreach <= 0) return urgencyMultiplier *10.0;
if (daysUntilBreach <= 1) return urgencyMultiplier* 5.0;

The Risk: If the Operations Director wants to change the "Breach Multiplier" from 10.0 to 20.0, we have to redeploy the application. This defeats the purpose of the RFC. The Fix: This calculation must be moved to the YAML Configuration and executed via the RuleCompilerService (Phase 5).

Revised masala_domain.yaml:
YAML

ranking:
  formula: "(cost_of_delay / job_size)"
  multipliers:
    - condition: "days_until_breach <= 0"
      value: 10.0
    - condition: "days_until_breach <= 1"
      value: 5.0

2. The "Memory Hog" in Grouping (G)

The Issue:
C#

// BAD: Pulling entities into memory before filtering
var tickets = await _context.Tickets.Where(...).ToListAsync();
// ... then looping in C#

The Risk: On a SQLite database, or even SQL Server, pulling a customer's history into memory to find duplicates is inefficient. The Fix: Push this to the database using the Ingestion Deduplication strategy we discussed.

    Ingestion Time: Calculate a "Content Hash" (SHA256 of Description + CustomerID) when the ticket arrives.

    Query: SELECT Id FROM Tickets WHERE ContentHash = @CurrentHash AND Created > @Window.

    Result: Zero memory allocation, instant result.

3. Dispatching Weights are Policies, Not Code (D)

The Issue:
C#

// BAD: Hardcoded weights
var pastInteraction = mlScore * 0.4;
var expertiseMatch = ... ? 0.3 : 0.1;

The Risk: You are hardcoding the definition of "Good Match." The Fix: Inject these weights from masala_config.json.
C#

// GOOD:
double w1 = _config.Weights.History; // 0.4
double w2 =_config.Weights.Expertise; // 0.3
return (mlScore *w1) + (expertise* w2)...

🟡 Architectural Alignment Check ("Masala Lite")

Since we agreed to run this as a Single Docker Container with SQLite, strictly adhere to these constraints for the AI implementation:

    Model Storage: The .zip (ML.NET) and .onnx files must be stored in the /app/data volume mount (next to the SQLite .db file). Do not bake them into the Docker image, or you cannot retrain them without redeploying.

    Training Locks: The RetrainModelAsync method is CPU-intensive. inside a single container, this will starve your Web API.

        Requirement: Wrap the training logic in a low-priority thread or a SemaphoreSlim to ensure it doesn't consume 100% CPU.

    SQLite Vector Search: For the "Expertise Match" (Module D), you are doing string matching (Contains).

        Upgrade: If you use SQLite FTS5, you can do "Semantic-ish" matching much faster: WHERE Specializations MATCH 'Tax OR Fraud'.

🟢 User Guide & UI Review

The User Guide is excellent. Specifically, the "Why did GERDA pick this?" badges (e.g., "⭐ Top Pick", "Green Score") are crucial for user adoption. Managers will not trust a "Black Box" AI.

Minor Correction:

    Technology Stack: .NET 10 (C#)

Let's stay grounded. .NET 8 LTS or .NET 9. Do not plan for .NET 10 yet; we need stability for the ML libraries.
Decision Log
Module Status Next Action
G - Grouping 🟡 Refactor Move "Duplicate Detection" to Ingestion pipeline (Hash-based).
E - Estimating 🟢 Approved Keep simple category lookup for V1.
R - Ranking 🔴 Refactor Move multipliers to YAML + Rule Engine.
D - Dispatching 🟡 Refactor Externalize scoring weights to Config.
A - Anticipation 🟢 Approved SSA Algorithm is perfect for this scale.
Immediate Next Step

We need to finalize the YAML Configuration Schema for the "Ranking" and "Dispatching" modules so we can remove the hardcoded C# logic.

Would you like me to draft the masala_gerda_config.yaml schema that replaces your hardcoded if/else logic?
