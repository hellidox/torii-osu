// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Game.Graphics.UserEffects
{
    /// <summary>
    /// A particle-aura definition that can be rendered around a username.
    ///
    /// A preset declares its tuning (spawn rate, particle cap) and is responsible
    /// for emitting its own particle drawables into a host container — this lets
    /// each preset have its own visual identity (admin embers vs goof leaves vs
    /// future cosmetics) without needing a one-size-fits-all particle type.
    ///
    /// Presets are looked up by a stable string id (<see cref="AuraId"/>) so that
    /// in the future a user-equipable cosmetic field on <c>APIUser</c> can drive
    /// preset selection directly. For now, presets are mapped to user groups via
    /// <see cref="AuraRegistry.ResolveForUser"/>.
    /// </summary>
    public abstract class AuraPreset
    {
        /// <summary>
        /// Stable identifier for this aura (e.g. "admin-embers", "goof-leaves").
        /// Used as the key in <see cref="AuraRegistry"/> and — eventually — as the
        /// value stored in <c>APIUser.EquippedAura</c> when cosmetic-equipping ships.
        /// </summary>
        public abstract string AuraId { get; }

        /// <summary>
        /// Average milliseconds between particle spawns. Combined with
        /// <see cref="SpawnJitterMs"/> for natural-looking emission.
        /// </summary>
        public virtual double SpawnIntervalMs => 280;

        /// <summary>
        /// Random extra delay (0..N) added to each spawn.
        /// </summary>
        public virtual double SpawnJitterMs => 180;

        /// <summary>
        /// Hard cap on simultaneously-alive particles. Keeps the worst case
        /// bounded so a chat full of elite users doesn't tank framerate.
        /// </summary>
        public virtual int MaxAlive => 10;

        /// <summary>
        /// Optional persistent layer (added once, never expires) drawn underneath
        /// the particle stream. Useful for pulsing halos, tint overlays, or any
        /// always-on effect that defines the preset's "base mood". Returns null
        /// when the preset only uses transient particles.
        /// </summary>
        public virtual Drawable? CreateBackground() => null;

        /// <summary>
        /// Spawns one particle into <paramref name="parent"/>, sized/positioned
        /// relative to <paramref name="parentSize"/> (the bounding box of the
        /// username being decorated). The spawned drawable is responsible for
        /// expiring itself.
        /// </summary>
        public abstract void EmitParticle(Container parent, Vector2 parentSize, Random random);
    }
}
