#if DEBUG

#else
using BenchmarkDotNet.Configs;
using static ClangenReborn.Tests.TestData;
using static ClangenReborn.Text;
#endif

namespace ClangenReborn.Tests;

internal class Program
{
    public static void Main(string[] args)
    {
#if DEBUG

#else
        BenchmarkRunner.Run<TextEngine_Benchmarks>();
#endif
    }
}
