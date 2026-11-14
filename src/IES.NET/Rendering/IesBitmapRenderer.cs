using IES.NET.Models;
using IES.NET.Options;
using IES.NET.Utils;
using System.Windows;
using System.Windows.Media.Imaging;

namespace IES.NET.Rendering;

public static class IesBitmapRenderer
{
    /// <summary>
    /// Render an IES dataset into a BGRA32 BitmapSource. Thread-safe.
    /// </summary>
    public static BitmapSource RenderToBitmapSource(IesData data, RenderOptions options)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (options == null) throw new ArgumentNullException(nameof(options));
        int sizeW = options.Width, sizeH = options.Height;

        var vAngles = data.VerticalAngles;
        var hAngles = data.HorizontalAngles;
        var candela = data.Candela;
        int numV = vAngles.Length, numH = hAngles.Length;

        double maxV = options.MaxAngleDeg > 0 ? options.MaxAngleDeg : vAngles.Max();
        if (maxV <= 0) maxV = 90.0;
        double maxVrad = maxV * Math.PI / 180.0;
        double halfPhysical = options.DistanceMeters * Math.Tan(maxVrad);
        double pixelToPhysicalX = (2.0 * halfPhysical) / sizeW;
        double pixelToPhysicalY = (2.0 * halfPhysical) / sizeH;

        var buffer = new float[sizeH * sizeW];

        double tiltRad = options.ViewTiltDeg * Math.PI / 180.0;
        double cosT = Math.Cos(tiltRad);
        double sinT = Math.Sin(tiltRad);

        // Local function for intensity sampling (bilinear on angles)
        double SampleIntensity(double thetaDeg, double phiDeg)
        {
            if (thetaDeg < vAngles[0]) thetaDeg = vAngles[0];
            if (thetaDeg > vAngles[numV - 1]) thetaDeg = vAngles[numV - 1];

            phiDeg %= 360.0;
            if (phiDeg < 0) phiDeg += 360.0;

            int vi = Array.BinarySearch(vAngles, thetaDeg);
            double vFrac = 0;
            if (vi < 0)
            {
                vi = ~vi;
                if (vi == 0) { vFrac = 0; vi = 0; }
                else if (vi >= numV) { vFrac = 0; vi = numV - 1; }
                else { double lo = vAngles[vi - 1], hi = vAngles[vi]; vFrac = (thetaDeg - lo) / (hi - lo); vi = vi - 1; }
            }

            int h0 = 0; double hFrac = 0;
            if (numH == 1) { h0 = 0; hFrac = 0; }
            else
            {
                int hsearch = Array.BinarySearch(hAngles, phiDeg);
                if (hsearch >= 0) { h0 = hsearch; hFrac = 0; }
                else
                {
                    int hpos = ~hsearch;
                    int hlo = (hpos - 1 + numH) % numH;
                    int hhi = hpos % numH;
                    double angLo = hAngles[hlo];
                    double angHi = hAngles[hhi];
                    if (angHi <= angLo) angHi += 360.0;
                    double phiAdj = phiDeg;
                    if (phiAdj < angLo) phiAdj += 360.0;
                    hFrac = (phiAdj - angLo) / (angHi - angLo);
                    h0 = hlo;
                }
            }

            int h1 = (h0 + 1) % numH;
            int v0 = vi;
            int v1 = Math.Min(vi + 1, numV - 1);

            double c00 = candela[h0, v0], c01 = candela[h0, v1];
            double c10 = candela[h1, v0], c11 = candela[h1, v1];

            double c0 = c00 + (c01 - c00) * vFrac;
            double c1 = c10 + (c11 - c10) * vFrac;
            return c0 + (c1 - c0) * hFrac;
        }

        Parallel.For(0, sizeH, y =>
        {
            int rowOffset = y * sizeW;
            double py = (y + 0.5 - sizeH / 2.0) * pixelToPhysicalY;
            for (int x = 0; x < sizeW; x++)
            {
                double px = (x + 0.5 - sizeW / 2.0) * pixelToPhysicalX;
                double newX = px;
                double newY = cosT * py - sinT * options.DistanceMeters;
                double newZ = sinT * py + cosT * options.DistanceMeters;
                double radialPerp = Math.Sqrt(newX * newX + newY * newY);
                double thetaRad = Math.Atan2(radialPerp, newZ);
                double thetaDeg = thetaRad * 180.0 / Math.PI;
                if (newZ <= 0 || thetaDeg > maxV) { buffer[rowOffset + x] = 0; continue; }
                double phiRad = Math.Atan2(newY, newX);
                double phiDeg = phiRad * 180.0 / Math.PI + options.HorizontalRotationDeg;
                double I = SampleIntensity(thetaDeg, phiDeg);
                double rDist = Math.Sqrt(newZ * newZ + radialPerp * radialPerp);
                double cosTheta = Math.Cos(thetaRad);
                double E = rDist > 0 ? (I * cosTheta) / (rDist * rDist) : 0.0;
                buffer[rowOffset + x] = (float)E;
            }
        });

        // normalize
        float maxVal = 0;
        for (int i = 0; i < buffer.Length; i++) if (buffer[i] > maxVal) maxVal = buffer[i];
        if (maxVal <= 0) maxVal = 1e-9f;
        var bytes = new byte[sizeW * sizeH * 4];
        float invMax = 1.0f / maxVal;
        double gammaInv = 1.0 / options.Gamma;

        Parallel.For(0, sizeH, y =>
        {
            int rowOffset = y * sizeW;
            int byteRowOffset = rowOffset * 4;
            for (int x = 0; x < sizeW; x++)
            {
                float v = buffer[rowOffset + x] * invMax * (float)options.Exposure;
                if (v < 0) v = 0;
                double g = Math.Pow(v, gammaInv);
                if (g > 1.0) g = 1.0;
                byte b = (byte)(Math.Max(0, Math.Min(255, (int)(g * 255.0 + 0.5))));
                int bi = byteRowOffset + x * 4;
                bytes[bi + 0] = b;
                bytes[bi + 1] = b;
                bytes[bi + 2] = b;
                bytes[bi + 3] = 255;
            }
        });

        // local blur
        if (options.LocalBlurRadius >= 1.0)
            ImageUtils.ApplyBoxBlurInPlace(bytes, sizeW, sizeH, (int)Math.Max(1, Math.Round(options.LocalBlurRadius)));

        // BLOOM PASS - after local blur & before creating BitmapSource
        if (options.BloomIntensity > 0.0 && options.BloomRadius > 0)
        {
            var bright = ImageUtils.ExtractBrightMask(bytes, sizeW, sizeH, options.BloomThreshold, options.BloomVerticalShift);
            ImageUtils.ApplyBoxBlurInPlace(bright, sizeW, sizeH, Math.Max(1, options.BloomRadius));
            ImageUtils.AddAdditive(bytes, bright, options.BloomIntensity);
        }

        var wb = new WriteableBitmap(sizeW, sizeH, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        var stride = sizeW * 4;
        wb.WritePixels(new Int32Rect(0, 0, sizeW, sizeH), bytes, stride, 0);
        wb.Freeze();
        return wb as BitmapSource;
    }
}
