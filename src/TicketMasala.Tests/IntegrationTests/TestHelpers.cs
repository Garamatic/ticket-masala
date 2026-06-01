using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketMasala.Domain.Data;

namespace TicketMasala.Tests.IntegrationTests;

public static class TestHelpers
{
    public static string? ExtractAntiforgeryToken(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        const string tokenPattern = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var startIndex = html.IndexOf(tokenPattern, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        startIndex += tokenPattern.Length;
        var endIndex = html.IndexOf("\"", startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return html.Substring(startIndex, endIndex - startIndex);
    }

    public static async Task<Guid> CreateTicketAsync(
        HttpClient client,
        CustomWebApplicationFactory factory,
        string description,
        string customerId = "11111111-1111-1111-1111-111111111111")
    {
        var createPageResponse = await client.GetAsync("/Ticket/Create");
        createPageResponse.EnsureSuccessStatusCode();
        var createPageHtml = await createPageResponse.Content.ReadAsStringAsync();
        var antiforgeryToken = ExtractAntiforgeryToken(createPageHtml);

        var formData = new Dictionary<string, string>
        {
            ["Description"] = description,
            ["CustomerId"] = customerId,
            ["ResponsibleId"] = "",
            ["ProjectGuid"] = "",
            ["CompletionTarget"] = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd"),
            ["WorkItemTypeCode"] = "INCIDENT",
            ["DomainId"] = "IT"
        };

        if (!string.IsNullOrEmpty(antiforgeryToken))
        {
            formData["__RequestVerificationToken"] = antiforgeryToken;
        }

        var response = await client.PostAsync("/Ticket/Create", new FormUrlEncodedContent(formData));

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MasalaDbContext>();
            var ticket = await context.Tickets
                .OrderByDescending(t => t.CreationDate)
                .FirstOrDefaultAsync(t => t.Description == description);

            if (ticket != null)
                return ticket.Guid;
        }

        throw new InvalidOperationException($"Failed to create ticket. Response: {response.StatusCode}");
    }
}
