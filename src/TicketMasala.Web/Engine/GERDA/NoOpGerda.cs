namespace TicketMasala.Web.Engine.GERDA;

/// <summary>
/// No-op implementation of IGerda when GERDA is disabled.
/// Returns disabled outcomes immediately without processing.
/// </summary>
internal sealed class NoOpGerda : IGerda
{
    public bool IsActive => false;

    public Task<GerdaOutcome> ProcessAsync(Guid ticketGuid)
    {
        return Task.FromResult(GerdaOutcome.Disabled(ticketGuid));
    }

    public IGerdaAdvancedBuilder Configure()
    {
        // Return a no-op builder that immediately returns disabled result
        return new NoOpGerdaAdvancedBuilder();
    }
}

/// <summary>
/// No-op advanced builder that immediately returns disabled result.
/// </summary>
internal sealed class NoOpGerdaAdvancedBuilder : IGerdaAdvancedBuilder
{
    public IGerdaAdvancedBuilder Stages(params GerdaStage[] stages) => this;
    public IGerdaAdvancedBuilder OnProgress(Action<GerdaStageProgress> progress) => this;
    public IGerdaAdvancedBuilder WithTimeout(TimeSpan timeout) => this;

    public Task<GerdaDetailedResult> ExecuteAsync(Guid ticketGuid, CancellationToken cancellationToken = default)
    {
        var disabledOutcome = GerdaOutcome.Disabled(ticketGuid);
        return Task.FromResult(new GerdaDetailedResult
        {
            Outcome = disabledOutcome,
            StageDetails = new Dictionary<GerdaStage, GerdaStageDetail>(),
            TotalDuration = TimeSpan.Zero,
            ExecutionLog = new List<GerdaStageProgress>()
        });
    }
}
