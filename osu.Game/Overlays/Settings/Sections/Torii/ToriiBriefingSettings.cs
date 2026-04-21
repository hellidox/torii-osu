// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.ToriiBriefing;

namespace osu.Game.Overlays.Settings.Sections.Torii
{
    public partial class ToriiBriefingSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Briefing";

        [Resolved(canBeNull: true)]
        private ToriiBriefingOverlay? briefingOverlay { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new SettingsButtonV2
            {
                Text = "Generate new briefing",
                TooltipText = "Fetch a fresh briefing from the Torii API and display it now.",
                Keywords = new[] { "torii", "briefing", "refresh", "generate", "daily" },
                Action = () => briefingOverlay?.ForceBriefingRefresh(),
            });
        }
    }
}
