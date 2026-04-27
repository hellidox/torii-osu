// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Graphics.UserEffects.Presets;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Graphics.UserEffects
{
    /// <summary>
    /// Central lookup for which <see cref="AuraPreset"/> a given user should render.
    ///
    /// Today: hardcoded group-based mapping (admin/dev/goof). Tomorrow: the user can
    /// equip any preset they've unlocked, and the server sends down their choice in
    /// a <c>equipped_aura</c> field on <c>APIUser</c>. This file is the single point
    /// of change for that migration — every other call-site only knows about the
    /// resolved <see cref="AuraPreset"/>.
    /// </summary>
    public static class AuraRegistry
    {
        // Group identifier (server-side `torii_titles.identifier`) → default aura id.
        // Mirrors `GroupBadge.elite_identifiers`. Keep in sync if new elite groups
        // are added.
        private static readonly Dictionary<string, string> default_aura_for_group = new Dictionary<string, string>
        {
            { "torii-admin", AdminAuraPreset.ID },
            { "torii-dev", AdminAuraPreset.ID },
            { "torii-goof", GoofAuraPreset.ID },
        };

        // Aura id → preset instance. Presets are stateless and shared between users.
        private static readonly Dictionary<string, AuraPreset> presets_by_id = new Dictionary<string, AuraPreset>
        {
            { AdminAuraPreset.ID, new AdminAuraPreset() },
            { GoofAuraPreset.ID, new GoofAuraPreset() },
        };

        /// <summary>
        /// Resolve the aura preset (if any) that should render around
        /// <paramref name="user"/>'s name everywhere their name appears.
        /// Returns null when the user has no qualifying group / equipped aura.
        /// </summary>
        public static AuraPreset? ResolveForUser(APIUser? user)
        {
            if (user?.Groups == null || user.Groups.Length == 0)
                return null;

            // FUTURE: when `user.EquippedAura` lands, prefer that here:
            //   if (!string.IsNullOrEmpty(user.EquippedAura) &&
            //       presets_by_id.TryGetValue(user.EquippedAura, out var equipped))
            //       return equipped;
            // (with a server-side check that the user actually owns the equipped aura)

            foreach (var group in user.Groups)
            {
                if (group.Identifier != null
                    && default_aura_for_group.TryGetValue(group.Identifier, out string? auraId)
                    && presets_by_id.TryGetValue(auraId, out var preset))
                {
                    return preset;
                }
            }

            return null;
        }
    }
}
