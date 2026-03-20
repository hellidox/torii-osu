// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class OsuConfigManagerStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.OsuConfigManager";

        /// <summary>
        /// "lazer-api.shikkesora.com"
        /// </summary>
        public static LocalisableString GamerherzDdnsNet => new TranslatableString(getKey(@"gamerherz_ddns_net"), @"lazer-api.shikkesora.com");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
