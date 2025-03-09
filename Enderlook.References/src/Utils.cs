using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

internal static partial class Utils
{
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    private static readonly MethodInfo UnsafeAsMethod = typeof(Unsafe)
        .GetMethod(
            nameof(Unsafe.As),
            1,
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(object)],
            null
        );
    private static Array _owner;

#if NET5_0_OR_GREATER
    [DynamicDependency("As", typeof(Unsafe))]
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo UnsafeAsFor(Type[] types) => UnsafeAsMethod.MakeGenericMethod(types);
#endif

#if !NET6_0_OR_GREATER
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    // This is used in platforms which doesn't have `MemoryMarshal.GetArrayDataReference(Array)`
    // when the array is non-zero-based single-dimensional, or a multi-dimensional array with rank > 32.
    private static readonly ConcurrentDictionary<Type, Delegate> ArrayAccess = new();
#if NET5_0_OR_GREATER
    [DynamicDependency("Add", typeof(Unsafe))]
#endif
    private static readonly Func<Type, Delegate> ArrayAccessCreator = static (arrayType) =>
    {
        Type[] arrayTypeArray = [arrayType];
        MethodInfo unsafeAddMethod = typeof(Unsafe)
            .GetMethod(
                nameof(Unsafe.Add),
                1,
                BindingFlags.Public | BindingFlags.Static,
                null,
                [Type.MakeGenericMethodParameter(0).MakeByRefType(), typeof(int)],
                null
            )!.MakeGenericMethod(arrayTypeArray);

        Type elementType = arrayType.GetElementType()!;
        DynamicMethod dynamicMethod = new(
            "GetElementRef",
            elementType.MakeByRefType(),
            [typeof(object), typeof(int).MakeByRefType()]
        );
        ILGenerator il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, UnsafeAsFor(arrayTypeArray));
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldind_I4);

        if (arrayType.IsSZArray)
        {
            il.Emit(OpCodes.Ldelema, elementType);
        }
        else
        {
            int rank = arrayType.GetArrayRank();

            int i = 1;
            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_3);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_5);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_6);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_7);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_8);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

            if (i++ >= rank)
                goto end;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, i);
            il.Emit(OpCodes.Call, unsafeAddMethod);
            il.Emit(OpCodes.Ldind_I4);

        end:
            Type[] parameters = new Type[rank];
            Array.Fill(parameters, typeof(int));
            MethodInfo addressMethod = arrayType.GetMethod("Address", parameters)!;
            il.Emit(OpCodes.Call, addressMethod);
        }
        il.Emit(OpCodes.Ret);

        return dynamicMethod.CreateDelegate(typeof(FuncArray<>).MakeGenericType(elementType.MakeByRefType()));
    };
#endif

    public unsafe static ref T GetReference<T>(object array, int[] indexes)
    {
        Debug.Assert(indexes.Length > 1);
        // In theory, the ECMA only supports up to 32 dimensions, but we have a fallback just to be sure.
        switch (indexes.Length)
        {
            case 1: return ref GetReference1<T>(array, indexes);
            case 2: return ref GetReference2<T>(array, indexes);
            case 3: return ref GetReference3<T>(array, indexes);
            case 4: return ref GetReference4<T>(array, indexes);
            case 5: return ref GetReference5<T>(array, indexes);
            case 6: return ref GetReference6<T>(array, indexes);
            case 7: return ref GetReference7<T>(array, indexes);
            case 8: return ref GetReference8<T>(array, indexes);
            case 9: return ref GetReference9<T>(array, indexes);
            case 10: return ref GetReference10<T>(array, indexes);
            case 11: return ref GetReference11<T>(array, indexes);
            case 12: return ref GetReference12<T>(array, indexes);
            case 13: return ref GetReference13<T>(array, indexes);
            case 14: return ref GetReference14<T>(array, indexes);
            case 15: return ref GetReference15<T>(array, indexes);
            case 16: return ref GetReference16<T>(array, indexes);
            case 17: return ref GetReference17<T>(array, indexes);
            case 18: return ref GetReference18<T>(array, indexes);
            case 19: return ref GetReference19<T>(array, indexes);
            case 20: return ref GetReference20<T>(array, indexes);
            case 21: return ref GetReference21<T>(array, indexes);
            case 22: return ref GetReference22<T>(array, indexes);
            case 23: return ref GetReference23<T>(array, indexes);
            case 24: return ref GetReference24<T>(array, indexes);
            case 25: return ref GetReference25<T>(array, indexes);
            case 26: return ref GetReference26<T>(array, indexes);
            case 27: return ref GetReference27<T>(array, indexes);
            case 28: return ref GetReference28<T>(array, indexes);
            case 29: return ref GetReference29<T>(array, indexes);
            case 30: return ref GetReference30<T>(array, indexes);
            case 31: return ref GetReference31<T>(array, indexes);
            case 32: return ref GetReference32<T>(array, indexes);
            default: return ref GetReferenceAny<T>(array, indexes);
        }
    }

    private static ref T GetReference1<T>(object array, int[] indexes)
    {
        Debug.Assert(indexes.Length == 1);
#if NET5_0_OR_GREATER
        return ref ObjectHelpers.GetReference1<T>(array, MemoryMarshal.GetArrayDataReference(indexes));
#else
        return ref ObjectHelpers.GetReference1<T>(array, indexes[0]);
#endif
    }

    public unsafe static ref T GetReferenceAny<T>(object owner, scoped ReadOnlySpan<int> indexes)
    {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        try
        {
            Debug.Assert(owner is Array);
            Debug.Assert(owner.GetType().GetArrayRank() == indexes.Length);
            Delegate @delegate = ArrayAccess.GetOrAdd(owner.GetType(), ArrayAccessCreator);
            Debug.Assert(@delegate is FuncArray<T>);
            return ref Unsafe.As<FuncArray<T>>(@delegate)(owner, ref MemoryMarshal.GetReference(indexes));
        }
        catch (NotSupportedException)
        {
        }
#endif
        ThrowNotImplementedException();
        return ref Unsafe.AsRef<T>(null);
    }
#endif

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBounds(Array array, ReadOnlySpan<int> indexes)
    {
        for (int i = indexes.Length - 1; i >= 0; i--)
        {
            int subIndex = indexes[i];
            int lowerBound = array.GetLowerBound(i);
            int upperBound = array.GetUpperBound(i);
            if (subIndex < lowerBound || subIndex > upperBound)
                ThrowArgumentException_OwnerIndexOutOfBounds();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsToggled<T>()
    {
        Debug.Assert(typeof(T) == typeof(Yes) || typeof(T) == typeof(No));
        return typeof(T) == typeof(Yes);
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

    public static void ThrowArgumentException_IndexMustBeLowerThanSequenceLength()
        => throw new ArgumentException("Index must be lower than sequence's length.", "index");

    public static void ThrowArgumentException_IndexOutOfBounds()
        => throw new ArgumentException("Index outside inline array's bounds.", "index");

    public static void ThrowArgumentException_InlineArrayElementTypeMismatch()
        => throw new ArgumentException("Inline array's element type is not the same as TReference.");

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

    public static void ThrowArgumentException_SingleIndexRequiredForMemoryOrArraySegmentOrIMemoryOwnerOrInlineArray()
        => throw new ArgumentException("Only indexes's of length 1 are allowed when TOwner is a Memory<T>, an ArraySegment<T>, a type assignable to IMemoryOwner<T> or a type with the attribute InlineArrayAttribute.", "indexes");

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
    public static void ThrowArgumentNullException_Owner()
        => throw new ArgumentNullException("owner");

    public static void ThrowArgumentNullException_Pointer()
        => throw new ArgumentNullException("pointer");

    public static void ThrowArgumentNullException_ReferenceProvider()
        => throw new ArgumentNullException("referenceProvider");

    public static void ThrowArrayTypeMismatchException_Array()
        => throw new ArrayTypeMismatchException("Array's actual element type doesn't match with TReference.");

    public static void ThrowArrayTypeMismatchException_ArrayAssignable()
        => throw new ArrayTypeMismatchException("Array's actual element type is not assignable to TReference");

    public static void ThrowArrayTypeMismatchException_Segment()
        => throw new ArrayTypeMismatchException("Segment's array's actual element type doesn't match with TReference.");

    public static void ThrowArrayTypeMismatchException_SegmentAssignable()
        => throw new ArrayTypeMismatchException("Segment's array's actual element type is not assignable to TReference.");

    public static void ThrowInvalidOperationException_InvalidTOwnerTypeValueTypeRestrictions<TReadOnly>()
        => throw new InvalidOperationException(IsToggled<TReadOnly>()
            ? "TOwner generic parameter must be a reference type, unless this offset is accessing an element index from Memory<TReference>, an ArraySegment<TReference> or from a type assignable to IMemoryOwner<TReference>."
            : "TOwner generic parameter must be a reference type, unless this offset is accessing an element index from Memory<TReference>, a ReadOnlyMemory<T>, an ArraySegment<TReference>, a ReadOnlySequence<TReference>, or from a type assignable to IMemoryOwner<TReference>.");

    public static void ThrowInvalidOperationException_InvalidTReferenceTypeOnlyValueTypes()
        => throw new InvalidOperationException("TRefence generic parameter must be a value type.");

    public static void ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner()
        => throw new InvalidOperationException("TOwner generic parameter is not an array whose element type is TReference, a Memory<TReference>, an ArraySegment<TReference>, a type assignable to IMemoryOwner<TReference> or a type with the attribute InlineArrayAttribute.");

    public static void ThrowNotImplementedException()
        => throw new NotImplementedException("Runtime doesn't support specified operation.");

    public static void ThrowRankException_TOwnerMustBeOfRank1()
        => throw new RankException("TOwner generic parameter is an array whose rank is not one.");
}

internal struct Yes { }
internal struct No { }

internal delegate ref TResult RefFuncRef<T, TResult>(ref T arg);

internal delegate ref TResult FuncArray<TResult>(object array, scoped ref int firstIndex);

/// <summary>
/// Encapsulates a method that has two parameters and returns a reference of the type specified by the <typeparamref name="TResult"/> parameter.
/// </summary>
/// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
/// <param name="managedState">The first parameter of the method that this delegate encapsulates.</param>
/// <param name="unmanagedState">The second parameter of the method that this delegate encapsulates.</param>
/// <returns>The return value of the method that this delegate encapsulates.</returns>
public delegate ref TResult ReferenceProvider<TResult>(object? managedState, nint unmanagedState);

/// <summary>
/// Encapsulates a method that has two parameters and returns a readonly reference of the type specified by the <typeparamref name="TResult"/> parameter.
/// </summary>
/// <typeparam name="TResult">The type of the return value of the method that this delegate encapsulates.</typeparam>
/// <param name="managedState">The first parameter of the method that this delegate encapsulates.</param>
/// <param name="unmanagedState">The second parameter of the method that this delegate encapsulates.</param>
/// <returns>The return value of the method that this delegate encapsulates.</returns>
public delegate ref readonly TResult ReadOnlyReferenceProvider<TResult>(object? managedState, nint unmanagedState);

internal enum Mode
{
    FieldInfo,
    SingleZeroArray,
    ArraySegment,
    Memory,
    ReadOnlyMemory,
    IMemoryOwner,
    InlineArray,
    SingleArray,
    Array,
    SingleArrayUnkown,
    ArrayUnkown,
    ReadOnlySequence,
}

internal sealed class MemoryWrapper<T>(Memory<T> memory) : IMemoryOwner<T>
{
    public Memory<T> Memory => memory;

    public void Dispose()
    {
    }
}

#if NET8_0_OR_GREATER
internal static class UnboxerHelper<TArray>
{
    public static readonly IUnboxer Impl = (IUnboxer)Activator.CreateInstance(typeof(Unboxer<>).MakeGenericType(typeof(TArray)))!;

    internal interface IUnboxer
    {
        public ref TArray Unbox(object owner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReferenceProvider<TReference> GetElementAccessor<TReference>()
    {
        Debug.Assert(typeof(TArray).IsDefined(typeof(InlineArrayAttribute)));
        return new ReferenceProvider<TReference>(Work);

        static ref TReference Work(object? owner, nint index)
        {
            Debug.Assert(owner is not null);
            Debug.Assert(typeof(TArray).IsDefined(typeof(InlineArrayAttribute)));
            Debug.Assert(index < typeof(TArray).GetCustomAttribute<InlineArrayAttribute>()!.Length);
            ref TArray array = ref UnboxerHelper<TArray>.Impl.Unbox(owner);
            return ref Unsafe.Add(ref Unsafe.As<TArray, TReference>(ref array), (int)index);
        }
    }
}

internal sealed class Unboxer<T> : UnboxerHelper<T>.IUnboxer
    where T : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Unbox(object owner) => ref Unsafe.Unbox<T>(owner);
}
#endif