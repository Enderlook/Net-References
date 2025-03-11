using System;
using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References.Tests;

public class RefTests
{
    [Fact]
    public unsafe void Pointer()
    {
        int* pointer = (int*)Marshal.AllocHGlobal(sizeof(int));
        Ref<int> reference = new(pointer);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.AsRef<int>(pointer)));
        reference = pointer;
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.AsRef<int>(pointer)));
        Marshal.FreeHGlobal((nint)pointer);

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int*)));
    }

    [Fact]
    public void ZSArray()
    {
        int[] array = new int[3];
        Ref<int> reference = new(array, 1);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

        foreach (Offset<int[], int> offset in new Offset<int[], int>[]
        {
            new(1),
            new([1]),
            new(e => e[1]),
            Offset.ForArrayElement<int>(1),
        })
        {
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
            scoped ref int reference_ = ref offset.FromRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));
            reference_ = ref offset.FromRef(ref array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));
            reference_ = ref offset.FromObjectRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef<int[]>(default(int[]))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0]));
        }

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[])!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(array, -1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, array.Length * 2));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3], 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(array, [-1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [array.Length * 2]));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3], [1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, []));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0]));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<int[], int>(-1));
        Assert.Throws<ArgumentNullException>(() => new Offset<int[], int>(default(Expression<Func<int[], int>>)!));
#pragma warning disable CS0251 // Indexando una matriz con un índice negativo
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<int[], int>(e => e[-1]));
#pragma warning restore CS0251 // Indexando una matriz con un índice negativo
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForArrayElement<int>(-1));
    }

    [Fact]
    public void Memory()
    {
        Memory<int>[] memories =
        [
            new int[3].AsMemory(),
            new int[10].AsMemory(2, 3),
            new ClassMemoryOwner<int>(new int[3]).Memory,
            new ClassMemoryOwner<int>(new int[10].AsMemory(2, 3)).Memory,
            new StructMemoryOwner<int>(new int[3]).Memory,
            new StructMemoryOwner<int>(new int[10].AsMemory(2, 3)).Memory,
            new ClassMemoryManager<int>(new int[3]).Memory,
            new ClassMemoryManager<int>(new int[10].AsMemory(2, 3)).Memory,
        ];

        Offset<Memory<int>, int>[] offsets =
        [
            new(1),
            new([1]),
            Offset.ForMemoryElement<int>(1),
        ];

        for (int i = 0; i < memories.Length; i++)
        {
            Memory<int> memory = memories[i];

            Ref<int> reference = new(memory, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
            foreach (Offset<Memory<int>, int> offset in offsets)
            {
                reference = offset.From(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
                reference = offset.FromObject(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Span[1]));
                ref int reference_ = ref offset.FromRef(ref memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));
                reference_ = ref offset.FromRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));
                reference_ = ref offset.FromObjectRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Span[1]));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(memory, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>(memory, memory.Length * 2));
        }

        foreach (Offset<Memory<int>, int> offset in offsets)
        {
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0].AsMemory()));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0].AsMemory()));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0].AsMemory()));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0].AsMemory()));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0].AsMemory()));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new int[0].AsMemory())));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0].AsMemory()));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<Memory<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForMemoryElement<int>(-1));
    }

    [Fact]
    public void MemoryManagerReference()
    {
        ClassMemoryManager<int>[] memories =
        [
            new(new int[3].AsMemory()),
            new(new int[10].AsMemory(2, 3)),
            new(new int[10].AsMemory(2, 3)),
        ];

        Offset<ClassMemoryManager<int>, int>[] offsets =
        [
            new(1),
            new([1]),
            Offset.ForIMemoryOwnerElement<ClassMemoryManager<int>, int>(1),
            Offset.ForMemoryManagerElement<ClassMemoryManager<int>, int>(1),
        ];

        for (int i = 0; i < memories.Length; i++)
        {
            ClassMemoryManager<int> memory = memories[i];

            Ref<int> reference = new(memory, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
            foreach (Offset<ClassMemoryManager<int>, int> offset in offsets)
            {
                reference = offset.From(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                reference = offset.FromObject(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                scoped ref int reference_ = ref offset.FromRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromRef(ref memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromObjectRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(MemoryManager<int>)!, 0));
            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(ClassMemoryManager<int>)!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(memory, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>(memory, memory.Memory.Length * 2));
        }

        foreach (Offset<ClassMemoryManager<int>, int> offset in offsets)
        {
            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(ClassMemoryManager<int>))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ClassMemoryManager<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ClassMemoryManager<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.From(new ClassMemoryManager<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ClassMemoryManager<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new ClassMemoryManager<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new ClassMemoryManager<int>(new int[0]))));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ClassMemoryManager<int>(new int[0])));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ClassMemoryManager<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForMemoryManagerElement<ClassMemoryManager<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForMemoryManagerElement<int>(-1));
    }

    [Fact]
    public void MemoryOwnerReference()
    {
        ClassMemoryOwner<int>[] memories =
        [
            new(new int[3].AsMemory()),
            new(new int[10].AsMemory(2, 3)),
            new(new int[10].AsMemory(2, 3)),
        ];

        Offset<ClassMemoryOwner<int>, int>[] offsets =
        [
            new(1),
            new([1]),
            Offset.ForIMemoryOwnerElement<ClassMemoryOwner<int>, int>(1),
        ];

        for (int i = 0; i < memories.Length; i++)
        {
            ClassMemoryOwner<int> memory = memories[i];

            Ref<int> reference = new(memory, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
            foreach (Offset<ClassMemoryOwner<int>, int> offset in offsets)
            {
                reference = offset.From(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                reference = offset.FromObject(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                scoped ref int reference_ = ref offset.FromRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromRef(ref memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromObjectRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(IMemoryOwner<int>)!, 0));
            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(ClassMemoryOwner<int>)!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(memory, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>(memory, memory.Memory.Length * 2));
        }

        foreach (Offset<ClassMemoryOwner<int>, int> offset in offsets)
        {
            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(ClassMemoryOwner<int>))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ClassMemoryOwner<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ClassMemoryOwner<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.From(new ClassMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ClassMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new ClassMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new ClassMemoryOwner<int>(new int[0]))));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ClassMemoryOwner<int>(new int[0])));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ClassMemoryOwner<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForIMemoryOwnerElement<ClassMemoryOwner<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForIMemoryOwnerElement<int>(-1));
    }

    [Fact]
    public void MemoryOwnerValue()
    {
        StructMemoryOwner<int>[] memories =
        [
            new(new int[3].AsMemory()),
            new(new int[10].AsMemory(2, 3)),
            new(new int[10].AsMemory(2, 3)),
        ];

        Offset<StructMemoryOwner<int>, int>[] offsets =
        [
            new(1),
            new([1]),
            Offset.ForIMemoryOwnerElement<StructMemoryOwner<int>, int>(1),
        ];

        for (int i = 0; i < memories.Length; i++)
        {
            StructMemoryOwner<int> memory = memories[i];

            Ref<int> reference = new(memory, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
            foreach (Offset<StructMemoryOwner<int>, int> offset in offsets)
            {
                reference = offset.From(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                reference = offset.FromObject(memory);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref memory.Memory.Span[1]));
                scoped ref int reference_ = ref offset.FromRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromRef(ref memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
                reference_ = ref offset.FromObjectRef(memory);
                Assert.True(Unsafe.AreSame(ref reference_, ref memory.Memory.Span[1]));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(memory, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>(memory, memory.Memory.Length * 2));
        }

        foreach (Offset<StructMemoryOwner<int>, int> offset in offsets)
        {
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new StructMemoryOwner<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new StructMemoryOwner<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.From(new StructMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new StructMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new StructMemoryOwner<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new StructMemoryOwner<int>(new int[0]))));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new StructMemoryOwner<int>(new int[0])));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<StructMemoryOwner<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForIMemoryOwnerElement<StructMemoryOwner<int>, int>(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForIMemoryOwnerElement<int>(-1));
    }

    [Fact]
    public void ArraySegment()
    {
        ArraySegment<int>[] arraySegments =
        [
            new(new int[3]),
            new ArraySegment<int>(new int[10], 2, 3)
        ];

        Offset<ArraySegment<int>, int>[] offsets =
        [
            new(1),
            new([1]),
            Offset.ForArraySegmentElement<int>(1),
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            new(e => e[1])
#endif
        ];

        for (int i = 0; i < arraySegments.Length; i++)
        {
            ArraySegment<int> arraySegment = arraySegments[i];
            Ref<int> reference = new(arraySegment, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
            foreach (Offset<ArraySegment<int>, int> offset in offsets)
            {
                reference = offset.From(arraySegment);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
                reference = offset.FromObject(arraySegment);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref arraySegment.AsSpan()[1]));
                ref int reference_ = ref offset.FromRef(arraySegment);
                Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));
                reference_ = ref offset.FromObjectRef(arraySegment);
                Assert.True(Unsafe.AreSame(ref reference_, ref arraySegment.AsSpan()[1]));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>(arraySegment, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>(arraySegment, arraySegment.Count * 2));
        }

        foreach (Offset<ArraySegment<int>, int> offset in offsets)
        {
            Assert.Throws<ArgumentException>(() => offset.From(default));
            Assert.Throws<ArgumentException>(() => offset.FromRef(default));
            Assert.Throws<ArgumentException>(() => offset.FromObject(default(ArraySegment<int>)));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(default(ArraySegment<int>)));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(default(ArraySegment<int>))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ArraySegment<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ArraySegment<float>(new float[0])));
            Assert.Throws<ArgumentException>(() => offset.From(new ArraySegment<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new ArraySegment<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new ArraySegment<int>(new int[0])));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new ArraySegment<int>(new int[0]))));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new ArraySegment<int>(new int[0])));
        }

        Assert.Throws<ArgumentException>(() => new Ref<int>(default(ArraySegment<int>), 0));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new ArraySegment<object>(new string[3]), 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ArraySegment<int>, int>(-1));
        Assert.Throws<ArgumentNullException>(() => new Offset<ArraySegment<int>, int>(default(Expression<Func<ArraySegment<int>, int>>)!));
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ArraySegment<int>, int>(e => e[-1]));
#endif
        Assert.Throws<ArgumentOutOfRangeException>(() => Offset.ForArraySegmentElement<int>(-1));
    }

    [Fact]
    public void NonZeroIndexArray()
    {
        (Array Array, int[] Indexes, Offset<Array, int>[] Offsets, Func<Array, Ref<int>> RefFactory)[] arrays =
        [
            (
                Array.CreateInstance(typeof(int), [3], [-1]),
                [1],
                [new(1)],
                e => new(e, 1)
            ),
            (
                Array.CreateInstance(typeof(int), [3, 4], [-1, -2]),
                [0, 1],
                [new(0, 1)],
                e => new(e, 0, 1)
            ),
            (
                Array.CreateInstance(typeof(int), [3, 4, 3], [-1, -2, -3]),
                [0, 1, -1],
                [new(0, 1, -1)],
                e => new(e, 0, 1, -1)
            ),
            (
                Array.CreateInstance(typeof(int), [3, 4, 3, 1], [-1, -2, -3, -4]),
                [0, 1, -1, -4],
                [new(0, 1, -1, -4)],
                e => new(e, 0, 1, -1, -4)
            ),
        ];

        for (int i = 0; i < arrays.Length; i++)
        {
            (Array array, int[] indexes, Offset<Array, int>[] offsets, Func<Array, Ref<int>> refFactory) = arrays[i];
            array.SetValue(1, indexes);
            Array.Resize(ref offsets, 2);
            offsets[1] = new(indexes);

            Ref<int> reference = new(array, indexes);
            Assert.Equal(1, reference);
            reference = refFactory(array);
            Assert.Equal(1, reference);

            foreach (Offset<Array, int> offset in offsets)
            {
                reference = offset.From(array);
                Assert.Equal(1, reference);
                reference = offset.FromObject(array);
                Assert.Equal(1, reference);
                scoped ref int reference_ = ref offset.FromRef(array);
                Assert.Equal(1, reference_);
                reference_ = ref offset.FromRef(ref array);
                Assert.Equal(1, reference_);
                reference_ = ref offset.FromObjectRef(array);
                Assert.Equal(1, reference_);

                Assert.Throws<ArgumentNullException>(() => offset.From(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(Array)!)));
                Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            }
        }

        // TODO: Are more checks
    }

    [Fact]
    public void Array2()
    {
        int[,] array = new int[2, 3];
        Ref<int> reference = new(array, 1, 2);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

        foreach (Offset<int[,], int> offset in new Offset<int[,], int>[]
        {
            new(1, 2),
            new([1, 2]),
            new(e => e[1, 2]),
            Offset.ForArrayElement<int>(1, 2),
        })
        {
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
            scoped ref int reference_ = ref offset.FromRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));
            reference_ = ref offset.FromRef(ref array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));
            reference_ = ref offset.FromObjectRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(int[,]))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new int[0, 0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0]));
        }

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,])!, 0, 0));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, -1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, -1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 5, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3], 1, 1));

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,])!, [0, 0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [-1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, -1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [5, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 5]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3], [1, 1]));

        Assert.Throws<ArgumentNullException>(() => new Offset<int[,], int>(default(Expression<Func<int[,], int>>)!));
    }

    [Fact]
    public void Array3()
    {
        int[,,] array = new int[2, 3, 4];
        Ref<int> reference = new(array, 1, 2, 3);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

        foreach (Offset<int[,,], int> offset in new Offset<int[,,], int>[]
        {
            new(1, 2, 3),
            new([1, 2, 3]),
            new(e => e[1, 2, 3]),
            Offset.ForArrayElement<int>(1, 2, 3),
        })
        {
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
            scoped ref int reference_ = ref offset.FromRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));
            reference_ = ref offset.FromRef(ref array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));
            reference_ = ref offset.FromObjectRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(int[,,]))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new int[0, 0, 0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0]));
        }

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,,])!, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, -1, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, -1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, -1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 5, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 5, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3, 3], 1, 1, 1));

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,,])!, [0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [-1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, -1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, -1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [5, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 5, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, 5]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3, 3], [1, 1, 1]));

        Assert.Throws<ArgumentNullException>(() => new Offset<int[,,], int>(default(Expression<Func<int[,,], int>>)!));
    }

    [Fact]
    public void Array4()
    {
        int[,,,] array = new int[2, 3, 4, 5];
        Ref<int> reference = new(array, 1, 2, 3, 4);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

        foreach (Offset<int[,,,], int> offset in new Offset<int[,,,], int>[]
        {
            new(1, 2, 3, 4),
            new([1, 2, 3, 4]),
            new(e => e[1, 2, 3, 4]),
            Offset.ForArrayElement<int>(1, 2, 3, 4),
        })
        {
            reference = offset.From(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
            reference = offset.FromObject(array);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
            scoped ref int reference_ = ref offset.FromRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));
            reference_ = ref offset.FromRef(ref array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));
            reference_ = ref offset.FromObjectRef(array);
            Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));

            Assert.Throws<ArgumentNullException>(() => offset.From(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
            Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(int[,,,]))));
            Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef(new int[0, 0, 0, 0])));
            Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0, 0]));
        }

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,,,])!, 0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, -1, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, -1, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, -1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, 1, -1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 5, 1, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 5, 1, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, 5, 1));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, 1, 1, 1, 5));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3, 3, 3], 1, 1, 1, 1));

        Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(int[,,,])!, [0, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [-1, 1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, -1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, -1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, 1, -1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [5, 1, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 5, 1, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, 5, 1]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [1, 1, 1, 5]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0, 0]));
        Assert.Throws<ArgumentException>(() => new Ref<int>(array, [0, 0, 0, 0, 0]));
        Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>(new string[3, 3, 3, 3], [1, 1, 1, 1]));

        Assert.Throws<ArgumentNullException>(() => new Offset<int[,,,], int>(default(Expression<Func<int[,,,], int>>)!));
    }

    [Fact]
    public void Array_()
    {
        {
            int[] array = new int[3];
            Ref<int> reference = new((Array)array, 1);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));

            foreach (Offset<Array, int> offset in new Offset<Array, int>[]
            {
                new(1),
                new([1]),
            })
            {
                reference = offset.From(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
                reference = offset.FromObject(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1]));
                scoped ref int reference_ = ref offset.FromRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));
                reference_ = ref offset.FromRef(ref Unsafe.AsRef((Array)array));
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));
                reference_ = ref offset.FromObjectRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1]));

                Assert.Throws<ArgumentNullException>(() => offset.From(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(Array)!)));
                Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
                Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef((Array)new int[0, 0])));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0]));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>((Array)array, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, array.Length * 2));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3], 1));

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, [0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Ref<int>((Array)array, [-1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [array.Length * 2]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, []));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3], [1]));
        }

        {
            int[,] array = new int[2, 3];
            Ref<int> reference = new((Array)array, 1, 2);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));

            foreach (Offset<Array, int> offset in new Offset<Array, int>[]
            {
                new(1, 2),
                new([1, 2]),
            })
            {
                reference = offset.From(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
                reference = offset.FromObject(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2]));
                scoped ref int reference_ = ref offset.FromRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));
                reference_ = ref offset.FromRef(ref Unsafe.AsRef((Array)array));
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));
                reference_ = ref offset.FromObjectRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2]));

                Assert.Throws<ArgumentNullException>(() => offset.From(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(Array)!)));
                Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
                Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef((Array)new int[0, 0, 0])));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, 0, 0));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, -1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 5, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3], 1, 1));

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, [0, 0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [-1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, -1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [5, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 5]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3], [1, 1]));
        }

        {
            int[,,] array = new int[2, 3, 4];
            Ref<int> reference = new((Array)array, 1, 2, 3);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));

            foreach (Offset<Array, int> offset in new Offset<Array, int>[]
            {
                new(1, 2, 3),
                new([1, 2, 3]),
            })
            {
                reference = offset.From(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
                reference = offset.FromObject(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3]));
                scoped ref int reference_ = ref offset.FromRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));
                reference_ = ref offset.FromRef(ref Unsafe.AsRef((Array)array));
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));
                reference_ = ref offset.FromObjectRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3]));

                Assert.Throws<ArgumentNullException>(() => offset.From(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(Array)!)));
                Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
                Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef((Array)new int[0, 0, 0, 0])));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0, 0]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, -1, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, -1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 5, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 5, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3, 3], 1, 1, 1));

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, [0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [-1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, -1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, -1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [5, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 5, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, 5]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3, 3], [1, 1, 1]));
        }

        {
            int[,,,] array = new int[2, 3, 4, 5];
            Ref<int> reference = new((Array)array, 1, 2, 3, 4);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));

            foreach (Offset<Array, int> offset in new Offset<Array, int>[]
            {
                new(1, 2, 3, 4),
                new([1, 2, 3, 4]),
            })
            {
                reference = offset.From(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
                reference = offset.FromObject(array);
                Assert.True(Unsafe.AreSame(ref reference.Value, ref array[1, 2, 3, 4]));
                scoped ref int reference_ = ref offset.FromRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));
                reference_ = ref offset.FromRef(ref Unsafe.AsRef((Array)array));
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));
                reference_ = ref offset.FromObjectRef(array);
                Assert.True(Unsafe.AreSame(ref reference_, ref array[1, 2, 3, 4]));

                Assert.Throws<ArgumentNullException>(() => offset.From(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(null!));
                Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(Array)!)));
                Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
                Assert.Throws<ArgumentException>(() => offset.From(new int[0, 0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new int[0, 0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(new int[0, 0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromRef(ref Unsafe.AsRef((Array)new int[0, 0, 0, 0, 0])));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new int[0, 0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObject(new float[0, 0, 0, 0]));
                Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new float[0, 0, 0, 0]));
            }

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, 0, 0, 0, 0));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, -1, 1, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, -1, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, -1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, 1, -1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 5, 1, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 5, 1, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, 5, 1));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, 1, 1, 1, 5));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3, 3, 3], 1, 1, 1, 1));

            Assert.Throws<ArgumentNullException>(() => new Ref<int>(default(Array)!, [0, 0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [-1, 1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, -1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, -1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, 1, -1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [5, 1, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 5, 1, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, 5, 1]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [1, 1, 1, 5]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0, 0]));
            Assert.Throws<ArgumentException>(() => new Ref<int>((Array)array, [0, 0, 0, 0, 0]));
            Assert.Throws<ArrayTypeMismatchException>(() => new Ref<object>((Array)new string[3, 3, 3, 3], [1, 1, 1, 1]));
        }
    }

    [Fact]
    public void FieldClass()
    {
        ClassA classA = new();

        Offset<ClassA, int> offset = new(e => e.c);
        Ref<int> reference = offset.From(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        reference = offset.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        scoped ref int reference_ = ref offset.FromRef(classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));
        reference_ = ref offset.FromRef(ref classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));
        reference_ = ref offset.FromObjectRef(classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));

        FieldInfo fieldInfo = typeof(ClassA).GetField("c", BindingFlags.Public | BindingFlags.Instance);
        offset = new(fieldInfo);
        reference = offset.From(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        reference = offset.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref reference.Value, ref classA.c));
        reference_ = ref offset.FromRef(classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));
        reference_ = ref offset.FromRef(ref classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));
        reference_ = ref offset.FromObjectRef(classA);
        Assert.True(Unsafe.AreSame(ref reference_, ref classA.c));

        Assert.Throws<ArgumentException>(() => new Offset<ClassA, float>(fieldInfo));
        Assert.Throws<ArgumentException>(() => new Offset<StructA, int>(fieldInfo));
        Assert.Throws<ArgumentNullException>(() => new Offset<ClassA, int>(default(FieldInfo)!));

        Assert.Throws<ArgumentNullException>(() => offset.From(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromRef(ref Unsafe.AsRef(default(ClassA))));
        Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new object()));
        Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new object()));

        Offset<ClassA, string?> offsetString = new(e => e.d);
        Ref<string?> referenceString = offsetString.From(classA);
        Assert.True(Unsafe.AreSame(ref referenceString.Value, ref classA.d));
        referenceString = offsetString.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref referenceString.Value, ref classA.d));
        scoped ref string? referenceString_ = ref offsetString.FromRef(classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));
        referenceString_ = ref offsetString.FromRef(ref classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));
        referenceString_ = ref offsetString.FromObjectRef(classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));

        FieldInfo fieldInfo_ = typeof(ClassA).GetField("d", BindingFlags.Public | BindingFlags.Instance);
        offsetString = new(fieldInfo_);
        referenceString = offsetString.From(classA);
        Assert.True(Unsafe.AreSame(ref referenceString.Value, ref classA.d));
        referenceString = offsetString.FromObject(classA);
        Assert.True(Unsafe.AreSame(ref referenceString.Value, ref classA.d));
        referenceString_ = ref offsetString.FromRef(classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));
        referenceString_ = ref offsetString.FromRef(ref classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));
        referenceString_ = ref offsetString.FromObjectRef(classA);
        Assert.True(Unsafe.AreSame(ref referenceString_, ref classA.d));

        Assert.Throws<ArgumentNullException>(() => offsetString.From(null!));
        Assert.Throws<ArgumentNullException>(() => offsetString.FromObject(null!));
        Assert.Throws<ArgumentNullException>(() => offsetString.FromRef(ref Unsafe.AsRef(default(ClassA))));
        Assert.Throws<ArgumentNullException>(() => offsetString.FromObjectRef(null!));
        Assert.Throws<ArgumentException>(() => offsetString.FromObject(new object()));
        Assert.Throws<ArgumentException>(() => offsetString.FromObjectRef(new object()));
    }

    [Fact]
    public void FieldStruct()
    {
        StructA structA = new() { c = 1 };
        object boxed = structA;

        Offset<StructA, int> offset = new(e => e.c);
        Ref<int> reference = offset.FromObject(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif
        Assert.Equal(1, reference.Value);
        scoped ref int reference_ = ref offset.FromRef(ref structA);
        Assert.True(Unsafe.AreSame(ref reference_, ref structA.c));
        reference_ = ref offset.FromObjectRef(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif

        FieldInfo fieldInfo = typeof(StructA).GetField("c", BindingFlags.Public | BindingFlags.Instance);
        offset = new(fieldInfo);
        reference = offset.FromObject(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif
        Assert.Equal(1, reference.Value);
        reference_ = ref offset.FromRef(ref structA);
        Assert.True(Unsafe.AreSame(ref reference_, ref structA.c));
        reference_ = ref offset.FromObjectRef(boxed);
#if NET5_0_OR_GREATER
        Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<StructA>(boxed).c));
#endif

        Assert.Throws<ArgumentException>(() => new Offset<StructA, float>(fieldInfo));
        Assert.Throws<ArgumentException>(() => new Offset<ClassA, int>(fieldInfo));
        Assert.Throws<ArgumentNullException>(() => new Offset<StructA, int>(default(FieldInfo)!));

        Assert.Throws<InvalidOperationException>(() => offset.From(default));
        Assert.Throws<InvalidOperationException>(() => offset.FromRef(structA));
        Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        Assert.Throws<ArgumentNullException>(() => offset.FromObjectRef(null!));
        Assert.Throws<ArgumentException>(() => offset.FromObject(new object()));
        Assert.Throws<ArgumentException>(() => offset.FromObjectRef(new object()));
    }

#if NET8_0_OR_GREATER
    [Fact]
    public void InlineArray()
    {
        ValueArray<int> valueArray = new();
        valueArray[2] = 1;

        object boxedValueArray = valueArray;

        foreach (Offset<ValueArray<int>, int> offset in new Offset<ValueArray<int>, int>[]
        {
            new(2),
            new([2]),
        })
        {
            Ref<int> reference = offset.FromObject(boxedValueArray);
#if NET5_0_OR_GREATER
            Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<ValueArray<int>>(boxedValueArray)[2]));
#endif
            Assert.Equal(1, reference.Value);
            ref int reference_ = ref offset.FromRef(ref valueArray);
            Assert.True(Unsafe.AreSame(ref reference_, ref valueArray[2]));
            reference_ = ref offset.FromObjectRef(boxedValueArray);
            Assert.True(Unsafe.AreSame(ref reference.Value, ref Unsafe.Unbox<ValueArray<int>>(boxedValueArray)[2]));

            Assert.Throws<InvalidOperationException>(() => offset.From(default));
            Assert.Throws<InvalidOperationException>(() => offset.FromRef(default));
            Assert.Throws<ArgumentNullException>(() => offset.FromObject(null!));
        }

        Assert.Throws<ArgumentException>(() => new Offset<ValueArray<int>, int>([]));
        Assert.Throws<ArgumentException>(() => new Offset<ValueArray<int>, int>([0, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ValueArray<int>, int>([-1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offset<ValueArray<int>, int>(-1));
        Assert.Throws<ArgumentException>(() => new Offset<ValueArray<int>, int>([6]));
        Assert.Throws<ArgumentException>(() => new Offset<ValueArray<int>, int>(6));
    }
#endif
}

internal class ClassA
{
    public int b;
    public int c;
    public string? d;
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

internal class ClassMemoryOwner<T>(Memory<T> memory) : IMemoryOwner<T>
{
    public Memory<T> Memory => memory;

    public void Dispose() => throw new NotImplementedException();
}

internal struct StructMemoryOwner<T>(Memory<T> memory) : IMemoryOwner<T>
{
    public Memory<T> Memory => memory;

    public void Dispose() => throw new NotImplementedException();
}

internal class ClassMemoryManager<T>(Memory<T> memory) : MemoryManager<T>
{
    public override Span<T> GetSpan() => memory.Span;

    public override MemoryHandle Pin(int elementIndex = 0) => throw new NotImplementedException();

    public override void Unpin() => throw new NotImplementedException();

    protected override void Dispose(bool disposing) => throw new NotImplementedException();
}