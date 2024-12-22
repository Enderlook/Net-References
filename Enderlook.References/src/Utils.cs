using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.References;

internal static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref byte GetReference<T>(T[,] array, int index1, int index2)
    {
        try
        {
            return ref Unsafe.As<T, byte>(ref array[index1, index2]);
        }
        catch (IndexOutOfRangeException exception)
        {
            ThrowArgumentException_IndexOutOfBounds(exception);
#if NET5_0_OR_GREATER
            return ref Unsafe.NullRef<byte>();
#else
            return ref Unsafe.AsRef<byte>(null);
#endif
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref byte GetReference<T>(T[,,] array, int index1, int index2, int index3)
    {
        try
        {
            return ref Unsafe.As<T, byte>(ref array[index1, index2, index3]);
        }
        catch (IndexOutOfRangeException exception)
        {
            ThrowArgumentException_IndexOutOfBounds(exception);
#if NET5_0_OR_GREATER
            return ref Unsafe.NullRef<byte>();
#else
            return ref Unsafe.AsRef<byte>(null);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref byte GetReference<T>(T[,,,] array, int index1, int index2, int index3, int index4)
    {
        try
        {
            return ref Unsafe.As<T, byte>(ref array[index1, index2, index3, index4]);
        }
        catch (IndexOutOfRangeException exception)
        {
            ThrowArgumentException_IndexOutOfBounds(exception);
#if NET5_0_OR_GREATER
            return ref Unsafe.NullRef<byte>();
#else
            return ref Unsafe.AsRef<byte>(null);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateIndex(Array array, ReadOnlySpan<int> indexes)
    {
        int index = 0;
        int m = 1;
        for (int i = indexes.Length - 1; i >= 0; i--)
        {
            int subIndex = indexes[i];
            int lowerBound = array.GetLowerBound(i);
            int upperBound = array.GetUpperBound(i);
            if (subIndex < lowerBound || subIndex > upperBound)
                ThrowArgumentException_OwnerIndexOutOfBounds();
            index += m * (subIndex - lowerBound);
            m *= 1 + upperBound - lowerBound;
        }
        return index;
    }

    public static void ThrowArgumentException_ArrayIndexesOutOfBounds()
        => throw new ArgumentException("Index is outside array's bounds", "indexes");

    public static void ThrowArgumentException_ArrayIndexesLengthDoesNotMatchRank()
        => throw new ArgumentException("indexes", "Array rank doesn't matches with indexes length.");

    public static void ThrowArgumentException_FieldInfoNotBelongToType()
        => throw new ArgumentException("FieldInfo doesn't belong to the type.", "fieldInfo");

    public static void ThrowArgumentException_FieldInfoFieldTypeIsNotTReference()
        => throw new ArgumentException("FieldInfo's field type is not the same as TReference.", "fieldInfo");

    public static void ThrowArgumentException_IndexMustBeLowerThanArrayLength()
        => throw new ArgumentException("Index must be lower than array's length.", "index");

    public static void ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength()
        => throw new ArgumentException("Index must be lower than memory owners's memory's length.", "index");

    public static void ThrowArgumentException_IndexMustBeLowerThanMemoryLength()
        => throw new ArgumentException("Index must be lower than memory's length.", "index");

    public static void ThrowArgumentException_IndexMustBeLowerThanSegmentCount()
        => throw new ArgumentException("Index must be lower than segment's count.", "index");

    public static void ThrowArgumentException_IndexOutOfBounds()
        => throw new ArgumentException("Index outside inline array's bounds.", "index");

    public static void ThrowArgumentException_IndexOutOfBounds(IndexOutOfRangeException exception)
        => throw new ArgumentException("array", exception);

    public static void ThrowArgumentException_InvalidExpression()
        => throw new ArgumentException(
            "Invalid expression. Valid expression are:\n" +
            "expression.Body is MemberExpression { Member: FieldInfo { FieldType: typeof(TReference) }} \n" +
            "expression.Body is BinaryExpression { NodeType: ExpressionTypes.ArrayIndex, Right: ConstantExpression { Value: int index } }: Additionally, if typeof(TOwner) == typeof(TReference[]), index can't be negative.\n" +
            "expression.Body is MethodCallExpression { Method.Name = \"Get\", Arguments: [ any number of ConstantExpression { Value: int index } ] }: Only supported when typeof(TOwner) is array. Additionally, if typeof(TOwner) == typeof(TReference[]), indexes must be a single value and can't be negative.\n" +
            "expression.Body is MethodCallExpression { Method.Name = \"get_Item\", Arguments: [ConstantExpression { Value: int index }]: Only supported when typeof(TOwner) == typeof(ArraySegment<TReference>): Additionally, index can't be negative."
        );

    public static void ThrowArgumentException_OwnerCountMustBeGreaterThanIndex()
        => throw new ArgumentException("Owner's count must be greater than index", "owner");

    public static void ThrowArgumentException_OwnerIndexOutOfBounds()
        => throw new ArgumentException("Index is outside owner's bounds", "owner");

    public static void ThrowArgumentException_OwnerLengthMustBeGreaterThanIndex()
        => throw new ArgumentException("Owner's length must be greater than index", "owner");

    public static void ThrowArgumentException_OwnerRankDoesNotMatchIndexes()
        => throw new ArgumentException("owner", "Array rank doesn't matches with the number of indexes provided.");

    [DoesNotReturn]
    public static void ThrowArgumentException_OwnerSegmentArrayIsNull()
        => throw new ArgumentException("Owner's array is null.", "owner");

    public static void ThrowArgumentException_OwnerSpanLengthMustBeGreaterThanIndex()
        => throw new ArgumentException("Owner's span's length must be greater than index", "owner");

    public static void ThrowArgumentException_OwnerTypeDoesNotMatch()
        => throw new ArgumentException("Owner's type is not assignable to TOwner.", "owner");

    [DoesNotReturn]
    public static void ThrowArgumentException_SegmentArrayIsNull()
        => throw new ArgumentException("Segment array is null.", "segment");

    public static void ThrowArgumentException_SingleIndexRequiredForIMemoryOwnerMemoryOrArraySegment()
        => throw new ArgumentException("Only indexes's of length 1 are allowed when TOwner is a IMemoryOwner<T>, Memory<T> or ArraySegment<T>.", "indexes");

    public static void ThrowArgumentException_TOwnerIndexesLengthDoesNotMatchRank()
        => throw new ArgumentException("indexes", "TOwner generic parameter is an array whose rank doesn't matches with indexes length.");

    public static void ThrowArgumentOutOfRangeException_IndexCanNotBeNegative()
        => throw new ArgumentOutOfRangeException("index", "Can't be negative.");

    public static void ThrowArgumentOutOfRangeException_IndexCanNotBeNegativeForSingleDimensionalArrayOrArraySegment()
        => throw new ArgumentOutOfRangeException("index", "Can't be negative when TOwner is a TReference[] or ArraySegment<TReference>.");

    public static void ThrowArgumentOutOfRangeException_IndexesCanNotBeNegative()
        => throw new ArgumentOutOfRangeException("indexes", "Can't be negative.");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Array()
        => throw new ArgumentNullException("array");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Expression()
        => throw new ArgumentNullException("expression");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_FieldInfo()
        => throw new ArgumentNullException("fieldInfo");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_IMemoryOwner()
        => throw new ArgumentNullException("memoryManager");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Indexes()
        => throw new ArgumentNullException("indexes");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Offset()
        => throw new ArgumentNullException("offset");

    [DoesNotReturn]
    public static void ThrowArgumentNullException_Owner()
        => throw new ArgumentNullException("owner");

    public static void ThrowArgumentNullException_Pointer()
        => throw new ArgumentNullException("pointer");

    public static void ThrowArrayTypeMismatchException_Array()
        => throw new ArrayTypeMismatchException("Array's actual element type doesn't match with TReference.");

    public static void ThrowArrayTypeMismatchException_Segment()
        => throw new ArrayTypeMismatchException("Segment's array's actual element type doesn't match with TReference.");

    public static void ThrowInvalidOperationException_InvalidTOwnerTypeValueTypeRestrictions()
        => throw new InvalidOperationException("TOwner generic parameter must be a reference type, unless this offset is accessing an element index from Memory<TReference>, an ArraySegment<TReference> or from a type assignable to IMemoryOwner<TReference>.");

    public static void ThrowInvalidOperationException_InvalidTReferenceTypeOrMode()
        => throw new InvalidOperationException("TReference generic parameter must be a value type and the offset must be for a field.");

    public static void ThrowInvalidOperationException_InvalidTReferenceTypeOnlyValueTypes()
        => throw new InvalidOperationException("TRefence generic parameter must be a value type.");

    public static void ThrowInvalidOperationException_InvalidTTypeOrMode()
        => throw new InvalidOperationException("T generic parameter must be a value type and the offset must be for a field.");

    public static void ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner()
        => throw new InvalidOperationException("TOwner generic parameter is not an array whose element type is TReference nor a IMemoryOwner<TReference>.");

    public static void ThrowNotImplementedException()
        => throw new NotImplementedException("Runtime doesn't support specified operation.");

    public static void ThrowRankException_TOwnerMustBeOfRank1()
        => throw new RankException("TOwner generic parameter is an array whose rank is not one.");
}

/// <summary>
/// Encapsulates a method that has one parameter and returns a reference of the type specified by the <typeparamref name="TResult"/> parameter.
/// </summary>
/// <typeparam name="T">The type of the parameter of the method that this delegate encapsulates.</typeparam>
/// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
/// <param name="arg">The parameter of the method that this delegate encapsulates.</param>
/// <returns>The return value of the method that this delegate encapsulates.</returns>
public delegate ref TResult FuncRef<T, TResult>(T arg);

internal delegate ref TResult RefFuncRef<T, TResult>(ref T arg);

internal enum Mode
{
    FieldInfo,
    SingleZeroArray,
    ArraySegment,
    Memory,
    IMemoryOwner,
    InlineArray,
    SingleArray,
    Array,
    SingleArrayUnkown,
    ArrayUnkown,
}

internal readonly struct RawMemory
{
    public readonly object? _object;
    public readonly int _index;
    public readonly int _length;
}

internal sealed class MemoryWrapper<T>(Memory<T> memory) : IMemoryOwner<T>
{
    public Memory<T> Memory => memory;

    public void Dispose()
    {
    }
}

internal static class UnboxerHelper<T>
{
    private static readonly IUnboxer Impl = (IUnboxer)Activator.CreateInstance(typeof(Unboxer<>).MakeGenericType(typeof(T)))!;

    public interface IUnboxer
    {
        public ref T Unbox(object o);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Unbox(object o) => ref Impl.Unbox(o);
}

internal sealed class Unboxer<T> : UnboxerHelper<T>.IUnboxer
    where T : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Unbox(object o)
    {
#if NET5_0_OR_GREATER
        return ref Unsafe.Unbox<T>(o);
#else
        return ref ObjectHelpers.Unbox<T>(o);
#endif
    }
}