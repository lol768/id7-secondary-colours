ID7 Secondary Nav Colour Enumeration
-------------------------------------

CodePen: https://codepen.io/anon/pen/LoKYwB

This project implements the secondary nav bar text colour/background colour generation logic ported from LESS:

```less
@secondary-colour: screen(@colour, lighten(black, 30%));
@text-color: #383838;

@secondary-accent-colour: multiply(@secondary-colour, darken(white, 20%));
@secondary-contrast-colour: contrast(@secondary-colour, @text-color, white, 50%);
```

It's able to determine, given a brand colour, if the derived secondary nav colours pass the contrast check or not for AA
at large and small sizes:

```csharp
// specify some ID7 brand colour, here we use the ITS #156294 blue
RgbColour brandRgbColour = new RgbColour(0x15, 0x62, 0x94);

// Get the tuple of <background, foreground> colours after applying the
// LESS logic:
var tuple = GetSecondaryNavFromBrandColour(brandRgbColour);

RgbColour secondaryRgbColour = tuple.Item1;
// Assert that the background is #5b91b4 (a sort of teal/light blue)
secondaryRgbColour.R.ShouldBe((byte)0x5b);
secondaryRgbColour.G.ShouldBe((byte)0x91);
secondaryRgbColour.B.ShouldBe((byte)0xb4);

// Assert that the most appropriate text colour for this background is white
var textColour = tuple.Item2;
textColour.R.ShouldBe((byte)255);
textColour.G.ShouldBe((byte)255);
textColour.B.ShouldBe((byte)255);

// Check the contrast ratio
var contrast = GetContrastRatio(secondaryRgbColour, textColour);
contrast.ShouldBe(3.4097, 0.01);

// Check whether this fails at AA for different text sizes
PassesAA(contrast, isLargeOrBold: false).ShouldBe(false);
PassesAA(contrast, isLargeOrBold: true).ShouldBe(true); // size >= 18pt or size >= 15 pt & bold
```

If you run it with `dotnet run` you'll be given a (big!) list of all "bad" colours that don't satisfy the contrast check.

This logic could be implemented as part of Sitebuilder to check if a site brand colour will be accessible or not.