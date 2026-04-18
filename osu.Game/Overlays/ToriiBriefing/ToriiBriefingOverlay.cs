// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.IO.Serialization;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.ToriiBriefing
{
    public partial class ToriiBriefingOverlay : OsuFocusedOverlayContainer
    {
        private const string snapshot_filename = @"briefing-state.json";

        private readonly ChannelManager channelManager;
        private readonly HashSet<string> shownThisSession = new HashSet<string>();
        private readonly HashSet<string> pendingThisSession = new HashSet<string>();
        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly IBindable<APIUser> localUser = new Bindable<APIUser>();

        private IAPIProvider api;
        private RulesetStore rulesets;
        private Storage briefingStorage;
        private TextureStore textures;

        private Container panel;
        private OsuSpriteText title;
        private OsuSpriteText subtitle;
        private FillFlowContainer cardFlow;

        protected override string PopInSampleName => @"UI/overlay-big-pop-in";
        protected override string PopOutSampleName => @"UI/overlay-big-pop-out";

        public override bool BlockScreenWideMouse => true;

        public ToriiBriefingOverlay(ChannelManager channelManager)
        {
            this.channelManager = channelManager;
            RelativeSizeAxes = Axes.Both;
            Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load(IAPIProvider api, RulesetStore rulesets, Storage storage, TextureStore textures)
        {
            this.api = api;
            this.rulesets = rulesets;
            this.textures = textures;
            briefingStorage = storage.GetStorageForDirectory(@"torii");

            var accentBlue = Color4Extensions.FromHex(@"69d7ff");
            var accentPink = Color4Extensions.FromHex(@"ff66b3");
            var deepNavy = Color4Extensions.FromHex(@"090b26");

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black.Opacity(0.58f),
                },
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.92f, 0.9f),
                    FillMode = FillMode.Fit,
                    FillAspectRatio = 1.62f,
                    Child = panel = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 26,
                        BorderThickness = 1.6f,
                        BorderColour = accentBlue.Opacity(0.35f),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Colour = accentPink.Opacity(0.22f),
                            Radius = 28,
                            Offset = new Vector2(0, 10),
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientVertical(deepNavy.Opacity(0.96f), Color4Extensions.FromHex(@"15112c").Opacity(0.96f)),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientHorizontal(accentPink.Opacity(0.08f), accentBlue.Opacity(0.08f)),
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Horizontal = 32, Vertical = 28 },
                                Child = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    RowDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.AutoSize),
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[] { createHeader(accentBlue, accentPink) },
                                        new Drawable[]
                                        {
                                            new OsuScrollContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Padding = new MarginPadding { Top = 28, Right = 6 },
                                                Child = cardFlow = new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(0, 14),
                                                },
                                            },
                                        },
                                        new Drawable[]
                                        {
                                            new Container
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                Height = 52,
                                                Margin = new MarginPadding { Top = 20 },
                                                Children = new Drawable[]
                                                {
                                                    new OsuSpriteText
                                                    {
                                                        Anchor = Anchor.CentreLeft,
                                                        Origin = Anchor.CentreLeft,
                                                        Text = "Generated from live Torii API data and your local session snapshot.",
                                                        Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                                                        Colour = Color4.White.Opacity(0.46f),
                                                    },
                                                    new RoundedButton
                                                    {
                                                        Anchor = Anchor.CentreRight,
                                                        Origin = Anchor.CentreRight,
                                                        Width = 190,
                                                        Height = 46,
                                                        Text = "enter Torii",
                                                        BackgroundColour = accentPink,
                                                        Action = Hide,
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        private Drawable createHeader(Color4 accentBlue, Color4 accentPink)
        {
            return new Container
            {
                RelativeSizeAxes = Axes.X,
                Height = 104,
                Children = new Drawable[]
                {
                    new CircularContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(70),
                        Masking = true,
                        BorderThickness = 1.4f,
                        BorderColour = accentPink.Opacity(0.36f),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Colour = accentPink.Opacity(0.3f),
                            Radius = 18,
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4Extensions.FromHex(@"141336"),
                            },
                            createToriiLogo(accentPink),
                        },
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        X = 92,
                        Padding = new MarginPadding { Right = 210 },
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = "Torii briefing",
                                        Font = OsuFont.GetFont(size: 34, weight: FontWeight.Bold),
                                    },
                                    new BriefingPill("daily portal", accentBlue),
                                },
                            },
                            title = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 19, weight: FontWeight.SemiBold),
                                Colour = Color4.White.Opacity(0.78f),
                            },
                            subtitle = new OsuSpriteText
                            {
                                Font = OsuFont.GetFont(size: 15, weight: FontWeight.SemiBold),
                                Colour = Color4.White.Opacity(0.46f),
                            },
                        },
                    },
                    new IconButton
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        Icon = FontAwesome.Solid.Times,
                        Colour = Color4.White.Opacity(0.64f),
                        Action = Hide,
                    },
                },
            };
        }

        private Drawable createToriiLogo(Color4 accentPink)
        {
            var logo = textures?.Get(@"Torii/logo");

            if (logo != null)
            {
                return new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(50),
                    FillMode = FillMode.Fit,
                    Texture = logo,
                };
            }

            return new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "T",
                Font = OsuFont.GetFont(size: 34, weight: FontWeight.Bold),
                Colour = accentPink,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            apiState.BindTo(api.State);
            localUser.BindTo(api.LocalUser);

            apiState.BindValueChanged(_ => queueBriefingIfReady(), true);
            localUser.BindValueChanged(_ => queueBriefingIfReady(), true);

            // Login restoration can complete before this overlay is fully loaded, depending on startup timing.
            // A few cheap retries make the briefing resilient without polling forever.
            Scheduler.AddDelayed(queueBriefingIfReady, 500);
            Scheduler.AddDelayed(queueBriefingIfReady, 2500);
            Scheduler.AddDelayed(queueBriefingIfReady, 7000);
        }

        private void queueBriefingIfReady()
        {
            if (!api.IsLoggedIn || apiState.Value != APIState.Online || localUser.Value?.Id <= 1)
                return;

            var user = localUser.Value;
            var ruleset = getCurrentRuleset(user);
            string variant = ToriiPpVariantState.UsePpDevVariant ? "pp_dev" : "stable";
            string sessionKey = $"{user.Id}:{ruleset.ShortName}:{variant}";

            if (shownThisSession.Contains(sessionKey) || !pendingThisSession.Add(sessionKey))
                return;

            Logger.Log($"Torii briefing queued for {sessionKey}.");
            fetchBriefingData(sessionKey, user.Id, ruleset.ShortName, ToriiPpVariantState.UsePpDevVariant);
        }

        private RulesetInfo getCurrentRuleset(APIUser user)
        {
            if (!string.IsNullOrEmpty(user.PlayMode))
            {
                var userRuleset = rulesets.GetRuleset(user.PlayMode);

                if (userRuleset != null)
                    return userRuleset;
            }

            return rulesets.GetRuleset("osu") ?? rulesets.GetRuleset(0) ?? rulesets.AvailableRulesets.First();
        }

        private void fetchBriefingData(string sessionKey, int userId, string rulesetName, bool usePpDev)
        {
            if (!api.IsLoggedIn || apiState.Value != APIState.Online || localUser.Value?.Id <= 1)
            {
                Logger.Log($"Torii briefing fetch skipped for {sessionKey} (loggedIn={api.IsLoggedIn}, state={apiState.Value}, localUser={localUser.Value?.Id}).");
                pendingThisSession.Remove(sessionKey);
                return;
            }

            var ruleset = rulesets.GetRuleset(rulesetName) ?? rulesets.GetRuleset("osu") ?? rulesets.GetRuleset(0) ?? rulesets.AvailableRulesets.First();
            var pending = new PendingBriefing(sessionKey, localUser.Value, ruleset, usePpDev);

            Logger.Log($"Torii briefing fetching for {sessionKey}.");

            var userRequest = new GetUserRequest(userId, ruleset);
            userRequest.Success += response =>
            {
                pending.User = response;
                pending.MarkComplete();
                showWhenComplete(pending);
            };
            userRequest.Failure += _ =>
            {
                pending.MarkComplete();
                showWhenComplete(pending);
            };

            var scoresRequest = new GetUserScoresRequest(userId, ScoreType.Best, new PaginationParameters(20), ruleset);
            scoresRequest.Success += response =>
            {
                pending.TopScores = response;
                pending.MarkComplete();
                showWhenComplete(pending);
            };
            scoresRequest.Failure += _ =>
            {
                pending.MarkComplete();
                showWhenComplete(pending);
            };

            var radarRequest = new GetToriiBriefingRadarRequest(ruleset);
            radarRequest.Success += response =>
            {
                pending.Radar = response;
                pending.MarkComplete();
                showWhenComplete(pending);
            };
            radarRequest.Failure += _ =>
            {
                // Older servers won't have this endpoint yet. Keep the briefing useful using local fallback cards.
                pending.MarkComplete();
                showWhenComplete(pending);
            };

            try
            {
                api.Queue(userRequest);
                api.Queue(scoresRequest);
                api.Queue(radarRequest);
            }
            catch
            {
                pendingThisSession.Remove(sessionKey);
            }
        }

        private void showWhenComplete(PendingBriefing pending)
        {
            if (!pending.IsComplete)
                return;

            var payload = createPayload(pending);

            if (payload == null)
            {
                pendingThisSession.Remove(pending.SessionKey);
                return;
            }

            displayPayload(payload);
            pendingThisSession.Remove(pending.SessionKey);
            shownThisSession.Add(pending.SessionKey);
            Logger.Log($"Torii briefing shown for {pending.SessionKey}.");
            Show();
        }

        private BriefingPayload createPayload(PendingBriefing pending)
        {
            var user = pending.User ?? pending.LocalUser;

            if (user == null)
                return null;

            var scores = pending.TopScores ?? new List<SoloScoreInfo>();
            string variant = pending.UsePpDev ? "pp_dev" : "stable";
            string snapshotKey = $"{user.Id}:{pending.Ruleset.ShortName}:{variant}";

            var currentSnapshot = new BriefingSnapshot
            {
                UserId = user.Id,
                Username = user.Username,
                Ruleset = pending.Ruleset.ShortName,
                Variant = variant,
                CapturedAt = DateTimeOffset.UtcNow,
                GlobalRank = user.Statistics?.GlobalRank,
                CountryRank = user.Statistics?.CountryRank,
                PP = toDouble(user.Statistics?.PP),
                TopScores = scores.Select(createScoreSnapshot).Where(s => s.ScoreId > 0).ToList(),
            };

            var state = loadSnapshotState();
            state.Users.TryGetValue(snapshotKey, out var previousSnapshot);

            state.Users[snapshotKey] = currentSnapshot;
            saveSnapshotState(state);

            return new BriefingPayload
            {
                User = user,
                Ruleset = pending.Ruleset,
                Variant = variant,
                Current = currentSnapshot,
                Previous = previousSnapshot,
                ScoreChanges = getScoreChanges(previousSnapshot, currentSnapshot),
                UnreadMessages = getUnreadMessages(user.Id),
                RadarFirstSnapshot = pending.Radar?.FirstSnapshot ?? false,
                RadarTrackedCount = pending.Radar?.TrackedCount ?? 0,
                RadarEvents = getRadarEvents(previousSnapshot, currentSnapshot, pending.Radar),
            };
        }

        private BriefingScoreSnapshot createScoreSnapshot(SoloScoreInfo score)
        {
            return new BriefingScoreSnapshot
            {
                ScoreId = score.ID ?? 0,
                Title = getScoreTitle(score),
                Rank = score.Rank.ToString(),
                PP = score.PP,
                Accuracy = score.Accuracy,
            };
        }

        private string getScoreTitle(SoloScoreInfo score)
        {
            string artist = score.Beatmap?.BeatmapSet?.Artist ?? "Unknown artist";
            string title = score.Beatmap?.BeatmapSet?.Title ?? "Unknown title";
            string difficulty = score.Beatmap?.DifficultyName ?? "Unknown difficulty";

            return $"{artist} - {title} [{difficulty}]";
        }

        private List<BriefingScoreChange> getScoreChanges(BriefingSnapshot previous, BriefingSnapshot current)
        {
            if (previous?.TopScores == null || previous.TopScores.Count == 0)
                return new List<BriefingScoreChange>();

            var previousById = previous.TopScores.ToDictionary(s => s.ScoreId);
            var changes = new List<BriefingScoreChange>();

            foreach (var score in current.TopScores)
            {
                if (!score.PP.HasValue || !previousById.TryGetValue(score.ScoreId, out var oldScore) || !oldScore.PP.HasValue)
                    continue;

                double delta = score.PP.Value - oldScore.PP.Value;

                if (Math.Abs(delta) < 0.05)
                    continue;

                changes.Add(new BriefingScoreChange
                {
                    Title = score.Title,
                    OldPP = oldScore.PP.Value,
                    NewPP = score.PP.Value,
                    Delta = delta,
                });
            }

            return changes.OrderByDescending(c => Math.Abs(c.Delta)).ToList();
        }

        private List<BriefingMessage> getUnreadMessages(int localUserId)
        {
            return channelManager.JoinedChannels
                                 .SelectMany(c => c.UnreadMessages.Select(m => new { Channel = c, Message = m }))
                                 .Where(m => m.Message.Sender?.Id != localUserId)
                                 .OrderByDescending(m => m.Message.Timestamp)
                                 .Take(4)
                                 .Select(m => new BriefingMessage
                                 {
                                     Sender = m.Message.Sender?.Username ?? m.Channel.Name ?? "someone",
                                     Channel = m.Channel.Name ?? "chat",
                                     Preview = !string.IsNullOrEmpty(m.Message.DisplayContent) ? m.Message.DisplayContent : m.Message.Content ?? string.Empty,
                                 })
                                 .ToList();
        }

        private List<BriefingRadarEvent> getRadarEvents(BriefingSnapshot previous, BriefingSnapshot current, ToriiBriefingRadarResponse serverRadar)
        {
            var serverEvents = serverRadar?.Events?
                                          .Where(e => !string.IsNullOrEmpty(e.Headline) || !string.IsNullOrEmpty(e.Detail))
                                          .Select(e => new BriefingRadarEvent
                                          {
                                              Title = string.IsNullOrEmpty(e.Headline) ? "Dojo radar shift" : e.Headline,
                                              Detail = e.Detail,
                                              Severity = e.Severity,
                                          })
                                          .ToList();

            if (serverEvents?.Count > 0)
                return serverEvents;

            if (previous?.TopScores == null || previous.TopScores.Count == 0)
                return new List<BriefingRadarEvent>();

            // Local fallback for dev/old servers. The real snipe feed comes from /api/v2/torii/briefing/radar.
            var currentScoreIds = current.TopScores.Select(s => s.ScoreId).ToHashSet();
            return previous.TopScores
                           .Where(s => s.ScoreId > 0 && !currentScoreIds.Contains(s.ScoreId))
                           .Take(3)
                           .Select(s => new BriefingRadarEvent
                           {
                               Title = s.Title,
                               Detail = "Left your locally tracked top-play set.",
                               Severity = "info",
                           })
                           .ToList();
        }

        private void displayPayload(BriefingPayload payload)
        {
            title.Text = $"Welcome back, {payload.User.Username}.";
            subtitle.Text = $"{payload.Ruleset.Name} - {(payload.Variant == "pp_dev" ? "latest pp-dev calculations" : "standard calculations")} - {DateTimeOffset.Now:MMM d, HH:mm}";

            cardFlow.Clear();

            addItem(new BriefingSectionHeader("your session", "changes from your plays, rank, and pp snapshots"));
            addItem(createRankCard(payload));
            addItem(createScoreCard(payload));
            addItem(createSyncCard(payload));

            addItem(new BriefingSectionHeader("dojo radar", "things that changed around you while you were away"));
            addItem(createMessageCard(payload));
            addItem(createRadarCard(payload));
        }

        public void ShowSampleBriefing()
        {
            var ruleset = rulesets?.GetRuleset("osu") ?? rulesets?.GetRuleset(0);

            if (ruleset == null)
                return;

            var sampleUser = new APIUser
            {
                Id = 19,
                Username = "Shikkesora",
            };

            displayPayload(new BriefingPayload
            {
                User = sampleUser,
                Ruleset = ruleset,
                Variant = "pp_dev",
                Previous = new BriefingSnapshot
                {
                    UserId = sampleUser.Id,
                    Username = sampleUser.Username,
                    Ruleset = ruleset.ShortName,
                    Variant = "pp_dev",
                    CapturedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    GlobalRank = 24,
                    CountryRank = 3,
                    PP = 2301.42,
                    TopScores = new List<BriefingScoreSnapshot>
                    {
                        new BriefingScoreSnapshot { ScoreId = 1001, Title = "FAIRY FORE - Vivid [Insane]", PP = 83.28 },
                        new BriefingScoreSnapshot { ScoreId = 1002, Title = "Will Stetson - Of Our Time [Clipfarm Edit]", PP = 348.10 },
                    },
                },
                Current = new BriefingSnapshot
                {
                    UserId = sampleUser.Id,
                    Username = sampleUser.Username,
                    Ruleset = ruleset.ShortName,
                    Variant = "pp_dev",
                    CapturedAt = DateTimeOffset.UtcNow,
                    GlobalRank = 19,
                    CountryRank = 2,
                    PP = 2325.13,
                    TopScores = new List<BriefingScoreSnapshot>
                    {
                        new BriefingScoreSnapshot { ScoreId = 1001, Title = "FAIRY FORE - Vivid [Insane]", PP = 53.60 },
                        new BriefingScoreSnapshot { ScoreId = 1002, Title = "Will Stetson - Of Our Time [Clipfarm Edit]", PP = 351.30 },
                    },
                },
                ScoreChanges = new List<BriefingScoreChange>
                {
                    new BriefingScoreChange { Title = "FAIRY FORE - Vivid [Insane]", OldPP = 83.28, NewPP = 53.60, Delta = -29.68 },
                    new BriefingScoreChange { Title = "Will Stetson - Of Our Time [Clipfarm Edit]", OldPP = 348.10, NewPP = 351.30, Delta = 3.20 },
                },
                UnreadMessages = new List<BriefingMessage>
                {
                    new BriefingMessage { Channel = "general", Sender = "Seba", Preview = "that pp-dev thing actually works now?" },
                    new BriefingMessage { Channel = "staff", Sender = "ToriiHalo", Preview = "2 scores were recalculated overnight." },
                },
                RadarEvents = new List<BriefingRadarEvent>
                {
                    new BriefingRadarEvent { Title = "Shoujo A (Cut Ver.) [gwb's Extreme]", Detail = "MommyAcheron pushed you from #1 to #2." },
                },
            });

            Show();
        }

        private void addItem(Drawable drawable)
        {
            int index = cardFlow.Count;

            drawable.Alpha = 0;
            drawable.Y = 10;
            cardFlow.Add(drawable);
            drawable.Delay(60 * index).FadeIn(320, Easing.OutQuint);
            drawable.Delay(60 * index).MoveToY(0, 420, Easing.OutQuint);
        }

        private BriefingCard createRankCard(BriefingPayload payload)
        {
            var previous = payload.Previous;
            var current = payload.Current;
            var accent = Color4Extensions.FromHex(@"69d7ff");

            string headline;
            string detail;

            if (previous == null)
            {
                headline = "First briefing snapshot created";
                detail = "I will compare rank and pp movement from this point onward.";
            }
            else
            {
                headline = getRankHeadline(previous.GlobalRank, current.GlobalRank, out accent);
                string ppLine = getPpDelta(previous.PP, current.PP);
                detail = $"{formatRank(previous.GlobalRank)} -> {formatRank(current.GlobalRank)} / {ppLine}";
            }

            return new BriefingCard(FontAwesome.Solid.ChartLine, "rank pulse", headline, detail, accent)
            {
                TooltipText = previous == null
                    ? "No previous local snapshot exists yet."
                    : $"Previous country rank: {formatRank(previous.CountryRank)}\nCurrent country rank: {formatRank(current.CountryRank)}",
            };
        }

        private BriefingCard createScoreCard(BriefingPayload payload)
        {
            var accent = payload.ScoreChanges.Count > 0 ? Color4Extensions.FromHex(@"ff66b3") : Color4Extensions.FromHex(@"69d7ff");
            string headline = payload.ScoreChanges.Count == 0
                ? "No top play recalcs detected"
                : $"{payload.ScoreChanges.Count} top {(payload.ScoreChanges.Count == 1 ? "score moved" : "scores moved")}";

            string detail = payload.ScoreChanges.Count == 0
                ? "Your top plays match the last briefing snapshot."
                : string.Join("\n", payload.ScoreChanges.Take(2).Select(c => $"{trim(c.Title, 48)}: {formatPP(c.OldPP)} -> {formatPP(c.NewPP)}"));

            return new BriefingCard(FontAwesome.Solid.Sync, "recalculation watch", headline, detail, accent, payload.ScoreChanges.Count > 0 ? 142 : 126)
            {
                TooltipText = payload.ScoreChanges.Count == 0
                    ? "When PP changes are detected, the changed scores will be listed here."
                    : string.Join("\n", payload.ScoreChanges.Select(c => $"{c.Title}: {formatPP(c.OldPP)} -> {formatPP(c.NewPP)} ({formatSignedPP(c.Delta)})")),
            };
        }

        private BriefingCard createMessageCard(BriefingPayload payload)
        {
            var accent = payload.UnreadMessages.Count > 0 ? Color4Extensions.FromHex(@"ffd36e") : Color4Extensions.FromHex(@"69d7ff");
            string headline = payload.UnreadMessages.Count == 0
                ? "No unread chat pings"
                : $"{payload.UnreadMessages.Count} unread chat {(payload.UnreadMessages.Count == 1 ? "ping" : "pings")}";

            string detail = payload.UnreadMessages.Count == 0
                ? "Nothing urgent from joined chat channels yet."
                : string.Join("\n", payload.UnreadMessages.Take(2).Select(m => $"{m.Sender}: {trim(m.Preview, 54)}"));

            return new BriefingCard(FontAwesome.Solid.Comments, "dojo whispers", headline, detail, accent, payload.UnreadMessages.Count > 0 ? 142 : 126)
            {
                TooltipText = payload.UnreadMessages.Count == 0
                    ? "Open chat to see live channels."
                    : string.Join("\n", payload.UnreadMessages.Select(m => $"#{m.Channel} - {m.Sender}: {m.Preview}")),
            };
        }

        private BriefingCard createRadarCard(BriefingPayload payload)
        {
            var accent = payload.RadarEvents.Count > 0 ? Color4Extensions.FromHex(@"8bffcf") : Color4Extensions.FromHex(@"73b7ff");
            string headline = payload.RadarEvents.Count == 0
                ? payload.RadarFirstSnapshot ? "Dojo radar baseline synced" : "No map radar alerts"
                : $"{payload.RadarEvents.Count} tracked {(payload.RadarEvents.Count == 1 ? "shift" : "shifts")} noticed";

            string detail = payload.RadarEvents.Count == 0
                ? payload.RadarFirstSnapshot
                    ? $"Watching {payload.RadarTrackedCount} map positions from now on."
                    : $"No watched map positions moved since the last briefing ({payload.RadarTrackedCount} tracked)."
                : string.Join("\n", payload.RadarEvents.Take(2).Select(e => $"{trim(e.Title, 48)}: {e.Detail}"));

            return new BriefingCard(FontAwesome.Solid.Crosshairs, "dojo radar", headline, detail, accent, payload.RadarEvents.Count > 0 ? 142 : 126)
            {
                TooltipText = payload.RadarEvents.Count == 0
                    ? "Torii tracks your watched map leaderboard positions server-side and compares them on each briefing."
                    : string.Join("\n", payload.RadarEvents.Select(e => $"{e.Title}: {e.Detail}")),
            };
        }

        private BriefingCard createSyncCard(BriefingPayload payload)
        {
            string variantName = payload.Variant == "pp_dev" ? "pp-dev" : "stable";
            var accent = payload.Variant == "pp_dev" ? Color4Extensions.FromHex(@"73b7ff") : Color4Extensions.FromHex(@"8bffcf");

            return new BriefingCard(FontAwesome.Solid.InfoCircle, "session mode", $"{variantName} profile synced", $"Tracking {payload.Current.TopScores.Count} top plays for future briefings.", accent)
            {
                TooltipText = "This briefing is generated client-side from Torii API responses and local snapshots.",
            };
        }

        private string getRankHeadline(int? previousRank, int? currentRank, out Color4 accent)
        {
            accent = Color4Extensions.FromHex(@"69d7ff");

            if (!previousRank.HasValue || !currentRank.HasValue)
                return "Rank data is warming up";

            int delta = previousRank.Value - currentRank.Value;

            if (delta > 0)
            {
                accent = Color4Extensions.FromHex(@"8bffcf");
                return $"You gained {delta.ToString("N0", CultureInfo.InvariantCulture)} ranks";
            }

            if (delta < 0)
            {
                accent = Color4Extensions.FromHex(@"ff8f9c");
                return $"You lost {Math.Abs(delta).ToString("N0", CultureInfo.InvariantCulture)} ranks";
            }

            return "Your rank held steady";
        }

        private string getPpDelta(double? previousPP, double? currentPP)
        {
            if (!previousPP.HasValue || !currentPP.HasValue)
                return "pp warming up";

            double delta = currentPP.Value - previousPP.Value;
            return $"{formatPP(previousPP.Value)} -> {formatPP(currentPP.Value)} ({formatSignedPP(delta)})";
        }

        private BriefingState loadSnapshotState()
        {
            try
            {
                if (!briefingStorage.Exists(snapshot_filename))
                    return new BriefingState();

                using (var stream = briefingStorage.GetStream(snapshot_filename, FileAccess.Read, FileMode.Open))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd().Deserialize<BriefingState>() ?? new BriefingState();
            }
            catch
            {
                return new BriefingState();
            }
        }

        private void saveSnapshotState(BriefingState state)
        {
            try
            {
                using (var stream = briefingStorage.GetStream(snapshot_filename, FileAccess.Write, FileMode.Create))
                using (var writer = new StreamWriter(stream))
                    writer.Write(state.Serialize());
            }
            catch
            {
                // Briefing snapshots are a convenience feature; never break login if local storage is unavailable.
            }
        }

        protected override void PopIn()
        {
            this.FadeIn(220, Easing.OutQuint);
            panel.ScaleTo(0.965f).Then().ScaleTo(1, 360, Easing.OutQuint);
            panel.MoveToY(18).Then().MoveToY(0, 360, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.FadeOut(180, Easing.OutQuint);
            panel.ScaleTo(0.985f, 180, Easing.OutQuint);
            panel.MoveToY(10, 180, Easing.OutQuint);
        }

        private static double? toDouble(decimal? value) => value.HasValue ? (double)value.Value : null;

        private static string formatRank(int? rank) => rank.HasValue ? $"#{rank.Value.ToString("N0", CultureInfo.InvariantCulture)}" : "unranked";

        private static string formatPP(double pp) => $"{pp:N2}pp";

        private static string formatSignedPP(double pp) => $"{(pp >= 0 ? "+" : string.Empty)}{pp:N2}pp";

        private static string trim(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return $"{text[..Math.Max(0, maxLength - 3)]}...";
        }

        private sealed class PendingBriefing
        {
            private int remainingRequests = 3;

            public readonly APIUser LocalUser;
            public readonly RulesetInfo Ruleset;
            public readonly bool UsePpDev;
            public readonly string SessionKey;

            public APIUser User;
            public List<SoloScoreInfo> TopScores;
            public ToriiBriefingRadarResponse Radar;
            public bool IsComplete => remainingRequests <= 0;

            public PendingBriefing(string sessionKey, APIUser localUser, RulesetInfo ruleset, bool usePpDev)
            {
                SessionKey = sessionKey;
                LocalUser = localUser;
                Ruleset = ruleset;
                UsePpDev = usePpDev;
            }

            public void MarkComplete() => remainingRequests--;
        }

        private sealed class BriefingPayload
        {
            public APIUser User;
            public RulesetInfo Ruleset;
            public string Variant;
            public BriefingSnapshot Current;
            public BriefingSnapshot Previous;
            public List<BriefingScoreChange> ScoreChanges;
            public List<BriefingMessage> UnreadMessages;
            public List<BriefingRadarEvent> RadarEvents;
            public bool RadarFirstSnapshot;
            public int RadarTrackedCount;
        }

        private sealed class BriefingState
        {
            [JsonProperty("users")]
            public Dictionary<string, BriefingSnapshot> Users { get; set; } = new Dictionary<string, BriefingSnapshot>();
        }

        private sealed class BriefingSnapshot
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Ruleset { get; set; }
            public string Variant { get; set; }
            public DateTimeOffset CapturedAt { get; set; }
            public int? GlobalRank { get; set; }
            public int? CountryRank { get; set; }
            public double? PP { get; set; }
            public List<BriefingScoreSnapshot> TopScores { get; set; } = new List<BriefingScoreSnapshot>();
        }

        private sealed class BriefingScoreSnapshot
        {
            public ulong ScoreId { get; set; }
            public string Title { get; set; }
            public string Rank { get; set; }
            public double? PP { get; set; }
            public double Accuracy { get; set; }
        }

        private sealed class BriefingScoreChange
        {
            public string Title { get; set; }
            public double OldPP { get; set; }
            public double NewPP { get; set; }
            public double Delta { get; set; }
        }

        private sealed class BriefingMessage
        {
            public string Sender { get; set; }
            public string Channel { get; set; }
            public string Preview { get; set; }
        }

        private sealed class BriefingRadarEvent
        {
            public string Title { get; set; }
            public string Detail { get; set; }
            public string Severity { get; set; }
        }

        private partial class BriefingCard : CompositeDrawable, IHasTooltip
        {
            public LocalisableString TooltipText { get; set; }

            public BriefingCard(IconUsage icon, string kicker, string headline, string detail, Color4 accent, float height = 126)
            {
                RelativeSizeAxes = Axes.X;
                Height = height;
                Masking = true;
                CornerRadius = 20;
                BorderThickness = 1;
                BorderColour = accent.Opacity(0.28f);
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = accent.Opacity(0.12f),
                    Radius = 14,
                };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex(@"18162d").Opacity(0.92f),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientHorizontal(accent.Opacity(0.08f), Color4.Transparent),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0.018f,
                        Colour = accent,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 22, Vertical = 18 },
                        Children = new Drawable[]
                        {
                            new CircularContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(58),
                                Masking = true,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = accent.Opacity(0.16f),
                                    },
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(22),
                                        Icon = icon,
                                        Colour = accent,
                                    },
                                },
                            },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                X = 76,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding { Right = 78 },
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 5),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = kicker.ToUpperInvariant(),
                                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.Bold),
                                        Colour = accent,
                                    },
                                    new OsuSpriteText
                                    {
                                        Text = headline,
                                        Font = OsuFont.GetFont(size: 22, weight: FontWeight.Bold),
                                    },
                                    new OsuTextFlowContainer(t =>
                                    {
                                        t.Font = OsuFont.GetFont(size: 14.5f, weight: FontWeight.SemiBold);
                                        t.Colour = Color4.White.Opacity(0.62f);
                                    })
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Text = detail,
                                    },
                                },
                            },
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Size = new Vector2(14),
                                Icon = FontAwesome.Solid.InfoCircle,
                                Colour = Color4.White.Opacity(0.32f),
                            },
                        },
                    },
                };
            }
        }

        private partial class BriefingSectionHeader : CompositeDrawable
        {
            public BriefingSectionHeader(string title, string subtitle)
            {
                RelativeSizeAxes = Axes.X;
                Height = 34;
                Margin = new MarginPadding { Top = 4 };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = 1,
                        Colour = Color4.White.Opacity(0.08f),
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Children = new Drawable[]
                        {
                            new BriefingPill(title, Color4Extensions.FromHex(@"69d7ff")),
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = subtitle,
                                Font = OsuFont.GetFont(size: 12.5f, weight: FontWeight.SemiBold),
                                Colour = Color4.White.Opacity(0.42f),
                            },
                        },
                    },
                };
            }
        }

        private partial class BriefingPill : CompositeDrawable
        {
            public BriefingPill(string text, Color4 accent)
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 12;
                Padding = new MarginPadding { Horizontal = 12, Vertical = 5 };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = accent.Opacity(0.12f),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = text.ToUpperInvariant(),
                        Font = OsuFont.GetFont(size: 11, weight: FontWeight.Bold),
                        Colour = accent,
                    },
                };
            }
        }
    }
}
