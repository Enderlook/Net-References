.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
}

.assembly Enderlook.References.ILHelpers
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = (
        01 00 14 45 6e 64 65 72 6c 6f 6f 6b 2e 52 65 66
        65 72 65 6e 63 65 73 00 00
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
        !!T& GetReference1<T>(
            object obj,
            int32 index
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .param type T
            .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 02 00 00
            )

        .maxstack 8

        ldarg.0
        call !!0 [mscorlib]System.Runtime.CompilerServices.Unsafe::As<!!T[0...]>(object)
        ldarg.1
        call instance !!T& !!T[0...]::Address(int32)
        ret
    }
}