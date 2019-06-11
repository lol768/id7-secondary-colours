using System;

namespace ColourEnumeratorCore
{
    class Program
    {
        private static readonly HslColour Black = new HslColour(0, 0, 0);
        private static readonly HslColour White = new HslColour(0, 0, 1);

        public struct RgbColour
        {
            private byte r;
            private byte g;
            private byte b;

            public RgbColour(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }

            public byte R => r;

            public byte G => g;

            public byte B => b;
        }

        public struct HslColour
        {
            private short h; // 0 through 360
            private double s;
            private double l;

            public HslColour(short h, double s, double l)
            {
                this.h = h;
                this.s = s;
                this.l = l;
            }

            public short H => h;

            public double S => s;

            public double L => l;
        }

        public static void Main(string[] args)
        {
            for (byte r = 0; r <= 254; r++)
            {
                for (byte g = 0; g <= 254; g++)
                {
                    for (byte b = 0; b <= 254; b++)
                    {
                        var brandColourToCheck = new RgbColour(r, g, b);
                        var secondary = GetSecondaryNavFromBrandColour(brandColourToCheck);

                        var contrastRatio = GetContrastRatio(secondary.Item1, secondary.Item2);
                        if (!PassesAA(contrastRatio, true))
                        {
                            Console.WriteLine(
                                $"Fail at large/bold size! Ratio {contrastRatio:F} for brand colour {PrintColour(brandColourToCheck)}");
                        }
                        else if (!PassesAA(contrastRatio, false))
                        {
                            Console.WriteLine(
                                $"Fail at small size! Ratio {contrastRatio:F} for brand colour {PrintColour(brandColourToCheck)}");
                        }
                    }
                }
            }
        }

        public static string PrintColour(RgbColour brandColourToCheck)
        {
            return $"#{brandColourToCheck.R:X2}{brandColourToCheck.G:X2}{brandColourToCheck.B:X2}";
        }

        /// <summary>
        /// Returns secondary BG colour (Item1) and contrast/text colour (Item2)
        /// </summary>
        /// <param name="c">Brand colour</param>
        /// <returns>Tuple of 2 colours</returns>
        public static Tuple<RgbColour, RgbColour> GetSecondaryNavFromBrandColour(RgbColour c)
        {
            var secondaryColour = ScreenColours(c, GetRgbFromHls(AdjustLightness(Black, 0.3)));
            var textColour = new RgbColour(0x38, 0x38, 0x38);
            var secondaryContrastColour = ContrastColours2(secondaryColour, textColour, GetRgbFromHls(White));
            return new Tuple<RgbColour, RgbColour>(secondaryColour, secondaryContrastColour);
        }

        public static bool PassesAA(double contrastRatio, bool isLargeOrBold)
        {
            return (!isLargeOrBold) ? contrastRatio >= 4.5 : contrastRatio >= 3;
        }

        [Obsolete("This is broken (doesn't do sRGB gamma correction), use the other Method.")]
        private static RgbColour ContrastColours(RgbColour colour, RgbColour dark, RgbColour light, double bias)
        {
            double relativeLumaColour = GetRelativeLuminance(colour);
            double relativeLumaDark = GetRelativeLuminance(dark);
            double relativeLumaLight = GetRelativeLuminance(light);

            if (relativeLumaDark > relativeLumaLight)
            {
                var temp = dark;
                dark = light;
                light = temp;
            }

            if (relativeLumaColour < bias)
            {
                return light;
            }

            return dark;
        }
        
        private static RgbColour ContrastColours2(RgbColour colour, RgbColour dark, RgbColour light)
        {
            var ratioForLightOnColour = GetContrastRatio(colour, light);
            var ratioForDarkOnColour = GetContrastRatio(colour, dark);
            return ratioForDarkOnColour >= ratioForLightOnColour ? dark : light;
        }

        public static double GetRelativeLuminance(RgbColour c)
        {
            return GetRelativeLuminance(c.R, c.G, c.B);
        }

        public static double GetRelativeLuminance(byte r, byte g, byte b)
        {
            var rs = r / 255.0d;
            var gs = g / 255.0d;
            var bs = b / 255.0d;

            var R = 0d;
            var G = 0d;
            var B = 0d;

            double srgbCalc(double d)
            {
                if (d <= 0.03928)
                {
                    return d / 12.92d;
                }

                return Math.Pow((d + 0.055) / 1.055, 2.4d);
            }

            R = srgbCalc(rs);
            G = srgbCalc(gs);
            B = srgbCalc(bs);
            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }

        public static double GetContrastRatio(RgbColour c1, RgbColour c2)
        {
            var luma2 = GetRelativeLuminance(c2);
            var luma1 = GetRelativeLuminance(c1);
            if (luma1 < luma2)
            {
                var temp = luma1;
                luma1 = luma2;
                luma2 = temp;
            }

            return (luma1 + 0.05) / (luma2 + 0.05);
        }

        public static RgbColour MultiplyColours(RgbColour c1, RgbColour c2)
        {
            return new RgbColour((byte) (c1.R * c2.R / 255), (byte) (c1.G * c2.G / 255), (byte) (c1.B * c2.B / 255));
        }

        public static RgbColour ScreenColours(RgbColour c1, RgbColour c2)
        {
            var c1I = Invert(c1);
            var c2I = Invert(c2);
            var product = MultiplyColours(c1I, c2I);
            return Invert(product);
        }

        public static HslColour AdjustLightness(HslColour c, double deltaL)
        {
            return new HslColour(c.H, c.S, Math.Min(Math.Max(0.0d, c.L + deltaL), 1.0d));
        }

        // http://james-ramsden.com/convert-from-hsl-to-rgb-colour-codes-in-c/
        public static HslColour GetHslFromRgb(RgbColour c)
        {
            // Convert RGB to a 0.0 to 1.0 range.
            double doubleR = c.R / 255.0;
            double doubleG = c.G / 255.0;
            double doubleB = c.B / 255.0;

            // Get the maximum and minimum RGB components.
            double max = doubleR;
            if (max < doubleG) max = doubleG;
            if (max < doubleB) max = doubleB;

            double min = doubleR;
            if (min > doubleG) min = doubleG;
            if (min > doubleB) min = doubleB;

            double diff = max - min;
            double l = (max + min) / 2;
            double doubleH = 0;
            double s;
            if (Math.Abs(diff) < 0.00001)
            {
                s = 0;
                doubleH = 0; // H is really undefined.
            }
            else
            {
                if (l <= 0.5) s = diff / (max + min);
                else s = diff / (2 - max - min);

                double r_dist = (max - doubleR) / diff;
                double g_dist = (max - doubleG) / diff;
                double b_dist = (max - doubleB) / diff;
                if (doubleR == max) doubleH = (b_dist - g_dist);
                else if (doubleG == max) doubleH = (2 + r_dist - b_dist);
                else doubleH = (4 + g_dist - r_dist);

                doubleH = (doubleH * 60);
                if (doubleH < 0) doubleH += 360;
            }

            return new HslColour((short) doubleH, s, l);
        }

        public static RgbColour GetRgbFromHls(HslColour colour)
        {
            double l = colour.L;
            double s = colour.S;
            double h = colour.H;

            double p2;
            if (l <= 0.5) p2 = l * (1 + s);
            else p2 = l + s - l * s;

            double p1 = 2 * l - p2;
            double doubleR, doubleG, doubleB;
            if (s == 0)
            {
                doubleR = l;
                doubleG = l;
                doubleB = l;
            }
            else
            {
                doubleR = QqhToRgb(p1, p2, h + 120);
                doubleG = QqhToRgb(p1, p2, h);
                doubleB = QqhToRgb(p1, p2, h - 120);
            }

            // Convert RGB to the 0 to 255 range.
            var r = (byte) (doubleR * 255.0);
            var g = (byte) (doubleG * 255.0);
            var b = (byte) (doubleB * 255.0);
            return new RgbColour(r, g, b);
        }

        private static double QqhToRgb(double q1, double q2, double hue)
        {
            if (hue > 360) hue -= 360;
            else if (hue < 0) hue += 360;

            if (hue < 60) return q1 + (q2 - q1) * hue / 60;
            if (hue < 180) return q2;
            if (hue < 240) return q1 + (q2 - q1) * (240 - hue) / 60;
            return q1;
        }

        private static RgbColour Invert(RgbColour c1)
        {
            return new RgbColour((byte) (255 - c1.R), (byte) (255 - c1.G), (byte) (255 - c1.B));
        }
    }
}