// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Utils;
using osu.Game.Graphics;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserEffects;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osu.Game.Overlays.Chat;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Select.Leaderboards;
using osu.Game.Screens.SelectV2;
using osu.Game.Users;
using osuTK;

namespace osu.Game.Tests.Visual.UserInterface
{
    /// <summary>
    /// One scene that wires up REAL production drawables (the V2 song-select
    /// leaderboard row, the in-game gameplay leaderboard, chat lines, and
    /// every user panel variant) with fake users carrying each
    /// <see cref="AuraPreset"/>'s owning group identifier. The point is to
    /// see the actual aura render in the actual UI surface where it ships
    /// — sister test scenes like <c>TestSceneBeatmapLeaderboardScore</c>
    /// only use generic users without staff groups, so they never trigger
    /// the aura code path; this scene fixes that gap.
    ///
    /// Steps swap between contexts. Every context renders one drawable per
    /// preset (Admin / Dev / Mod / QAT / Supporter / Goof) plus a "no aura"
    /// row at the top for direct visual comparison.
    /// </summary>
    [TestFixture]
    public partial class TestSceneAurasInRealUI : OsuTestScene
    {
        // BeatmapLeaderboardScore + chat lines pull this from the resolved
        // overlay tree at construction; without it they won't theme.
        [Cached]
        private OverlayColourProvider colourProvider { get; set; } = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        // Same set of "personas" used everywhere — keeps the visual
        // comparison consistent across contexts (same name shows up in the
        // same colour family across leaderboard / chat / panels).
        private static readonly (string username, string? presetGroupKey)[] personas =
        {
            ("PlainPlayer",   null),                // baseline, no aura
            ("Shikkesora",    "torii-admin"),       // admin embers
            ("Imperation",    "torii-dev"),         // dev bits
            ("Boreas",        "torii-mod"),         // mod shields
            ("Mash39",        "torii-qat"),         // qat notes
            ("NahuelSupports","torii-supporter"),   // supporter aura
            ("GoofGuy",       "torii-goof"),        // goof leaves
        };

        public TestSceneAurasInRealUI()
        {
            // Default to slanted leaderboard since that's the most-asked-
            // about surface during tuning.
            showSlantedLeaderboard();

            AddStep("Slanted song-select leaderboard (V2 sheared)", showSlantedLeaderboard);
            AddStep("Plain leaderboard (V2 not sheared)",            showPlainLeaderboard);
            AddStep("In-game gameplay leaderboard",                  showGameplayLeaderboard);
            AddStep("Chat lines",                                    showChatLines);
            AddStep("User panels (Brick / Grid / List / Rank)",      showUserPanels);
        }

        // ---------- Persona / score factory ------------------------------

        private static APIUser makeFakeUser(string username, string? presetGroupKey, int id) => new APIUser
        {
            Id = id,
            Username = username,
            CountryCode = CountryCode.AR,
            Groups = presetGroupKey == null
                ? null
                : new[]
                {
                    new APIUserGroup
                    {
                        Identifier = presetGroupKey,
                        // Display name doesn't matter for aura resolution — the
                        // identifier is the only thing AuraRegistry looks at.
                        Name = presetGroupKey,
                        // Visible colour for any badge UI that surfaces the
                        // group; muted grey keeps the username's natural colour
                        // (or the aura) as the dominant visual.
                        Colour = "#888888",
                    },
                },
        };

        private static ScoreInfo makeFakeScore(string username, string? presetGroupKey, int id, int position)
        {
            return new ScoreInfo
            {
                Position = position,
                Rank = position == 1 ? ScoreRank.X : position <= 3 ? ScoreRank.S : ScoreRank.A,
                Accuracy = 0.99f - position * 0.005f,
                MaxCombo = 1500 - position * 80,
                TotalScore = (long)(2_000_000 - position * 110_000 + RNG.Next(-30_000, 30_000)),
                MaximumStatistics = { { HitResult.Great, 3000 } },
                Ruleset = new OsuRuleset().RulesetInfo,
                User = makeFakeUser(username, presetGroupKey, id),
                Date = DateTimeOffset.Now.AddDays(-position),
            };
        }

        private static IEnumerable<ScoreInfo> makeFakeScoreboard()
        {
            for (int i = 0; i < personas.Length; i++)
                yield return makeFakeScore(personas[i].username, personas[i].presetGroupKey, 1000 + i, i + 1);
        }

        // ---------- Steps -------------------------------------------------

        private void showSlantedLeaderboard()
        {
            Clear();
            Add(new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        // 50% width matches what SongSelectComponentsTestScene
                        // uses by default — keeps the row width close to the
                        // real song-select sidebar layout.
                        RelativeSizeAxes = Axes.X,
                        Width = 0.5f,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(0, 2),
                        // V2 leaderboard rows live inside a sheared parent in
                        // production. The sheared boolean on the row itself
                        // controls whether the username de-shears back to
                        // upright; we keep that on for the slanted variant.
                        Shear = OsuGame.SHEAR,
                        Children = makeFakeScoreboard()
                                   .Select(score => (Drawable)new BeatmapLeaderboardScore(score, sheared: true)
                                   {
                                       Rank = score.Position,
                                       Shear = Vector2.Zero,
                                   })
                                   .ToArray(),
                    },
                },
            });
        }

        private void showPlainLeaderboard()
        {
            Clear();
            Add(new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new OsuContextMenuContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        Width = 0.5f,
                        AutoSizeAxes = Axes.Y,
                        Spacing = new Vector2(0, 2),
                        // No outer shear — non-sheared variant of the same row
                        // used outside song-select (e.g. results panel).
                        Children = makeFakeScoreboard()
                                   .Select(score => (Drawable)new BeatmapLeaderboardScore(score, sheared: false)
                                   {
                                       Rank = score.Position,
                                   })
                                   .ToArray(),
                    },
                },
            });
        }

        private void showGameplayLeaderboard()
        {
            Clear();
            Add(new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Width = 0.4f,
                Spacing = new Vector2(0, 4),
                // The in-game leaderboard renders 250-wide rows in production
                // (top-right HUD strip during play). We stack them vertically
                // here for static review; in real play they animate position.
                Children = personas
                           .Select((p, idx) =>
                           {
                               // The internal ctor we use here is what the
                               // other gameplay LB test uses — takes IUser +
                               // tracked + a BindableLong total score.
                               var user = makeFakeUser(p.username, p.presetGroupKey, 2000 + idx);
                               var displayScore = new BindableLong(2_000_000 - idx * 100_000);

                               var score = new GameplayLeaderboardScore(user, tracked: idx == 0, displayScore)
                               {
                                   Position = { Value = idx + 1 },
                               };

                               return (Drawable)new Container
                               {
                                   RelativeSizeAxes = Axes.X,
                                   Height = 50,
                                   Child = new DrawableGameplayLeaderboardScore(score)
                                   {
                                       Anchor = Anchor.Centre,
                                       Origin = Anchor.Centre,
                                       Width = 250,
                                       RelativeSizeAxes = Axes.Y,
                                       Expanded = { Value = true },
                                   },
                               };
                           })
                           .ToArray(),
            });
        }

        private void showChatLines()
        {
            Clear();
            Add(new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                Width = 0.7f,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(0, 4),
                Padding = new MarginPadding(20),
                Children = personas
                           .Select((p, idx) =>
                           {
                               var sender = makeFakeUser(p.username, p.presetGroupKey, 3000 + idx);
                               var message = new Message(idx + 1)
                               {
                                   Content = chatSnippets[idx % chatSnippets.Length],
                                   Sender = sender,
                                   Timestamp = DateTimeOffset.Now.AddSeconds(-idx),
                               };
                               return (Drawable)new ChatLine(message);
                           })
                           .ToArray(),
            });
        }

        private void showUserPanels()
        {
            Clear();
            // Mix of all panel variants stacked vertically so a single screen
            // shows all 6 auras × 4 panel shapes.
            var flow = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(8),
                Direction = FillDirection.Vertical,
            };

            // Brick — compact one-line panel
            flow.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(6),
                Direction = FillDirection.Horizontal,
                Children = personas.Select((p, idx) =>
                    (Drawable)new UserBrickPanel(makeFakeUser(p.username, p.presetGroupKey, 4000 + idx))).ToArray(),
            });

            // Grid — square card with avatar + stats
            flow.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(8),
                Direction = FillDirection.Horizontal,
                Children = personas.Select((p, idx) =>
                    (Drawable)new UserGridPanel(makeFakeUser(p.username, p.presetGroupKey, 4100 + idx)) { Width = 220 }).ToArray(),
            });

            // List — wide horizontal panel with extended stats
            flow.Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Spacing = new Vector2(0, 4),
                Direction = FillDirection.Vertical,
                Children = personas.Select((p, idx) =>
                    (Drawable)new UserListPanel(makeFakeUser(p.username, p.presetGroupKey, 4200 + idx))).ToArray(),
            });

            Add(new BasicScrollContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = flow,
            });
        }

        // Different message bodies per row so the chat view doesn't read as
        // identical clones — helps spot misalignment / spacing issues
        // specific to long vs short messages.
        private static readonly string[] chatSnippets =
        {
            "yo",
            "gg, that map is brutal",
            "anyone up for a multi?",
            "i found a bug in beatmap submission",
            "lol",
            "thanks for the donation tier <3",
            "pls don't ask me to mod ranked maps :)",
        };
    }
}
