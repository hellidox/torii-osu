// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserEffects;
using osu.Game.Online.API.Requests.Responses;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tests.Visual.UserInterface
{
    /// <summary>
    /// Visual review of every <see cref="AuraPreset"/> rendered inside
    /// realistic UI contexts — leaderboard rows, multiplayer slots, chat
    /// messages, profile headers, and the in-game tab-held player list.
    /// Sister scene to <see cref="TestSceneUserAuras"/> which shows the
    /// auras in isolation (just the username on a flat background); this
    /// scene catches the issues that only appear when the aura sits next
    /// to the rest of a panel — colour clashes with row backgrounds,
    /// readability against avatars, glow bleeding into adjacent stats,
    /// scaling with different font sizes.
    ///
    /// The mock rows are deliberately hand-rolled rather than reusing the
    /// production <c>LeaderboardScore</c> / <c>UserListPanel</c> drawables
    /// because:
    ///   1. Those production drawables pull in a long tail of dependencies
    ///      (rulesets, beatmaps, scoring) that aren't relevant to this
    ///      scene's purpose.
    ///   2. Hand-rolling lets us pose the auras at the exact font sizes /
    ///      paddings that production ships, without the test breaking when
    ///      production rearranges its panel internals.
    /// If a mock visibly drifts from production, fix the mock — but don't
    /// import the production drawable.
    /// </summary>
    [TestFixture]
    public partial class TestSceneUserAurasInContext : OsuTestScene
    {
        private FillFlowContainer<ContextRow> rows = null!;
        private ContextKind currentContext = ContextKind.Leaderboard;

        public TestSceneUserAurasInContext()
        {
            rebuild();

            AddStep("Leaderboard row", () => switchContext(ContextKind.Leaderboard));
            AddStep("Player list (TAB held in game)", () => switchContext(ContextKind.PlayerList));
            AddStep("Multiplayer slot", () => switchContext(ContextKind.MultiSlot));
            AddStep("Chat message", () => switchContext(ContextKind.Chat));
            AddStep("Profile header (large)", () => switchContext(ContextKind.ProfileHeader));
        }

        private void switchContext(ContextKind kind)
        {
            currentContext = kind;
            rebuild();
        }

        private void rebuild()
        {
            // Tear down — we're rebuilding the whole flow each step so the
            // user sees the "first second" of every aura at the same time
            // (otherwise the tile that was already on screen is mid-cycle
            // and reads differently from the new ones).
            Clear();

            Add(new Box
            {
                // Same near-black as the sibling test scene — matches the
                // overlay backgrounds the auras were tuned against.
                RelativeSizeAxes = Axes.Both,
                Colour = new Color4(20, 20, 28, 255),
            });

            Add(rows = new FillFlowContainer<ContextRow>
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Padding = new MarginPadding(20),
                Spacing = new Vector2(0, 12),
                Direction = FillDirection.Vertical,
            });

            foreach (var preset in AuraRegistry.AllPresets)
                rows.Add(new ContextRow(preset, currentContext));
        }

        private enum ContextKind
        {
            Leaderboard,
            PlayerList,
            MultiSlot,
            Chat,
            ProfileHeader,
        }

        /// <summary>
        /// One mock row showing a username (decorated by the given preset's
        /// aura) inside the chosen context. The row's layout and styling
        /// follow the corresponding production panel closely enough that
        /// font sizes / paddings produce the same visual cadence — the
        /// goal is to catch readability issues, not to be 1:1 with prod.
        /// </summary>
        private partial class ContextRow : Container
        {
            private readonly AuraPreset preset;
            private readonly ContextKind kind;
            private const string sample_username = "Shikkesora";

            public ContextRow(AuraPreset preset, ContextKind kind)
            {
                this.preset = preset;
                this.kind = kind;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                APIUser fakeUser = makeFakeUser();

                Child = kind switch
                {
                    ContextKind.Leaderboard => buildLeaderboardRow(fakeUser),
                    ContextKind.PlayerList => buildPlayerListRow(fakeUser),
                    ContextKind.MultiSlot => buildMultiSlotRow(fakeUser),
                    ContextKind.Chat => buildChatRow(fakeUser),
                    ContextKind.ProfileHeader => buildProfileHeaderRow(fakeUser),
                    _ => new Container(),
                };
            }

            private APIUser makeFakeUser() => new APIUser
            {
                Id = -1,
                Username = sample_username,
                Groups = new[]
                {
                    new APIUserGroup
                    {
                        Identifier = preset.OwningGroupIdentifiers[0],
                        Name = preset.OwningGroupIdentifiers[0],
                        Colour = "#888888",
                    },
                },
            };

            // ---------- Leaderboard row ----------
            // Mirrors the in-game gameplay leaderboard cadence: rank | avatar
            // placeholder | username | score | accuracy. Avatars on the
            // leaderboard are tight 36px circles, username is medium-weight
            // ~14pt — small enough to test that the glow doesn't bleed into
            // the score column on the right.
            private Drawable buildLeaderboardRow(APIUser user) => new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 44,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(28, 28, 38, 255),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 12, Vertical = 6 },
                        Spacing = new Vector2(12, 0),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            // Rank pill
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Width = 36,
                                Height = 22,
                                Masking = true,
                                CornerRadius = 4,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(60, 60, 80, 255) },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = "#1",
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                    },
                                },
                            },
                            // Avatar placeholder
                            avatarPlaceholder(32, preset),
                            // Username with aura
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Child = UserAuraContainer.Wrap(user, new OsuSpriteText
                                {
                                    Text = sample_username,
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                    Colour = Color4.White,
                                }),
                            },
                            // Score column on the right side
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "1,234,567",
                                Font = OsuFont.GetFont(size: 13),
                                Colour = new Color4(200, 200, 210, 255),
                                Margin = new MarginPadding { Left = 80 },
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "98.42%",
                                Font = OsuFont.GetFont(size: 13),
                                Colour = new Color4(150, 220, 255, 255),
                                Margin = new MarginPadding { Left = 18 },
                            },
                        },
                    },
                },
            };

            // ---------- Player list (TAB during gameplay) ----------
            // Compact horizontal pill — small avatar, username, ready-state
            // dot. This is the densest UI the aura ever appears in, so it's
            // the worst-case for glow-bleed into adjacent rows.
            private Drawable buildPlayerListRow(APIUser user) => new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 32,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(24, 24, 32, 255),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 10, Vertical = 4 },
                        Spacing = new Vector2(8, 0),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            avatarPlaceholder(24, preset),
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Child = UserAuraContainer.Wrap(user, new OsuSpriteText
                                {
                                    Text = sample_username,
                                    Font = OsuFont.GetFont(size: 13),
                                    Colour = Color4.White,
                                }),
                            },
                            // Ready-state dot to anchor the right edge
                            new Circle
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(8),
                                Colour = new Color4(120, 230, 150, 255),
                                Margin = new MarginPadding { Left = 12 },
                            },
                        },
                    },
                },
            };

            // ---------- Multiplayer slot ----------
            // Wider card with an avatar block, larger username, mode +
            // status. Production multi slots have a coloured team strip on
            // the left that we're skipping for visual cleanliness here.
            private Drawable buildMultiSlotRow(APIUser user) => new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(34, 34, 48, 255),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 16, Vertical = 8 },
                        Spacing = new Vector2(14, 0),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            avatarPlaceholder(44, preset),
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 2),
                                Children = new Drawable[]
                                {
                                    UserAuraContainer.Wrap(user, new OsuSpriteText
                                    {
                                        Text = sample_username,
                                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                                        Colour = Color4.White,
                                    }),
                                    new OsuSpriteText
                                    {
                                        Text = "osu! · ready",
                                        Font = OsuFont.GetFont(size: 11),
                                        Colour = new Color4(160, 160, 170, 255),
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // ---------- Chat message ----------
            // Avatar + "username: message text" on a single row. Username
            // here is the user's chat-name colour (we use white to keep the
            // aura colour as the sole chromatic accent for testing).
            private Drawable buildChatRow(APIUser user) => new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 36,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 12, Vertical = 4 },
                        Spacing = new Vector2(10, 0),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            avatarPlaceholder(28, preset),
                            new Container
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Child = UserAuraContainer.Wrap(user, new OsuSpriteText
                                {
                                    Text = sample_username + ":",
                                    Font = OsuFont.GetFont(size: 13, weight: FontWeight.SemiBold),
                                    Colour = Color4.White,
                                }),
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "gg, was a fun map",
                                Font = OsuFont.GetFont(size: 13),
                                Colour = new Color4(220, 220, 225, 255),
                            },
                        },
                    },
                },
            };

            // ---------- Profile header ----------
            // Big-ass username next to a large avatar. This is where the
            // glow has the most room to breathe — and also where any halo
            // bleed becomes most visible because of the larger area.
            private Drawable buildProfileHeaderRow(APIUser user) => new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 110,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(38, 38, 56, 255),
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 24, Vertical = 18 },
                        Spacing = new Vector2(20, 0),
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            avatarPlaceholder(74, preset),
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 4),
                                Children = new Drawable[]
                                {
                                    UserAuraContainer.Wrap(user, new OsuSpriteText
                                    {
                                        Text = sample_username,
                                        Font = OsuFont.GetFont(size: 32, weight: FontWeight.SemiBold),
                                        Colour = Color4.White,
                                    }),
                                    new OsuSpriteText
                                    {
                                        Text = "🌍 Argentina · #4,204 worldwide",
                                        Font = OsuFont.GetFont(size: 13),
                                        Colour = new Color4(180, 180, 190, 255),
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // ---------- helpers ----------

            // Avatar placeholder — a coloured Circle tinted by the preset's
            // first owning group. We don't load real avatars in tests because
            // it would require an online dependency and slow the runner.
            // The colour gives just enough chroma to distinguish presets at
            // a glance without competing with the aura itself.
            private static Drawable avatarPlaceholder(float size, AuraPreset preset)
            {
                Color4 tint = preset.GlowColour ?? new Color4(80, 80, 100, 255);
                // Mute the avatar tint heavily — it's secondary to the aura,
                // not the visual focus.
                tint = new Color4(tint.R * 0.5f, tint.G * 0.5f, tint.B * 0.5f, 1f);

                return new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(size),
                    Masking = true,
                    CornerRadius = size / 2f,
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = tint },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "S",
                            Font = OsuFont.GetFont(size: size * 0.5f, weight: FontWeight.Bold),
                            Colour = Color4.White,
                        },
                    },
                };
            }
        }
    }
}
