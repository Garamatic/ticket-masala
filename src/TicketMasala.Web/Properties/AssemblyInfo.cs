using System.Runtime.CompilerServices;

// Allow the test project to access internal members for testing the deep module implementation
[assembly: InternalsVisibleTo("TicketMasala.Tests")]

// Allow the domain tests project to access internals if needed
[assembly: InternalsVisibleTo("TicketMasala.Domain.Tests")]

// Allow Moq to mock internal interfaces (required for proxy generation)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
