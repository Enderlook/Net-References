using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.References;

/// <summary>
/// Represent the offset of a managed reference.
/// </summary>
/// <typeparam name="TOwner">Type of owner.</typeparam>
/// <typeparam name="TReference">Type of the inner reference.</typeparam>
public sealed class Offset<TOwner, TReference>
{
    internal readonly Mode _mode;
    internal readonly object? _payload;
    private readonly int _index;

    private bool _referenceCached;
    private object? _referencePayload;

    private bool _valueCached;
    private RefFuncRef<TOwner, TReference>? _valuePayload;

    internal Offset(Mode mode, object? payload, int index)
    {
        _mode = mode;
        _payload = payload;
        _index = index;
    }

    /// <summary>
    /// Creates an offset for an element array or field.
    /// </summary>
    /// <param name="expression">Expression which contains the access to make an offset.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index inside the <paramref name="expression"/> is negative and <typeparamref name="TOwner"/> is a single-dimensional zero-based array.</exception>
    /// <exception cref="ArgumentException">Thrown when expression is not valid.</exception>
    public Offset(Expression<Func<TOwner, TReference>> expression)
    {
        if (expression is null)
            Utils.ThrowArgumentNullException_Expression();

        int index_;

        if (typeof(TOwner) == typeof(ArraySegment<TReference>))
        {
            _mode = Mode.ArraySegment;
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (expression.Body is MethodCallExpression { Method.Name: "get_Item", Arguments: [ConstantExpression { Value: int index }] })
#else
            if (expression.Body is MethodCallExpression { Method.Name: "get_Item" } methodCallExpression && methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0] is ConstantExpression { Value: int index })
#endif
            {
                index_ = index;
                goto checkNegativeIndex;
            }
        }
        else
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        if (typeof(TOwner).IsSZArray)
        {
            if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference)) Utils.ThrowArrayTypeMismatchException_Array();
            _mode = Mode.SingleZeroArray;
            switch (expression.Body)
            {
                case BinaryExpression { NodeType: ExpressionType.ArrayIndex, Right: ConstantExpression { Value: int index } }:
                    index_ = index;
                    goto checkNegativeIndex;
                case MethodCallExpression { Method.Name: "Get", Arguments: [ConstantExpression { Value: int index }] }:
                    index_ = index;
                    goto checkNegativeIndex;
            }
        }
        else
#endif
        if (typeof(TOwner).IsArray)
        {
            if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference)) Utils.ThrowArrayTypeMismatchException_Array();
#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
            if (typeof(TReference).MakeArrayType() == typeof(TOwner))
            {
                _mode = Mode.SingleZeroArray;
                switch (expression.Body)
                {
                    case BinaryExpression { NodeType: ExpressionType.ArrayIndex, Right: ConstantExpression { Value: int index } }:
                        index_ = index;
                        goto checkNegativeIndex;
#if NETSTANDARD2_1_OR_GREATER
                    case MethodCallExpression { Method.Name: "Get", Arguments: [ConstantExpression { Value: int index }] }:
#else
                    case MethodCallExpression { Method.Name: "Get" } methodCallExpression when methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0] is ConstantExpression { Value: int index }:
#endif
                        index_ = index;
                        goto checkNegativeIndex;
                }
            }
            else
#endif
            {
                int rank = typeof(TOwner).GetArrayRank();
                if (rank == 1)
                {
                    switch (expression.Body)
                    {
                        case BinaryExpression { NodeType: ExpressionType.ArrayIndex, Right: ConstantExpression { Value: int index } }:
                            _mode = Mode.SingleArray;
                            _index = index;
                            return;
#if NETSTANDARD2_1_OR_GREATER
                        case MethodCallExpression { Method.Name: "Get", Arguments: [ConstantExpression { Value: int index }] }:
#else
                        case MethodCallExpression { Method.Name: "Get" } methodCallExpression when methodCallExpression.Arguments.Count == 1 && methodCallExpression.Arguments[0] is ConstantExpression { Value: int index }:
#endif
                            _mode = Mode.SingleArray;
                            _index = index;
                            return;
                    }
                }
                else if (expression.Body is MethodCallExpression { Method.Name: "Get" } methodCallExpression)
                {
                    _mode = Mode.Array;
                    ReadOnlyCollection<Expression> arguments = methodCallExpression.Arguments;
                    if (rank == arguments.Count)
                    {
                        int[] indexes = new int[arguments.Count];
                        _payload = indexes;
                        for (int i = 0; i < indexes.Length; i++)
                            if (arguments[i] is ConstantExpression { Value: int index })
                                indexes[i] = index;
                        return;
                    }
                }
            }
        }

        if (expression.Body is MemberExpression { Member: FieldInfo fieldInfo })
        {
            if (fieldInfo.FieldType != typeof(TReference))
                Utils.ThrowArgumentException_FieldInfoFieldTypeIsNotTReference();

            _payload = fieldInfo;
            return;
        }

        Utils.ThrowArgumentException_InvalidExpression();
        return;

    checkNegativeIndex:
        _index = index_;
        if (index_ < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegativeForSingleDimensionalArrayOrArraySegment();
    }

    /// <summary>
    /// Create an offset to an specific field.
    /// </summary>
    /// <param name="fieldInfo">Field to calculate offset.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldInfo"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldInfo"/> is not a field that is found in the type <typeparamref name="TOwner"/> or its <see cref="FieldInfo.FieldType"/> is not <typeparamref name="TReference"/>.</exception>
    public Offset(FieldInfo fieldInfo)
    {
        if (fieldInfo is null)
            Utils.ThrowArgumentNullException_FieldInfo();
        if ((!fieldInfo.DeclaringType?.IsAssignableFrom(typeof(TOwner))) ?? true)
            Utils.ThrowArgumentException_FieldInfoNotBelongToType();
        if (fieldInfo.FieldType != typeof(TReference))
            Utils.ThrowArgumentException_FieldInfoFieldTypeIsNotTReference();

        _payload = fieldInfo;
        _mode = Mode.FieldInfo;
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>, <see cref="ArraySegment{T}"/>, <see cref="Memory{T}"/> or <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <typeparamref name="TOwner"/> is an array and <see cref="Type.GetElementType"/> is not <typeparamref name="TReference"/>, unless <typeparamref name="TOwner"/> is <see cref="Array"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="TOwner"/> is a single-dimensional zero-bound array, <see cref="IMemoryOwner{T}"/>, <see cref="Memory{T}"/>, <see cref="ArraySegment{T}"/>, or inline array, and <paramref name="index"/> is negative.</exception>
    /// <exception cref="RankException">Thrown when <typeparamref name="TOwner"/> is an array whose <see cref="Type.GetArrayRank"/> is not 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOwner"/> is array whose element type is not <typeparamref name="TReference"/> or a <see cref="IMemoryOwner{T}"/> whose generic argument is not <typeparamref name="TReference"/>.</exception>
    public Offset(int index)
    {
        _index = index;
        if (typeof(TOwner).IsValueType)
        {
            if (typeof(TOwner) == typeof(ArraySegment<TReference>))
            {
                _mode = Mode.ArraySegment;
                goto checkNegativeIndex;
            }

            if (typeof(TOwner) == typeof(Memory<TReference>))
            {
                _mode = Mode.Memory;
                goto checkNegativeIndex;
            }

#if NET8_0_OR_GREATER
            if (typeof(TOwner).IsValueType && typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                _mode = Mode.InlineArray;
                if (index >= inlineArrayAttribute.Length)
                    Utils.ThrowArgumentException_IndexOutOfBounds();
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
                // Field can't be removed by IL trimmer.
                FieldInfo[] fieldInfos = typeof(TOwner).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
#pragma warning restore IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
                Type fieldType = fieldInfos[0].FieldType;
                if (fieldType != typeof(TReference))
                    Utils.ThrowArgumentException_InlineArrayElementTypeMismatch();
                goto checkNegativeIndex;
            }
#endif
        }
        else
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (typeof(TOwner).IsSZArray)
            {
                _mode = Mode.SingleZeroArray;
                if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference))
                    Utils.ThrowArrayTypeMismatchException_Array();
                goto checkNegativeIndex;
            }
#endif

            if (typeof(TOwner).IsArray)
            {
                if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference))
                    Utils.ThrowArrayTypeMismatchException_Array();
                if (typeof(TOwner).GetArrayRank() != 1)
                    Utils.ThrowRankException_TOwnerMustBeOfRank1();
#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
                if (typeof(TOwner) != typeof(TReference).MakeArrayType())
                {
                    _mode = Mode.SingleZeroArray;
                    goto checkNegativeIndex;
                }
                else
#endif
                _mode = Mode.SingleArray;
                return;
            }

            if (typeof(TOwner) == typeof(Array))
            {
                _mode = Mode.SingleArrayUnkown;
                return;
            }

            if (typeof(MemoryManager<TReference>).IsAssignableFrom(typeof(TOwner)))
            {
                _mode = Mode.MemoryManager;
                goto checkNegativeIndex;
            }
        }

        if (typeof(IMemoryOwner<TReference>).IsAssignableFrom(typeof(TOwner)))
        {
            _mode = Mode.IMemoryOwner;
            goto checkNegativeIndex;
        }

        Utils.ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner();
        return;

    checkNegativeIndex:
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
    }

    /// <summary>
    /// Creates an offset to an specific location of an <see cref="Array"/>, <see cref="ArraySegment{T}"/>, <see cref="Memory{T}"/> or <see cref="MemoryManager{T}"/>.
    /// </summary>
    /// <param name="indexes">Indexes of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <typeparamref name="TOwner"/> is an array and <see cref="Type.GetElementType"/> is not <typeparamref name="TReference"/>, unless <typeparamref name="TOwner"/> is <see cref="Array"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="TOwner"/> is a single-dimensional zero-bound array, <see cref="IMemoryOwner{T}"/>, <see cref="Memory{T}"/>, or <see cref="ArraySegment{T}"/>, and <paramref name="indexes"/> only element is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="indexes"/> length doesn't match <see cref="Type.GetArrayRank"/> when <typeparamref name="TOwner"/> is an array, or 1 when is an <see cref="MemoryManager{T}"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOwner"/> is array whose element type is not <typeparamref name="TReference"/> or a <see cref="IMemoryOwner{T}"/> whose generic argument is not <typeparamref name="TReference"/>.</exception>
    public Offset(params ReadOnlySpan<int> indexes)
    {
        int index;
        if (typeof(TOwner).IsValueType)
        {
            if (typeof(TOwner) == typeof(Memory<TReference>))
            {
                _mode = Mode.Memory;
                goto getNegativeSingleIndex;
            }

            if (typeof(TOwner) == typeof(ArraySegment<TReference>))
            {
                _mode = Mode.ArraySegment;
                goto getNegativeSingleIndex;
            }

#if NET8_0_OR_GREATER
            if (typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                _mode = Mode.InlineArray;
                if (indexes.Length != 1)
                    Utils.ThrowArgumentException_SingleIndexRequiredForMemoryOrArraySegmentOrIMemoryOwnerOrInlineArray();
                index = _index = indexes[0];
                if (index >= inlineArrayAttribute.Length)
                    Utils.ThrowArgumentException_IndexOutOfBounds();
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
                // Field can't be removed by IL trimmer.
                FieldInfo[] fieldInfos = typeof(TOwner).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
#pragma warning restore IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
                Type fieldType = fieldInfos[0].FieldType;
                if (fieldType != typeof(TReference))
                    Utils.ThrowArgumentException_InlineArrayElementTypeMismatch();
                goto checkNegativeIndex;
            }
#endif
        }
        else
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (typeof(TOwner).IsSZArray)
            {
                _mode = Mode.SingleZeroArray;
                if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference))
                    Utils.ThrowArrayTypeMismatchException_Array();
                if (indexes.Length != 1)
                    Utils.ThrowArgumentException_TOwnerIndexesLengthDoesNotMatchRank();
                index = _index = indexes[0];
                if (index < 0)
                    Utils.ThrowArgumentOutOfRangeException_IndexesCanNotBeNegative();
                return;
            }
#endif

            if (typeof(TOwner).IsArray)
            {
                if (!typeof(TReference).IsValueType && typeof(TOwner).GetElementType() != typeof(TReference))
                    Utils.ThrowArrayTypeMismatchException_Array();
                if (typeof(TOwner).GetArrayRank() != indexes.Length)
                    Utils.ThrowArgumentException_TOwnerIndexesLengthDoesNotMatchRank();

                if (indexes.Length == 1)
                {
                    index = _index = indexes[0];
#if !(NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER)
                    if (typeof(TOwner) != typeof(TReference).MakeArrayType())
                    {
                        _mode = Mode.SingleZeroArray;
                        goto checkNegativeIndex;
                    }
#endif
                    _mode = Mode.SingleArray;
                }
                else
                {
                    _mode = Mode.Array;
                    _payload = indexes.ToArray();
                }
                return;
            }

            if (typeof(TOwner) == typeof(Array))
            {
                if (indexes.Length == 1)
                {
                    _index = indexes[0];
                    _mode = Mode.SingleArrayUnkown;
                }
                else
                {
                    _payload = indexes.ToArray();
                    _mode = Mode.ArrayUnkown;
                }
                return;
            }

            if (typeof(MemoryManager<TReference>).IsAssignableFrom(typeof(TOwner)))
            {
                _mode = Mode.MemoryManager;
                goto getNegativeSingleIndex;
            }
        }

        if (typeof(IMemoryOwner<TReference>).IsAssignableFrom(typeof(TOwner)))
        {
            _mode = Mode.IMemoryOwner;
            goto getNegativeSingleIndex;
        }

        Utils.ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner();
        return;

    getNegativeSingleIndex:
        if (indexes.Length != 1)
            Utils.ThrowArgumentException_SingleIndexRequiredForMemoryOrArraySegmentOrIMemoryOwnerOrInlineArray();
        index = _index = indexes[0];

#pragma warning disable CS0164 // This label has not been referenced
    checkNegativeIndex:
#pragma warning restore CS0164 // This label has not been referenced
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
    }

    /// <summary>
    /// Creates an inner reference for the specified owner.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="ArgumentException">Thrown when the offset is an index from an <see cref="Array"/>, a <see cref="Memory{T}"/>, an <see cref="ArraySegment{T}"/>, or a type assignable to <see cref="IMemoryOwner{T}"/> and is out of bounds, or <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when the offset is an index from an <see cref="Array"/> but the provided array runtime type doesn't match <typeparamref name="TOwner"/>.</exception>
    /// <exception cref="InvalidOperationException">Throw when <typeparamref name="TOwner"/> is not a reference type, unless the offest is an element index from a <see cref="Memory{T}"/>, an <see cref="ArraySegment{T}"/>, or a type assignable to <see cref="IMemoryOwner{T}"/>.</exception>
    public Ref<TReference> From(TOwner owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();
        if (typeof(TOwner).IsValueType)
        {
            if (typeof(TOwner) == typeof(Memory<TReference>))
            {
                if (_mode != Mode.Memory)
                    goto error;
            }
            else if (typeof(TOwner) == typeof(ArraySegment<TReference>))
            {
                if (_mode != Mode.ArraySegment)
                    goto error;
            }
            else if (typeof(IMemoryOwner<TReference>).IsAssignableFrom(typeof(TOwner)))
            {
                if (_mode != Mode.IMemoryOwner)
                    goto error;
            }
            else
                goto error;
        }
        return FromObjectUnsafe(owner);
    error:
        Utils.ThrowInvalidOperationException_InvalidTOwnerTypeValueTypeRestrictions();
        return default;
    }

    /// <summary>
    /// Creates an inner reference for the specified owner, supports boxed value types.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="owner"/> is not assignable to <typeparamref name="TOwner"/>, the offset is an index from an <see cref="Array"/>, a <see cref="Memory{T}"/>, an <see cref="ArraySegment{T}"/>, or a type assignable to <see cref="IMemoryOwner{T}"/> and is out of bounds, or <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when the offset is an index from an <see cref="Array"/> but the provided array runtime type doesn't match <typeparamref name="TOwner"/>.</exception>
    public Ref<TReference> FromObject(object owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();
        if (!typeof(TOwner).IsAssignableFrom(owner.GetType()))
            Utils.ThrowArgumentException_OwnerTypeDoesNotMatch();
        return FromObjectUnsafe(owner);
    }

    /// <summary>
    /// Creates an inner reference for the specified owner, the <see langword="ref"/> is required for value types.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>, or is an offset from an <see cref="ArraySegment{T}"/> or a <see cref="Memory{T}"/> and index is out of range.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/>'s <see cref="Type.GetElementType()"/> is not <typeparamref name="TReference"/>.</exception>
    public unsafe ref TReference FromRef(TOwner owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();

        if (typeof(TOwner).IsValueType)
        {
            if (typeof(TOwner) == typeof(Memory<TReference>))
            {
                if (_mode != Mode.Memory)
                    goto error;
            }
            else if (typeof(TOwner) == typeof(ArraySegment<TReference>))
            {
                if (_mode != Mode.ArraySegment)
                    goto error;
            }
            else if (typeof(IMemoryOwner<TReference>).IsAssignableFrom(typeof(TOwner)))
            {
                if (_mode != Mode.IMemoryOwner)
                    goto error;
            }
            else
                goto error;
        }
        return ref FromObjectUnsafeRef(ref owner);
    error:
        Utils.ThrowInvalidOperationException_InvalidTOwnerTypeValueTypeRestrictions();
#if NET5_0_OR_GREATER
        return ref Unsafe.NullRef<TReference>();
#else
        unsafe
        {
            return ref Unsafe.AsRef<TReference>(null);
        }
#endif
    }

    /// <summary>
    /// Creates an inner reference for the specified owner, the <see langword="ref"/> is required for value types.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>, or is an offset from an <see cref="ArraySegment{T}"/> or a <see cref="Memory{T}"/> and index is out of range.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/>'s <see cref="Type.GetElementType()"/> is not <typeparamref name="TReference"/>.</exception>
    public unsafe ref TReference FromRef(ref TOwner owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();
        return ref FromObjectUnsafeRef(ref owner);
    }

    /// <summary>
    /// Creates an inner reference for the specified owner, supports boxed value types.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="owner"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="owner"/> is not assignable to <typeparamref name="TOwner"/>, the offset is an index from an <see cref="Array"/>, a <see cref="Memory{T}"/>, an <see cref="ArraySegment{T}"/>, or a type assignable to <see cref="IMemoryOwner{T}"/> and is out of bounds, or <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when the offset is an index from an <see cref="Array"/> but the provided array runtime type doesn't match <typeparamref name="TOwner"/>.</exception>
    public ref TReference FromObjectRef(object owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();
        if (!typeof(TOwner).IsAssignableFrom(owner.GetType()))
            Utils.ThrowArgumentException_OwnerTypeDoesNotMatch();
        return ref FromObjectUnsafeRef(ref owner);
    }

    internal unsafe Ref<TReference> FromObjectUnsafe<T>(T owner)
    {
        Debug.Assert(owner is not null);

        switch (_mode)
        {
            case Mode.FieldInfo:
            {
                if (!_referenceCached)
                {
                    Debug.Assert(_payload is FieldInfo);
                    FromField(owner, Unsafe.As<FieldInfo>(_payload));
                    _referenceCached = true;
                }
                Debug.Assert(_referencePayload is not null);
                // Cast is required to use correct overload.
                return new(owner, default, (object)_referencePayload);
            }
            case Mode.SingleZeroArray:
            {
                if (!typeof(TReference).IsValueType && owner.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Array();
                Debug.Assert(owner is TReference[]);
                TReference[] array = Unsafe.As<TReference[]>(owner);
                int index = _index;
                if (index >= array.Length)
                    Utils.ThrowArgumentException_OwnerLengthMustBeGreaterThanIndex();
                // `default(object)` is required to use correct overload.
                return new(owner, index, default(object));
            }
            case Mode.ArraySegment:
            {
                ref ArraySegment<TReference> arraySegment = ref Utils.From<T, ArraySegment<TReference>>(ref owner
#if !NET5_0_OR_GREATER
                    , out _
#endif
                    );
                if (arraySegment.Array is null)
                    Utils.ThrowArgumentException_OwnerSegmentArrayIsNull();
                if (!typeof(TReference).IsValueType && arraySegment.Array.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Segment();
                int index = _index;
                if (index >= arraySegment.Count)
                    Utils.ThrowArgumentException_OwnerCountMustBeGreaterThanIndex();
                // `default(object)` is required to use correct overload.
                return new(arraySegment.Array, index + arraySegment.Offset, default(object));
            }
            case Mode.Memory:
            {
                Debug.Assert(owner is Memory<TReference>);
                ref Memory<TReference> memory = ref Utils.From<T, Memory<TReference>>(ref owner
#if !NET5_0_OR_GREATER
                    , out _
#endif
                    );
                int index = _index;
                if (index >= memory.Length)
                    Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();
                object a;
                int i;
                // Try to avoid boxing the `Memory<T>`.
                if (MemoryMarshal.TryGetArray(memory, out ArraySegment<TReference> segment))
                {
                    a = segment.Array;
                    i = segment.Offset + index;
                }
                else if (MemoryMarshal.TryGetMemoryManager<TReference, MemoryManager<TReference>>(memory, out MemoryManager<TReference>? manager, out int start, out _))
                {
                    a = manager;
                    i = (start + index) | int.MinValue;
                }
                else
                {
                    a = typeof(T).IsValueType ? memory : owner;
                    i = index | int.MinValue;
                }
                // `default(object)` is required to use correct overload.
                return new(a, i, default(object));
            }
            case Mode.IMemoryOwner:
            {
                Debug.Assert(owner is IMemoryOwner<TReference>);
                int index = _index;
                if (index >= (typeof(T).IsValueType ? ((IMemoryOwner<TReference>)owner).Memory : Unsafe.As<IMemoryOwner<TReference>>(owner).Memory).Length)
                    Utils.ThrowArgumentException_OwnerSpanLengthMustBeGreaterThanIndex();
                // `default(object)` is required to use correct overload.
                return new(owner, index | int.MinValue, default(object));
            }
            case Mode.MemoryManager:
            {
                Debug.Assert(owner is MemoryManager<TReference>);
                int index = _index;
                if (index >= Unsafe.As<MemoryManager<TReference>>(owner).GetSpan().Length)
                    Utils.ThrowArgumentException_OwnerSpanLengthMustBeGreaterThanIndex();
                // `default(object)` is required to use correct overload.
                return new(owner, index | int.MinValue, default(object));
            }
#if NET8_0_OR_GREATER
            case Mode.InlineArray:
            {
                Debug.Assert(typeof(TOwner).IsDefined(typeof(InlineArrayAttribute)));
                Debug.Assert(_index < typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>()!.Length);
                if (!_referenceCached)
                {
                    _referencePayload = UnboxerHelper<TOwner>.GetElementAccessor<TReference>();
                    _referenceCached = true;
                }
                // Cast is required to use correct overload.
                return new(owner, _index, (object)_referencePayload);
            }
#endif
            case Mode.SingleArray:
            {
                return FromSingleArray(owner, owner.GetType());
            }
            case Mode.Array:
            {
                Debug.Assert(_payload is int[]);
                return FromMultiArray(owner, owner.GetType(), Unsafe.As<int[]>(_payload));
            }
            case Mode.SingleArrayUnkown:
            {
                Type ownerType = owner.GetType();
                if (ownerType.GetArrayRank() != 1)
                    Utils.ThrowArgumentException_OwnerRankDoesNotMatchIndexes();
                return FromSingleArray(owner, ownerType);
            }
            case Mode.ArrayUnkown:
            {
                Debug.Assert(_payload is int[]);
                int[] indexes = Unsafe.As<int[]>(_payload);
                Type ownerType = owner.GetType();
                if (ownerType.GetArrayRank() != indexes.Length)
                    Utils.ThrowArgumentException_OwnerRankDoesNotMatchIndexes();
                Debug.Assert(_payload is int[]);
                return FromMultiArray(owner, owner.GetType(), indexes);
            }
        }
        Debug.Fail("Impossible state.");
        return default;
    }

    internal unsafe ref TReference FromObjectUnsafeRef<T>(scoped ref T owner)
    {
        Debug.Assert(owner is not null);

        switch (_mode)
        {
            case Mode.FieldInfo:
            {
                if (typeof(T).IsValueType)
                {
                    Debug.Assert(typeof(T) == typeof(TOwner));
                    if (!_valueCached)
                    {
                        Debug.Assert(_payload is FieldInfo);
                        FromFieldValue(Unsafe.As<FieldInfo>(_payload));
                        _valueCached = true;
                    }
                    Debug.Assert(_valuePayload is not null);
                    return ref _valuePayload(ref Unsafe.As<T, TOwner>(ref owner));
                }
                else
                {
                    if (!_referenceCached)
                    {
                        Debug.Assert(_payload is FieldInfo);
                        FromField(owner, Unsafe.As<FieldInfo>(_payload));
                        _referenceCached = true;
                    }
                    object? payload = _referencePayload;
                    Debug.Assert(payload is not null);
                    if (payload.GetType() == typeof(FieldInfo[]))
                    {
                        TypedReference typedReference = TypedReference.MakeTypedReference(owner, Unsafe.As<FieldInfo[]>(payload));
                        ref TReference field = ref __refvalue(typedReference, TReference);
#pragma warning disable CS9082 // Local is returned by reference but was initialized to a value that cannot be returned by reference
                        return ref field;
#pragma warning restore CS9082 // Local is returned by reference but was initialized to a value that cannot be returned by reference
                    }
                    else
                    {
                        Debug.Assert(payload is ReferenceProvider<TReference>);
                        return ref Unsafe.As<ReferenceProvider<TReference>>(payload)(owner, default);
                    }
                }
            }
            case Mode.SingleZeroArray:
            {
                if (!typeof(TReference).IsValueType && owner.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Array();
                Debug.Assert(owner is TReference[]);
                TReference[] array = Unsafe.As<TReference[]>(owner);
                int index = _index;
                if (unchecked((uint)index >= (uint)array.Length))
                    Utils.ThrowArgumentException_OwnerLengthMustBeGreaterThanIndex();
#if NET5_0_OR_GREATER
                return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
                return ref array[index];
#endif
            }
            case Mode.ArraySegment:
            {
                Debug.Assert(typeof(TOwner) == typeof(ArraySegment<TReference>));
                ref ArraySegment<TReference> arraySegment = ref Utils.From<T, ArraySegment<TReference>>(ref owner
#if !NET5_0_OR_GREATER
                    , out _
#endif
                    );
                TReference[]? array = arraySegment.Array;
                if (array is null)
                    Utils.ThrowArgumentException_OwnerSegmentArrayIsNull();
                if (!typeof(TReference).IsValueType && array.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Segment();
                int index = _index;
                if (index >= arraySegment.Count)
                    Utils.ThrowArgumentException_OwnerCountMustBeGreaterThanIndex();
#if NET5_0_OR_GREATER
                return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), arraySegment.Offset + index);
#else
                return ref array[arraySegment.Offset + index];
#endif
            }
            case Mode.Memory:
            {
                Debug.Assert(typeof(TOwner) == typeof(Memory<TReference>));
                return ref FromMemory(ref Utils.From<T, Memory<TReference>>(ref owner
#if !NET5_0_OR_GREATER
                    , out _
#endif
                    ));
            }
            case Mode.IMemoryOwner:
            {
                Debug.Assert(owner is IMemoryOwner<TReference>);
                Memory<TReference> memory = typeof(TOwner).IsValueType ? ((IMemoryOwner<TReference>)owner).Memory : Unsafe.As<IMemoryOwner<TReference>>(owner).Memory;
                return ref FromMemory(ref memory);
            }
            case Mode.MemoryManager:
            {
                Debug.Assert(owner is MemoryManager<TReference>);
                return ref FromMemorySpan(Unsafe.As<MemoryManager<TReference>>(owner).GetSpan());
            }
#if NET8_0_OR_GREATER
            case Mode.InlineArray:
            {
                Debug.Assert(typeof(TOwner).IsDefined(typeof(InlineArrayAttribute)));
                Debug.Assert(_index < typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>()!.Length);

                return ref Unsafe.Add(ref Unsafe.As<TOwner, TReference>(ref typeof(T).IsValueType ? ref Unsafe.As<T, TOwner>(ref owner) : ref UnboxerHelper<TOwner>.Unbox(owner)), _index);
            }
#endif

            case Mode.SingleArray:
            {
                return ref FromSingleArrayRef(owner, owner.GetType());
            }
            case Mode.Array:
            {
                Debug.Assert(_payload is int[]);
                return ref FromMultiArrayRef(owner, owner.GetType(), Unsafe.As<int[]>(_payload));
            }
            case Mode.SingleArrayUnkown:
            {
                Type ownerType = owner.GetType();
                if (ownerType.GetArrayRank() != 1)
                    Utils.ThrowArgumentException_OwnerRankDoesNotMatchIndexes();
                return ref FromSingleArrayRef(owner, ownerType);
            }
            case Mode.ArrayUnkown:
            {
                Debug.Assert(_payload is int[]);
                int[] indexes = Unsafe.As<int[]>(_payload);
                Type ownerType = owner.GetType();
                if (ownerType.GetArrayRank() != indexes.Length)
                    Utils.ThrowArgumentException_OwnerRankDoesNotMatchIndexes();
                Debug.Assert(_payload is int[]);
                return ref FromMultiArrayRef(owner, owner.GetType(), indexes);
            }
            default:
            {
                Debug.Fail("Impossible state.");
#if NET5_0_OR_GREATER
                return ref Unsafe.NullRef<TReference>();
#else
                return ref Unsafe.AsRef<TReference>(null);
#endif
            }
        }
    }

#if NET5_0_OR_GREATER
    [DynamicDependency("As", typeof(Unsafe))]
#endif
    private void FromField<T>(T owner, FieldInfo fieldInfo)
    {
        Debug.Assert(owner is not null);
#if !NET10_0_OR_GREATER
        if (!typeof(TReference).IsPrimitive)
#endif
        {
            try
            {
                object boxed = owner;
                Debug.Assert(boxed is not null);
                FieldInfo[] array = [fieldInfo];
                TypedReference typedReference = TypedReference.MakeTypedReference(boxed, array);
                ref TReference field = ref __refvalue(typedReference, TReference);
                _referencePayload = array;
                return;
            }
            catch (NotImplementedException)
            {
                // The runtime doesn't have support for varargs stuff.
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        if (typeof(TOwner).IsValueType)
        {
            DynamicMethod dynamicMethod = new("GetFieldRef", typeof(TReference).MakeByRefType(), [typeof(object), typeof(nint)]);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Unbox, typeof(TOwner));
            ilGenerator.Emit(OpCodes.Ldflda, fieldInfo);
            ilGenerator.Emit(OpCodes.Ret);
#if NET5_0_OR_GREATER
            _referencePayload = dynamicMethod.CreateDelegate<ReferenceProvider<TReference>>();
#else
            _referencePayload = dynamicMethod.CreateDelegate(typeof(ReferenceProvider<TReference>));
#endif
        }
        else
        {
            DynamicMethod dynamicMethod = new("GetFieldRef", typeof(TReference).MakeByRefType(), [typeof(object), typeof(nint)]);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, Utils.UnsafeAsFor([typeof(TOwner)]));
            ilGenerator.Emit(OpCodes.Ldflda, fieldInfo);
            ilGenerator.Emit(OpCodes.Ret);
#if NET5_0_OR_GREATER
            _referencePayload = dynamicMethod.CreateDelegate<ReferenceProvider<TReference>>();
#else
            _referencePayload = dynamicMethod.CreateDelegate(typeof(ReferenceProvider<TReference>));
#endif
        }
        return;
#endif
        Utils.ThrowNotImplementedException();
    }

    private void FromFieldValue(FieldInfo fieldInfo)
    {
        Debug.Assert(typeof(TOwner).IsValueType);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        DynamicMethod dynamicMethod = new("GetFieldRef", typeof(TReference).MakeByRefType(), [typeof(TOwner).MakeByRefType()]);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Ldflda, fieldInfo);
        ilGenerator.Emit(OpCodes.Ret);
#if NET5_0_OR_GREATER
        RefFuncRef<TOwner, TReference> @delegate = dynamicMethod.CreateDelegate<RefFuncRef<TOwner, TReference>>();
#else
        RefFuncRef<TOwner, TReference> @delegate = Unsafe.As<RefFuncRef<TOwner, TReference>>(dynamicMethod.CreateDelegate(typeof(RefFuncRef<TOwner, TReference>)));
#endif
        _valuePayload = @delegate;
        return;
#endif

        Utils.ThrowNotImplementedException();
    }

    private Ref<TReference> FromSingleArray(object owner, Type ownerType)
    {
        Debug.Assert(owner.GetType() == ownerType);
        if (!typeof(TReference).IsValueType && ownerType.GetElementType()! != typeof(TReference))
            Utils.ThrowArrayTypeMismatchException_Array();
        Debug.Assert(owner is Array);
        Array array = Unsafe.As<Array>(owner);
        int index = _index;
        int lowerBound = array.GetLowerBound(0);
        if (index < lowerBound || index > array.GetUpperBound(0))
            Utils.ThrowArgumentException_OwnerIndexOutOfBounds();
#if NET6_0_OR_GREATER
                // `default(object)` is required to use correct overload.
        return new(owner, index - lowerBound, default(object));
#else
        // `default(object)` is required to use correct overload.
        return new(owner, index, default(object));
#endif
    }

    private ref TReference FromSingleArrayRef(object owner, Type ownerType)
    {
        Debug.Assert(owner.GetType() == ownerType);
        if (!typeof(TReference).IsValueType && ownerType.GetElementType()! != typeof(TReference))
            Utils.ThrowArrayTypeMismatchException_Array();
        Debug.Assert(owner is Array);
        Array array = Unsafe.As<Array>(owner);
        int index = _index;
        int lowerBound = array.GetLowerBound(0);
        if (index < lowerBound || index > array.GetUpperBound(0))
            Utils.ThrowArgumentException_OwnerIndexOutOfBounds();
#if NET6_0_OR_GREATER
        return ref Unsafe.Add(ref Unsafe.As<byte, TReference>(ref MemoryMarshal.GetArrayDataReference(array)), index - lowerBound);
#else
        return ref ObjectHelpers.GetReference1<TReference>(owner, index);
#endif
    }

    private static Ref<TReference> FromMultiArray(object owner, Type ownerType, int[] indexes)
    {
        Debug.Assert(owner.GetType() == ownerType);
        if (!typeof(TReference).IsValueType && ownerType.GetElementType() != typeof(TReference))
            Utils.ThrowArrayTypeMismatchException_Array();
        Debug.Assert(owner is Array);
        Array array = Unsafe.As<Array>(owner);
#if NET6_0_OR_GREATER
        int index = Utils.CalculateIndex(array, indexes);
        // `default(object)` is required to use correct overload.
        return new(owner, index, default(object));
#else
        Utils.CheckBounds(array, indexes);
        // Cast is required to use correct overload.
        return new(owner, default, (object)indexes);
#endif
    }

    private static ref TReference FromMultiArrayRef(object owner, Type ownerType, int[] indexes)
    {
        Debug.Assert(owner.GetType() == ownerType);
        if (!typeof(TReference).IsValueType && ownerType.GetElementType() != typeof(TReference))
            Utils.ThrowArrayTypeMismatchException_Array();
        Debug.Assert(owner is Array);
        Array array = Unsafe.As<Array>(owner);
#if NET6_0_OR_GREATER
        int index = Utils.CalculateIndex(array, indexes);
        return ref Unsafe.Add(ref Unsafe.As<byte, TReference>(ref MemoryMarshal.GetArrayDataReference(array)), index);
#else
        Utils.CheckBounds(array, indexes);
        return ref Utils.GetReference<TReference>(array, indexes);
#endif
    }

    private ref TReference FromMemory(scoped ref Memory<TReference> memory)
    {
        int index = _index;
        if (memory.Length < index)
            Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(memory.Span), index);
    }

    private ref TReference FromMemorySpan(Span<TReference> span)
    {
        int index = _index;
        if (span.Length < index)
            Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
    }
}