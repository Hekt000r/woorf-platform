namespace Woorf.Engine.Projects;

internal static class RobloxLz4
{
    internal static byte[] Decode(ReadOnlySpan<byte> input, int size)
    {
        if (input.StartsWith(new byte[] { 0x28, 0xb5, 0x2f, 0xfd }))
            throw new NotSupportedException("This binary Roblox place uses Zstandard. Export it as .rbxlx in Roblox Studio.");
        var output = new byte[size];
        var source = 0;
        var target = 0;
        while (source < input.Length)
        {
            var token = input[source++];
            var literalLength = ReadLength(input, token >> 4, ref source);
            if (literalLength > input.Length - source || literalLength > output.Length - target) throw InvalidBlock();
            input.Slice(source, literalLength).CopyTo(output.AsSpan(target));
            source += literalLength;
            target += literalLength;
            if (source == input.Length) break;
            if (input.Length - source < 2) throw InvalidBlock();
            var distance = input[source] | input[source + 1] << 8;
            source += 2;
            var matchLength = ReadLength(input, token & 15, ref source) + 4;
            if (distance == 0 || distance > target || matchLength > output.Length - target) throw InvalidBlock();
            for (var index = 0; index < matchLength; index++, target++)
                output[target] = output[target - distance];
        }
        return target == size ? output : throw InvalidBlock();
    }

    private static int ReadLength(ReadOnlySpan<byte> input, int length, ref int source)
    {
        if (length != 15) return length;
        byte extension;
        do
        {
            if (source >= input.Length || length > int.MaxValue - 260) throw InvalidBlock();
            extension = input[source++];
            length += extension;
        } while (extension == 255);
        return length;
    }

    private static InvalidDataException InvalidBlock() => new("Invalid LZ4 block in Roblox place.");
}
