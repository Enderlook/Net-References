using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
public sealed class InnerOffset<TOwner, TReference> : InnerOffset
{
    internal readonly Mode _mode;
    internal readonly object? _payload;
    private readonly int _index;

    private bool _referenceCached;
    private object? _referencePayload;

    private bool _valueCached;
    private RefFuncRef<TOwner, TReference>? _valuePayload;

    internal InnerOffset(Mode mode, object? payload, int index)
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
    public InnerOffset(Expression<Func<TOwner, TReference>> expression)
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
    public InnerOffset(FieldInfo fieldInfo)
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
    /// Creates an offset to an specific index of an <see cref="Array"/>, <see cref="Memory{T}"/> or <see cref="MemoryManager{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <typeparamref name="TOwner"/> is an array and <see cref="Type.GetElementType"/> is not <typeparamref name="TReference"/>, unless <typeparamref name="TOwner"/> is <see cref="Array"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="TOwner"/> is a single-dimensional zero-bound array or <see cref="MemoryManager{T}"/> or <see cref="Memory{T}"/> or <see cref="ArraySegment{T}"/>, and <paramref name="index"/> is negative.</exception>
    /// <exception cref="RankException">Thrown when <typeparamref name="TOwner"/> is an array whose <see cref="Type.GetArrayRank"/> is not 1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOwner"/> is not an array whose element type is <typeparamref name="TReference"/> nor a <see cref="MemoryManager{T}"/> whose generic argument is <typeparamref name="TReference"/>.</exception>
    public InnerOffset(int index)
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
            if (typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>() is InlineArrayAttribute inlineArrayAttribute)
            {
                _mode = Mode.InlineArray;
                if (index >= inlineArrayAttribute.Length)
                    Utils.ThrowArgumentException_IndexOutOfBounds();
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
                _mode = Mode.IMemoryOwner;
                goto checkNegativeIndex;
            }
        }

        Utils.ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner();
        return;

    checkNegativeIndex:
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
    }

    /// <summary>
    /// Creates an offset to an specific location of an <see cref="Array"/>, <see cref="Memory{T}"/> or <see cref="MemoryManager{T}"/>.
    /// </summary>
    /// <param name="indexes">Indexes of the element to get its reference.</param>
    /// <exception cref="ArrayTypeMismatchException">Throw when <typeparamref name="TOwner"/> is an array and <see cref="Type.GetElementType"/> is not <typeparamref name="TReference"/>, unless <typeparamref name="TOwner"/> is <see cref="Array"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="TOwner"/> is a single-dimensional zero-bound array or is <see cref="MemoryManager{T}"/> and <paramref name="indexes"/> only element is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="indexes"/> length doesn't match <see cref="Type.GetArrayRank"/> when <typeparamref name="TOwner"/> is an array, or 1 when is an <see cref="MemoryManager{T}"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOwner"/> is not an array whose element type is <typeparamref name="TReference"/> nor a <see cref="MemoryManager{T}"/> whose generic argument is <typeparamref name="TReference"/>.</exception>
    public InnerOffset(params ReadOnlySpan<int> indexes)
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
                    Utils.ThrowArgumentException_SingleIndexRequiredForIMemoryOwnerMemoryOrArraySegment();
                index = _index = indexes[0];
                if (index >= inlineArrayAttribute.Length)
                    Utils.ThrowArgumentException_IndexOutOfBounds();
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
                _mode = Mode.IMemoryOwner;
                goto getNegativeSingleIndex;
            }
        }

        Utils.ThrowInvalidOperationException_OnlyArrayOrIMemoryOwner();
        return;

    getNegativeSingleIndex:
        if (indexes.Length != 1)
            Utils.ThrowArgumentException_SingleIndexRequiredForIMemoryOwnerMemoryOrArraySegment();
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
    /// <exception cref="ArgumentException">Thrown when the offset is an index from an <see cref="Array"/>, <see cref="ArraySegment{T}"/> or <see cref="IMemoryOwner{T}"/> and is out of bounds, or <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when the offset is an index from an <see cref="Array"/> but the provided array runtime type doesn't match <typeparamref name="TOwner"/>.</exception>
    /// <exception cref="InvalidOperationException">Throw when <typeparamref name="TOwner"/> is a value type and the offset is not an element index of a <see cref="Memory{T}"/>, an <see cref="ArraySegment{T}"/> nor from a type assignable to <see cref="IMemoryOwner{T}"/>.</exception>
    public InnerRef<TReference> From(TOwner owner)
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
    /// <exception cref="ArgumentException">Thrown when the type of <paramref name="owner"/> is not assignable to <typeparamref name="TOwner"/>, the offset is an index from an <see cref="Array"/>, <see cref="ArraySegment{T}"/> or <see cref="IMemoryOwner{T}"/> and is out of bounds, or <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when the offset is an index from an <see cref="Array"/> but the provided array runtime type doesn't match <typeparamref name="TOwner"/>.</exception>
    public InnerRef<TReference> FromObject(object owner)
    {
        if (owner is null)
            Utils.ThrowArgumentNullException_Owner();
        if (!typeof(TOwner).IsAssignableFrom(owner.GetType()))
            Utils.ThrowArgumentException_OwnerTypeDoesNotMatch();
        return FromObjectUnsafe(owner);
    }

    /// <summary>
    /// Creates an inner reference for the specified owner, only valid for value types.
    /// </summary>
    /// <param name="owner">Owner where reference point to.</param>
    /// <returns>Inner reference.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TOwner"/> is not a value type.</exception>
    /// <exception cref="ArgumentException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/> is <see langword="null"/>, or is an offset from a <see cref="ArraySegment{T}"/> or <see cref="Memory{T}"/> and index is out of range.</exception>
    /// <exception cref="ArrayTypeMismatchException">Thrown when <typeparamref name="TOwner"/> is an <see cref="ArraySegment{T}"/> and its <see cref="ArraySegment{T}.Array"/>'s <see cref="Type.GetElementType()"/> is not <typeparamref name="TReference"/>.</exception>
    public unsafe ref TReference FromRef(ref TOwner owner)
    {
        if (!typeof(TOwner).IsValueType) Utils.ThrowInvalidOperationException_InvalidTReferenceTypeOnlyValueTypes();
        switch (_mode)
        {
            case Mode.FieldInfo:
            {
                if (!_valueCached)
                {
                    Debug.Assert(_payload is FieldInfo);
                    FromFieldValue(Unsafe.As<FieldInfo>(_payload));
                    _valueCached = true;
                }
                Debug.Assert(_valuePayload is not null);
                return ref _valuePayload(ref owner);
            }
            case Mode.ArraySegment:
            {
                Debug.Assert(typeof(TOwner) == typeof(ArraySegment<TReference>));
                ArraySegment<TReference> arraySegment = Unsafe.As<TOwner, ArraySegment<TReference>>(ref owner);
                if (arraySegment.Array is null)
                    Utils.ThrowArgumentException_OwnerSegmentArrayIsNull();
                if (!typeof(TReference).IsValueType && arraySegment.Array.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Segment();
                int index = _index;
                if (index >= arraySegment.Count)
                    Utils.ThrowArgumentException_OwnerCountMustBeGreaterThanIndex();

#if NET5_0_OR_GREATER
                return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arraySegment.Array), arraySegment.Offset + index);
#else
                return ref arraySegment.Array[arraySegment.Offset + index];
#endif
            }
            case Mode.Memory:
            {
                Debug.Assert(typeof(TOwner) == typeof(Memory<TReference>));
                ref Memory<TReference> memory = ref Unsafe.As<TOwner, Memory<TReference>>(ref owner);
                return ref FromMemory(ref memory);
            }
            case Mode.IMemoryOwner:
            {
                int index = _index;
                Memory<TReference> memory = ((IMemoryOwner<TReference>)owner).Memory;
                return ref FromMemory(ref memory);
            }
#if NET8_0_OR_GREATER
            case Mode.InlineArray:
            {
                Debug.Assert(typeof(TOwner).IsDefined(typeof(InlineArrayAttribute)));
                Debug.Assert(_index < typeof(TOwner).GetCustomAttribute<InlineArrayAttribute>()!.Length);
                return ref Unsafe.Add(ref Unsafe.As<TOwner, TReference>(ref owner), _index);
            }
#endif
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

    internal unsafe InnerRef<TReference> FromObjectUnsafe<T>(T owner)
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
                return InnerRef<TReference>.CreateUnsafe(owner, default, _referencePayload);
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
                return InnerRef<TReference>.CreateUnsafe(owner, index, default);
            }
            case Mode.ArraySegment:
            {
                Debug.Assert(owner is ArraySegment<TReference>);
                ArraySegment<TReference> arraySegment;
                if (typeof(T).IsValueType)
                {
                    Debug.Assert(typeof(T) == typeof(ArraySegment<TReference>));
                    arraySegment = Unsafe.As<T, ArraySegment<TReference>>(ref owner);
                }
                else
                    arraySegment = (ArraySegment<TReference>)(object)owner;
                if (arraySegment.Array is null)
                    Utils.ThrowArgumentException_OwnerSegmentArrayIsNull();
                if (!typeof(TReference).IsValueType && arraySegment.Array.GetType() != typeof(TReference[]))
                    Utils.ThrowArrayTypeMismatchException_Segment();
                int index = _index;
                if (index >= arraySegment.Count)
                    Utils.ThrowArgumentException_OwnerCountMustBeGreaterThanIndex();
                return InnerRef<TReference>.CreateUnsafe(arraySegment.Array, index + arraySegment.Offset, default);
            }
            case Mode.Memory:
            {
                Debug.Assert(owner is Memory<TReference>);
                if (typeof(T).IsValueType)
                {
                    Debug.Assert(typeof(T) == typeof(Memory<TReference>));
                    ref Memory<TReference> memory = ref Unsafe.As<T, Memory<TReference>>(ref owner);
                    int index = _index;
                    if (index >= memory.Length)
                        Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();
                    return InnerRef<TReference>.CreateUnsafe(new MemoryWrapper<TReference>(memory), index | int.MinValue, default);
                }
                else
                {
                    Debug.Assert(owner is Memory<TReference>);
#if NET5_0_OR_GREATER
                    ref Memory<TReference> memory = ref Unsafe.Unbox<Memory<TReference>>(owner);
#else
                    Memory<TReference> memory = (Memory<TReference>)(object)owner;
#endif
                    int index = _index;
                    if (index >= memory.Length)
                        Utils.ThrowArgumentException_IndexMustBeLowerThanMemoryLength();
                    return InnerRef<TReference>.CreateUnsafe(owner, index | int.MinValue, default);
                }
            }
            case Mode.IMemoryOwner:
            {
                int index = _index;
                if (index >= (typeof(T).IsValueType ? ((IMemoryOwner<TReference>)owner).Memory : Unsafe.As<IMemoryOwner<TReference>>(owner).Memory).Length)
                    Utils.ThrowArgumentException_OwnerSpanLengthMustBeGreaterThanIndex();
                return InnerRef<TReference>.CreateUnsafe(owner, index | int.MinValue, default);
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
                return InnerRef<TReference>.CreateUnsafe(owner, _index, _referencePayload);
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
            ilGenerator.Emit(OpCodes.Call, Utils.UnsafeAsMethod.MakeGenericMethod([typeof(TOwner)]));
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

    private InnerRef<TReference> FromSingleArray(object owner, Type ownerType)
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
        return InnerRef<TReference>.CreateUnsafe(owner, index - lowerBound, default);
#else
        return InnerRef<TReference>.CreateUnsafe(owner, index, default);
#endif
    }

    private static InnerRef<TReference> FromMultiArray(object owner, Type ownerType, int[] indexes)
    {
        Debug.Assert(owner.GetType() == ownerType);
        if (!typeof(TReference).IsValueType && ownerType.GetElementType() != typeof(TReference))
            Utils.ThrowArrayTypeMismatchException_Array();
        Debug.Assert(owner is Array);
        Array array = Unsafe.As<Array>(owner);
#if NET6_0_OR_GREATER
        int index = Utils.CalculateIndex(array, indexes);
        return InnerRef<TReference>.CreateUnsafe(owner, index, default);
#else
        Utils.CheckBounds(array, indexes);
        return InnerRef<TReference>.CreateUnsafe(owner, default, indexes);
#endif
    }

    private ref TReference FromMemory(scoped ref Memory<TReference> memory)
    {
        int index = _index;
        if (memory.Length < index)
            Utils.ThrowArgumentException_IndexMustBeLowerThanIMemoryOwnerMemoryLength();
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(memory.Span), index);
    }
}

/// <summary>
/// Helper methods for <see cref="InnerOffset{TOwner, TReference}"/>.
/// </summary>
public abstract class InnerOffset
{
    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static InnerOffset<TReference[], TReference> ForArrayElement<TReference>(int index)
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
    public static InnerOffset<TReference[,], TReference> ForArrayElement<TReference>(int index1, int index2)
        => new(Mode.Array, new int[] { index1, index2 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    public static InnerOffset<TReference[,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3)
        => new(Mode.Array, new int[] { index1, index2, index3 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="Array"/>.
    /// </summary>
    /// <param name="index1">First index of element to take.</param>
    /// <param name="index2">Second index of element to take.</param>
    /// <param name="index3">Third index of element to take.</param>
    /// <param name="index4">Fourth index of element to take.</param>
    public static InnerOffset<TReference[,,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3, int index4)
        => new(Mode.Array, new int[] { index1, index2, index3, index4 }, default);

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="ArraySegment{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static InnerOffset<ArraySegment<TReference>, TReference> ForArraySegmentElement<TReference>(int index)
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
    public static InnerOffset<Memory<TReference>, TReference> ForMemoryElement<TReference>(int index)
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
    public static InnerOffset<TMemoryOwner, TReference> ForIMemoryOwnerElement<TMemoryOwner, TReference>(int index)
        where TMemoryOwner : IMemoryOwner<TReference>
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.IMemoryOwner, default, index);
    }

    /// <summary>
    /// Creates an offset to an specific index of an <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="index">Index of the element to get its reference.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    public static InnerOffset<IMemoryOwner<TReference>, TReference> ForIMemoryOwnerElement<TReference>(int index)
    {
        if (index < 0)
            Utils.ThrowArgumentOutOfRangeException_IndexCanNotBeNegative();
        return new(Mode.IMemoryOwner, default, index);
    }
}