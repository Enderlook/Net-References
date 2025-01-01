using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System.Text;

namespace Enderlook.References.SourceGenerators;

[Generator]
public sealed class UtilsHelper : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        StringBuilder builder = new();

        builder
            .AppendLine(@"
            using System;
            using System.Buffers;
            using System.Diagnostics;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;

            namespace Enderlook.References;

            internal static partial class Utils
            {
#if !NET6_0_OR_GREATER
            ");

        for (int i = 2; i <= 32; i++)
        {
            builder.Append($@"
    public unsafe static ref T GetReference{i}<T>(object array, int[] indexes)
    {{
        Debug.Assert(array is T[").Append(',', i - 1).Append($@"]);
        Debug.Assert(indexes.Length == {i});
        T[").Append(',', i - 1).Append($@"] array_ = Unsafe.As<T[").Append(',', i - 1).Append($@"]>(array);
#if NET5_0_OR_GREATER
        ref int indexes_ = ref MemoryMarshal.GetArrayDataReference(indexes);
#else
        ref int indexes_ = ref MemoryMarshal.GetReference((Span<int>)indexes);
#endif
        return ref array_[
            indexes_,");
        for (int j = 1; j < i; j++)
            builder.Append("Unsafe.Add(ref indexes_, ").Append(j).Append("),\n");
        builder.Length -= 2;
        builder.Append($@"
        ];
    }}").Append("\n");
        }

        builder.Append("#endif\n}");

        context.AddSource("UtilsHelper.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }
}