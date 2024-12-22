.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
}

.assembly Enderlook.References.ILHelpers
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = (
        01 00 12 45 6e 64 65 72 6c 6f 6f 6b 2e 49 6e 6e
        65 72 52 65 66 00 00
    )
}

.class private auto ansi abstract sealed beforefieldinit Enderlook.References.ObjectHelpers
    extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 01 00 00
    )

    .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )

    .method public hidebysig static 
        native int CalculateInnerOffset (
            object obj,
            uint8& reference
        ) cil managed aggressiveinlining
    {
        .maxstack 2
        .locals (object pinned)
        
        ldarg.0
        stloc.0
        ldarg.1
        conv.u
        ldloc.0
        conv.u
        sub
        ret
    }

    .method public hidebysig static 
        uint8& GetFromInnerOffset (
            object obj,
            native int offset
        ) cil managed aggressiveinlining
    {
        .maxstack 8
        .locals (object pinned, uint8&)
        
        ldarg.0
        stloc.0
        ldloc.0
        conv.u
        ldarg.1
        add
        stloc.1
        ldloc.1
        ret
    }

    .method public hidebysig static
        !!T& Unbox<valuetype .ctor ([mscorlib]System.ValueType) T> (
            object 'box'
        ) cil managed 
    {
        .param [1]
            .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 01 00 00
            )

        .maxstack 8

        ldarg.0
        unbox.any !!T
        ret
    }
}