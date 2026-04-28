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
    /// rather than its rectangular bounding box.
    ///
    /// Implementation: a <see cref="BufferedContainer"/> with
    /// <see cref="Drawable.RelativeSizeAxes"/> = <c>Both</c> so it always
    /// matches its parent (the <see cref="UserAuraContainer"/>'s
    /// auto-sized text bounds) EXACTLY. A duplicate of the username
    /// <see cref="SpriteText"/> is rendered into the buffer and a small
    /// gaussian blur is applied; because the buffered child is the actual
    /// glyph shapes, the result reads as a soft halo following the letter
    /// outlines.
    ///
    /// Earlier revisions used <see cref="CompositeDrawable.AutoSizeAxes"/>
    /// + <see cref="CompositeDrawable.Padding"/> to grow the buffer slightly
    /// past the text so the blur could fade out without being clipped at
    /// the buffer edge. That produced visible misalignment in production
    /// surfaces (chat, leaderboards, profile header): the glow drifted off
    /// the username because the auto-size growth combined with
    /// <see cref="Drawable.BypassAutoSizeAxes"/> didn't always cancel out
    /// in practice — slight differences in layout pass timing left the
    /// glow's centre N pixels away from the text's centre. Locking to the
    /// parent's bounds via <see cref="Drawable.RelativeSizeAxes"/> removes
    /// that whole class of bug at the cost of the blur being clipped at
    /// the text edges (which actually reads as a tighter, more
    /// "letter-hugging" glow — exactly what users were asking for).
    ///
    /// The mirror <see cref="OsuSpriteText"/> uses TopLeft anchor / origin
    /// so it lands at the same pixel position as the wrapped target text
    /// (which <see cref="UserAuraContainer.Wrap"/> resets to TopLeft inside
    /// the wrapper). Identical font + same coordinate origin = pixel-perfect
    /// alignment regardless of italic / spacing / kerning quirks.
    /// </summary>
    public partial class TextShapeGlow : BufferedContainer
    {
        /// <summary>Alpha at the peak of the breath cycle.</summary>
        public float MaxAlpha { get; init; } = 0.85f;

        /// <summary>Alpha at the trough of the breath cycle.</summary>
        public float MinAlpha { get; init; } = 0.40f;

        /// <summary>Duration of one fade direction (full cycle is 2x this).</summary>
        public double DurationMs { get; init; } = 1500;

        public TextShapeGlow(LocalisableString text, FontUsage font, Color4 colour)
            : base(cachedFrameBuffer: false)
        {
            // Fill the parent (UserAuraContainer) exactly. The wrapper has
            // already auto-sized to the wrapped text, so RelativeSizeAxes
            // = Both makes this glow exactly that size — never bigger,
            // never smaller, never offset.
            RelativeSizeAxes = Axes.Both;

            // Tight blur. The buffer is exactly text-sized, so the
            // gaussian falloff "softens" the letter shapes inside the
            // buffer rather than producing a wide outer halo. Matches the
            // user-facing requirement: "outer glow that hugs the letter
            // shapes" — not a stretched blob.
            BlurSigma = new Vector2(2.5f, 2f);

            BackgroundColour = new Color4(0, 0, 0, 0);
            Alpha = 0;

            // Mirror SpriteText pinned to TopLeft so it lands at the same
            // pixel position as the wrapped target (which Wrap also resets
            // to TopLeft). With RelativeSizeAxes=Both above plus this
            // TopLeft anchor here, the mirror text overlaps the wrapped
            // text 1:1 — alignment is structural rather than approximate.
            //
            // OsuSpriteText (not raw SpriteText) per the project's banned-
            // API analyzer.
            Child = new OsuSpriteText
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Text = text,
                Font = font,
                Colour = colour,
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Snap to the floor first so the breath cycle's fade-IN is the
            // user's first impression rather than a flash of full alpha.
            Alpha = MinAlpha;

            this.Loop(t => t
                .FadeTo(MaxAlpha, DurationMs, Easing.InOutSine)
                .Then()
                .FadeTo(MinAlpha, DurationMs, Easing.InOutSine));
        }
    }
}
