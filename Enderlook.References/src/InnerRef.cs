using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

/// <summary>
/// Represent an inner reference to an allocation.
/// </summary>
/// <typeparam name="T">Type of the inner reference.</typeparam>
[DebuggerDisplay("{_owner} + {_offset}")]
public readonly struct InnerRef<T>
{
    internal readonly object? _owner;
    internal readonly nint _offset;

    /// <summary>
    /// Get reference to the value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    public readonly unsafe ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            object? owner = _owner;
            nint offset = _offset;

            if (owner is null)
            {
                return ref Unsafe.AsRef<T>((void*)offset);
            }

            if (offset >= 0)
            {
                return ref Unsafe.As<byte, T>(ref ObjectHelpers.GetFromInnerOffset(owner, offset));
            }

            Debug.Assert(owner is IMemoryOwner<T>);
            Span<T> span = Unsafe.As<IMemoryOwner<T>>(owner).Memory.Span;
            nint offset_ = offset & ~int.MinValue;
            // Do the check again as implementors of `IMemoryOwner<T>` can lie and give us a different span.
            if ((span.Length * Unsafe.SizeOf<T>()) < offset_)
                Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();
            return ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(span), offset_);
        }
    }

    /// <summary>
    /// Creates an internal reference from a pointer.
    /// </summary>
    /// <param name="pointer">Pointer to make the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pointer"/> is zero.</exception>
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe InnerRef(T* pointer)
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    {
        if (pointer == null)
            Utils.ThrowArgumentNullException_Pointer();
        _owner = default;
        _offset = (nint)pointer;
    }

    /// <summary>
    /// Creates a reference to an element of an span emitted by a <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="memoryManager"><see cref="IMemoryOwner{T}"/> which holds the reference.</param>
    /// <param name="index">Index of the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="memoryManager"/>'s span's length.</exception>
    public unsafe InnerRef(IMemoryOwner<T> memoryManager, int index)
    {
        if (memoryManager is null)
            Utils.ThrowArgumentNullException_IMemoryOwner();
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > memoryManager.Memory.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();

        _owner = memoryManager;
        _offset = (index * Unsafe.SizeOf<T>()) | int.MinValue;
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
    public InnerRef(T[] array, int index)
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
#if NET5_0_OR_GREATER
        ref byte element = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index));
        Debug.Assert(Unsafe.AreSame(ref element, ref Unsafe.As<T, byte>(ref array[index])));
#else
        ref byte element = ref Unsafe.As<T, byte>(ref array[index]);
#endif
        _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
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
    public InnerRef(Memory<T> memory, int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        if (index > memory.Length)
            Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();

        ref RawMemory raw = ref Unsafe.As<Memory<T>, RawMemory>(ref memory);
        int index_ = (raw._index & ~int.MinValue) + index;
        switch (raw._object)
        {
            case T[] array:
            {
                _owner = array;
#if NET5_0_OR_GREATER
                ref byte element = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index_));
                Debug.Assert(Unsafe.AreSame(ref element, ref Unsafe.As<T, byte>(ref array[index_])));
#else
                ref byte element = ref Unsafe.As<T, byte>(ref array[index_]);
#endif
                _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
                break;
            }
            case IMemoryOwner<T> memoryManager:
            {
                _owner = memoryManager;
                _offset = (index_ * Unsafe.SizeOf<T>()) | int.MinValue;
                break;
            }
            default:
            {
                _owner = new MemoryWrapper<T>(memory);
                _offset = (index * Unsafe.SizeOf<T>()) | int.MinValue;
                break;
            }
        }
    }

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="segment">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="ArraySegment{T}.Array"/> of <paramref name="segment"/> is <see langword="null"/>, or <paramref name="index"/> is equal or greater than <paramref name="segment"/>'s length.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="segment"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    public InnerRef(ArraySegment<T> segment, int index)
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

        index += segment.Offset;
#if NET5_0_OR_GREATER
        ref byte element = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(segment.Array), index));
        Debug.Assert(Unsafe.AreSame(ref element, ref Unsafe.As<T, byte>(ref segment.Array[index])));
#else
        ref byte element = ref Unsafe.As<T, byte>(ref segment.Array[index]);
#endif
        _offset = ObjectHelpers.CalculateInnerOffset(segment.Array, ref element);
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
    public InnerRef(T[,] array, int index1, int index2)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
        _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(array, index1, index2));
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
    public InnerRef(T[,,] array, int index1, int index2, int index3)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        int lowerBound1 = array.GetLowerBound(0);
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
        _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(array, index1, index2, index3));
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
    public InnerRef(T[,,,] array, int index1, int index2, int index3, int index4)
    {
        if (array is null)
            Utils.ThrowArgumentNullException_Array();
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[,,,]))
            Utils.ThrowArrayTypeMismatchException_Segment();

        _owner = array;
        _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(array, index1, index2, index3, index4));
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
    public unsafe InnerRef(Array array, params ReadOnlySpan<int> indexes)
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

#if NET5_0_OR_GREATER
            ref byte element = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array_), index));
            Debug.Assert(Unsafe.AreSame(ref element, ref Unsafe.As<T, byte>(ref array_[index])));
#else
            ref byte element = ref Unsafe.As<T, byte>(ref array_[index]);
#endif
            _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
        }
        else
#endif
        {
            if (arrayType.GetArrayRank() != indexes.Length)
                Utils.ThrowArgumentException_ArrayIndexesLengthDoesNotMatchRank();
            switch (indexes.Length)
            {
#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
                case 1 when arrayType == typeof(T).MakeArrayType():
                {                    
                    Debug.Assert(array is T[]);
                    T[] array_ = Unsafe.As<T[]>(array);

                    int index = indexes[0];
                    if (index < 0)
                        Utils.ThrowArgumentOutOfRangeException_IndexesCanNotBeNegative();
                    if (index > array_.Length)
                        Utils.ThrowArgumentException_ArrayIndexesOutOfBounds();
                
#if NET5_0_OR_GREATER
                    ref byte element = ref Unsafe.As<T, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array_), index));
                    Debug.Assert(Unsafe.AreSame(ref element, ref Unsafe.As<T, byte>(ref array_[index])));
#else
                    ref byte element = ref Unsafe.As<T, byte>(ref array_[index]);
#endif
                    _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
                    break;
                }
#endif
                case 2:
                {
                    Debug.Assert(array is T[,]);
                    ref int index_ = ref MemoryMarshal.GetReference(indexes);
                    _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(
                        Unsafe.As<T[,]>(array),
                        index_,
                        Unsafe.Add(ref index_, 1)
                        )
                    );
                    break;
                }
                case 3:
                {
                    Debug.Assert(array is T[,,]);
                    ref int index_ = ref MemoryMarshal.GetReference(indexes);
                    _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(
                        Unsafe.As<T[,,]>(array),
                        index_,
                        Unsafe.Add(ref index_, 1),
                        Unsafe.Add(ref index_, 2)
                        )
                    );
                    break;
                }
                case 4:
                {
                    Debug.Assert(array is T[,,,]);
                    ref int index_ = ref MemoryMarshal.GetReference(indexes);
                    _offset = ObjectHelpers.CalculateInnerOffset(array, ref Utils.GetReference(
                        Unsafe.As<T[,,,]>(array),
                        index_,
                        Unsafe.Add(ref index_, 1),
                        Unsafe.Add(ref index_, 2),
                        Unsafe.Add(ref index_, 3)
                        )
                    );
                    break;
                }
                default:
                {
                    int index = Utils.CalculateIndex(array, indexes);
#if NET9_0_OR_GREATER
                    ref byte element = ref Unsafe.AddByteOffset(
                        ref MemoryMarshal.GetArrayDataReference(array),
                        RuntimeHelpers.SizeOf(typeof(T).TypeHandle) * index
                    );
                    _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
#else
                    GCHandle handle = default;
                    try
                    {
                        handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                        ref byte element = ref Unsafe.AsRef<byte>((byte*)Marshal.UnsafeAddrOfPinnedArrayElement(array, index));
                        _offset = ObjectHelpers.CalculateInnerOffset(array, ref element);
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                            handle.Free();
                    }
#endif
                    break;
                }
            }
        }
    }

    internal InnerRef(object? owner, nint offset)
    {
        _owner = owner;
        _offset = offset;
    }

    /// <summary>
    /// Reads the inner reference.
    /// </summary>
    /// <param name="self">Reference to read.</param>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    /// <exception cref="NullReferenceException">Thrown when instance is <see langword="default"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(InnerRef<T> self) => self.Value;

    /// <summary>
    /// Convert a pointer into an inner reference.
    /// </summary>
    /// <param name="pointer">Pointer to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe static implicit operator InnerRef<T>(T* pointer) => new(pointer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}
