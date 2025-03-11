using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

/// <summary>
/// Represent a reference to an allocation.<br/>
/// It also support inner references or pointers.
/// </summary>
/// <typeparam name="T">Type of the reference.</typeparam>
public readonly struct Ref<T>
{
    private readonly object? _owner;
    private readonly nint _unmanaged;
    private readonly object? _managed;

    /// <summary>
    /// Get reference to the value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    /// <exception cref="Exception">May be thrown by <see cref="ReferenceProvider{TResult}.Invoke(object?, nint)"/> if the instance was constructed with one of them.</exception>
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
                    Span<T> span;
                    Memory<T> memory;
#if NET5_0_OR_GREATER
                    scoped ref Memory<T> memoryRef = ref Unsafe.NullRef<Memory<T>>();
#else
                    scoped ref Memory<T> memoryRef = ref Unsafe.AsRef<Memory<T>>(null);
#endif

                    if (owner is IMemoryOwner<T> memoryOwner)
                    {
                        if (memoryOwner is MemoryManager<T> memoryManager)
                        {
                            span = memoryManager.GetSpan();
                            goto hasSpan;
                        }
                        memory = memoryOwner.Memory;
                        memoryRef = ref memory;
                    }
                    else
                    {
#if NET5_0_OR_GREATER
                        memoryRef = ref Unsafe.Unbox<Memory<T>>(owner);
#else
                        memory = (Memory<T>)owner;
                        memoryRef = ref memory;
#endif
                    }
                    span = memoryRef.Span;
                hasSpan:
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

            Debug.Assert(managed is ReferenceProvider<T>);
            ReferenceProvider<T> referenceProvider = Unsafe.As<ReferenceProvider<T>>(managed);
            return ref referenceProvider(owner, unmanaged);
        }
    }

    /// <summary>
    /// Creates an reference from a pointer.
    /// </summary>
    /// <param name="pointer">Pointer to make the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pointer"/> is zero.</exception>
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe Ref(T* pointer)
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    {
        if (pointer == null)
            Utils.ThrowArgumentNullException_Pointer();
        _owner = default;
        _unmanaged = (nint)pointer;
    }

    /// <summary>
    /// Creates a reference to an element of an span emitted by a <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="memoryManager"><see cref="IMemoryOwner{T}"/> which holds the reference.</param>
    /// <param name="index">Index of the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="memoryManager"/>'s span's length.</exception>
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

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="array"/>'s length.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(T[] array, int index)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > array.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanArrayLength();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
        _unmanaged = index;
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="memory">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="memory"/>'s length.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="memory"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(Memory<T> memory, int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > memory.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();

        // Try to avoid boxing the `Memory<T>`.
        if (MemoryMarshal.TryGetArray(memory, out ArraySegment<T> arraySegment))
        {
            _owner = arraySegment.Array;
            _unmanaged = arraySegment.Offset + index;
        }
        else if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out MemoryManager<T>? manager, out int start, out _))
        {
            _owner = manager;
            _unmanaged = (start + index) | int.MinValue;
        }
        else
        {
            _owner = memory;
            _unmanaged = index | int.MinValue;
        }
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="segment">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="ArraySegment{T}.Array"/> of <paramref name="segment"/> is <see langword="null"/>, or <paramref name="index"/> is equal or greater than <paramref name="segment"/>'s count.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="segment"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(ArraySegment<T> segment, int index)
    {
        if (segment.Array is null)
            Utils.ThrowArgumentException_SegmentArrayIsNull();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > segment.Count)
            Utils.ThrowArgumentException_IndexMustBeLowerThanSegmentCount();
        if (!typeof(T).IsValueType && segment.Array.GetType() != typeof(T[]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = segment.Array;
        _unmanaged = index + segment.Offset;
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index1"/> or <paramref name="index2"/> is out of <paramref name="array"/> bounds.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(T[,] array, int index1, int index2)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2]);
#else
        Utils.CheckBounds(array, [index1, index2]);
        _managed = new int[] { index1, index2 };
#endif
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index1"/>, <paramref name="index2"/> or <paramref name="index3"/> is out of <paramref name="array"/> bounds.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(T[,,] array, int index1, int index2, int index3)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2, index3]);
#else
        Utils.CheckBounds(array, [index1, index2, index3]);
        _managed = new int[] { index1, index2, index3 };
#endif
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    /// <param name="index4">Fourth index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index1"/>, <paramref name="index2"/>, <paramref name="index3"/> or <paramref name="index4"/> is out of <paramref name="array"/> bounds.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public Ref(T[,,,] array, int index1, int index2, int index3, int index4)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,,,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
#if NET6_0_OR_GREATER
        _unmanaged = Utils.CalculateIndex(array, [index1, index2, index3, index4]);
#else
        Utils.CheckBounds(array, [index1, index2, index3, index4]);
        _managed = new int[] { index1, index2, index3, index4 };
#endif
    }

    /// <summary>
    /// Creates an offset to an specific location of an <see cref="Array"/>.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="indexes">Indexes of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <paramref name="array"/> element type is not <typeparamref name="T"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when indexes are out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="indexes"/> length doesn't match <paramref name="array"/>'s rank or indexes are out of bounds.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="array"/> is a single-dimensional zero-based index and <paramref name="indexes"/> only value is negative.</exception>
    public unsafe Ref(Array array, params ReadOnlySpan<int> indexes)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        Type arrayType = array.GetType();
        if (!typeof(T).IsValueType && arrayType.GetElementType() != typeof(T))
            Utils.ThrowArrayTypeMismatchException_Array();

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

    /// <summary>
    /// Creates a wrapper around a method which returns a reference.
    /// </summary>
    /// <param name="managedState">Managed state to pass to the delegate.</param>
    /// <param name="unmanagedState">Unmanaged state to pass to the delegate.</param>
    /// <param name="referenceProvider">Delegate which wraps the method that produces a reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="referenceProvider"/> is <see langword="null"/>.</exception>
    public Ref(object managedState, nint unmanagedState, ReferenceProvider<T> referenceProvider)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Ref<T> CreateUnsafe(object? owner, nint unmanaged, object? managed) => new(owner, unmanaged, managed);

    /// <summary>
    /// Reads the reference.
    /// </summary>
    /// <param name="self">Reference to read.</param>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    /// <exception cref="NullReferenceException">Thrown when instance is <see langword="default"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Ref<T> self) => self.Value;

    /// <summary>
    /// Convert a pointer into a reference.
    /// </summary>
    /// <param name="pointer">Pointer to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe static implicit operator Ref<T>(T* pointer) => new(pointer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}
