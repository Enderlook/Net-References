using System.Buffers;

namespace Enderlook.References;

/// <summary>
/// Helper methods for <see cref="Offset{TOwner, TReference}"/>.
/// </summary>
public static class Offset
{
    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<TReference[], TReference> ForArrayElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.SingleZeroArray, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    public static Offset<TReference[,], TReference> ForArrayElement<TReference>(int index1, int index2)
        => new(Mode.Array, new int[] { index1, index2 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    public static Offset<TReference[,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3)
        => new(Mode.Array, new int[] { index1, index2, index3 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    /// <param name="index4">Fourth index of element to take.</param>
    public static Offset<TReference[,,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3, int index4)
        => new(Mode.Array, new int[] { index1, index2, index3, index4 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="ArraySegment{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<ArraySegment<TReference>, TReference> ForArraySegmentElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.ArraySegment, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Memory{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<Memory<TReference>, TReference> ForMemoryElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.Memory, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <typeparamref name="TMemoryOwner"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<TMemoryOwner, TReference> ForIMemoryOwnerElement<TMemoryOwner, TReference>(int index)
        where TMemoryOwner : IMemoryOwner<TReference>
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(typeof(MemoryManager<TReference>).IsAssignableFrom(typeof(TMemoryOwner)) ? Mode.MemoryManager : Mode.IMemoryOwner, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<IMemoryOwner<TReference>, TReference> ForIMemoryOwnerElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.IMemoryOwner, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="MemoryManager{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<MemoryManager<TReference>, TReference> ForMemoryManagerElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.MemoryManager, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <typeparamref name="TMemoryManager"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static Offset<TMemoryManager, TReference> ForMemoryManagerElement<TMemoryManager, TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.MemoryManager, default, index);
    }
}