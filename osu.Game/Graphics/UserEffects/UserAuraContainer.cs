// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.Online.API.Requests.Responses;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserEffects
{
    /// <summary>
    /// Wraps a username drawable and renders the user's aura (a particle effect
    /// derived from their groups / equipped cosmetic) BEHIND it.
    ///
    /// Auto-sizes to its child so the particle field tracks the username's
    /// bounding box exactly — useful because usernames change width with text.
    ///
    /// If the user has no aura, this renders just the wrapped child with zero
    /// emission overhead (the emitter is swapped out when the user changes,
    /// not just hidden).
    ///
    /// Respects the <see cref="OsuSetting.UserAuraEnabled"/> setting so users
    /// on weaker hardware can disable the global effect.
    /// </summary>
    public partial class UserAuraContainer : Container
    {
        private APIUser? user;
        private readonly Drawable target;

        private ParticleAuraEmitter? emitter;

        // Optional pulsing text-shape glow rendered UNDER the emitter and the
        // target. Only created when the resolved preset opts in via
        // AuraPreset.GlowColour AND the target is a SpriteText we can mirror.
        private TextShapeGlow? textGlow;

        private Bindable<bool> auraEnabled = null!;
        private bool loaded;

        /// <summary>
        /// Wrap <paramref name="target"/> with an aura matching <paramref name="user"/>'s groups.
        /// Use the static <see cref="Wrap"/> helper from call-sites for the cleanest one-liner.
        /// </summary>
        /// <param name="user">The user whose aura (if any) should render behind the target.</param>
        /// <param name="target">The drawable (usually a username SpriteText) to decorate.</param>
        /// <param name="relativeSizeAxes">
        /// Axes the wrapper should size relative to its own parent. Defaults
        /// to <see cref="Axes.None"/> (wrapper auto-sizes both axes to the
        /// target's natural size — correct for free-flowing usernames). Pass
        /// <see cref="Axes.X"/> when wrapping a <c>TruncatingSpriteText</c>
        /// that needs a fixed parent width to know where to truncate; the
        /// wrapper then matches the target's RelativeSizeAxes so the
        /// truncation column stays correct.
        /// </param>
        public UserAuraContainer(APIUser? user, Drawable target, Axes relativeSizeAxes = Axes.None)
        {
            this.user = user;
            this.target = target;

            // Size policy:
            //   - Wrapper takes the target's relative-size axes (so a
            //     RelativeSizeAxes=X target keeps its width relative to the
            //     real parent transitively).
            //   - Auto-size on whichever axes are NOT relative-sized so the
            //     emitter still has a meaningful (non-zero, non-100%) bounds
            //     to spawn particles within on those axes.
            if (relativeSizeAxes != Axes.None)
            {
                RelativeSizeAxes = relativeSizeAxes;
                AutoSizeAxes = Axes.Both & ~relativeSizeAxes;
            }
            else
            {
                AutoSizeAxes = Axes.Both;
            }
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager? config)
        {
            auraEnabled = config?.GetBindable<bool>(OsuSetting.UserAuraEnabled) ?? new Bindable<bool>(true);
            rebuildEmitter();
            // Target is added LAST so the emitter (added inside rebuildEmitter)
            // renders behind it — additive blending then reads as a soft glow
            // around the name without obscuring legibility.
            Add(target);
            loaded = true;
        }

        /// <summary>
        /// Swap the user this container renders an aura for. Safe to call before
        /// or after BDL has run — the emitter is rebuilt accordingly. Useful
        /// when a single header drawable is reused as the active profile changes
        /// (e.g. profile overlay switching between users).
        /// </summary>
        public void SetUser(APIUser? newUser)
        {
            if (ReferenceEquals(newUser, user))
                return;

            user = newUser;
            if (loaded)
                rebuildEmitter();
        }

        private void rebuildEmitter()
        {
            // Tear down both layers so SetUser swaps don't leak old visuals.
            if (emitter != null)
            {
                Remove(emitter, disposeImmediately: true);
                emitter = null;
            }

            if (textGlow != null)
            {
                Remove(textGlow, disposeImmediately: true);
                textGlow = null;
            }

            var preset = AuraRegistry.ResolveForUser(user);
            if (preset == null)
                return;

            // Glow is added FIRST so it sits at the bottom of the z-stack,
            // beneath the emitter (and beneath the username target which we
            // re-front below). Only attaches when (a) preset opts in via
            // GlowColour, and (b) target is a SpriteText we can mirror —
            // otherwise there's nothing to base the text-shape blur on.
            //
            // The glow itself uses RelativeSizeAxes = Both internally
            // (TextShapeGlow sets that in its constructor), so it
            // automatically matches the wrapper's bounds exactly. Anchor +
            // Origin TopLeft pin the glow to the same pixel origin as the
            // wrapped target text (which Wrap resets to TopLeft inside the
            // wrapper). This combination produces structural alignment —
            // the glow's mirror SpriteText draws at exactly the same
            // coordinates as the original, regardless of italic / kerning
            // / spacing quirks. Earlier revisions used Anchor.Centre +
            // BypassAutoSizeAxes which produced visible drift in
            // production surfaces (chat, leaderboard rows) due to layout
            // pass timing differences.
            if (preset.GlowColour is Color4 glowColour && target is SpriteText spriteText)
            {
                Add(textGlow = new TextShapeGlow(spriteText.Text, spriteText.Font, glowColour)
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                });
            }

            Add(emitter = new ParticleAuraEmitter(preset)
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            // After re-adding the emitter, push the existing target back to the
            // front so it draws on top of the new emitter. Without this the
            // emitter (added last) would obscure the username after a SetUser swap.
            if (loaded && target.Parent == this)
                ChangeChildDepth(target, -1);

            applyEnabledState();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            auraEnabled.BindValueChanged(_ => applyEnabledState(), true);
        }

        private void applyEnabledState()
        {
            if (emitter != null)
                emitter.Alpha = auraEnabled.Value ? 1 : 0;
        }

        /// <summary>
        /// Convenience helper: returns the original drawable when the user has
        /// no aura (so we don't pay any wrapping cost), otherwise returns a new
        /// <see cref="UserAuraContainer"/>. Anchor/origin of the original
        /// drawable are MOVED to the wrapper (and reset to TopLeft on the
        /// target) so:
        ///   - external layout sees the wrapper at the same effective position
        ///     as the bare drawable would have been.
        ///   - the target lays out naturally as a top-left child inside the
        ///     auto-sized wrapper, which is the only configuration that plays
        ///     nicely with <c>AutoSizeAxes = Axes.Both</c>. Leaving the target
        ///     at e.g. <c>Anchor.CentreLeft</c> caused the wrapper's auto-size
        ///     calculation to misbehave (and in some panels, throw outright).
        /// </summary>
        public static Drawable Wrap(APIUser? user, Drawable target)
        {
            if (AuraRegistry.ResolveForUser(user) == null)
                return target;

            var anchor = target.Anchor;
            var origin = target.Origin;
            // Pull the target's RelativeSizeAxes onto the wrapper so we
            // preserve any X-relative / Y-relative sizing the layout above
            // expected. Without this, wrapping a TruncatingSpriteText (which
            // is RelativeSizeAxes=X to know how wide to truncate) would
            // collapse to zero width because the wrapper auto-sizes to a
            // child that's trying to be 100% of the wrapper.
            var relativeSizeAxes = target.RelativeSizeAxes;

            // Reset anchor/origin on the inner target so AutoSize on the
            // wrapper sees a top-left-anchored child it can size to
            // predictably. RelativeSizeAxes stays on the target so it can
            // continue to fill the wrapper transitively.
            target.Anchor = Anchor.TopLeft;
            target.Origin = Anchor.TopLeft;

            return new UserAuraContainer(user, target, relativeSizeAxes)
            {
                Anchor = anchor,
                Origin = origin,
            };
        }
    }
}
