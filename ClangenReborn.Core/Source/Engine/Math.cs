using SharpFont;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static ClangenReborn.MathConsts;

namespace ClangenReborn;

public static class MathConsts
{
    public const uint MT19937 = 0x6C078965;
    public const ulong MT19937_64 = 0x5851F42D4C957F2D;
}

public static class MathC
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Lerp(double A, double B, double T) => A + T * (B - A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float A, float B, float T) => A + T * (B - A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LerpInv(double A, double B, double T) => (T - A) / (B - A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LerpInv(float A, float B, float T) => (T - A) / (B - A);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Floor(double X) { int X_I = (int)X; return X < X_I ? X_I - 1 : X_I; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Floor(float X) { int X_I = (int)X; return X < X_I ? X_I - 1 : X_I; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(double X) => X < 0 ? (int)(X - 0.5d) : (int)(X + 0.5d);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Round(float X) => X < 0 ? (int)(X - 0.5f) : (int)(X + 0.5f);


    #region Easing Functions
    // ^ Formulas provided from the BEAUTIFUL website known as https://easings.net/
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EaseInCubic(double T) => T * T * T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EaseOutCubic(double T) => 1 - Math.Pow(1 - T, 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EaseOutExpo(double T) => T == 1 ? 1 : 1 - Math.Pow(2, -10 * T);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EaseInExpo(double T) => T == 0 ? 0 : Math.Pow(2, 10 * T - 10);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double EaseOutQuad(double T) => 1 - (1 - T) * (1 - T);
    #endregion
}

public abstract class RandomGenerator // I work on this whenever I do not know what to do
{
    #region Instantiation
    /// <summary>
    /// Initialise this RNG randomly.
    /// </summary>
    public void Seed() => Seed((uint)Environment.TickCount);

    /// <summary>
    /// Initialise this RNG with an unsigned integer seed.
    /// </summary>
    public abstract void Seed(uint Seed);

    /// <summary>
    /// Initialise this RNG with a string seed.
    /// </summary>
    public unsafe void Seed(string Seed)
    {
        uint Hash = 0;
        fixed (char* SeedPtr = Seed)
        {
            int Index = Seed.Length;
            while (0 < --Index)
                Hash ^= (Hash += (Hash += SeedPtr[Index]) << 10) >> 6;
        }

        this.Seed(Hash += (Hash ^= (Hash += Hash << 3) >> 11) << 15);
    }
    #endregion

    #region Base Methods
    /// <summary>
    /// Generate a non-negative random integer.
    /// </summary>
    /// <returns>A 32-bit unsigned integer greater than or equal to 0 and less than <see cref="uint.MaxValue"/></returns>
    public abstract uint NextUInt32();

    /// <summary>
    /// Generate a non-negative random integer less than the specified maximum.
    /// </summary>
    /// <returns>A 32-bit unsigned integer greater than or equal to 0 and less than <paramref name="Max"/></returns>
    public uint NextUInt32(uint Max)
    {
        ArgumentOutOfRangeException.ThrowIfZero(Max);
        return NextUInt32() / (uint.MaxValue / Max);
    }

    /// <summary>
    /// Generate a non-negative random integer greater than the specified minimum and less than the specified maximum.
    /// </summary>
    /// <returns>A 32-bit unsigned integer greater than or equal to <paramref name="Min"/> and less than <paramref name="Max"/></returns>
    public uint NextUInt32(uint Min, uint Max) => Min + NextUInt32((Min > Max) ? Min - Max : Max - Min);

    /// <summary>
    /// Generate a non-negative random integer.
    /// </summary>
    /// <returns>A 32-bit signed integer greater than or equal to 0 and less than <see cref="int.MaxValue"/></returns>
    public int NextInt32() => (int)(0x7FFFFFFF & NextUInt32());

    /// <summary>
    /// Generate a non-negative random integer less than the specified maximum.
    /// </summary>
    /// <returns>A 32-bit signed integer greater than or equal to 0 and less than <paramref name="Max"/></returns>
    public int NextInt32(int Max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Max, nameof(Max));
        return NextInt32() / (int.MaxValue / Max);
    }

    /// <summary>
    /// Generate a random integer greater than the specified minimum and less than the specified maximum.
    /// </summary>
    /// <returns>A 32-bit signed integer greater than or equal to <paramref name="Min"/> and less than <paramref name="Max"/></returns>
    public int NextInt32(int Min, int Max) => (Min > Max) ? Max + NextInt32(Min - Max) : Min + NextInt32(Max - Min);

    /// <summary>
    /// Generate a non-negative random integer.
    /// </summary>
    /// <returns>A 64-bit unsigned integer greater than or equal to 0 and less than <see cref="ulong.MaxValue"/></returns>
    public ulong NextUInt64() => (ulong)NextUInt32() << 32 | NextUInt32();
    public ulong NextUInt64(ulong Max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Max, nameof(Max));
        return NextUInt64() / (ulong.MaxValue / Max);
    }

    public ulong NextUInt64(ulong Min, ulong Max) => (Min > Max) ? Max + NextUInt64(Min - Max) : Min + NextUInt64(Max - Min);

    public long NextInt64() => (long)(0x7FFFFFFFFFFFFFFFL & NextUInt64());
    public long NextInt64(long Max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Max, nameof(Max));
        return NextInt64() / (int.MaxValue / Max);
    }
    public long NextInt64(long Min, long Max) => (Min > Max) ? Max + NextInt64(Min - Max) : Min + NextInt64(Max - Min);

    public double NextDouble() => NextUInt32() * (1.0d / (uint.MaxValue + 1.0));
    public double NextDoubleSigned() => (2 * NextDouble()) - 1;
    public double NextDouble(double Max) => Max * NextDouble();
    public double NextDouble(double Min, double Max) => Min + ((Max - Min) * NextDouble());

    public float NextSingle() => (NextUInt32() >> 8) * (1.0f / (1u << 24));
    public float NextSingleSigned() => (2 * NextSingle()) - 1;
    public float NextSingle(float Max) => Max * NextSingle();
    public float NextSingle(float Min, float Max) => Min + ((Max - Min) * NextSingle());

    public bool NextBoolean() => (NextUInt32() & 1) == 0;

    public BigInteger NextBigInt(int BitCount, bool IsUnsigned = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(BitCount, nameof(BitCount));

        Span<byte> Bytes = stackalloc byte[(int)Math.Ceiling(BitCount / 8.0f)];
        Fill(Bytes);

        if ((BitCount & 0b111) != 0)
            Bytes[^1] = (byte)(Bytes[^1] & ~((1 << (BitCount & 0b111)) - 1));

        return new(Bytes, IsUnsigned);
    }

    public T NextInteger<T>() where T : IBinaryInteger<T>, IMinMaxValue<T>
    {
        int BitCount = int.CreateChecked(T.PopCount(T.MaxValue));
        T Result = T.Zero;

        if (BitCount <= 32)
        {
            Result = T.CreateTruncating((int)NextUInt32() >> (32 - BitCount));
        }
        else
        {
            while ((BitCount -= 32) > 32)
            {
                Result = (Result << 32) | T.CreateTruncating(NextUInt32());
            }

            Result = (Result << 32) | (T.CreateTruncating(NextUInt32() >> (32 - BitCount)));
        }

        return Result;
    }
    public T NextInteger<T>(T Max) where T : IBinaryInteger<T>, IMinMaxValue<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Max);
        int BitCount = Max.GetShortestBitLength();

        T Result = T.Zero;
        while (BitCount > 32)
        {
            Result = (Result << 32) | T.CreateTruncating(NextUInt32());
            BitCount -= 32;
            Max >>= 32;
        }

        return (Result << 32) | (T.CreateTruncating(NextUInt32(uint.CreateChecked(Max))));
    }
    public T NextInteger<T>(T Min, T Max) where T : IBinaryInteger<T>, IMinMaxValue<T> => (Min > Max) ? Max + NextInteger(Min - Max) : Min + NextInteger(Max - Min);

    public T NextFloating<T>() where T : IFloatingPoint<T>, IBinaryNumber<T>, IMinMaxValue<T>
    {
        int SignificandBits = T.AllBitsSet.GetSignificandBitLength();
        int ExponentBits = T.AllBitsSet.GetExponentShortestBitLength();

        T _ = T.CreateChecked(4294967296);

        T Significand = T.Zero;
        if (SignificandBits <= 32)
        {
            Significand = T.CreateTruncating((int)NextUInt32() >> (32 - SignificandBits));
        }
        else
        {
            int Loops = SignificandBits;
            while ((Loops -= 32) > 32)
                Significand = (Significand * _) | T.CreateTruncating(NextUInt32());

            Significand = (Significand * _) | (T.CreateTruncating(NextUInt32() >> (32 - Loops)));
        }

        Significand /= T.One + T.One;

        T Result;
        if (ExponentBits <= 32)
        {
            int Exponent = (int)NextUInt32() >> (32 - ExponentBits);
            Result = Significand;

            T __ = (Exponent < 0) ? T.One / (T.One + T.One) : (T.One + T.One);
            int __2 = (Exponent < 0) ? 1 : -1;

            while (Exponent != 0)
            {
                Result *= __;
                Exponent += __2;
            }
        }
        else
        {
            throw new OverflowException(); // TODO Implement for expontents over 32 bits
        }

        return Result;
    }
    #endregion

    #region Chances
    public bool RollChance(double Value) => (Value >= 1.0d) || (Value > 0.0d && NextDouble() < Value);
    public bool RollChance(float Value) => (Value >= 1.0f) || (Value > 0.0f && NextSingle() < Value);
    public bool RollChanceEx2(int Exponent)
    {
        Exponent = (Exponent + (Exponent >> 31)) ^ (Exponent >> 31);
        while (31 < Exponent)
        {
            if (0 < NextUInt32()) return false;
            Exponent -= 32;
        }

        return (uint)(NextDouble() * (1u << Exponent)) == 0;
    }
    #endregion

    #region Fill
    public void Fill(byte[] Buffer) => Fill(Buffer.AsSpan());
    public void Fill(byte[] Buffer, int StartIndex, int Length) => Fill(Buffer.AsSpan(), StartIndex, Length);

    public unsafe void Fill(Span<byte> Buffer)
    {
        while (Buffer.Length >= sizeof(uint))
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(Buffer), NextUInt32());
            Buffer = Buffer[sizeof(uint)..];
        }

        if (!Buffer.IsEmpty)
        {
            uint Next = NextUInt32();
            byte* RemainingBytes = (byte*)&Next;
            for (int I = 0; I < Buffer.Length; I++)
                Buffer[I] = RemainingBytes[I];
        }
    }
    public unsafe void Fill(Span<byte> Buffer, int StartIndex, int Length)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Shuffles
    public void Shuffle<T>(T[] Values) => Shuffle(Values.AsSpan());
    public void Shuffle<T>(Span<T> Values)
    {
        int N = Values.Length;
        while (N-- > 1)
        {
            int I = NextInt32(N + 1);
            (Values[N], Values[I]) = (Values[I], Values[N]);
        }
    }
    public Span<T> Shuffle<T>(ReadOnlySpan<T> Values)
    {
        Span<T> New = Values.AsSpan();

        int N = Values.Length;
        while (N-- > 1)
        {
            int I = NextInt32(N + 1);
            (New[N], New[I]) = (New[I], New[N]);
        }

        return New;
    }
    public void Shuffle<T>(IList<T> Values)
    {
        int N = Values.Count;
        while (N-- > 1)
        {
            int I = NextInt32(N + 1);
            (Values[N], Values[I]) = (Values[I], Values[N]);
        }
    }
    public T[] Shuffle<T>(IReadOnlyList<T> Values)
    {
        T[] New = new T[Values.Count];

        int N = Values.Count;
        while (N-- > 1)
        {
            int I = NextInt32(N + 1);
            (New[N], New[I]) = (New[I], New[N]);
        }

        return New;
    }
    #endregion

    #region Get
    protected int GetIndex(double[] Weights)
    {
        double RemainingDistance = NextDouble() * Weights.Sum();
        for (int I = 0; I < Weights.Length; I++) if ((RemainingDistance -= Weights[I]) < 0) return I;
        throw new NotImplementedException(); // Should never be called, but just incase.
    }

    public T GetEnum<T>() where T : struct, Enum => GetItem(Enum.GetValues<T>());
    public T[] GetEnums<T>(int Population) where T : struct, Enum => GetItems(Enum.GetValues<T>(), Population);

    public object? GetItem(Array Array) => Array.GetValue(NextInt32(Array.Length));
    public T GetItem<T>(T[] Array) => Array[NextInt32(Array.Length)];
    public T GetItem<T>(Span<T> Span) => Span[NextInt32(Span.Length)];
    public T GetItem<T>(ReadOnlySpan<T> Span) => Span[NextInt32(Span.Length)];
    public T GetItem<T>(IList<T> Sample) => Sample[NextInt32(Sample.Count)];
    public void GetItem<T>(IReadOnlyList<T> Sample, out T Results) => Results = Sample[NextInt32(Sample.Count)];


    public T GetItem<T>(T[,] Array) => Array[NextInt32(Array.GetLength(0)), NextInt32(Array.GetLength(1))];
    public T GetItem<T>(T[,,] Array) => Array[NextInt32(Array.GetLength(0)), NextInt32(Array.GetLength(1)), NextInt32(Array.GetLength(2))];
    public T GetItem<T>(T[,,,] Array) => Array[NextInt32(Array.GetLength(0)), NextInt32(Array.GetLength(1)), NextInt32(Array.GetLength(2)), NextInt32(Array.GetLength(3))];


    public T GetItem<T>(T[][] Array) => GetItem(GetItem<T[]>(Array));
    public T GetItem<T>(IList<IList<T>> Sample) => GetItem(GetItem<IList<T>>(Sample));
    public void GetItem<T>(IReadOnlyList<IReadOnlyList<T>> Sample, out T Results) { GetItem(Sample, out IReadOnlyList<T> F); GetItem(F, out Results); }
 

    public T GetItem<T>(T[][][] Array) => GetItem(GetItem<T[]>(GetItem<T[][]>(Array)));
    public T GetItem<T>(IList<IList<IList<T>>> Sample) => GetItem(GetItem<IList<T>>(GetItem<IList<IList<T>>>(Sample)));
    public void GetItem<T>(IReadOnlyList<IReadOnlyList<IReadOnlyList<T>>> Sample, out T Results) 
    { 
        GetItem(Sample, out IReadOnlyList<IReadOnlyList<T>> A); GetItem(A, out IReadOnlyList<T> B); GetItem(B, out Results);
    }

     
    public T[] GetItems<T>(T[] Array, int Population)
    {
        T[] Results = new T[Array.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItem(Array);
        return Results;
    }
    public T[] GetItems<T>(Span<T> Span, int Population)
    {
        T[] Results = new T[Span.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItem(Span);
        return Results;
    }
    public T[] GetItems<T>(ReadOnlySpan<T> Span, int Population)
    {
        T[] Results = new T[Span.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItem(Span);
        return Results;
    }
    public T[] GetItems<T>(IList<T> Sample, int Population)
    {
        T[] Results = new T[Sample.Count];
        for (int I = 0; I < Population; I++) Results[I] = GetItem(Sample);
        return Results;
    }
    public void GetItems<T>(IReadOnlyList<T> Sample, int Population, out T[] Results)
    {
        Results = new T[Sample.Count];
        for (int I = 0; I < Population; I++) GetItem(Sample, out Results[I]);
    }



    public T GetItemBiased<T>(T[] Array, double[] Weights) => Array[GetIndex(Weights)];
    public T GetItemBiased<T>(Span<T> Span, double[] Weights) => Span[GetIndex(Weights)];
    public T GetItemBiased<T>(ReadOnlySpan<T> Span, double[] Weights) => Span[GetIndex(Weights)];
    public T GetItemBiased<T>(IList<T> Sample, double[] Weights) => Sample[GetIndex(Weights)];
    public void GetItemBiased<T>(IReadOnlyList<T> Sample, double[] Weights, out T Result) => Result = Sample[GetIndex(Weights)];



    public T[] GetItemsBiased<T>(T[] Array, double[] Weights, uint Population)
    {
        T[] Results = new T[Array.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItemBiased(Array, Weights);
        return Results;
    }
    public T[] GetItemsBiased<T>(Span<T> Span, double[] Weights, uint Population)
    {
        T[] Results = new T[Span.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItemBiased(Span, Weights);
        return Results;
    }    
    public T[] GetItemsBiased<T>(ReadOnlySpan<T> Span, double[] Weights, uint Population)
    {
        T[] Results = new T[Span.Length];
        for (int I = 0; I < Population; I++) Results[I] = GetItemBiased(Span, Weights);
        return Results;
    }
    public T[] GetItemsBiased<T>(IList<T> Sample, double[] Weights, uint Population)
    {
        T[] Results = new T[Sample.Count];
        for (int I = 0; I < Population; I++) Results[I] = GetItemBiased(Sample, Weights);
        return Results;
    }
    public void GetItemsBiased<T>(IReadOnlyList<T> Sample, double[] Weights, uint Population, out T[] Results)
    {
        Results = new T[Sample.Count];
        for (int I = 0; I < Population; I++) GetItemBiased(Sample, Weights, out Results[I]);
    }



    public T GetItemExcept<T>(T[] Array, T Exception)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Array.Length, 2, nameof(Array));
        int Index = NextInt32(Array.Length);
        return Array[Index]!.Equals(Exception) ? Array[Index + (Index == 0 ? 1 : -1)] : Array[Index];
    }
    public T GetItemExcept<T>(Span<T> Span, T Exception)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Span.Length, 2, nameof(Span));
        int Index = NextInt32(Span.Length);
        return Span[Index]!.Equals(Exception) ? Span[Index + (Index == 0 ? 1 : -1)] : Span[Index];
    }
    public T GetItemExcept<T>(ReadOnlySpan<T> Span, T Exception)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Span.Length, 2, nameof(Span));
        int Index = NextInt32(Span.Length);
        return Span[Index]!.Equals(Exception) ? Span[Index + (Index == 0 ? 1 : -1)] : Span[Index];
    }
    public T GetItemExcept<T>(IList<T> Sample, T Exception)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Sample.Count, 2, nameof(Sample));
        int Index = NextInt32(Sample.Count);
        return Sample[Index]!.Equals(Exception) ? Sample[Index + (Index == 0 ? 1 : -1)] : Sample[Index];
    }
    public void GetItemExcept<T>(IReadOnlyList<T> Sample, T Exception, out T Result)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Sample.Count, 2, nameof(Sample));
        int Index = NextInt32(Sample.Count);
        Result = Sample[Index]!.Equals(Exception) ? Sample[Index + (Index == 0 ? 1 : -1)] : Sample[Index];
    }
    #endregion
}

public interface IRandomTable : IEnumerable
{
    int Count { get; }

    public object GetItem();
    public object[] GetItems(int Population)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Population, nameof(Population));

        object[] Results = new object[Population];
        for (int I = 0; I < Population; I++) Results[I] = GetItem();
        return Results;
    }
    public void Fill(int Population, out object[] Results) => Results = GetItems(Population);
    public void Fill(object[] Buffer) { for (int I = 0; I < Buffer.Length; I++) Buffer[I] = GetItem(); }
    public void Fill(object[] Buffer, int StartIndex, int Count) { for (int I = 0; I < Count; I++) Buffer[StartIndex + I] = GetItem(); }
}

public interface IRandomTable<T> : IRandomTable, IReadOnlyList<T>
{
    object IRandomTable.GetItem() => GetItem()!;

    public new T GetItem();
    public new T[] GetItems(int Population)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Population, nameof(Population));

        T[] Results = new T[Population];
        for (int I = 0; I < Population; I++) Results[I] = GetItem();
        return Results;
    }
    public void Fill(int Population, out T[] Results) => Results = GetItems(Population);
    public void Fill(T[] Buffer) { for (int I = 0; I < Buffer.Length; I++) Buffer[I] = GetItem(); }
    public void Fill(T[] Buffer, int StartIndex, int Count) { for (int I = 0; I < Count; I++) Buffer[StartIndex + I] = GetItem(); }
}

public interface IRandomBiasedTable<T> : IRandomTable<T>, IReadOnlyList<T>
{
    public double[] GetProbabilityTable();
}

/// <summary>
/// Helper class for random functions.
/// </summary>
public class XorRandom : RandomGenerator // TODO MAYBE optimize for funsies (currently equal or slower than existing Random)
{
    private uint S0, S1, S2, S3;
    private uint T0, T1;
    private uint BitBuffer;
    private uint BitMask;

    public static XorRandom Shared { get; } = new(); // TODO Ensure Thread Safety

    public XorRandom() : this((uint)Environment.TickCount) { }
    public XorRandom(uint Seed) => this.Seed(Seed);
    public XorRandom(uint X, uint Y, uint Z, uint W) => this.Seed(X, Y, Z, W);

    public override int GetHashCode() => unchecked((int)(this.S3 >> 19 ^ this.S0 ^ this.S0 << 11 ^ (this.S0 ^ this.S0 << 11) >> 8));
    public override bool Equals(object? obj) => obj is XorRandom objXRandom && this == objXRandom;
    public override string ToString() => $"XorRandom({this.S0}, {this.S1}, {this.S2}, {this.S3})";

    public static bool operator ==(XorRandom A, XorRandom B) => A.S0 == B.S0 && A.S1 == B.S1 && A.S2 == B.S2 && A.S3 == B.S3 && A.BitBuffer == B.BitBuffer && A.BitMask == B.BitMask;
    public static bool operator !=(XorRandom A, XorRandom B) => !(A == B);

    /// <summary>
    /// Create a copy of the current <see cref="XorRandom"/>.
    /// </summary>
    public XorRandom Copy() => new() { S0 = this.S0, S1 = this.S1, S2 = this.S2, S3 = this.S3, T0 = this.T0, T1 = this.T1, BitBuffer = this.BitBuffer, BitMask = this.BitMask };

    /// <summary>
    /// Seeds the first element of state to <paramref name="Seed"/>, with the rest being derived from <paramref name="Seed"/>.
    /// </summary>
    public override void Seed(uint Seed)
    {
        this.S3 = unchecked(MT19937 * (this.S2 = MT19937 * (this.S1 = MT19937 * (this.S0 = Seed) + 1) + 1) + 1);
        while ((this.S0 | this.S1 | this.S2 | this.S3) == 0)
            this.S3 = unchecked(MT19937 * (this.S2 = MT19937 * (this.S1 = MT19937 * (this.S0 = Seed) + 1) + 1) + 1);

        this.T1 = this.T0 = 0; this.BitMask = 1;
    }

    /// <summary>
    /// Seeds the whole state.
    /// </summary>
    public void Seed(uint X, uint Y, uint Z, uint W)
    {
        this.S0 = X; this.S1 = Y; this.S2 = Z; this.S3 = W; this.T1 = this.T0 = 0; this.BitMask = 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override uint NextUInt32()
    {
        this.T0 = this.S0 ^ this.S0 << 11; this.S0 = this.S1; this.S1 = this.S2; this.S2 = this.S3; return this.S3 ^= this.S3 >> 19 ^ this.T0 ^ this.T0 >> 8;
    }

    //public new bool NextBoolean() => ((this.BitMask == 1 ? this.BitBuffer = NextUInt32() : this.BitBuffer) & (this.BitMask = (this.BitMask >> 1) | (this.BitMask << 31))) == 0;
}

public sealed class XorTable<T> : IRandomTable<T>
{
    private readonly T[] Items;
    private readonly double RealConstant;
    private uint S0, S1, S2, S3;
    private uint T0;

    public static XorTable<TResult> Create<TResult>(params TResult[] Enumerable) => new(Enumerable);
    public static XorTable<TResult> Create<TResult>(IEnumerable<TResult> Enumerable, uint? Seed = null) => new([.. Enumerable], Seed);
    public static XorTable<TEnum> Create<TEnum>(uint? Seed = null) where TEnum : struct, Enum => new(Enum.GetValues<TEnum>(), Seed);

    private XorTable(IEnumerable<T> Items) : this(Items, (uint)((1 + Items.GetHashCode()) * (1 << Items.Count()))) { }
    private XorTable(IEnumerable<T> Items, uint? Seed = null)
    {
        if ((this.Items = [.. Items]).Length == 0) throw new ArgumentException("Items have no items", nameof(Items));
        if (Seed is not null) ArgumentOutOfRangeException.ThrowIfZero(Seed.Value, nameof(Seed));

        Seed ??= (uint)Environment.TickCount;

        this.RealConstant = (this.Count = this.Items.Length) * (1.0d / (int.MaxValue + 1.0));
        this.S3 = unchecked(MT19937 * (this.S2 = MT19937 * (this.S1 = MT19937 * (this.S0 = Seed.Value) + 1) + 1) + 1);
    }

    public int Count { get; private init; }

    public T this[int Index] => this.Items[Index];
    public T GetItem()
    {
        this.T0 = this.S0 ^ this.S0 << 11; this.S0 = this.S1; this.S1 = this.S2; this.S2 = this.S3;
        return this.Items[(uint)(this.RealConstant * (this.S3 ^= this.S3 >> 19 ^ this.T0 ^ this.T0 >> 8))];
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int I = 0; I < this.Count; I++) yield return this.Items[I];
    }
    IEnumerator IEnumerable.GetEnumerator() => this.Items.GetEnumerator();
}

public sealed class XorWeightedTable<T> : IRandomBiasedTable<T>
{
    private readonly T[] Items;
    private readonly int[] Alias;
    private readonly double[] Probability;
    private uint S0, S1, S2, S3;
    private uint T0;

    public static XorWeightedTable<TResult> Create<TResult>(IEnumerable<TResult> Enumerable, double[] Weights, uint? Seed = null) => new(Enumerable, Weights, Seed);
    public static XorWeightedTable<TEnum> Create<TEnum>(double[] Weights, uint? Seed = null) where TEnum : struct, Enum => new(Enum.GetValues<TEnum>(), Weights, Seed);

    public XorWeightedTable(IEnumerable<T> Items, double[] Weights) : this(Items, Weights, (uint)((1 + Items.GetHashCode()) ^ (1 << (Weights.Length % 32)))) { }
    public XorWeightedTable(IEnumerable<T> Items, double[] Weights, uint? Seed = null)
    {
        Seed ??= (uint)Environment.TickCount;

        this.Items = Items.ToArray();
        this.Count = Weights.Length;
        this.Alias = new int[this.Count];
        this.Probability = new double[this.Count];

        double Average = 1.0d / this.Count;
        Stack<int> Small = [], Large = [];

        for (int I = 0; I < this.Count; I++) (Weights[I] >= Average ? Large : Small).Push(I);
        while (0 < Small.Count && 0 < Large.Count)
        {
            int Less = Small.Pop(), More = Large.Pop();
            this.Probability[Less] = Weights[Less] * this.Count;
            this.Alias[Less] = More;

            (((Weights[More] += Weights[Less] - Average) >= Average) ? Large : Small).Push(More);
        }
        while (0 < Large.Count) this.Probability[Large.Pop()] = 1;
        while (0 < Small.Count) this.Probability[Small.Pop()] = 1;
        
        this.S3 = unchecked(MT19937 * (this.S2 = MT19937 * (this.S1 = MT19937 * (this.S0 = Seed.Value) + 1) + 1) + 1);
    }

    public int Count { get; private init; }
    public T this[int Index] => this.Items[Index];
    public T GetItem()
    {
        this.T0 = this.S0 ^ this.S0 << 11; this.S0 = this.S1; this.S1 = this.S2; this.S2 = this.S3;
        int Column = (int)((1.0d / (uint.MaxValue + 1.0)) * (this.S3 ^= this.S3 >> 19 ^ this.T0 ^ this.T0 >> 8) * this.Count);
        this.T0 = this.S0 ^ this.S0 << 11; this.S0 = this.S1; this.S1 = this.S2; this.S2 = this.S3;
        return this.Items[((1.0d / (uint.MaxValue + 1.0)) * (this.S3 ^= this.S3 >> 19 ^ this.T0 ^ this.T0 >> 8)) < this.Probability[Column] ? Column : this.Alias[Column]];
    }

    public double[] GetProbabilityTable() => this.Probability;

    public IEnumerator<T> GetEnumerator()
    {
        for (int I = 0; I < this.Count; I++) yield return this.Items[I];
    }
    IEnumerator IEnumerable.GetEnumerator() => this.Items.GetEnumerator();
    
}