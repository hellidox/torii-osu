// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Gold supporter hearts — unlocked at 36+ cumulative months (3 years)
    /// of supporting. Top-tier loyalty preset. Rich warm gold palette.
    /// Only the gold tier owns this one — it's the visible distinction
    /// that comes with sustained long-term support.
    /// </summary>
    public class SupporterGoldAuraPreset : SupporterHeartsAuraBase
    {
        public const string ID = "supporter-hearts-gold";

        // Rich warm gold. Three tones for natural variation, weighted
        // toward the brightest so the aura visually pops compared to
        // bronze/silver — the perk for sticking around 3+ years.
        private static readonly Color4[] GOLD_PALETTE =
        {
            new Color4(255, 215,   0, 255), // pure gold
            new Color4(255, 195,  60, 255), // amber gold
            new Color4(230, 175,  20, 255), // deeper gold
        };

        protected override Color4[] Palette => GOLD_PALETTE;

        public override string AuraId => ID;

        public override IReadOnlyList<string> OwningGroupIdentifiers { get; } = new[]
        {
            "torii-supporter-gold",
        };

        public override int DefaultPriority => 37;
    }
}
