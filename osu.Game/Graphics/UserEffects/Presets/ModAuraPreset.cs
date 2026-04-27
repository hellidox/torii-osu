// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Mod aura: small gold shield glyphs orbiting the name slowly with a
    /// gentle pulse. Reads as community-protector / authority-but-friendly,
    /// distinct from admin's heat — a moderator vibe rather than fire-and-power.
    /// Fewer, larger particles than admin so it feels deliberate / steady.
    /// </summary>
    public class ModAuraPreset : AuraPreset
    {
        public const string ID = "mod-shields";

        // Warm gold palette. Two tones so highlights and bases differ slightly.
        private static readonly Color4 shield_gold  = new Color4(255, 210, 110, 255);
        private static readonly Color4 shield_amber = new Color4(255, 175, 70, 255);
        private static readonly Color4 halo_gold    = new Color4(255, 195, 90, 255);

        public override string AuraId => ID;

        // Slower spawn rate — moderation is about presence, not noise.
        public override double SpawnIntervalMs => 600;
        public override double SpawnJitterMs => 250;
        public override int MaxAlive => 5;

        public override Drawable? CreateBackground()
        {
            var halo = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = halo_gold,
                Alpha = 0,
                Scale = new Vector2(1.2f, 1.5f),
            };

            halo.Loop(t => t
                .FadeTo(0.12f, 1700, Easing.InOutSine)
                .Then()
                .FadeTo(0.03f, 1700, Easing.InOutSine));

            return halo;
        }

        public override void EmitParticle(Container parent, Vector2 parentSize, Random random)
        {
            // Spawn around the name with a slight bias toward the sides — gives
            // the impression of shields orbiting the username rather than
            // rising columns (which is admin/dev's character).
            float startX = (float)(0.05 + random.NextDouble() * 0.9) * parentSize.X;
            float startY = (float)(0.15 + random.NextDouble() * 0.7) * parentSize.Y;

            // Drift sideways more than vertically — orbital feel.
            float driftX = (float)((random.NextDouble() - 0.5) * parentSize.X * 0.3f);
            float driftY = (float)((random.NextDouble() - 0.5) * parentSize.Y * 0.3f);

            float size = 6f + (float)random.NextDouble() * 2.5f;
            Color4 colour = random.NextDouble() < 0.5 ? shield_gold : shield_amber;

            var halo = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.ShieldAlt,
                Size = new Vector2(size * 1.5f),
                Colour = colour,
                Alpha = 0.18f,
            };

            var shield = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.ShieldAlt,
                Size = new Vector2(size),
                Colour = colour,
                Alpha = 0.85f,
            };

            var particle = new Container
            {
                AutoSizeAxes = Axes.Both,
                Position = new Vector2(startX, startY),
                Children = new Drawable[] { halo, shield },
                Alpha = 0,
            };

            parent.Add(particle);

            double lifetime = 1600 + random.NextDouble() * 600;
            particle.FadeTo(1f, 280, Easing.OutQuad);
            particle.MoveTo(new Vector2(startX + driftX, startY + driftY), lifetime, Easing.InOutSine);

            // Subtle scale pulse to feel "alive" — same trick as goof leaves.
            shield.ScaleTo(1.1f, 800, Easing.InOutSine).Then().ScaleTo(1f, 800, Easing.InOutSine).Loop();

            particle.Delay(lifetime - 360).FadeOut(360, Easing.OutQuad).Expire();
        }
    }
}
