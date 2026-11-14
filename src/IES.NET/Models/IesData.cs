namespace IES.NET.Models;

/// <summary>
/// Immutable representation of parsed IES photometric data.
/// Candela[hIndex, vIndex].
/// </summary>
public sealed class IesData
{
    public double CandelaMultiplier { get; }
    public double[] VerticalAngles { get; }
    public double[] HorizontalAngles { get; }
    public double[,] Candela { get; }

    public IesData(double candelaMultiplier, double[] verticalAngles, double[] horizontalAngles, double[,] candela)
    {
        CandelaMultiplier = candelaMultiplier;
        VerticalAngles = verticalAngles ?? throw new ArgumentNullException(nameof(verticalAngles));
        HorizontalAngles = horizontalAngles ?? throw new ArgumentNullException(nameof(horizontalAngles));
        Candela = candela ?? throw new ArgumentNullException(nameof(candela));
    }
}
