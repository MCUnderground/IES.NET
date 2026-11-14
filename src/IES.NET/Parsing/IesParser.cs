using IES.NET.Interfaces;
using IES.NET.Models;
using System.Globalization;
using System.IO;

namespace IES.NET.Parsing;

/// <summary>
/// Robust IES LM-63 parser. Safe, purely managed code.
/// Supports basic TILT handling and numeric LM-63 variants commonly encountered.
/// </summary>
public sealed class IesParser : IIesParser
{
    public IesData Parse(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("IES file not found", path);

        string text = File.ReadAllText(path);
        // replace commas with spaces (they are sometimes used)
        text = text.Replace(',', ' ');
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim()).ToList();

        // find TILT line
        int idx = 0;
        while (idx < lines.Count && !lines[idx].StartsWith("TILT", StringComparison.OrdinalIgnoreCase))
            idx++;

        if (idx >= lines.Count)
            throw new IesParsingException("TILT line not found.");

        var tiltLine = lines[idx];
        if (tiltLine.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
            idx++;
        else
        {
            idx++;
            while (idx < lines.Count && !LineContainsNumeric(lines[idx]))
                idx++;
        }

        var tokens = new List<string>();
        for (int i = idx; i < lines.Count; i++)
        {
            var parts = lines[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                // try parse as float-like token
                if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    tokens.Add(p);
            }
        }

        if (tokens.Count < 10) throw new IesParsingException("Insufficient numeric data.");

        int pos = 0;
        try
        {
            int numLamps = (int)ParseDouble(tokens[pos++]);
            double lumensPerLamp = ParseDouble(tokens[pos++]);
            double candelaMultiplier = ParseDouble(tokens[pos++]);
            int numVertical = (int)ParseDouble(tokens[pos++]);
            int numHorizontal = (int)ParseDouble(tokens[pos++]);
            int photometricType = (int)ParseDouble(tokens[pos++]);
            int unitsType = (int)ParseDouble(tokens[pos++]);

            // try read width/length/height if present
            double width = 0, length = 0, height = 0;
            if (pos + 2 < tokens.Count)
            {
                width = ParseDouble(tokens[pos++]);
                length = ParseDouble(tokens[pos++]);
                height = ParseDouble(tokens[pos++]);
            }

            if (numVertical <= 0 || numHorizontal <= 0)
                throw new IesParsingException("Invalid angle counts.");

            if (pos + numVertical + numHorizontal + numHorizontal * numVertical > tokens.Count)
                throw new IesParsingException("File missing angle arrays or candela values.");

            var vAngles = new double[numVertical];
            for (int i = 0; i < numVertical; i++) vAngles[i] = ParseDouble(tokens[pos++]);

            var hAngles = new double[numHorizontal];
            for (int i = 0; i < numHorizontal; i++) hAngles[i] = ParseDouble(tokens[pos++]);

            var candela = new double[numHorizontal, numVertical];
            for (int h = 0; h < numHorizontal; h++)
                for (int v = 0; v < numVertical; v++)
                    candela[h, v] = ParseDouble(tokens[pos++]) * candelaMultiplier;

            return new IesData(candelaMultiplier, vAngles, hAngles, candela);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new IesParsingException("Unexpected end of numeric data.", ex);
        }
        catch (FormatException ex)
        {
            throw new IesParsingException("Numeric parse error.", ex);
        }
    }

    private static bool LineContainsNumeric(string s)
    {
        var parts = s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
            if (double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return true;
        return false;
    }

    private static double ParseDouble(string s) => double.Parse(s, CultureInfo.InvariantCulture);
}