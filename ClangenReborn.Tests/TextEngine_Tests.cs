using BenchmarkDotNet.Attributes;
using ClangenReborn;
using static ClangenReborn.Tests.TestData;


namespace ClangenReborn.Tests;

public static partial class TestData
{

}

public class NameString(string Value) : CatName
{
    public override string GetName() => Value;
    public override bool IsValidName() => true;
}

public class TextEngine_Tests
{

}


[MemoryDiagnoser]
public class TextEngine_Benchmarks
{

}