namespace IES.NET.Utils;

internal static class ImageUtils
{
    // Separable box blur in-place (BGRA32)
    public static void ApplyBoxBlurInPlace(byte[] bytes, int w, int h, int radius)
    {
        if (radius <= 0) return;
        int stride = w * 4;
        var tmp = new byte[bytes.Length];
        int kernel = radius * 2 + 1;

        // horizontal
        for (int y = 0; y < h; y++)
        {
            int row = y * stride;
            int[] acc = new int[4];
            for (int x = -radius; x <= radius; x++)
            {
                int xx = Math.Min(w - 1, Math.Max(0, x));
                int idx = row + xx * 4;
                acc[0] += bytes[idx + 0];
                acc[1] += bytes[idx + 1];
                acc[2] += bytes[idx + 2];
                acc[3] += bytes[idx + 3];
            }

            for (int x = 0; x < w; x++)
            {
                int outIdx = row + x * 4;
                tmp[outIdx + 0] = (byte)(acc[0] / kernel);
                tmp[outIdx + 1] = (byte)(acc[1] / kernel);
                tmp[outIdx + 2] = (byte)(acc[2] / kernel);
                tmp[outIdx + 3] = (byte)(acc[3] / kernel);

                int removeX = Math.Max(0, x - radius);
                int addX = Math.Min(w - 1, x + radius + 1);
                int removeIdx = row + removeX * 4;
                int addIdx = row + addX * 4;
                acc[0] += bytes[addIdx + 0] - bytes[removeIdx + 0];
                acc[1] += bytes[addIdx + 1] - bytes[removeIdx + 1];
                acc[2] += bytes[addIdx + 2] - bytes[removeIdx + 2];
                acc[3] += bytes[addIdx + 3] - bytes[removeIdx + 3];
            }
        }

        // vertical
        for (int x = 0; x < w; x++)
        {
            int[] acc = new int[4];
            for (int y = -radius; y <= radius; y++)
            {
                int yy = Math.Min(h - 1, Math.Max(0, y));
                int idx = yy * stride + x * 4;
                acc[0] += tmp[idx + 0];
                acc[1] += tmp[idx + 1];
                acc[2] += tmp[idx + 2];
                acc[3] += tmp[idx + 3];
            }

            for (int y = 0; y < h; y++)
            {
                int outIdx = y * stride + x * 4;
                bytes[outIdx + 0] = (byte)(acc[0] / kernel);
                bytes[outIdx + 1] = (byte)(acc[1] / kernel);
                bytes[outIdx + 2] = (byte)(acc[2] / kernel);
                bytes[outIdx + 3] = (byte)(acc[3] / kernel);

                int removeY = Math.Max(0, y - radius);
                int addY = Math.Min(h - 1, y + radius + 1);
                int removeIdx = removeY * stride + x * 4;
                int addIdx = addY * stride + x * 4;
                acc[0] += tmp[addIdx + 0] - tmp[removeIdx + 0];
                acc[1] += tmp[addIdx + 1] - tmp[removeIdx + 1];
                acc[2] += tmp[addIdx + 2] - tmp[removeIdx + 2];
                acc[3] += tmp[addIdx + 3] - tmp[removeIdx + 3];
            }
        }
    }

    // Return a bright-only BGRA buffer with shift applied (no blur done here).
    public static byte[] ExtractBrightMask(byte[] srcBgra, int w, int h, double threshold, int verticalShift)
    {
        var outBuf = new byte[srcBgra.Length];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int bi = (y * w + x) * 4;
                double v = srcBgra[bi] / 255.0;
                double keep = v > threshold ? (v - threshold) / (1.0 - threshold) : 0.0;
                if (keep <= 0) continue;
                int sy = y + verticalShift;
                if (sy < 0 || sy >= h) continue;
                int outIdx = (sy * w + x) * 4;
                byte bval = (byte)Math.Max(0, Math.Min(255, (int)(keep * 255.0 + 0.5)));
                outBuf[outIdx + 0] = bval;
                outBuf[outIdx + 1] = bval;
                outBuf[outIdx + 2] = bval;
                outBuf[outIdx + 3] = 255;
            }
        }
        return outBuf;
    }

    // Add bright buffer into target additively using intensity scalar
    public static void AddAdditive(byte[] target, byte[] bright, double intensity)
    {
        if (intensity <= 0) return;
        int len = Math.Min(target.Length, bright.Length);
        for (int i = 0; i < len; i += 4)
        {
            int add = (int)(bright[i] * intensity + 0.5);
            int r = target[i + 2] + add;
            int g = target[i + 1] + add;
            int b = target[i + 0] + add;
            target[i + 2] = (byte)(r > 255 ? 255 : r);
            target[i + 1] = (byte)(g > 255 ? 255 : g);
            target[i + 0] = (byte)(b > 255 ? 255 : b);
        }
    }
}
