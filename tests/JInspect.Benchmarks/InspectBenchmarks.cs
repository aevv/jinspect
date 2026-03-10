using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using JInspect.Commands;
using Spectre.Console.Testing;

namespace JInspect.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(Config))]
public class InspectBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            SummaryStyle = SummaryStyle.Default
                .WithRatioStyle(RatioStyle.Trend);
        }
    }

    private string _fixturePath = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixturePath = GenerateLargeFixture.FixturePath;
        GenerateLargeFixture.EnsureExists();

        var sizeMb = new FileInfo(_fixturePath).Length / (1024.0 * 1024.0);
        Console.WriteLine($"Fixture: {_fixturePath} ({sizeMb:F1} MB)");
    }

    [Benchmark(Baseline = true)]
    [Arguments(50)]
    [Arguments(500)]
    [Arguments(5000)]
    public int Inspect(int sampleSize)
    {
        using var console = new TestConsole();
        var command = new InspectCommand(console);
        return command.Run(new InspectSettings
        {
            FilePath = _fixturePath,
            SampleSize = sampleSize,
            MaxDepth = 10,
            InnerSampleSize = 5
        });
    }

    [Benchmark]
    [Arguments(3)]
    [Arguments(5)]
    [Arguments(10)]
    public int InspectVaryDepth(int maxDepth)
    {
        using var console = new TestConsole();
        var command = new InspectCommand(console);
        return command.Run(new InspectSettings
        {
            FilePath = _fixturePath,
            SampleSize = 500,
            MaxDepth = maxDepth
        });
    }
}
