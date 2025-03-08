using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

internal readonly struct Ref<T, TReadOnly>
{
    internal readonly object? _owner;
    internal readonly nint _unmanaged;
    internal readonly object? _managed;

    public readonly unsafe ref T Value
    {
        get
        {
            object? owner = _owner;
            nint unmanaged = _unmanaged;
            if (owner is null)
                return ref Unsafe.AsRef<T>((void*)unmanaged);

            object? managed = _managed;
            switch (managed)
            {
                case null when unmanaged >= 0:
                {
#if NET6_0_OR_GREATER
                    Debug.Assert(owner is Array);
                    Array array = Unsafe.As<Array>(owner);
                    Debug.Assert(unmanaged < array.Length);
                    return ref Unsafe.Add(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), unmanaged);
#else
                    return ref owner is T[] array ? ref array[(int)unmanaged] : ref ObjectHelpers.GetReference1<T>(owner, (int)unmanaged);
#endif
                }
                case null when unmanaged < 0:
                {
                    int index = (int)unmanaged & ~int.MinValue;
                    ReadOnlyMemory<T> memory;
#if NET5_0_OR_GREATER
                    scoped ref ReadOnlyMemory<T> memoryRef = ref Unsafe.NullRef<ReadOnlyMemory<T>>();
#else
                    scoped ref ReadOnlyMemory<T> memoryRef = ref Unsafe.AsRef<ReadOnlyMemory<T>>(null);
#endif

                    if (owner is IMemoryOwner<T> memoryOwner)
                    {
                        memory = memoryOwner.Memory;
                        memoryRef = ref memory;
                    }
#if NET5_0_OR_GREATER
                    else if (Utils.IsToggled<TReadOnly>() && owner is ReadOnlyMemory<T>)
                    {
                        memoryRef = ref Unsafe.Unbox<ReadOnlyMemory<T>>(owner);
                    }
                    else
                    {
                        memoryRef = ref Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref Unsafe.Unbox<Memory<T>>(owner));
                    }
#else
                    else if (Utils.IsToggled<TReadOnly>() && owner is ReadOnlyMemory<T> readOnlyMemory)
                    {
                        memory = readOnlyMemory;
                        memoryRef = ref memory;
                    }
                    else
                    {
                        memory = (Memory<T>)owner;
                        memoryRef = ref memory;
                    }
#endif
                    ReadOnlySpan<T> span = memoryRef.Span;
                    if (unmanaged >= span.Length) // Add in case memory returns a different span with another length.
                        Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();
                    return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
                }
                case FieldInfo[] fieldInfo:
                {
                    TypedReference typedReference = TypedReference.MakeTypedReference(owner, fieldInfo);
                    ref T field = ref __refvalue(typedReference, T);
#pragma warning disable CS9082 // Local is returned by reference but was initialized to a value that cannot be returned by reference
                    return ref field;
#pragma warning restore CS9082 // Local is returned by reference but was initialized to a value that cannot be returned by reference
                }
#if !NET6_0_OR_GREATER
                case int[] indexes:
                    return ref Utils.GetReference<T>(owner, indexes);
#endif
            }

            if (Utils.IsToggled<TReadOnly>() && managed is ReadOnlyReferenceProvider<T> provider)
            {
                return ref Unsafe.AsRef(in provider(managed, unmanaged));
            }

            Debug.Assert(managed is ReferenceProvider<T>);
            ReferenceProvider<T> referenceProvider = Unsafe.As<ReferenceProvider<T>>(managed);
            return ref referenceProvider(owner, unmanaged);
        }
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe Ref(T* pointer)
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    {
        if (pointer == null)
            Utils.ThrowArgumentNullException_Pointer();
        _owner = default;
        _unmanaged = (nint)pointer;
    }

    public unsafe Ref(IMemoryOwner<T> memoryManager, int index)
    {
        if (memoryManager is null)
            Utils.ThrowArgumentNullException_IMemoryOwner();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > memoryManager.Memory.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();

        _owner = memoryManager;
        _unmanaged = index | int.MinValue;
    }

    public Ref(T[] array, int index)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > array.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanArrayLength();
        if (!typeof(T).IsValueType && !Utils.IsToggled<TReadOnly>() && array.GetType() != typeof(T[]))
            Utils.ThrowArrayTypeMismatchException_Array();

        _owner = array;
        _unmanaged = index;
    }

    public Ref(Memory<T> memory, int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > memory.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();

        _owner = new MemoryWrapper<T>(memory);
        _unmanaged = index | int.MinValue;
    }

    public Ref(ArraySegment<T> segment, int index)
    {
        if (segment.Array is null)
            Utils.ThrowArgumentException_SegmentArrayIsNull();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > segment.Count)
            Utils.ThrowArgumentException_IndexMustBeLowerThanSegmentCount();
        if (!typeof(T).IsValueType && !Utils.IsToggled<TReadOnly>() && segment.Array.GetType() != typeof(T[]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = segment.Array;
        _unmanaged = index + segment.Offset;
    }

    public Ref(T[,] array, int index1, int index2)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && !Utils.IsToggled<TReadOnly>() && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Array();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2]);
#else
        Utils.CheckBounds(array, [index1, index2]);
        _managed = new int[] { index1, index2 };
#endif
    }

    public Ref(T[,,] array, int index1, int index2, int index3)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && !Utils.IsToggled<TReadOnly>() && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Array();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2, index3]);
#else
        Utils.CheckBounds(array, [index1, index2, index3]);
        _managed = new int[] { index1, index2, index3 };
#endif
    }

    public Ref(T[,,,] array, int index1, int index2, int index3, int index4)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && !Utils.IsToggled<TReadOnly>() && array.GetType() != typeof(T[,,,]))
            Utils.ThrowArrayTypeMismatchException_Array();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2, index3, index4]);
#else
        Utils.CheckBounds(array, [index1, index2, index3, index4]);
        _managed = new int[] { index1, index2, index3, index4 };
#endif
    }

    public unsafe Ref(Array array, params ReadOnlySpan<int> indexes)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        Type arrayType = array.GetType();
        if (!typeof(T).IsValueType)
        {
            if (Utils.IsToggled<TReadOnly>())
            {
                if (typeof(T).IsAssignableFrom(arrayType.GetElementType()))
                    Utils.ThrowArrayTypeMismatchException_ArrayAssignable();
            }
            else if (arrayType.GetElementType() != typeof(T))
                Utils.ThrowArrayTypeMismatchException_Array();
        }

        _owner = array;
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        if (arrayType.IsSZArray)
        {
            Debug.Assert(array is T[]);
            T[] array_ = Unsafe.As<T[]>(array);

            if (indexes.Length != 1)
                Utils.ThrowArgumentException_ArrayIndexesLengthDoesNotMatchRank();
            int index = indexes[0];
            if (index < 0)
                Utils.ThrowArgumentOutOfRangeException_IndexesCanNotBeNegative();
            if (index > array_.Length)
                Utils.ThrowArgumentException_ArrayIndexesOutOfBounds();

            _unmanaged = index;
        }
        else
#endif
        {
            if (arrayType.GetArrayRank() != indexes.Length)
                Utils.ThrowArgumentException_ArrayIndexesLengthDoesNotMatchRank();

#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
            if (indexes.Length == 1)
            {      
                Debug.Assert(array is T[]);
                T[] array_ = Unsafe.As<T[]>(array);

                int index = indexes[0];
                if (index < 0)
                    Utils.ThrowArgumentOutOfRangeException_IndexesCanNotBeNegative();
                if (index > array_.Length)
                    Utils.ThrowArgumentException_ArrayIndexesOutOfBounds();

                _unmanaged = index;
                return;
            }
            else
#endif
            {
#if NET6_0_OR_GREATER
                _unmanaged = Utils.CalculateIndex(array, indexes);
#else
                Utils.CheckBounds(array, indexes);
                _managed = indexes.ToArray();
#endif
            }
        }
    }

    public Ref(object managedState, nint unmanagedState, Delegate referenceProvider)
    {
        if (referenceProvider is null) Utils.ThrowArgumentNullException_ReferenceProvider();

        _managed = managedState;
        _unmanaged = unmanagedState;
        _owner = referenceProvider;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Ref(object? owner, nint unmanaged, object? managed)
    {
        _owner = owner;
        _unmanaged = unmanaged;
        _managed = managed;
    }
}
