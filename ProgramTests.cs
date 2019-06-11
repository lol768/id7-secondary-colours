using System;
using NUnit.Framework;
using Shouldly;
using static ColourEnumeratorCore.Program;

namespace ColourEnumeratorCore
{
    public class ProgramTests
    {
        [Test]
        public void TestSbTwo8861CommentFromSara()
        {
            var brandColour = new RgbColour(0x2E, 0xAF, 0xDF);
            var secondaryColourExpected = new RgbColour(0x6D, 0xC7, 0xE9);
            var computed = GetSecondaryNavFromBrandColour(brandColour);
            
            computed.Item1.R.ShouldBe(secondaryColourExpected.R);
            computed.Item1.G.ShouldBe(secondaryColourExpected.G);
            computed.Item1.B.ShouldBe(secondaryColourExpected.B);

            var matchesCodepenBehaviour =
                computed.Item2.R == 255 &&
                computed.Item2.G == 255 &&
                computed.Item2.B == 255;
            
            matchesCodepenBehaviour.ShouldBeFalse();
            
            computed.Item2.R.ShouldBe(computed.Item2.G);
            computed.Item2.B.ShouldBe(computed.Item2.G);
        }

        [Test]
        public void CheckRelativeLuminance()
        {
            GetRelativeLuminance(0xfa, 0x70, 0x14).ShouldBe(0.3196, 0.01);
        }
        
        [Test]
        public void CheckContrastRatioFailsWcag()
        {
            RgbColour c1 = new RgbColour(0x80, 0x00, 0x80);
            RgbColour c2 = new RgbColour(0x66, 0x33, 0x99);
            GetContrastRatio(c1, c2).ShouldBe(1.12d, 0.01d);
        }
        
        [Test]
        public void CheckContrastRatioPassesWcag()
        {
            RgbColour c1 = new RgbColour(0, 255, 0);
            RgbColour c2 = new RgbColour(0, 0, 0);
            GetContrastRatio(c1, c2).ShouldBe(15.3d, 0.01d);
        }

        [Test]
        public void CheckId7SecondaryNavCalculation()
        {
            RgbColour brandRgbColour = new RgbColour(0x15, 0x62, 0x94);
            var tuple = GetSecondaryNavFromBrandColour(brandRgbColour);
            RgbColour secondaryRgbColour = tuple.Item1;
            secondaryRgbColour.R.ShouldBe((byte)0x5b);
            secondaryRgbColour.G.ShouldBe((byte)0x91);
            secondaryRgbColour.B.ShouldBe((byte)0xb4);

            var textColour = tuple.Item2;
            textColour.R.ShouldBe((byte)0x38);
            textColour.G.ShouldBe((byte)0x38);
            textColour.B.ShouldBe((byte)0x38);

            var contrast = GetContrastRatio(secondaryRgbColour, textColour);
            contrast.ShouldBe(3.4362, 0.01);
            
            PassesAA(contrast, isLargeOrBold: false).ShouldBe(false);
            PassesAA(contrast, isLargeOrBold: true).ShouldBe(true); // size >= 18pt or size >= 15 pt & bold
        }

        [Test]
        public void CheckMultiplyColours()
        {
            // multiply(#ff6600, #00ff00) = #006600
            RgbColour c1 = new RgbColour(0xff, 0x66, 0x00);
            RgbColour c2 = new RgbColour(0x00, 0xff, 0x00);

            var result = MultiplyColours(c1, c2);
            result.R.ShouldBe((byte)0);
            result.G.ShouldBe((byte)0x66);
            result.B.ShouldBe((byte)0);
        }

        [Test]
        public void CheckRgbToHsl()
        {
            var rgb = new RgbColour(170, 0, 255);
            var hslFromRgb = GetHslFromRgb(rgb);
            hslFromRgb.H.ShouldBe((short)280);
            hslFromRgb.S.ShouldBe(1.0, 0.1);
            hslFromRgb.L.ShouldBe(0.5, 0.1);

            var rgb2 = GetRgbFromHls(hslFromRgb);
            rgb2.R.ShouldBe(rgb.R);
            rgb2.G.ShouldBe(rgb.G);
            rgb2.B.ShouldBe(rgb.B);
        }

        [Test]
        public void CheckScreenColours()
        {
            // screen(#ff6600, #999999) = #ffc299
            RgbColour c1 = new RgbColour(0xff, 0x66, 0x00);
            RgbColour c2 = new RgbColour(0x99, 0x99, 0x99);

            var result = ScreenColours(c1, c2);
            result.R.ShouldBe((byte)0xff);
            result.G.ShouldBe((byte)0xc2);
            result.B.ShouldBe((byte)0x99);
        }

        [Test]
        public void TestAllPossibleColours()
        {
            for (byte r = 0; r <= 254; r++)
            {
                for (byte g = 0; r <= 254; r++)
                {
                    for (byte b = 0; r <= 254; r++)
                    {
                        var brandColourToCheck = new RgbColour(r, g, b);
                        var secondary = GetSecondaryNavFromBrandColour(brandColourToCheck);

                        var contrastRatio = GetContrastRatio(secondary.Item1, secondary.Item2);
                        if (!PassesAA(contrastRatio, false))
                        {
                            Console.WriteLine("Fail at small size! " + contrastRatio + " for brand colour " + PrintColour(brandColourToCheck));
                        } else if (!PassesAA(contrastRatio, true))
                        {
                            Console.WriteLine("Fail at large/bold size! " + contrastRatio + " for brand colour " + PrintColour(brandColourToCheck));
                        }
                    }
                }
            }
        }
    }
}