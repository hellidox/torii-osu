// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects
{
    /// <summary>
    /// A pulsing soft glow that hugs the actual letter shapes of a username
    /// rather than its rectangular bounding box. Replaces the previous
    /// <see cref="PulsingHalo"/> (a solid pulsing <c>Box</c>) which read as
    /// a visible rectangle behind the text — exactly what users complained
    /// about.
    ///
    /// Implementation: a <see cref="BufferedContainer"/> renders a duplicate
    /// of the username text into an offscreen buffer, then applies a
    /// gaussian blur (<see cref="BufferedContainer.BlurSigma"/>) before
    /// compositing back. Because the buffered child is the text glyphs
    /// themselves, the blurred result follows the letter outlines rather
    /// than producing a square halo. The padding around the text is what
    /// gives the blur room to bleed outward without being clipped at the
    /// buffer edge.
    ///
    /// Each preset opts in by setting <see cref="AuraPreset.GlowColour"/>;
    /// <see cref="UserAuraContainer"/> instantiates this when the wrapped
    /// target is a <see cref="SpriteText"/>-derived drawable so we can
    /// mirror its text and font.
    ///
    /// Performance: this allocates one render target per glow, which is
    /// non-trivial. Only attach it where a single user is decorated (e.g.
    /// profile header, current chat row); avoid in long lists of users
    /// rendering simultaneously, or set <see cref="BufferedContainer.RedrawOnScale"/>
    /// false to keep the buffer cached across resizes.
    /// </summary>
    public partial class TextShapeGlow : BufferedContainer
    {
        /// <summary>Alpha at the peak of the breath cycle.</summary>
        public float MaxAlpha { get; init; } = 0.55f;

        /// <summary>Alpha at the trough of the breath cycle.</summary>
        public float MinAlpha { get; init; } = 0.20f;

        /// <summary>Duration of one fade direction (full cycle is 2x this).</summary>
        public double DurationMs { get; init; } = 1500;

        public TextShapeGlow(LocalisableString text, FontUsage font, Color4 colour)
            : base(cachedFrameBuffer: false)
        {
            // Auto-size to the text (via the SpriteText child) plus the padding
            // we add — the padding is what gives blur kernel room to fade out
            // before hitting the buffer edge.
            AutoSizeAxes = Axes.Both;

            // Generous padding for the blur falloff. ~3x the larger BlurSigma
            // axis so the gaussian tail is fully resident inside the buffer
            // and doesn't get clipped to a hard edge.
            Padding = new MarginPadding(24);

            // Asymmetric blur — a touch wider than tall so the glow reads as a
            // soft horizontal smear that follows reading direction, rather
            // than a perfect circular pillow.
            BlurSigma = new Vector2(8f, 6f);

            // Slightly desaturated/dim base alpha. The pulse animates on top of
            // this; we don't want full opacity ever.
            Alpha = 0;

            // Background colour stays transparent — only the blurred text
            // glyphs contribute pixel data, and they are coloured by the
            // child's Colour below.
            BackgroundColour = new Color4(0, 0, 0, 0);

            // osu! bans raw SpriteText via analyzer (see RS0030); use the
            // project-standard OsuSpriteText subclass which sets the default
            // font + theme integration. The glyph metrics are identical for
            // mirroring purposes.
            Child = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = text,
                Font = font,
                Colour = colour,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Same Loop pattern as PulsingHalo — set Alpha first so we never
            // flash at full opacity for a frame before the loop kicks in, and
            // build the loop here (NOT in the constructor) because the Loop
            // helper reads Transformable.Time which is null on a freshly-
            // constructed drawable.
            Alpha = MinAlpha;

            this.Loop(t => t
                .FadeTo(MaxAlpha, DurationMs, Easing.InOutSine)
                .Then()
                .FadeTo(MinAlpha, DurationMs, Easing.InOutSine));
        }
    }
}
