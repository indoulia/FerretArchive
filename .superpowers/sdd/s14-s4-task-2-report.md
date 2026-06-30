# S14-S4 Task 2 Report — `--log-level` global option

**Status:** DONE

**Commits:**
- `b31a2b4` feat(sprint-14): --log-level global option wired to ILoggerFactory

**Test summary:** 4 new tests in `GlobalOptionsTests.cs` (option name, visibility, AddAll wiring, integration smoke test with `--log-level debug version`); 170/170 total pass.

**Concerns:**
- The brief's `AddAll_Adds_LogLevel_To_Root` test used `o.Name == "log-level"` but System.CommandLine 2.0.9 returns `Name == "--log-level"` (with dashes); corrected to match actual API.
- CA1308 (`ToLowerInvariant`) triggered; switched to `ToUpperInvariant` with uppercase switch cases.

**Report file:** <repo-root>\.superpowers\sdd\s14-s4-task-2-report.md

---

## Review-Finding Fixes

**Status:** DONE

**How each finding was resolved:**

- **Critical 1 — Default is NullLoggerFactory, not Information:**
  Added `DefaultValueFactory = _ => "Information"` to the `LogLevel` option in `GlobalOptions.cs`. Additionally changed `BuildLoggerFactory` to use `(logLevel ?? "Information").ToUpperInvariant()` eliminating the null/empty early-return branch that previously returned `NullLoggerFactory.Instance`.

- **Critical 2 — Unknown value maps to Warning, not Information:**
  Changed the switch default case from `LogLevel.Warning` to `LogLevel.Information` in `RootCommandFactory.BuildLoggerFactory`.

- **Important — ILoggerFactory never disposed:**
  Changed `var loggerFactory = BuildLoggerFactory(...)` to `using var loggerFactory = BuildLoggerFactory(...)` in `RegisterHandlerAction`. The factory is now disposed at the end of the handler's async scope. Also removed the now-unused `using Microsoft.Extensions.Logging.Abstractions` import.

**Tests added (3 new):**
- `LogLevel_Option_DefaultValue_Is_Information` — parses with no arg, asserts value = "Information"
- `LogLevel_Omitted_DoesNot_Throw` — full CLI invocation without `--log-level`
- `LogLevel_UnknownValue_FallsThrough_To_Information` — unknown value `verbosely_loud` succeeds

**Test count:** 173/173 pass.
