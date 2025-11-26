using System.Diagnostics;

namespace IT_Project2526.ViewModels
{
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
}
