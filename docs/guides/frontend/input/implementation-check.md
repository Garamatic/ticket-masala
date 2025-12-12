I have updated my internal context to Frontend Setup v5 and Design System v2.

As your UI/UX Director, I am marking this specification as APPROVED for Implementation.
🕵️ Design Review: v5 & System v2

    🏆 The Big Win: The addition of Spicy Orange (Text): #BF360C is the critical piece. You have solved the accessibility violation while keeping the brand identity.

    ✨ Visual Harmony: The new gradient (--gradient-dispatch) anchoring on Tech Blue (#4C5AF5) into Deep Slate makes the application feel cohesive and "engineered," rather than decorative.

    🧠 Cognitive Load: The "Pulsing" vs. "Outlined" distinction for badges allows a user to scan the dashboard and immediately distinguish "Active Processing" from "Waiting in Queue" without reading text.

🛠️ Implementation: The DispatchBacklog View

Since you have defined the system, I will now perform Function A (Component Factory) to generate the Razor View that strictly adheres to these v5 constraints (Razor + HTMX + Semantic CSS).

Here is the updated Views/Manager/DispatchBacklog.cshtml and the necessary additions to your CSS.
1. CSS Updates (wwwroot/css/design-system.css)

Add this to your existing CSS file to support the new animations and semantic badges.
CSS

/* --- Semantic Badges --- */
.badge {
    display: inline-flex;
    align-items: center;
    padding: var(--spacing-xs) var(--spacing-sm);
    border-radius: var(--border-radius-full);
    font-family: var(--font-body);
    font-size: var(--font-size-xs);
    font-weight: var(--font-weight-medium);
    line-height: 1;
    white-space: nowrap;
}

/* Status: Dispatching (Active/Pulsing) */
.badge-dispatching {
    background-color: var(--color-accent); /* #FF8A65 */
    color: var(--bg-primary); /* Dark text on light orange, or White */
    /* Note: If using White text, ensure #FF8A65 is dark enough, 
       otherwise use #BF360C text or Dark Slate text */
    color: #1E293B; 
    box-shadow: var(--shadow-sm);
}

.badge-dispatching .status-icon {
    margin-right: 6px;
    animation: pulse 2s infinite;
}

/* Status: Anticipation (Passive/Outlined) */
.badge-anticipation {
    background-color: transparent;
    border: 1px solid var(--color-accent);
    color: var(--color-accent-text); /* #BF360C - The accessible text color */
}

/* Status: Success/Standard */
.badge-success {
    background-color: var(--color-success-light);
    color: var(--color-brand-accent);
    border: 1px solid var(--color-success-border);
}

/* --- Animations --- */
@keyframes pulse {
    0% { transform: scale(0.95); opacity: 0.7; }
    50% { transform: scale(1); opacity: 1; }
    100% { transform: scale(0.95); opacity: 0.7; }
}

/* --- Header Gradient --- */
.dispatch-header {
    background: var(--gradient-dispatch);
    color: white; /* Ensure text sits on dark slate/blue */
    padding: var(--spacing-lg);
    border-radius: var(--border-radius-lg);
    margin-bottom: var(--spacing-lg);
    box-shadow: var(--shadow-md);
}

2. Razor View (Views/Manager/DispatchBacklog.cshtml)

Strictly using Razor logic (no React), HTMX, and the new design tokens.
HTML

@model TicketMasala.Web.ViewModels.GerdaDispatchViewModel
@{
    ViewData["Title"] = "GERDA Dispatch Backlog";
}

<div class="dispatch-header flex justify-between items-center">
    <div>
        <h2 style="font-family: var(--font-heading); font-weight: 700; font-size: var(--font-size-2xl);">
            GERDA Dispatch Backlog
        </h2>
        <p class="text-white opacity-90" style="font-family: var(--font-body);">
            AI-powered ticket allocation dashboard
        </p>
    </div>
    
    <div class="bg-white/10 p-3 rounded-lg backdrop-blur-sm border border-white/20">
         <span class="text-sm font-medium mr-2">System Status:</span>
         @if (Model.IsDispatching)
         {
             <span class="badge badge-dispatching">
                 <span class="status-icon">●</span> Processing
             </span>
         }
         else
         {
             <span class="badge badge-success">
                 Online
             </span>
         }
    </div>
</div>

<div class="grid-layout" style="display: grid; grid-template-columns: 2fr 1fr; gap: var(--spacing-lg);">
    
    <section class="card" style="background: var(--bg-primary); border: 1px solid var(--border-color); border-radius: var(--border-radius-lg); box-shadow: var(--shadow);">
        <div class="p-4 border-b" style="border-color: var(--border-color);">
            <h3 style="font-family: var(--font-heading); font-weight: 600; color: var(--text-primary);">
                Low Confidence Allocations
            </h3>
        </div>

        <div id="allocations-list" class="p-4">
            @foreach (var ticket in Model.LowConfidenceAllocations)
            {
                <div class="ticket-row p-3 mb-3 border rounded-md hover:shadow-md transition-all"
                     style="background: var(--bg-secondary); border-color: var(--border-color);">
                    
                    <div class="flex justify-between items-start mb-2">
                        <span class="font-bold text-lg" style="color: var(--color-primary);">#@ticket.Guid.ToString().Substring(0, 8)</span>
                        
                        @if (ticket.Status == "Dispatching")
                        {
                            <span class="badge badge-dispatching" title="AI is currently analyzing this ticket">
                                <span class="status-icon">●</span> Dispatching
                            </span>
                        }
                        else if (ticket.Status == "Anticipation")
                        {
                            <span class="badge badge-anticipation" title="Waiting for manager review">
                                Anticipation
                            </span>
                        }
                        else 
                        {
                            <span class="badge" style="background: var(--bg-primary); border: 1px solid var(--border-color);">@ticket.Status</span>
                        }
                    </div>

                    <p class="mb-3" style="color: var(--text-secondary);">@ticket.Description</p>

                    <div class="flex gap-4 text-sm">
                        <div class="flex items-center">
                            <span class="text-xs uppercase tracking-wider mr-2" style="color: var(--text-secondary);">AI Confidence</span>
                            <span style="color: var(--color-warning); font-weight: 700;">
                                @(ticket.PriorityScore * 10)%
                            </span>
                        </div>
                        <div class="flex items-center">
                            <span class="text-xs uppercase tracking-wider mr-2" style="color: var(--text-secondary);">Suggested Project</span>
                            <span style="color: var(--text-primary); font-weight: 600;">@ticket.RecommendedProjectName</span>
                        </div>
                    </div>

                    <div class="mt-4 flex gap-2 justify-end">
                        <button class="btn btn-sm" 
                                style="color: var(--color-danger); border: 1px solid var(--border-color);"
                                hx-post="/Manager/RejectSuggestion/@ticket.Guid"
                                hx-swap="outerHTML"
                                hx-target="closest .ticket-row">
                            Reject
                        </button>
                        <button class="btn btn-sm"
                                style="background-color: var(--color-primary); color: white;"
                                hx-post="/Manager/ConfirmSuggestion/@ticket.Guid"
                                hx-swap="outerHTML"
                                hx-target="closest .ticket-row">
                            Approve
                        </button>
                    </div>
                </div>
            }
        </div>
    </section>

    <aside>
        <div class="card mb-6 p-4" style="background: var(--bg-primary); border-radius: var(--border-radius-lg); border: 1px solid var(--border-color);">
             <h4 class="uppercase text-xs font-bold mb-4" style="color: var(--text-secondary);">Dispatch Velocity</h4>
             <div class="text-3xl font-bold" style="font-family: var(--font-heading); color: var(--text-primary);">
                 @Model.Statistics.AvgAssignmentTime <span class="text-sm font-normal text-gray-400">min/avg</span>
             </div>
        </div>
        
        <div class="card p-4" style="background: var(--bg-primary); border-radius: var(--border-radius-lg); border: 1px solid var(--border-color);">
             <h4 class="uppercase text-xs font-bold mb-4" style="color: var(--text-secondary);">Agent Availability</h4>
             @foreach(var agent in Model.AvailableAgents)
             {
                 <div class="flex justify-between items-center py-2 border-b last:border-0" style="border-color: var(--border-color);">
                     <span style="color: var(--text-primary);">@agent.Name</span>
                     <span class="text-xs" style="color: var(--color-success);">@agent.CurrentLoad tickets</span>
                 </div>
             }
        </div>
    </aside>
</div>

Next Step

Would you like me to generate the C# ViewModel updates (TicketDispatchInfo properties) to match this view?