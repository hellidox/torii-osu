// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Silver supporter hearts — unlocked at 12+ cumulative months of
    /// supporting. Cool platinum palette. Owning groups include gold so
    /// gold supporters can equip silver if they prefer it.
    /// </summary>
    public class SupporterSilverAuraPreset : SupporterHeartsAuraBase
    {
        public const string ID = "supporter-hearts-silver";

        // Cool platinum. Slightly bluish-tinted so it reads "silver" not
        // "uncoloured grey" — important for the additive-blend look.
        private static readonly Color4[] SILVER_PALETTE =
        {
            new Color4(220, 224, 232, 255), // bright platinum
            new Color4(192, 197, 207, 255), // base silver
            new Color4(168, 178, 195, 255), // cool steel
        };

        protected override Color4[] Palette => SILVER_PALETTE;

        public override string AuraId => ID;

        public override IReadOnlyList<string> OwningGroupIdentifiers { get; } = new[]
        {
            "torii-supporter-silver",
            "torii-supporter-gold",
        };

        public override int DefaultPriority => 38;
    }
}
