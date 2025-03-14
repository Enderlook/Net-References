using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Enderlook.References;

#if NET8_0_OR_GREATER
internal static class UnboxerHelper<TArray>
{
    public static readonly IUnboxer Impl = (IUnboxer)Activator.CreateInstance(typeof(Unboxer<>).MakeGenericType(typeof(TArray)))!;

    internal interface IUnboxer
    {
        public ref TArray Unbox(object owner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TArray Unbox(object obj)
    {
        Debug.Assert(typeof(TArray).IsDefined(typeof(InlineArrayAttribute)));
        return ref Impl.Unbox(obj);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReferenceProvider<TReference> GetElementAccessor<TReference>()
    {
        Debug.Assert(typeof(TArray).IsDefined(typeof(InlineArrayAttribute)));
        return Container<TReference>.ReferenceProvider;
    }

    private static class Container<TReference>
    {
        public static readonly ReferenceProvider<TReference> ReferenceProvider = (object? managedState, nint unmanagedState) =>
        {
            Debug.Assert(managedState is not null);
            Debug.Assert(managedState is TArray);
            Debug.Assert(typeof(TArray).IsDefined(typeof(InlineArrayAttribute)));
            Debug.Assert(unmanagedState < typeof(TArray).GetCustomAttribute<InlineArrayAttribute>()!.Length);
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
            // Field can't be removed by IL trimmer.
            Debug.Assert(typeof(TArray).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0].FieldType == typeof(TReference));
#pragma warning restore IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
            return ref Unsafe.Add(ref Unsafe.As<TArray, TReference>(ref UnboxerHelper<TArray>.Unbox(managedState)), (int)unmanagedState);
        };
    }
}

internal sealed class Unboxer<T> : UnboxerHelper<T>.IUnboxer
    where T : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Unbox(object owner) => ref Unsafe.Unbox<T>(owner);
}
#endif