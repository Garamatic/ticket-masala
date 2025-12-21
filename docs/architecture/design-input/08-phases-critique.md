🛑 Critical Gap: Phase 4.5 - Configuration Versioning

The Risk: You are about to implement a High-Performance Rule Compiler (Phase 5). If you deploy this and then change a rule in masala_domains.yaml, every existing ticket in the database that violates the new rule might become unreadable or throw exceptions when the Rule Engine tries to validate its current state.

The Fix: Before you build the Compiler, you must implement the Snapshot Strategy we discussed.

    Task: Create DomainConfigVersion entity.

    Task: On Ticket creation, store ConfigVersionId.

    Task: Update IRuleEngineService to request the specific version of the compiled rules that matches the ticket, not the latest "Live" version.

🟡 Phase 5 Guidance: The Rule Compiler Implementation

You are moving from Interpreter (slow) to Compiler (fast). Do not underestimate the complexity of System.Linq.Expressions.

Architectural Constraint: Your Compiler Service must be Stateless but use a Stateful Cache.

The Blueprint:

    The Cache Key: Dictionary<(string DomainId, string VersionHash), CompiledPolicy>

    The Trigger:

        Startup: Compile "Head" version of all domains.

        On Request: If a ticket references an old ConfigVersionId not in cache, compile and cache it on demand (Lazy Loading).

    The Safety Valve: Wrap the Expression.Compile() in a try/catch. If a rule is syntactically invalid (e.g., comparing "Apple" > 5), the service must return a Safe Fallback Delegate (e.g., _ => false) and log a critical error, rather than crashing the app.

🟡 Phase 8 Decision: Template Engine (Scriban vs. Liquid)

You left Phase 8 (Ingestion) as "Next," and we had an open question regarding the string interpolation for webhooks: Title: "Soil Alert: {{ location_name }}"

My Recommendation: Use Scriban.

    Why?

        Performance: Scriban is significantly faster than DotLiquid. It parses once and renders multiple times (perfect for the "Gatekeeper" pattern).

        Async Support: Fully supports async/await, which matters if a template needs to do a quick lookup (though we should avoid that).

        Syntax: It is compatible with Liquid syntax, so if you switch later, the configuration YAML doesn't break.

Implementation Snippet (The "Mapper"):

// In your DigestionWorker
var template = Scriban.Template.Parse("Sensor {{ device_id }} detected {{ value }}");
var result = await template.RenderAsync(new { device_id = "A1", value = 9.5 });

Phase 8 Context: Event Driven Architecture

Since you are moving toward a decoupled ingestion system (Gatekeeper + Worker), you are effectively adopting an Event Driven Architecture for the intake layer. This ensures that your high-traffic inputs (IoT sensors) are decoupled from your business logic processing.

🏁 Principal Architect's Sign-Off

I am approving the move to Phase 5 (Performance Optimization), provided you include the Versioning/Snapshot logic.
