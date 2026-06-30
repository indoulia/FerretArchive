# Performance Checklist

Run on any PR that touches: index build pipeline, context assembly, knowledge query scoring, model invocation, or file system hot paths. Mark: ✓ Pass | ✗ Fail | N/A.

## Blocking Anti-Patterns
- [ ] No `Task.Result` or `.GetAwaiter().GetResult()` — synchronous blocking on async is a Blocker
- [ ] No unbounded loops without a cancellation check between iterations
- [ ] No `Thread.Sleep` or `Task.Delay` in hot paths
- [ ] No `lock` held across an `await` — use `SemaphoreSlim` instead

## Allocation
- [ ] No per-file or per-node object allocation in the index dispatch loop without pooling
- [ ] No `string.Format` or string interpolation in hot paths — use `StringBuilder` or structured logging
- [ ] Collections sized at construction where count is known — no repeated `Add` to an unsized `List<T>` in a loop

## Instrumentation
- [ ] New operation has a corresponding `Histogram` metric for duration (ARCH-012 §5)
- [ ] New operation has a corresponding `Counter` metric for invocations and errors
- [ ] `ActivitySource.StartActivity` creates a span for any operation > 10ms expected duration

## Parallelism
- [ ] Parser dispatch in the index pipeline uses `Task.WhenAll` — not sequential `await` in a loop
- [ ] No engine that performs parallel work blocks the thread pool (all async)
- [ ] Parallel degree is bounded — `Parallel.ForEachAsync` with a `ParallelOptions.MaxDegreeOfParallelism`

## Baseline Comparison
- [ ] If this PR modifies a measured hot path: a benchmark exists in `tests/Ferret.Performance.Tests/`
- [ ] Benchmark has been run before and after the change
- [ ] Post-change p99 latency is within 20% of baseline (regressions > 20% are Blockers)
- [ ] Baseline result committed to `docs/011-Performance/` if this is a new benchmark

## Memory
- [ ] No event handler registered that prevents garbage collection of large objects (event handler leak)
- [ ] `IAsyncDisposable` implemented for any type that holds pooled resources
- [ ] `ArrayPool<T>` or `MemoryPool<T>` used for large temporary buffers in hot paths

## Severity of Findings
- **Blocker**: Synchronous blocking on async, regression > 20% vs baseline
- **Suggestion**: Allocation improvement, missing instrumentation, parallelism opportunity
