// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osuTK;

namespace osu.Game.Overlays.Profile.Header.Components
{
    public partial class GroupBadge : Container, IHasTooltip
    {
        public LocalisableString TooltipText { get; private set; }

        public int TextSize { get; set; } = 12;

        private readonly APIUserGroup group;

        // Torii-specific
        private readonly bool isToriiGroup;
        private readonly bool isEliteGroup;
        private Box? elitePulseLayer;

        private static readonly string[] elite_identifiers = { "torii-admin", "torii-dev" };

        public GroupBadge(APIUserGroup group)
        {
            this.group = group;
            isToriiGroup = group.Identifier.StartsWith("torii-", StringComparison.Ordinal);
            isEliteGroup = elite_identifiers.Contains(group.Identifier);

            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 8;

            TooltipText = group.Name;

            if (group.IsProbationary)
            {
                Alpha = 0.6f;
            }
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider? colourProvider, RulesetStore rulesets)
        {
            FillFlowContainer innerContainer;

            var groupColour = Color4Extensions.FromHex(group.Colour ?? Colour4.White.ToHex());

            // Base dark background (matches original behaviour)
            AddInternal(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = colourProvider?.Background6 ?? Colour4.Black,
                // Normal badges background opacity is 75%, probationary is full opacity as the whole badge gets a bit transparent
                // Goal is to match osu-web so this is the most accurate it can be, its a bit scuffed but it is what it is
                // Source: https://github.com/ppy/osu-web/blob/master/resources/css/bem/user-group-badge.less#L50
                Alpha = group.IsProbationary ? 1 : 0.75f,
            });

            // Torii groups: subtle tinted background overlay
            if (isToriiGroup)
            {
                AddInternal(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = groupColour,
                    Alpha = 0.10f,
                });
            }

            // Elite Torii (admin / dev): additive glow layer that pulses
            if (isEliteGroup)
            {
                AddInternal(elitePulseLayer = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = groupColour,
                    Alpha = 0f,
                    Blending = BlendingParameters.Additive,
                });
            }

            // Badge text — GlowingSpriteText for elite, plain OsuSpriteText otherwise
            Drawable textDrawable;

            if (isEliteGroup)
            {
                textDrawable = new GlowingSpriteText
                {
                    Text = group.ShortName,
                    Font = OsuFont.GetFont(size: TextSize, weight: FontWeight.Bold, italics: true),
                    TextColour = groupColour,
                    GlowColour = groupColour.Opacity(0.75f),
                };
            }
            else
            {
                textDrawable = new OsuSpriteText
                {
                    Text = group.ShortName,
                    Colour = groupColour,
                    Shadow = false,
                    Font = OsuFont.GetFont(size: TextSize, weight: FontWeight.Bold, italics: true),
                };
            }

            AddInternal(innerContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Padding = new MarginPadding { Vertical = 2, Horizontal = 10 },
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children = new[] { textDrawable },
            });

            if (group.Playmodes?.Length > 0)
            {
                innerContainer.AddRange(group.Playmodes.Select(p =>
                        (rulesets.GetRuleset(p)?.CreateInstance().CreateIcon() ?? new SpriteIcon { Icon = FontAwesome.Regular.QuestionCircle }).With(icon =>
                        {
                            icon.Size = new Vector2(TextSize - 1);
                        })).ToList()
                );

                var badgeModesList = group.Playmodes.Select(p => rulesets.GetRuleset(p)?.Name).ToList();

                string modesDisplay = string.Join(", ", badgeModesList);
                TooltipText += $" ({modesDisplay})";
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Start pulsing glow for elite Torii groups
            if (isEliteGroup && elitePulseLayer != null)
            {
                elitePulseLayer.Loop(t => t
                    .FadeTo(0.22f, 1200, Easing.InOutSine)
                    .Then()
                    .FadeTo(0f, 1200, Easing.InOutSine));
            }
        }
    }
}
