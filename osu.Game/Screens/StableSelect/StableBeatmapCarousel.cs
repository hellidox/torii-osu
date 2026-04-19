// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Pooling;
using osu.Game.Graphics.Carousel;
using osu.Game.Screens.SelectV2;

namespace osu.Game.Screens.StableSelect;

[Cached(typeof(BeatmapCarousel))]
public partial class StableBeatmapCarousel : BeatmapCarousel
{
    private readonly DrawablePool<StablePanelBeatmap> beatmapPanelPool = new DrawablePool<StablePanelBeatmap>(200);
    private readonly DrawablePool<StablePanelBeatmapSet> setPanelPool = new DrawablePool<StablePanelBeatmapSet>(150);
    private readonly DrawablePool<StablePanelGroup> groupPanelPool = new DrawablePool<StablePanelGroup>(150);

    [BackgroundDependencyLoader]
    private void load()
    {
        AddInternal(groupPanelPool);
        AddInternal(beatmapPanelPool);
        AddInternal(setPanelPool);
    }

    protected override Drawable GetDrawableForDisplay(CarouselItem item)
    {
        switch (item.Model)
        {
            case GroupedBeatmapSet:
                return setPanelPool.Get();

            case GroupedBeatmap:
                return beatmapPanelPool.Get();

            case GroupDefinition:
                return groupPanelPool.Get();
        }

        throw new InvalidOperationException($"Unsupported stable song select item type: {item.Model.GetType().Name}");
    }

    protected override float GetPanelXOffset(Drawable panel) => base.GetPanelXOffset(panel) * 0.18f;
}
