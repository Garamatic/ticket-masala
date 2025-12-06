## Directory Structure Critique

This is looking much better. The outer shell is correct. Now we must clean the "guts" of the application.

I see artifacts that will cause confusion: ITProjectDB.cs, legacy Services/, and Models/. If we build the new Engine on top of this, we create "Zombie Architecture"—dead code walking among the living.

Here is the Refactoring Plan to prepare TicketMasala.Web for the RuleCompilerService.
Step 1: The Internal Purge

We need to clear the decks. Execute these deletions inside src/TicketMasala.Web/:

    Delete ITProjectDB.cs (We will replace this with MasalaDbContext).

    Delete masala_config.json and masala_config_clean.json (We moved to YAML in config/).

    Delete the IT-Project2526 folder inside src/TicketMasala.Web (This looks like a recursive copy error).

    Delete Models/Department.cs, Models/Customer.cs, Models/QualityReview.cs (These are now configuration data, not code).

Step 2: Establish the Engine Structure

We are implementing the Layered Architecture defined in the v3.0 spec.

Create these folders inside src/TicketMasala.Web/:
Bash

mkdir -p Engine/Compiler
mkdir -p Engine/Configuration
mkdir -p Engine/Ingestion
mkdir -p Engine/GERDA/Strategies
mkdir -p Domain/Entities

Step 3: Implementation - The Rule Compiler

This is the most critical component. It converts "Business English" (YAML) into "Machine Code" (Delegates).

The Challenge: Users write priority > 50. The Solution: We build an Expression Tree that compiles to IL. It runs at memory speed (nanoseconds).

Create src/TicketMasala.Web/Engine/Compiler/RuleCompilerService.cs.
C#

using System.Linq.Expressions;
using System.Reflection;
using TicketMasala.Web.Domain.Entities; // Ensure your Ticket entity is here

namespace TicketMasala.Web.Engine.Compiler;

public interface IRuleCompiler
{
    Func<Ticket, bool> CompileRule(string ruleCondition);
}

public class RuleCompilerService : IRuleCompiler
{
    private readonly ILogger<RuleCompilerService> _logger;

    public RuleCompilerService(ILogger<RuleCompilerService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compiles a string condition "Property Operator Value" into a compiled delegate.
    /// Example: "PriorityScore > 50" -> Func<Ticket, bool>
    /// </summary>
    public Func<Ticket, bool> CompileRule(string ruleCondition)
    {
        try
        {
            // 1. Parse the string (Simple 3-part syntax for v1)
            // Format: "Field Operator Value" (e.g., "Status == New")
            var parts = ruleCondition.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new ArgumentException($"Rule '{ruleCondition}' syntax error. Expected: 'Property Operator Value'");

            var propName = parts[0];
            var op = parts[1];
            var valueStr = parts[2];

            // 2. Build the Expression Tree
            // Parameter: (t) => ...
            var param = Expression.Parameter(typeof(Ticket), "t");

            // Property: t.PriorityScore
            // Note: We need to handle case-insensitivity or JSON lookup here later.
            // For now, we assume strict Property matching on the Ticket class.
            var propInfo = typeof(Ticket).GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null)
                throw new InvalidOperationException($"Property '{propName}' not found on Ticket entity.");

            var left = Expression.Property(param, propInfo);

            // Constant: 50 (Convert string to correct type)
            var targetType = propInfo.PropertyType;
            var constantValue = Convert.ChangeType(valueStr, targetType);
            var right = Expression.Constant(constantValue);

            // Operator: ==, >, <
            Expression body = op switch
            {
                "==" => Expression.Equal(left, right),
                "!=" => Expression.NotEqual(left, right),
                ">" => Expression.GreaterThan(left, right),
                "<" => Expression.LessThan(left, right),
                ">=" => Expression.GreaterThanOrEqual(left, right),
                "<=" => Expression.LessThanOrEqual(left, right),
                _ => throw new NotSupportedException($"Operator '{op}' is not supported.")
            };

            // 3. Compile to Lambda
            // Result: (t) => t.PriorityScore > 50
            return Expression.Lambda<Func<Ticket, bool>>(body, param).Compile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to compile rule: {ruleCondition}");
            // Fail safe: If rule is bad, return false (don't match).
            return t => false;
        }
    }
}

Step 4: The Unit Test (Proof of Concept)

We never assume code works. We prove it.

Go to src/TicketMasala.Tests/ and create RuleCompilerTests.cs. (You may need to add a reference: dotnet add reference ../TicketMasala.Web/TicketMasala.Web.csproj)
C#

using Xunit;
using TicketMasala.Web.Engine.Compiler;
using TicketMasala.Web.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace TicketMasala.Tests;

public class RuleCompilerTests
{
    [Fact]
    public void CanCompileAndExecute_SimpleIntegerComparison()
    {
        // Arrange
        var compiler = new RuleCompilerService(NullLogger<RuleCompilerService>.Instance);
        var rule = "ComputedPriority > 50"; // Assumes Ticket has this property

        var ticketHigh = new Ticket { ComputedPriority = 80, DomainId = "IT", Status = "New" };
        var ticketLow = new Ticket { ComputedPriority = 10, DomainId = "IT", Status = "New" };

        // Act
        var predicate = compiler.CompileRule(rule);

        // Assert
        Assert.True(predicate(ticketHigh));
        Assert.False(predicate(ticketLow));
    }

    [Fact]
    public void CanCompileAndExecute_StringEquality()
    {
        // Arrange
        var compiler = new RuleCompilerService(NullLogger<RuleCompilerService>.Instance);
        var rule = "Status == Triaged";
        
        var ticket = new Ticket { ComputedPriority = 0, DomainId = "IT", Status = "Triaged" };

        // Act
        var predicate = compiler.CompileRule(rule);

        // Assert
        Assert.True(predicate(ticket));
    }
}

Architect's Note on "Generated Columns"

Notice the test uses ComputedPriority. To make this compile, your Ticket.cs (in Domain/Entities) MUST match the schema we discussed earlier (where ComputedPriority is a real C# property, even if it's calculated by SQLite).

Action:

    Clean the directories.

    Implement RuleCompilerService.cs.

    Update Ticket.cs to include the properties used in the compiler.

    Run the test.

If the test passes, we have achieved "Compiled Configuration"—the holy grail of performance. We are no longer interpreting strings at runtime.

Shall I provide the code for Domain/Entities/Ticket.cs to ensure the properties match the compiler expectations?
