using Xunit;

// Enable parallelization at assembly level
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, MaxParallelThreads = 4)]

// Disable parallelization for specific test collections that need isolation
// (Database tests already use [Collection("Database")] which enforces sequential within)
