using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

/// <summary>
/// Represent a readonly reference to an allocation.<br/>
/// It also support inner references or pointers.
/// </summary>
/// <typeparam name="T">Type of the reference.</typeparam>
public readonly struct ReadOnlyRef<T>
{
    private readonly Ref<T, Yes> value;

    /// <summary>
    /// Get reference to the value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    /// <exception cref="Exception">May be thrown by <see cref="ReferenceProvider{TResult}.Invoke(object?, nint)"/> or <see cref="ReadOnlyReferenceProvider{TResult}.Invoke(object?, nint)"/>  if the instance was constructed with one of them.</exception>
    public readonly unsafe ref readonly T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref value.Value;
    }

    /// <summary>
    /// Creates an reference from a pointer.
    /// </summary>
    /// <param name="pointer">Pointer to make the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pointer"/> is zero.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe ReadOnlyRef(T* pointer) => value = new(pointer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

    /// <summary>
    /// Creates a reference to an element of an span emitted by a <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="memoryManager"><see cref="IMemoryOwner{T}"/> which holds the reference.</param>
    /// <param name="index">Index of the reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memoryManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="memoryManager"/>'s span's length.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(IMemoryOwner<T> memoryManager, int index) => value = new(memoryManager, index);

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="array"/>'s length.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but is not assignable to <typeparamref name="T"/> type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(T[] array, int index) => value = new(array, index);

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="memory">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index"/> is equal or greater than <paramref name="memory"/>'s length.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(ReadOnlyMemory<T> memory, int index) => value = new(MemoryMarshal.AsMemory(memory), index);

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="segment">Array to take a reference.</param>
    /// <param name="index">Index of element to take.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="ArraySegment{T}.Array"/> of <paramref name="segment"/> is <see langword="null"/>, or <paramref name="index"/> is equal or greater than <paramref name="segment"/>'s count.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="segment"/>'s actual element type is a reference type, but doesn't match with <typeparamref name="T"/> type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(ArraySegment<T> segment, int index) => value = new(segment, index);

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index1"/> or <paramref name="index2"/> is out of <paramref name="array"/> bounds.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but is not assignable to <typeparamref name="T"/> type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(T[,] array, int index1, int index2) => value = new(array, index1, index2);

    /// <summary>
    /// Creates a reference to an element of the array.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="index1"/>, <paramref name="index2"/> or <paramref name="index3"/> is out of <paramref name="array"/> bounds.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but is not assignable to <typeparamref name="T"/> type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(T[,,] array, int index1, int index2, int index3) => value = new(array, index1, index2, index3);

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
    /// <exception cref="ArrayTypeMismatchException">Thrown when <paramref name="array"/>'s actual element type is a reference type, but is not assignable to <typeparamref name="T"/> type.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(T[,,,] array, int index1, int index2, int index3, int index4) => value = new(array, index1, index2, index3, index4);

    /// <summary>
    /// Creates an offset to an specific location of an <see cref="Array"/>.
    /// </summary>
    /// <param name="array">Array to take a reference.</param>
    /// <param name="indexes">Indexes of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <paramref name="array"/> element type is not <typeparamref name="T"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when indexes are out of range.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="indexes"/> length doesn't match <paramref name="array"/>'s rank or indexes are out of bounds.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="array"/> is a single-dimensional zero-based index and <paramref name="indexes"/> only value is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(Array array, params ReadOnlySpan<int> indexes) => value = new(array, indexes);

    /// <summary>
    /// Creates a wrapper around a method which returns a reference.
    /// </summary>
    /// <param name="managedState">Managed state to pass to the delegate.</param>
    /// <param name="unmanagedState">Unmanaged state to pass to the delegate.</param>
    /// <param name="referenceProvider">Delegate which wraps the method that produces a reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="referenceProvider"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(object managedState, nint unmanagedState, ReadOnlyReferenceProvider<T> referenceProvider) => value = new(managedState, unmanagedState, referenceProvider);

    /// <summary>
    /// Creates a wrapper around a method which returns a reference.
    /// </summary>
    /// <param name="managedState">Managed state to pass to the delegate.</param>
    /// <param name="unmanagedState">Unmanaged state to pass to the delegate.</param>
    /// <param name="referenceProvider">Delegate which wraps the method that produces a reference.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="referenceProvider"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] 
    public ReadOnlyRef(object managedState, nint unmanagedState, ReferenceProvider<T> referenceProvider) => value = new(managedState, unmanagedState, referenceProvider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlyRef(in Ref<T, Yes> value) => this.value = value;

    /// <summary>
    /// Reads the reference.
    /// </summary>
    /// <param name="self">Reference to read.</param>
    /// <exception cref="ArgumentException">Thrown when reference is backed by <see cref="IMemoryOwner{T}"/> and it changed its span's length to a lower one that the index of this reference.</exception>
    /// <exception cref="Exception">May be thrown by <see cref="ReferenceProvider{TResult}.Invoke(object?, nint)"/> if the instance was constructed with one of them.</exception>
    /// <exception cref="NullReferenceException">Thrown when instance is <see langword="default"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(ReadOnlyRef<T> self) => self.Value;

    /// <summary>
    /// Convert a pointer into a reference.
    /// </summary>
    /// <param name="pointer">Pointer to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public unsafe static implicit operator ReadOnlyRef<T>(T* pointer) => new(pointer);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}