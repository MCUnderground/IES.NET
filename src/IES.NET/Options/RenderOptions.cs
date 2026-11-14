namespace IES.NET.Options;

/// <summary>
/// Options controlling the rendering pipeline.
/// </summary>
public sealed class RenderOptions
{
    public int Width { get; init; } = 512;
    public int Height { get; init; } = 512;
    public double DistanceMeters { get; init; } = 0.9;
    public double HorizontalRotationDeg { get; init; } = 0.0;
    public double MaxAngleDeg { get; init; } = 75.0;
    public double Exposure { get; init; } = 1.0;
    public double Gamma { get; init; } = 2.2;
    public double LocalBlurRadius { get; init; } = 5.0;
    public double ViewTiltDeg { get; init; } = 90;

    // Bloom options
    public double BloomThreshold { get; init; } = 0.08;
    public int BloomRadius { get; init; } = 0;
    public double BloomIntensity { get; init; } = 1.0;
    public int BloomVerticalShift { get; init; } = -48;
}
