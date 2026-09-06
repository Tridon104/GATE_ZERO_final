# Gate Zero cinematic pass

This branch adds a non-destructive cinematic look pass for `GZ_Hub_01`.

## Automatic at Play

`Assets/GateZero/Scripts/GateZeroCinematicBootstrap.cs` applies the look automatically when the hub scene loads. No component needs to be added manually.

The pass currently:

- forces post-processing and HDR on the main camera;
- adds dark blue exponential-squared atmospheric fog;
- lowers ambient light so neon becomes the main visual source;
- configures Bloom, ACES tonemapping, contrast, cool white balance, vignette, subtle film grain and very small chromatic aberration;
- refines rain color, wind direction, stretched-rain rendering and culling;
- refines collision splash lifetime, size, color and billboard rendering;
- boosts wet-road smoothness/reflection response when objects/materials contain `wetroad`, `wet_road` or `wetoverlay` in their names;
- creates one modest realtime reflection probe only if the scene has no reflection probe already;
- raises PC shadow distance, MSAA, color-grading LUT quality, additional-light budget, soft particles and realtime reflection-probe support;
- leaves Synty source assets untouched.

## Visual target

The intended target is a rainy neon metaverse hub: dark but readable, strong localized cyan/magenta reflections, wet asphalt, atmospheric depth and a clean cinematic presentation rather than heavy horror-style fog.

## Important

The huge Real Stars cubemap files remain local and are intentionally ignored by Git. Unity can keep using the local copies on the development machine.

A correct `.gitignore` was added at repository root; the older `.gitignore.txt` can be removed later after this branch is merged.
