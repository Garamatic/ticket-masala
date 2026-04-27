using Xunit;

// Enable parallelization at assembly level - collections run in parallel, tests within a collection run sequentially
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, MaxParallelThreads = 4)]
