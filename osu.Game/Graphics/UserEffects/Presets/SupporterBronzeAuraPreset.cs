// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Bronze supporter hearts — unlocked at 6+ cumulative months of
    /// supporting. Same heart motion as the pink tier, recoloured to a
    /// warm copper palette. Owning groups include silver/gold so higher
    /// tier supporters can equip this if they prefer the warmer look.
    /// </summary>
    public class SupporterBronzeAuraPreset : SupporterHeartsAuraBase
    {
        public const string ID = "supporter-hearts-bronze";

        // Warm copper. Three nearby tones so a cluster reads as a glow
        // rather than monochrome dots.
        private static readonly Color4[] BRONZE_PALETTE =
        {
            new Color4(205, 127,  50, 255), // base copper
            new Color4(232, 155,  76, 255), // brighter copper
            new Color4(180, 100,  35, 255), // deeper bronze
        };

        protected override Color4[] Palette => BRONZE_PALETTE;

        public override string AuraId => ID;

        public override IReadOnlyList<string> OwningGroupIdentifiers { get; } = new[]
        {
            "torii-supporter-bronze",
            "torii-supporter-silver",
            "torii-supporter-gold",
        };

        public override int DefaultPriority => 39;
    }
}
