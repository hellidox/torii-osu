// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Scoring;

namespace osu.Game.Screens.Ranking
{
    /// <summary>
    /// Dedicated entry point for stable-style results rendering.
    /// This currently inherits the existing solo results behaviour and allows
    /// iterative stable-style visual parity work behind a runtime toggle.
    /// </summary>
    public partial class StableStyleSoloResultsScreen : SoloResultsScreen
    {
        private readonly HashSet<Drawable> styledTextCache = new HashSet<Drawable>();
        private double lastStylePassTime;

        public StableStyleSoloResultsScreen(ScoreInfo score)
            : base(score)
        {
        }

        protected override void Update()
        {
            base.Update();

            if (Time.Current - lastStylePassTime < 250)
                return;

            lastStylePassTime = Time.Current;

            foreach (var text in this.ChildrenOfType<OsuSpriteText>())
            {
                if (!styledTextCache.Add(text))
                    continue;

                text.Font = text.Font.With(typeface: Typeface.Venera);
            }
        }
    }
}
