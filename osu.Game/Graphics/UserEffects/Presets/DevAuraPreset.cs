// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects.Presets
{
    /// <summary>
    /// Dev aura: rising "data bits" — small cyan squares with the occasional
    /// angle-bracket glyph — that look like syntax tokens floating up. Reads
    /// as code/build/tech without copying the admin's heat motif. Tighter and
    /// more orderly than admin: dev = precise, admin = chaotic.
    /// </summary>
    public class DevAuraPreset : AuraPreset
    {
        public const string ID = "dev-bits";

        // Cool palette: electric cyan + light blue. Three nearby tones so a
        // burst of bits doesn't read as monochrome.
        private static readonly Color4 bit_cyan   = new Color4(120, 220, 255, 255);
        private static readonly Color4 bit_pale   = new Color4(180, 240, 255, 255);
        private static readonly Color4 bit_deep   = new Color4(80,  170, 230, 255);

        public override string AuraId => ID;

        public override IReadOnlyList<string> OwningGroupIdentifiers { get; } = new[] { "torii-dev" };

        public override int DefaultPriority => 10;

        public override double SpawnIntervalMs => 240;
        public override double SpawnJitterMs => 140;
        public override int MaxAlive => 8;

        // Faint cyan halo, slower pulse than admin — dev = "calm focus" vs
        // admin's "barely contained heat".
        public override Drawable? CreateBackground() => new PulsingHalo
        {
            Colour = bit_cyan,
            Scale = new Vector2(1.2f, 1.5f),
            MaxAlpha = 0.12f,
            MinAlpha = 0.03f,
            DurationMs = 1800,
        };

        public override void EmitParticle(Container parent, Vector2 parentSize, Random random)
        {
            // 80% data bit (small square), 20% bracket glyph — keeps the
            // "syntax floating up" character with a rare iconographic accent.
            if (random.NextDouble() < 0.80)
                emitBit(parent, parentSize, random);
            else
                emitBracket(parent, parentSize, random);
        }

        private void emitBit(Container parent, Vector2 parentSize, Random random)
        {
            float startX = (float)(0.05 + random.NextDouble() * 0.9) * parentSize.X;
            float startY = parentSize.Y * (0.6f + (float)random.NextDouble() * 0.35f);

            // Mostly straight up. Smaller lateral drift than admin embers
            // because dev "data bits" should look orderly / column-aligned.
            float driftX = (float)((random.NextDouble() - 0.5) * parentSize.X * 0.08f);
            float driftY = -parentSize.Y * (0.6f + (float)random.NextDouble() * 0.3f);

            float size = 2.2f + (float)random.NextDouble() * 1.5f;

            Color4 colour = random.NextDouble() < 0.5
                ? bit_cyan
                : (random.NextDouble() < 0.5 ? bit_pale : bit_deep);

            var bit = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(size),
                Colour = colour,
                Alpha = 0,
                Position = new Vector2(startX, startY),
            };

            parent.Add(bit);

            double lifetime = 800 + random.NextDouble() * 400;
            bit.FadeTo(0.95f, 100, Easing.OutQuad);
            bit.MoveTo(new Vector2(startX + driftX, startY + driftY), lifetime, Easing.OutCubic);
            bit.RotateTo(45f, lifetime, Easing.OutSine); // diamond as it rises
            bit.Delay(lifetime - 200).FadeOut(200, Easing.InQuad).Expire();
        }

        private void emitBracket(Container parent, Vector2 parentSize, Random random)
        {
            float startX = (float)(0.1 + random.NextDouble() * 0.8) * parentSize.X;
            float startY = parentSize.Y * (0.55f + (float)random.NextDouble() * 0.35f);

            float driftY = -parentSize.Y * (0.7f + (float)random.NextDouble() * 0.2f);
            float size = 4.5f + (float)random.NextDouble() * 2f;

            // Pick < or > based on a coin flip — gives the impression of
            // syntax tokens rising rather than identical glyphs.
            var bracket = new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Icon = random.NextDouble() < 0.5 ? FontAwesome.Solid.LessThan : FontAwesome.Solid.GreaterThan,
                Size = new Vector2(size),
                Colour = bit_pale,
                Alpha = 0,
                Position = new Vector2(startX, startY),
            };

            parent.Add(bracket);

            double lifetime = 900 + random.NextDouble() * 300;
            bracket.FadeTo(0.85f, 120, Easing.OutQuad);
            bracket.MoveTo(new Vector2(startX, startY + driftY), lifetime, Easing.OutSine);
            bracket.ScaleTo(0.6f, lifetime, Easing.OutQuad);
            bracket.Delay(lifetime - 220).FadeOut(220, Easing.InQuad).Expire();
        }
    }
}
