Critique 1: The "Fragile Hash" Trap (Grouping)

Your plan: SHA256(Description + CustomerId). The reality: Users are messy.

    Ticket A: "Server is down"

    Ticket B: "Server is down " (Double space + trailing space)

If you hash these raw strings, you get two different hashes. The duplicate check fails.

The Fix: Canonicalization You must implement a Canonicalize() method before hashing.

    Trim whitespace.

    Lowercase everything.

    Remove non-alphanumeric characters (optional, but recommended for strictness).

C#

// The "Masala Standard" Canonicalizer
public static string ComputeContentHash(string description, string customerId)
{
    // 1. Sanitize
    var raw = $"{customerId}|{description}".ToLowerInvariant();
    // 2. Normalize whitespace (Regex compiled static)
    var clean = Regex.Replace(raw, @"\s+", " ").Trim();

    // 3. Hash
    using var sha = SHA256.Create();
    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clean));
    return Convert.ToHexString(bytes);
}

Critique 2: The "Unit Mismatch" Trap (Dispatching)

Your config allows weights: ml_score: 0.4 and expertise_match: 0.3.

    ml_score (Prediction) usually outputs a probability: 0.0 to 1.0.

    expertise_match (FTS5 BM25) outputs an arbitrary score: 3.5, 12.0, 150.0 depending on term frequency.

If you sum these (
0.4×0.9+0.3×15.0

), the FTS5 score completely dominates the ML score. The AI becomes irrelevant.

The Fix: Normalization Strategy You must normalize the FTS5 score to a 0-1 range before weighting.

    Option A (Strict): MinMax normalization (requires knowing the max score in the batch).

    Option B (Sigmoid): Squash the FTS score using a sigmoid function so it never exceeds 1.0.

    Getty Images

For Ticket Masala v2.1, use Option B. It's stateless.
Scorenorm​=Scoreraw​+10Scoreraw​​

(Where 10 is a tunable "half-saturation" constant).
Critique 3: The SQLite FTS5 Implementation

You cannot just "add an index" for FTS5. In SQLite, FTS5 is a Virtual Table. You need a specific migration strategy to keep the "Search Table" in sync with the "Data Table."

The Architecture:

    Tickets Table: The source of truth (JSON, Status, ID).

    Tickets_Search Table: The FTS5 Virtual Table.

    Triggers: The glue that keeps them in sync.

Here is the SQL you need to inject into your EF Core Migration (Up() method):
C#

// Inside your Migration.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Create the Virtual Table
    migrationBuilder.Sql(@"
        CREATE VIRTUAL TABLE Tickets_Search USING fts5(
            Id UNINDEXED,
            Description,
            CustomFieldsJson,
            content='Tickets',
            content_rowid='RowId'
        );
    ");

    // 2. Create Triggers to Sync INSERTs
    migrationBuilder.Sql(@"
        CREATE TRIGGER Tickets_AI AFTER INSERT ON Tickets BEGIN
            INSERT INTO Tickets_Search(rowid, Id, Description, CustomFieldsJson) 
            VALUES (new.RowId, new.Id, new.Description, new.CustomFieldsJson);
        END;
    ");

    // 3. Create Triggers to Sync DELETEs
    migrationBuilder.Sql(@"
        CREATE TRIGGER Tickets_AD AFTER DELETE ON Tickets BEGIN
            INSERT INTO Tickets_Search(Tickets_Search, rowid, Id, Description, CustomFieldsJson) 
            VALUES('delete', old.RowId, old.Id, old.Description, old.CustomFieldsJson);
        END;
    ");

    // 4. Create Triggers to Sync UPDATEs
    migrationBuilder.Sql(@"
        CREATE TRIGGER Tickets_AU AFTER UPDATE ON Tickets BEGIN
            INSERT INTO Tickets_Search(Tickets_Search, rowid, Id, Description, CustomFieldsJson) 
            VALUES('delete', old.RowId, old.Id, old.Description, old.CustomFieldsJson);
            INSERT INTO Tickets_Search(rowid, Id, Description, CustomFieldsJson) 
            VALUES (new.RowId, new.Id, new.Description, new.CustomFieldsJson);
        END;
    ");
}

Decision Point: The Next Step

The spec is approved with the above caveats.

We have a choice for the immediate coding task:

    The Brain (Compiler): Implement RuleCompilerService so we can actually execute those YAML rules.

    The Muscle (Dispatching): Implement the DispatchingService with the FTS5 query logic and Sigmoid normalization.

I recommend starting with the RuleCompilerService. It is the core dependency for Ranking, Dispatching, AND Routing. Without it, the YAML is just text.

Shall I generate the RuleCompilerService using strict System.Linq.Expressions?i
