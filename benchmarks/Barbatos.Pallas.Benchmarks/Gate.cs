// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// Runs the benchmarks and fails when one misses its target (<see cref="TargetAttribute"/>) by more than the margin.
/// </summary>
/// <remarks>
/// The benchmarks run in the process that runs them: a benchmark project BenchmarkDotNet generates would sit inside the
/// repository and be built with its analyzers, which its generated code does not satisfy. What is compared is the 99th
/// percentile of each benchmark's measured iterations - every iteration the mean of many calls - so one slow iteration
/// counts and a single slow call among millions does not.
/// </remarks>
internal static class Gate
{
    /// <summary>How far past its target a benchmark may be on the machine that runs the gate.</summary>
    /// <remarks>
    /// A hosted build machine is slower than a developer's and its timings move from one run to the next, which is why
    /// the gate compares against a target rather than a baseline (maintainer, 24 Sep 2026). The targets of §9 are the
    /// application's; a benchmark's measured p99 sits well inside its target on a developer's machine, and the gate
    /// allows a hosted one half as much again before it fails.
    /// </remarks>
    public const double Margin = 1.5;

    /// <summary>The configuration: in process, with the allocations, a table in the console and GitHub markdown.</summary>
    /// <param name="fixedIterations">Whether every benchmark runs the same number of iterations, as the gate does.</param>
    public static IConfig Config(bool fixedIterations)
    {
        Job job = Job.Default.WithToolchain(InProcessEmitToolchain.Instance);
        if (fixedIterations)
        {
            job = job.WithWarmupCount(3).WithIterationCount(20);
        }

        return ManualConfig.CreateEmpty()
            .AddJob(job)
            .AddColumnProvider(DefaultColumnProviders.Instance)
            .AddColumn(StatisticColumn.P95)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddLogger(ConsoleLogger.Default)
            .AddExporter(MarkdownExporter.GitHub)
            .WithArtifactsPath(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts"));
    }

    /// <summary>Runs the benchmarks the arguments select, or all of them, and checks each against its target.</summary>
    /// <param name="arguments">BenchmarkDotNet's arguments, such as <c>--filter *Graph*</c>; none runs every benchmark.</param>
    /// <returns>0 when every benchmark ran within its target and the margin; 1 otherwise.</returns>
    public static int Run(string[] arguments)
    {
        BenchmarkSwitcher switcher = BenchmarkSwitcher.FromAssembly(typeof(Gate).Assembly);
        IEnumerable<Summary> summaries = arguments.Length == 0 ? switcher.RunAll(Config(fixedIterations: true)) : switcher.Run(arguments, Config(fixedIterations: true));

        StringBuilder table = new();
        table.AppendLine("| Benchmark | p99 (ms) | Target (ms) | Limit (ms) | |");
        table.AppendLine("|---|---:|---:|---:|---|");
        List<string> failures = [];
        foreach (BenchmarkReport report in summaries.SelectMany(summary => summary.Reports))
        {
            string name = $"{report.BenchmarkCase.Descriptor.Type.Name}.{report.BenchmarkCase.Descriptor.WorkloadMethod.Name} {report.BenchmarkCase.Parameters.DisplayInfo}".Trim();
            TargetAttribute? target = report.BenchmarkCase.Descriptor.WorkloadMethod.GetCustomAttribute<TargetAttribute>();
            if (target is null)
            {
                failures.Add($"{name} has no target.");
                table.AppendLine(CultureInfo.InvariantCulture, $"| {name} | - | - | - | no target |");
                continue;
            }

            if (!report.Success || report.ResultStatistics is null)
            {
                failures.Add($"{name} did not run.");
                table.AppendLine(CultureInfo.InvariantCulture, $"| {name} | - | {target.Milliseconds} | {target.Milliseconds * Margin} | did not run |");
                continue;
            }

            double p99 = report.ResultStatistics.Percentiles.Percentile(99) / 1e6;
            string verdict = p99 <= target.Milliseconds ? "within its target" : p99 <= target.Milliseconds * Margin ? "within the margin" : "**missed**";
            if (p99 > target.Milliseconds * Margin)
            {
                failures.Add(string.Create(CultureInfo.InvariantCulture, $"{name}: {p99:G4} ms, beyond {target.Milliseconds * Margin} ms."));
            }

            table.AppendLine(CultureInfo.InvariantCulture, $"| {name} | {p99:G4} | {target.Milliseconds} | {target.Milliseconds * Margin} | {verdict} |");
        }

        Console.WriteLine();
        Console.WriteLine(table);
        string? stepSummary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(stepSummary))
        {
            File.AppendAllText(stepSummary, "### Benchmarks against their targets\n\n" + table + "\n");
        }

        foreach (string failure in failures)
        {
            Console.Error.WriteLine(failure);
        }

        return failures.Count == 0 ? 0 : 1;
    }
}
