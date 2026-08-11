namespace VectorViewer.Domain;

/// <summary>
/// A colour expressed as Alpha, Red, Green, Blue — each channel 0..255, alpha first,
/// matching the ordering used by the input files.
/// </summary>
public readonly record struct ArgbColor(byte A, byte R, byte G, byte B)
{
    public bool IsFullyTransparent => A == 0;

    /// <summary>Packs the colour as <c>0xAARRGGBB</c>. Used as a cache key by rendering back ends.</summary>
    public uint ToUInt32() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;

    public static ArgbColor FromUInt32(uint value) => new(
        (byte)(value >> 24),
        (byte)(value >> 16),
        (byte)(value >> 8),
        (byte)value);

    public override string ToString() => $"#{ToUInt32():X8}";
}
