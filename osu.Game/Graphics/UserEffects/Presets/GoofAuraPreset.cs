// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Goof aura: small chibi leaves slowly hover-drifting around the name with
    /// a soft pastel-green halo. Reads as cute + playful — matches the goofball
    /// energy without being visually loud. Each leaf rotates slowly as it
    /// drifts and pulses subtly in size to feel alive rather than mechanical.
    /// </summary>
    public class GoofAuraPreset : AuraPreset
    {
        public const string ID = "goof-leaves";

        // Pastel-green palette tuned around the GOOF badge colour (#9CE5A0).
        // Three nearby tones so a cluster of leaves doesn't look uniform.
        private static readonly Color4[] leaf_palette =
        {
            new Color4(156, 229, 160, 255), // base GOOF green
            new Color4(190, 240, 175, 255), // lime
            new Color4(120, 210, 145, 255), // deeper sage
        };

        public override string AuraId => ID;

        // Slower & sparser than admin embers — the visual goal is "a couple of
        // leaves drifting around" not "swarm".
        public override double SpawnIntervalMs => 520;
        public override double SpawnJitterMs => 280;
        public override int MaxAlive => 5;

        public override void EmitParticle(Container parent, Vector2 parentSize, Random random)
        {
            // Spawn within the name's bounding box (with a small inset so leaves
            // don't pop in flush against the edge). Vertical range is now ALL
            // inside the box — earlier values let leaves spawn well above/below
            // and made the aura look way too spread out.
            float startX = (float)(0.05 + random.NextDouble() * 0.9) * parentSize.X;
            float startY = (float)(0.1 + random.NextDouble() * 0.8) * parentSize.Y;

            // Drift bounds tightened roughly 3x — leaves still wander gently
            // but stay visually anchored to the username.
            float driftX = (float)((random.NextDouble() - 0.5) * parentSize.X * 0.25f);
            float driftY = (float)((random.NextDouble() - 0.5) * parentSize.Y * 0.5f);

            float size = 5.5f + (float)random.NextDouble() * 3f;
            Color4 colour = leaf_palette[random.Next(leaf_palette.Length)];

            // The leaf glyph itself plus a fainter, larger halo behind it to
            // sell the glow without needing a real blur shader. Halo scaled
            // down (1.4x vs 1.8x) so the glow doesn't bleed too far past edges.
            var halo = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.Leaf,
                Size = new Vector2(size * 1.4f),
                Colour = colour,
                Alpha = 0.16f,
            };

            var leaf = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = FontAwesome.Solid.Leaf,
                Size = new Vector2(size),
                Colour = colour,
                Alpha = 0.85f,
                Rotation = (float)((random.NextDouble() - 0.5) * 60), // varied initial tilt
            };

            // Wrap in a centre-anchored container so we can rotate/scale around
            // the leaf's own midpoint without dragging the position offset.
            var particle = new Container
            {
                AutoSizeAxes = Axes.Both,
                Position = new Vector2(startX, startY),
                Children = new Drawable[] { halo, leaf },
                Alpha = 0,
            };

            parent.Add(particle);

            double lifetime = 1800 + random.NextDouble() * 900;
            float endRotation = leaf.Rotation + (float)((random.NextDouble() - 0.5) * 90);

            // Fade in slow & soft, drift to destination, gently rotate, then fade out.
            // The hover bob is a nested loop on the inner leaf so it bobs INSIDE
            // the drifting envelope — gives a real "floating" feel rather than
            // just translation.
            particle.FadeTo(1f, 320, Easing.OutQuad);
            particle.MoveTo(new Vector2(startX + driftX, startY + driftY), lifetime, Easing.InOutSine);
            leaf.RotateTo(endRotation, lifetime, Easing.InOutSine);

            // Gentle hover bob (1.5-2.5px Y oscillation) layered on top of the
            // drift. Reduced from earlier 3-4px so the bob doesn't add to the
            // visual spread that pushed leaves outside the name area.
            float bob = 1.5f + (float)random.NextDouble();
            leaf.Loop(t => t
                .MoveToOffset(new Vector2(0, -bob), 700, Easing.InOutSine)
                .Then()
                .MoveToOffset(new Vector2(0, bob), 700, Easing.InOutSine));

            // Subtle scale pulse so the leaf feels chibi-alive rather than static.
            leaf.ScaleTo(1.08f, 600, Easing.InOutSine).Then().ScaleTo(1f, 600, Easing.InOutSine).Loop();

            particle.Delay(lifetime - 400).FadeOut(400, Easing.OutQuad).Expire();
        }
    }
}
