using System.Text;

namespace RemoteShouter.Services;

/// <summary>
/// Creates the small ICO used by the tray icon and desktop window.
/// The mark is an antialiased open ring, matching the OpenRemoteShouter logo.
/// </summary>
internal static class AppIconFactory
{
    public static Stream CreateStream()
    {
        const int size = 32;
        const int xorBytes = size * size * 4;
        const int andBytes = size * 4;
        const int imageBytes = 40 + xorBytes + andBytes;
        const int imageOffset = 22;

        var stream = new MemoryStream(imageOffset + imageBytes);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // ICO header and one 32-bit DIB entry.
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write((byte)size);
        writer.Write((byte)size);
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(imageBytes);
        writer.Write(imageOffset);

        writer.Write(40); // BITMAPINFOHEADER
        writer.Write(size);
        writer.Write(size * 2); // XOR bitmap plus AND mask
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0); // BI_RGB
        writer.Write(xorBytes);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        const double center = (size - 1) / 2.0;
        const double outerRadius = 14.5;
        const double innerRadius = 7.6; // slightly heavier ring than the reference

        // DIB rows are stored bottom-up. Supersampling keeps the ring smooth
        // at 16/32px taskbar sizes without requiring an external image asset.
        for (var y = size - 1; y >= 0; y--)
        {
            for (var x = 0; x < size; x++)
            {
                var coverage = 0;
                const int samples = 4;
                for (var sampleY = 0; sampleY < samples; sampleY++)
                {
                    for (var sampleX = 0; sampleX < samples; sampleX++)
                    {
                        var dx = x + (sampleX + 0.5) / samples - center;
                        var dy = y + (sampleY + 0.5) / samples - center;
                        var radius = Math.Sqrt(dx * dx + dy * dy);
                        var angle = Math.Atan2(dy, dx);
                        var inRing = radius >= innerRadius && radius <= outerRadius;
                        var absoluteAngle = Math.Abs(angle);
                        // Keep a small right-hand arc and the large left/main
                        // arc, leaving two angled breaks between them.
                        var inRightArc = absoluteAngle <= 0.46;
                        var inMainArc = absoluteAngle >= 1.03;
                        if (inRing && (inRightArc || inMainArc))
                        {
                            coverage++;
                        }
                    }
                }

                var alpha = (byte)(coverage * 255 / (samples * samples));
                writer.Write((byte)0x1F); // B
                writer.Write((byte)0x1F); // G
                writer.Write((byte)0x1F); // R
                writer.Write(alpha);
            }
        }

        // Fully transparent AND mask because the XOR bitmap carries alpha.
        writer.Write(new byte[andBytes]);
        stream.Position = 0;
        return stream;
    }
}
