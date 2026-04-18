// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Online.Chat;
using osu.Game.Overlays.ToriiBriefing;

namespace osu.Game.Tests.Visual.Online
{
    public partial class TestSceneToriiBriefingOverlay : OsuTestScene
    {
        private ToriiBriefingOverlay briefing;
        private ChannelManager channelManager;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = new DependencyProvidingContainer
            {
                RelativeSizeAxes = Axes.Both,
                CachedDependencies = new (Type, object)[]
                {
                    (typeof(ChannelManager), channelManager = new ChannelManager(API)),
                },
                Children = new Drawable[]
                {
                    channelManager,
                    briefing = new ToriiBriefingOverlay(channelManager),
                },
            };
        });

        [Test]
        public void TestSampleBriefingShows()
        {
            AddStep("show sample briefing", () => briefing.ShowSampleBriefing());
            AddUntilStep("briefing visible", () => briefing.State.Value == Visibility.Visible);
        }

        [Test]
        public void TestSampleBriefingCanClose()
        {
            AddStep("show sample briefing", () => briefing.ShowSampleBriefing());
            AddUntilStep("briefing visible", () => briefing.State.Value == Visibility.Visible);
            AddStep("hide briefing", () => briefing.Hide());
            AddUntilStep("briefing hidden", () => briefing.State.Value == Visibility.Hidden);
        }
    }
}
