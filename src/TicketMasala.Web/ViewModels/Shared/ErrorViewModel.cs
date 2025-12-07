using System.Diagnostics;

namespace TicketMasala.Web.ViewModels.Shared;
    public sealed class ErrorViewModel
    {
        public ErrorViewModel(string? requestId = null)
        {
            RequestId = string.IsNullOrWhiteSpace(requestId) ? null : requestId;
        }

        public static ErrorViewModel FromActivity() => new ErrorViewModel(Activity.Current?.Id);

        public string? RequestId { get; init; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
