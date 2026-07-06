using Xunit;

// WorkspaceE2ETests mutates the process-wide Environment.CurrentDirectory across its tests; running
// test collections in parallel in this assembly lets that global state race with whatever else is
// executing concurrently. Integration tests exercise real filesystem/process state anyway, so
// serializing this one assembly is the standard, low-cost fix.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
