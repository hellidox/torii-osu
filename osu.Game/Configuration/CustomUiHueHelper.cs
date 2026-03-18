// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;

namespace osu.Game.Configuration
{
    public enum CustomUiHueScope
    {
        Menu,
        Overlays,
        SettingsPanel,
    }

    public static class CustomUiHueHelper
    {
        public static int ResolveHue(OsuConfigManager config, int fallbackHue, CustomUiHueScope scope)
        {
            return ResolveHue(
                config.Get<bool>(OsuSetting.CustomUIHueEnabled),
                config.Get<float>(OsuSetting.CustomUIHue),
                config.Get<bool>(OsuSetting.CustomUIHueApplyToMenu),
                config.Get<bool>(OsuSetting.CustomUIHueApplyToOverlays),
                config.Get<bool>(OsuSetting.CustomUIHueApplyToSettingsPanel),
                fallbackHue,
                scope);
        }

        public static int ResolveHue(
            bool customHueEnabled,
            float customHue,
            bool applyToMenu,
            bool applyToOverlays,
            bool applyToSettingsPanel,
            int fallbackHue,
            CustomUiHueScope scope)
        {
            if (!customHueEnabled)
                return normaliseHue(fallbackHue);

            bool scopeEnabled = scope switch
            {
                CustomUiHueScope.Menu => applyToMenu,
                CustomUiHueScope.Overlays => applyToOverlays,
                CustomUiHueScope.SettingsPanel => applyToSettingsPanel,
                _ => false,
            };

            return scopeEnabled ? normaliseHue(customHue) : normaliseHue(fallbackHue);
        }

        /// <summary>
        /// Creates a binding that keeps <paramref name="applyHue"/> updated with the resolved hue for the requested scope.
        /// </summary>
        public static IDisposable BindHue(OsuConfigManager config, int fallbackHue, CustomUiHueScope scope, Action<int> applyHue)
            => new CustomUiHueBinding(config, fallbackHue, scope, applyHue);

        private sealed class CustomUiHueBinding : IDisposable
        {
            private readonly Bindable<bool> customHueEnabled;
            private readonly Bindable<float> customHue;
            private readonly Bindable<bool> applyToMenu;
            private readonly Bindable<bool> applyToOverlays;
            private readonly Bindable<bool> applyToSettingsPanel;

            private readonly int fallbackHue;
            private readonly CustomUiHueScope scope;
            private readonly Action<int> applyHue;

            public CustomUiHueBinding(OsuConfigManager config, int fallbackHue, CustomUiHueScope scope, Action<int> applyHue)
            {
                this.fallbackHue = fallbackHue;
                this.scope = scope;
                this.applyHue = applyHue;

                customHueEnabled = config.GetBindable<bool>(OsuSetting.CustomUIHueEnabled);
                customHue = config.GetBindable<float>(OsuSetting.CustomUIHue);
                applyToMenu = config.GetBindable<bool>(OsuSetting.CustomUIHueApplyToMenu);
                applyToOverlays = config.GetBindable<bool>(OsuSetting.CustomUIHueApplyToOverlays);
                applyToSettingsPanel = config.GetBindable<bool>(OsuSetting.CustomUIHueApplyToSettingsPanel);

                customHueEnabled.BindValueChanged(_ => update());
                customHue.BindValueChanged(_ => update());
                applyToMenu.BindValueChanged(_ => update());
                applyToOverlays.BindValueChanged(_ => update());
                applyToSettingsPanel.BindValueChanged(_ => update(), true);
            }

            private void update()
            {
                applyHue(ResolveHue(
                    customHueEnabled.Value,
                    customHue.Value,
                    applyToMenu.Value,
                    applyToOverlays.Value,
                    applyToSettingsPanel.Value,
                    fallbackHue,
                    scope));
            }

            public void Dispose()
            {
                customHueEnabled.UnbindAll();
                customHue.UnbindAll();
                applyToMenu.UnbindAll();
                applyToOverlays.UnbindAll();
                applyToSettingsPanel.UnbindAll();
            }
        }

        private static int normaliseHue(float hue)
        {
            int rounded = (int)MathF.Round(hue);
            int normalised = rounded % 360;

            if (normalised < 0)
                normalised += 360;

            return normalised;
        }
    }
}
