﻿using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References.Tests;

public class InnerRefTests
{
    [Fact]
    public unsafe void Pointer()
    {
        int* pointer = (int*)Marshal.AllocHGlobal(sizeof(int));
        InnerRef<int> reference = new(pointer);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.AsRef<int>(pointer)));
        reference = pointer;
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.AsRef<int>(pointer)));
        Marshal.FreeHGlobal((nint)pointer);

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(int*)));
    }

    [Fact]
    public void ZSArray()
    {
        int[] array = new int[3];
        InnerRef<int> reference = new(array, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        InnerOffset<int[], int> offset = new(1);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        offset = new([1]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        offset = new(e => e[1]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        offset = InnerOffset.ForArrayElement<int>(1);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(int[])!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(array, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, array.Length * 2));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3], 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(array, [-1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [array.Length * 2]));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3], [1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, []));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<int[], int>(-1));
        Assert.Throws<ArgumentNullException>(() => new InnerOffset<int[], int>(default(Expression<Func<int[], int>>)!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<int[], int>(e => e[-1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => InnerOffset.ForArrayElement<int>(-1));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0]));
        Assert.Throws<ArgumentException>(() => offset.From(new int[0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0]));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref array));
    }

    [Fact]
    public void Memory()
    {
        Memory<int> memory = new int[3].AsMemory();
        InnerRef<int> reference = new(memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));

        InnerOffset<Memory<int>, int> offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        ref int reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = InnerOffset.ForMemoryElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        memory = new int[10].AsMemory(2, 3);
        reference = new(memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));

        offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = InnerOffset.ForMemoryElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        memory = new int[3].AsMemory();
        reference = new(new MemoryWrapper<int>(memory).Memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));

        offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = InnerOffset.ForMemoryElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        memory = new int[10].AsMemory(2, 3);
        reference = new(new MemoryWrapper<int>(memory).Memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));

        offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        offset = InnerOffset.ForMemoryElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
        reference_ = ref offset.FromRef(ref memory);
        Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(memory, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(memory, memory.Length * 2));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(memory, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(memory, memory.Length * 2));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<Memory<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => InnerOffset.ForMemoryElement<int>(-1));

        Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0].AsMemory()));
        Assert.Throws<ArgumentException>(() => offset.From(new int[0].AsMemory()));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0].AsMemory()));
        Assert.Throws<ArgumentException>(() =>
        {
            Memory<int> memory_ = new int[0].AsMemory();
            return offset.FromRef(ref memory_);
        });
    }

    [Fact]
    public void MemoryWrapper()
    {
        MemoryWrapper<int> memory = new(new int[3].AsMemory());
        InnerRef<int> reference = new(memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        InnerOffset<MemoryWrapper<int>, int> offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset = InnerOffset.ForIMemoryOwnerElement<MemoryWrapper<int>, int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        InnerOffset<IMemoryOwner<int>, int> offset2 = InnerOffset.ForIMemoryOwnerElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        memory = new MemoryWrapper<int>(new int[10].AsMemory(2, 3));
        reference = new(memory, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset = new(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset = new([1]);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset = InnerOffset.ForIMemoryOwnerElement<MemoryWrapper<int>, int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        offset2 = InnerOffset.ForIMemoryOwnerElement<int>(1);
        reference = offset.From(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));
        reference = offset.FromObject(memory);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.GetSpan()[1]));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(IMemoryOwner<int>)!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(memory, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(memory, memory.Memory.Length * 2));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<MemoryWrapper<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => InnerOffset.ForIMemoryOwnerElement<MemoryWrapper<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => InnerOffset.ForIMemoryOwnerElement<int>(-1));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new MemoryWrapper<float>(new float[0])));
        Assert.Throws<ArgumentException>(() => offset.From(new MemoryWrapper<int>(new int[0])));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new MemoryWrapper<int>(new int[0])));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref memory));
    }

    [Fact]
    public void ArraySegment()
    {
        ArraySegment<int> arraySegment = new(new int[3]);
        InnerRef<int> reference = new(arraySegment, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));

        InnerOffset<ArraySegment<int>, int> offset = new(1);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        ref int reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

        offset = new([1]);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        offset = new(e => e[1]);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));
#endif

        offset = InnerOffset.ForArraySegmentElement<int>(1);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

        arraySegment = new ArraySegment<int>(new int[10], 2, 3);
        reference = new(arraySegment, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));

        offset = new(1);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

        offset = new([1]);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        offset = new(e => e[1]);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));
#endif

        offset = InnerOffset.ForArraySegmentElement<int>(1);
        reference = offset.From(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference = offset.FromObject(arraySegment);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
        reference_ = ref offset.FromRef(ref arraySegment);
        Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));

        Assert.Throws<ArgumentException>(() => new InnerRef<int>(default(ArraySegment<int>), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>(arraySegment, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(arraySegment, arraySegment.Count * 2));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new ArraySegment<object>(new string[3]), 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<ArraySegment<int>, int>(-1));
        Assert.Throws<ArgumentNullException>(() => new InnerOffset<ArraySegment<int>, int>(default(Expression<Func<ArraySegment<int>, int>>)!));
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<ArraySegment<int>, int>(e => e[-1]));
#endif
        Assert.Throws<ArgumentOutOfRangeException>(() => InnerOffset.ForArraySegmentElement<int>(-1));

        Assert.Throws<ArgumentException>(() => offset.From(default));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new ArraySegment<int>()));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new ArraySegment<float>(new float[0])));
        Assert.Throws<ArgumentException>(() => offset.From(new ArraySegment<int>(new int[0])));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new ArraySegment<int>(new int[0])));
        Assert.Throws<ArgumentException>(() =>
        {
            ArraySegment<int> arraySegment_ = new ArraySegment<int>(new int[0]);
            offset.FromRef(ref arraySegment_);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            ArraySegment<int> arraySegment_ = default;
            offset.FromRef(ref arraySegment_);
        });
    }

    [Fact]
    public void NonZeroIndexArray()
    {
        Array array = Array.CreateInstance(typeof(int), [3], [-1]);
        array.SetValue(1, 1);
        InnerRef<int> reference = new(array, 1);
        Assert.Equal(1, reference);

        InnerOffset<Array, int> offset = new(1);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        offset = new([1]);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        array = Array.CreateInstance(typeof(int), [3, 4], [-1, -2]);
        array.SetValue(1, 0, 1);
        reference = new(array, 0, 1);
        Assert.Equal(1, reference);

        offset = new(0, 1);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        offset = new([0, 1]);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        array = Array.CreateInstance(typeof(int), [3, 4, 3], [-1, -2, -3]);
        array.SetValue(1, 0, 1, -1);
        reference = new(array, 0, 1, -1);
        Assert.Equal(1, reference);

        offset = new(0, 1, -1);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        offset = new([0, 1, -1]);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        array = Array.CreateInstance(typeof(int), [3, 4, 3, 1], [-1, -2, -3, -4]);
        array.SetValue(1, 0, 1, -1, -4);
        reference = new(array, 0, 1, -1, -4);
        Assert.Equal(1, reference);

        offset = new(0, 1, -1, -4);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        offset = new([0, 1, -1, -4]);
        reference = offset.From(array);
        Assert.Equal(1, reference);
        reference = offset.FromObject(array);
        Assert.Equal(1, reference);

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.From(Array.CreateInstance(typeof(int), [0], [-1])));
        Assert.Throws<ArgumentException>(() => offset.From(Array.CreateInstance(typeof(int), [1], [5])));
        Assert.Throws<ArgumentException>(() => offset.FromObject(Array.CreateInstance(typeof(int), [0], [-1])));
        Assert.Throws<ArgumentException>(() => offset.FromObject(Array.CreateInstance(typeof(int), [1], [5])));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref array));
    }

    [Fact]
    public void Array2()
    {
        int[,] array = new int[2, 3];
        InnerRef<int> reference = new(array, 1, 2);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        InnerOffset<int[,], int> offset = new(1, 2);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        offset = new([1, 2]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        offset = new(e => e[1, 2]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        offset = InnerOffset.ForArrayElement<int>(1, 2);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, 0, 0));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, -1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 5, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3], 1, 1));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, [0, 0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [-1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, -1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [5, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 5]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3], [1, 1]));

        Assert.Throws<ArgumentNullException>(() => new InnerOffset<int[,], int>(default(Expression<Func<int[,], int>>)!));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0]));
        Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref array));
    }

    [Fact]
    public void Array3()
    {
        int[,,] array = new int[2, 3, 4];
        InnerRef<int> reference = new(array, 1, 2, 3);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        InnerOffset<int[,,], int> offset = new(1, 2, 3);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        offset = new([1, 2, 3]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        offset = new(e => e[1, 2, 3]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        offset = InnerOffset.ForArrayElement<int>(1, 2, 3);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, -1, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, -1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 5, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 5, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3, 3], 1, 1, 1));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, [0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [-1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, -1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, -1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [5, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 5, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, 5]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3, 3], [1, 1, 1]));

        Assert.Throws<ArgumentNullException>(() => new InnerOffset<int[,,], int>(default(Expression<Func<int[,,], int>>)!));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref array));
    }

    [Fact]
    public void Array4()
    {
        int[,,,] array = new int[2, 3, 4, 5];
        InnerRef<int> reference = new(array, 1, 2, 3, 4);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        InnerOffset<int[,,,], int> offset = new(1, 2, 3, 4);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        offset = new([1, 2, 3, 4]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        offset = new(e => e[1, 2, 3, 4]);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        offset = InnerOffset.ForArrayElement<int>(1, 2, 3, 4);
        reference = offset.From(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
        reference = offset.FromObject(array);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, 0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, -1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, -1, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, -1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, 1, -1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 5, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 5, 1, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, 5, 1));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, 1, 1, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3, 3, 3], 1, 1, 1, 1));

        Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default!, [0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [-1, 1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, -1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, -1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, 1, -1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [5, 1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 5, 1, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, 5, 1]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [1, 1, 1, 5]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new InnerRef<int>(array, [0, 0, 0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>(new string[3, 3, 3, 3], [1, 1, 1, 1]));

        Assert.Throws<ArgumentNullException>(() => new InnerOffset<int[,,,], int>(default(Expression<Func<int[,,,], int>>)!));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref array));
    }

    [Fact]
    public void Array_()
    {
        {
            int[] array = new int[3];
            InnerRef<int> reference = new((Array)array, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

            InnerOffset<Array, int> offset = new(1);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

            offset = new([1]);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>((Array)array, -1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, array.Length * 2));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3], 1));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, [0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InnerRef<int>((Array)array, [-1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [array.Length * 2]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, []));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3], [1]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0]));
            Assert.Throws<InvalidOperationException>(() =>
            {
                Array array_ = array;
                offset.FromRef(ref array_);
            });
        }

        {
            int[,] array = new int[2, 3];
            InnerRef<int> reference = new((Array)array, 1, 2);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

            InnerOffset<Array, int> offset = new(1, 2);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

            offset = new([1, 2]);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, 0, 0));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, -1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, -1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 5, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3], 1, 1));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, [0, 0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [-1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, -1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [5, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 5]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3], [1, 1]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0]));
            Assert.Throws<InvalidOperationException>(() =>
            {
                Array array_ = array;
                offset.FromRef(ref array_);
            });
        }

        {
            int[,,] array = new int[2, 3, 4];
            InnerRef<int> reference = new((Array)array, 1, 2, 3);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

            InnerOffset<Array, int> offset = new(1, 2, 3);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

            offset = new([1, 2, 3]);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, -1, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, -1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, -1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 5, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 5, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3, 3], 1, 1, 1));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, [0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [-1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, -1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, -1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [5, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 5, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, 5]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3, 3], [1, 1, 1]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0]));
            Assert.Throws<InvalidOperationException>(() =>
            {
                Array array_ = array;
                offset.FromRef(ref array_);
            });
        }

        {
            int[,,,] array = new int[2, 3, 4, 5];
            InnerRef<int> reference = new((Array)array, 1, 2, 3, 4);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

            InnerOffset<Array, int> offset = new(1, 2, 3, 4);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

            offset = new([1, 2, 3, 4]);
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, 0, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, -1, 1, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, -1, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, -1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, 1, -1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 5, 1, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 5, 1, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, 5, 1));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, 1, 1, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3, 3, 3], 1, 1, 1, 1));

            Assert.Throws<ArgumentNullException>(() => new InnerRef<int>(default(Array)!, [0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [-1, 1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, -1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, -1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, 1, -1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [5, 1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 5, 1, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, 5, 1]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [1, 1, 1, 5]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new InnerRef<int>((Array)array, [0, 0, 0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new InnerRef<object>((Array)new string[3, 3, 3, 3], [1, 1, 1, 1]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0, 0]));
            Assert.Throws<InvalidOperationException>(() =>
            {
                Array array_ = array;
                offset.FromRef(ref array_);
            });
        }
    }

    [Fact]
    public void FieldClass()
    {
        ClassA classA = new();

        InnerOffset<ClassA, int> offset = new(e => e.c);
        InnerRef<int> reference = offset.From(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        reference = offset.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));

        FieldInfo fieldInfo = typeof(ClassA).GetField("c", BindingFlags.Public | BindingFlags.Instance);
        offset = new(fieldInfo);
        reference = offset.From(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        reference = offset.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));

        Assert.Throws<ArgumentException>(() => new InnerOffset<ClassA, float>(fieldInfo));
        Assert.Throws<ArgumentException>(() => new InnerOffset<StructA, int>(fieldInfo));
        Assert.Throws<ArgumentNullException>(() => new InnerOffset<ClassA, int>(default(FieldInfo)!));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new object()));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(ref classA));
    }

    [Fact]
    public void FieldStruct()
    {
        StructA structA = new StructA() { c = 1 };
        object boxed = structA;

        InnerOffset<StructA, int> offset = new(e => e.c);
        InnerRef<int> reference = offset.FromObject(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif
        Assert.Equal(1, reference.Value);
        ref int reference_ = ref offset.FromRef(ref structA);
        Assert.True(Unsafe.AreSame(ref reference_, ref structA.c));

        FieldInfo fieldInfo = typeof(StructA).GetField("c", BindingFlags.Public | BindingFlags.Instance);
        offset = new(fieldInfo);
        reference = offset.FromObject(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif
        Assert.Equal(1, reference.Value);
        reference_ = ref offset.FromRef(ref structA);
        Assert.True(Unsafe.AreSame(ref reference_, ref structA.c));

        Assert.Throws<ArgumentException>(() => new InnerOffset<StructA, float>(fieldInfo));
        Assert.Throws<ArgumentException>(() => new InnerOffset<ClassA, int>(fieldInfo));
        Assert.Throws<ArgumentNullException>(() => new InnerOffset<StructA, int>(default(FieldInfo)!));

        Assert.Throws<InvalidOperationException>(() => offset.From(default));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new object()));
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void InlineArray()
    {
        ValueArray<int> valueArray = new();
        valueArray[2] = 1;

        object boxedValueArray = valueArray;

        InnerOffset<ValueArray<int>, int> offset = new(2);
        InnerRef<int> reference = offset.FromObject(boxedValueArray);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<ValueArray<int>>(boxedValueArray)[2]));
#endif
        Assert.Equal(1, reference.Value);
        ref int reference_ = ref offset.FromRef(ref valueArray);
        Assert.True(Unsafe.AreSame(ref reference_, ref valueArray[2]));

        offset = new([2]);
        reference = offset.FromObject(boxedValueArray);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<ValueArray<int>>(boxedValueArray)[2]));
#endif
        Assert.Equal(1, reference.Value);
        reference_ = ref offset.FromRef(ref valueArray);
        Assert.True(Unsafe.AreSame(ref reference_, ref valueArray[2]));

        Assert.Throws<ArgumentException>(() => new InnerOffset<ValueArray<int>, int>([]));
        Assert.Throws<ArgumentException>(() => new InnerOffset<ValueArray<int>, int>([0, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InnerOffset<ValueArray<int>, int>([-1]));
        Assert.Throws<ArgumentException>(() => new InnerOffset<ValueArray<int>, int>([6]));

        Assert.Throws<InvalidOperationException>(() => offset.From(default));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
    }
#endif
}

internal class ClassA
{
    public int b;
    public int c;
}

internal struct StructA
{
    public int b;
    public int c;
}

#if NET8_0_OR_GREATER
[InlineArray(5)]
internal struct ValueArray<T>
{
    private T value;
}
#endif

internal class MemoryWrapper<T>(Memory<T> memory) : MemoryManager<T>
{
    public override Span<T> GetSpan() => memory.Span;

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        throw new NotImplementedException();
    }

    public override void Unpin()
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
    }
}