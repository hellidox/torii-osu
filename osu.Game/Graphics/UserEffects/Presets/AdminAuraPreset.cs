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
    /// Admin / Dev aura: hot embers rising upward with the occasional brighter
    /// "static charge" flash. Reads as authority + heat without being noisy —
    /// each particle is a small additive ember + a slightly bigger glow halo.
    /// Color palette mirrors the red used for the admin name everywhere.
    /// </summary>
    public class AdminAuraPreset : AuraPreset
    {
        public const string ID = "admin-embers";

        // Warm red palette tuned to match the existing admin name colour. Three
        // shades so individual embers feel distinct rather than monochrome.
        private static readonly Color4[] ember_palette =
        {
            new Color4(255, 90, 76, 255),   // bright cherry
            new Color4(255, 140, 80, 255),  // amber-orange
            new Color4(255, 60, 40, 255),   // deep red
        };

        public override string AuraId => ID;

        public override double SpawnIntervalMs => 200;
        public override double SpawnJitterMs => 140;
        public override int MaxAlive => 12;

        public override void EmitParticle(Container parent, Vector2 parentSize, Random random)
        {
            // Spawn from a band along the bottom edge of the username so embers
            // appear to lift OFF the text rather than pop in around it.
            float startX = (float)(random.NextDouble() * parentSize.X);
            float startY = parentSize.Y * (0.6f + (float)random.NextDouble() * 0.4f);

            // Drift upward + a small horizontal sway so columns of embers don't
            // line up. Distance is roughly 1.2x the username height.
            float driftX = (float)((random.NextDouble() - 0.5) * parentSize.X * 0.4f);
            float driftY = -(parentSize.Y * 1.2f + (float)random.NextDouble() * 8f);

            float startSize = 3.5f + (float)random.NextDouble() * 2.5f;
            float endSize = startSize * (0.4f + (float)random.NextDouble() * 0.3f);

            Color4 colour = ember_palette[random.Next(ember_palette.Length)];

            // Small chance of a "spark" — brighter, faster, shorter-lived ember.
            // Keeps the aura from looking like a uniform conveyor belt.
            bool isSpark = random.NextDouble() < 0.18;
            if (isSpark)
            {
                startSize *= 1.6f;
                endSize *= 0.6f;
                colour = new Color4(255, 220, 180, 255);
            }

            double lifetime = isSpark ? 700 : 1100 + random.NextDouble() * 500;

            // Each particle is a tiny container holding an ember "core" plus a
            // softer halo at 1.8x size — additive blending fakes a glow without
            // needing a real blur filter.
            var halo = new Circle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(startSize * 2.0f),
                Colour = colour,
                Alpha = 0.35f,
            };

            var core = new Circle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(startSize),
                Colour = colour,
                Alpha = 0.9f,
            };

            var particle = new Container
            {
                AutoSizeAxes = Axes.Both,
                Position = new Vector2(startX, startY),
                Children = new Drawable[] { halo, core },
                Alpha = 0,
            };

            parent.Add(particle);

            // Animation: fade in fast, drift up while shrinking and fading out.
            particle.FadeTo(1f, 120, Easing.OutQuad);
            particle.MoveTo(new Vector2(startX + driftX, startY + driftY), lifetime, Easing.OutQuad);
            particle.ScaleTo(endSize / startSize, lifetime, Easing.OutSine);
            particle.Delay(lifetime - 200).FadeOut(200, Easing.InQuad).Expire();
        }
    }
}
