// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Base class for the supporter / loyalty-tier hearts auras.
    ///
    /// All four variants (pink supporter, bronze, silver, gold) share the
    /// same particle motion — small FaHeart drifting up with a gentle
    /// heartbeat pulse — and only differ in their colour palette,
    /// AuraId, owning groups, and DefaultPriority. Concrete subclasses
    /// supply just those four pieces; the emit logic stays here so any
    /// tuning we do (cadence, drift bounds, halo scale) lands once for
    /// the whole tier family.
    /// </summary>
    public abstract class SupporterHeartsAuraBase : AuraPreset
    {
        protected abstract Color4[] Palette { get; }

        public override double SpawnIntervalMs => 540;
        public override double SpawnJitterMs => 240;
        public override int MaxAlive => 6;

        public override void EmitParticle(Container parent, Vector2 parentSize, Random random)
        {
            float startX = (float)(0.08 + random.NextDouble() * 0.84) * parentSize.X;
            float startY = parentSize.Y * (0.55f + (float)random.NextDouble() * 0.4f);

            // Hearts rise mostly straight up with a tiny side-to-side wobble.
            // No drama — supporter hearts are a quiet thank-you, not a flex.
            float driftX = (float)((random.NextDouble() - 0.5) * parentSize.X * 0.18f);
            float driftY = -parentSize.Y * (0.7f + (float)random.NextDouble() * 0.3f);

            float size = 5f + (float)random.NextDouble() * 2.5f;
            Color4 colour = Palette[random.Next(Palette.Length)];

            var halo = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.Heart,
                Size = new Vector2(size * 1.5f),
                Colour = colour,
                Alpha = 0.18f,
            };

            var heart = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.Heart,
                Size = new Vector2(size),
                Colour = colour,
                Alpha = 0.85f,
            };

            var particle = new Container
            {
                AutoSizeAxes = Axes.Both,
                Position = new Vector2(startX, startY),
                Children = new Drawable[] { halo, heart },
                Alpha = 0,
            };

            parent.Add(particle);

            double lifetime = 1500 + random.NextDouble() * 500;
            particle.FadeTo(1f, 240, Easing.OutQuad);
            particle.MoveTo(new Vector2(startX + driftX, startY + driftY), lifetime, Easing.OutSine);

            // Gentle "heartbeat" pulse on the inner heart only.
            heart.ScaleTo(1.15f, 700, Easing.OutQuad).Then().ScaleTo(1f, 700, Easing.InQuad).Loop();

            particle.Delay(lifetime - 340).FadeOut(340, Easing.OutQuad).Expire();
        }
    }

    /// <summary>
    /// Default supporter aura — pink hearts. Granted to anyone who has
    /// supported for 1+ months (the lowest tier). Mirrors the osu!supporter
    /// heart icon so it reads instantly.
    /// </summary>
    public class SupporterAuraPreset : SupporterHeartsAuraBase
    {
        public const string ID = "supporter-hearts";

        // Pinks tuned around the existing osu! supporter pink so the aura
        // reads as "this person supports the project" without needing a label.
        private static readonly Color4[] PINK_PALETTE =
        {
            new Color4(255, 130, 200, 255), // pink
            new Color4(255, 165, 215, 255), // rose
            new Color4(230, 100, 175, 255), // deep pink
        };

        protected override Color4[] Palette => PINK_PALETTE;

        public override string AuraId => ID;

        // All four supporter tier groups grant this aura — gold supporters
        // can pick the basic pink hearts if they want, etc.
        public override IReadOnlyList<string> OwningGroupIdentifiers { get; } = new[]
        {
            "torii-supporter",
            "torii-supporter-bronze",
            "torii-supporter-silver",
            "torii-supporter-gold",
        };

        public override int DefaultPriority => 40;
    }
}
