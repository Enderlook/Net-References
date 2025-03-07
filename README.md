A library to safety store (fat) inner references (or just references, or pointers) to objects in the heap.

# API

```cs
namespace Enderlook.References;


/// Represent an reference to an allocation.
public readonly struct Ref<T>
{
    /// Get reference to the value.
    public readonly unsafe ref T Value { get; }
		
    /// Creates an reference from a pointer.
    public unsafe Ref(T* pointer);    

    /// Creates a reference to an element of the collection.
	public Ref(IMemoryOwner<T> memoryManager, int index);
    public Ref(T[] array, int index);
	public Ref(T[,] array, int index1, int index2);
	public Ref(T[,,] array, int index1, int index2, int index3);
	public Ref(T[,,,] array, int index1, int index2, int index3, int index4);
	public unsafe Ref(Array array, params ReadOnlySpan<int> indexes);
    public Ref(Memory<T> memory, int index);
	public Ref(ArraySegment<T> segment, int index);
	
	// Creates a wrapper around a method which returns a reference.
	public Ref(object managedState, nint unmanagedState, ReferenceProvider<T> referenceProvider);
		
    /// Reads the inner reference.
    public static implicit operator T(Ref<T> self);

    /// Convert a pointer into an reference.
    public unsafe static implicit operator Ref<T>(T* pointer);
}

/// Represent the offset of a managed reference.
public sealed class Offset<TOwner, TReference>
{
    /// Creates an offset for an element array or field.
	/// The expression can be an array (including multidimensional or non zero-based) index access, array segment index access, field access of any reference type.
    public Offset(Expression<Func<TOwner, TReference>> expression);
	
	/// Create an offset to an specific field.
    public Offset(FieldInfo fieldInfo);
		
    /// Creates an offset to an specific index of an array (including multidimensional or non zero-based), array segment, memory or memory manager.
    public Offset(int index);
    public Offset(params ReadOnlySpan<int> indexes);
	
    /// Creates an inner reference for the specified owner.
    public Ref<TReference> From(TOwner owner);
	
	/// Creates an inner reference for the specified owner, supports boxed value types.
    public Ref<TReference> FromObject(object owner);
	
    /// Creates an inner reference for the specified owner, only valid for value types.
	public unsafe ref TReference FromRef(ref TOwner owner);
}

/// Helper methods for Offset<TOwner, TReference>.
public static class Offset
{
    /// Creates an offset to an specific index of a collectionn.
    public static Offset<TReference[], TReference> ForArrayElement<TReference>(int index)
    public static Offset<TReference[,], TReference> ForArrayElement<TReference>(int index1, int index2);
    public static Offset<TReference[,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3);
    public static Offset<TReference[,,,], TReference> ForArrayElement<TReference>(int index1, int index2, int index3, int index4);
    public static Offset<ArraySegment<TReference>, TReference> ForArraySegmentElement<TReference>(int index);
    public static Offset<Memory<TReference>, TReference> ForMemoryElement<TReference>(int index);
    public static Offset<TMemoryOwner, TReference> ForIMemoryOwnerElement<TMemoryOwner, TReference>(int index)
        where TMemoryOwner : IMemoryOwner<TReference>;
    public static Offset<IMemoryOwner<TReference>, TReference> ForIMemoryOwnerElement<TReference>(int index);

}

/// Encapsulates a method that has two parameters and returns a reference.
public delegate ref TResult ReferenceProvider<TResult>(object? managedState, nint unmanagedState);
```