// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Settings.Sections.Torii
{
    public partial class ToriiExperimentalSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Alpha Features";

        private Bindable<bool> alphaToolbarUnlocked = null!;
        private Bindable<bool> alphaToolbarUse = null!;
        private Bindable<bool> alphaStableSongSelectEnabled = null!;

        private readonly Bindable<SettingsNote.Data?> alphaToolbarNote = new Bindable<SettingsNote.Data?>();

        private Container codeInputContainer = null!;
        private OsuTextBox codeBox = null!;
        private Box codeInputGlow = null!;
        private OsuSpriteText statusText = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            alphaToolbarUnlocked = config.GetBindable<bool>(OsuSetting.AlphaToolbarEnabled);
            alphaToolbarUse = config.GetBindable<bool>(OsuSetting.AlphaToolbarUse);
            alphaStableSongSelectEnabled = config.GetBindable<bool>(OsuSetting.AlphaStableSongSelectEnabled);


            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Use alpha navbar style",
                    Current = alphaToolbarUse,
                })
                {
                    Keywords = new[] { "torii", "navbar", "alpha", "experimental" },
                    Note = { BindTarget = alphaToolbarNote },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Use stable song select (alpha)",
                    Current = alphaStableSongSelectEnabled,
                })
                {
                    Keywords = new[] { "torii", "stable", "song", "select", "legacy", "alpha" },
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(6),
                    Padding = SettingsPanel.CONTENT_PADDING,
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = "Access",
                            Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold),
                            Colour = new Color4(220, 220, 220, 255),
                        },
                        codeInputContainer = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 36,
                            Masking = true,
                            CornerRadius = 6,
                            CornerExponent = 3f,
                            Children = new Drawable[]
                            {
                                codeBox = new OsuTextBox
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 36,
                                    PlaceholderText = "Enter access token",
                                },
                                codeInputGlow = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                },
                            }
                        },
                        statusText = new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(size: 11),
                            Text = string.Empty,
                            Colour = new Color4(180, 180, 180, 255),
                        },
                    },
                },
            };

            codeBox.OnCommit += (_, __) => applyCode(codeBox.Current.Value);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            alphaToolbarUnlocked.BindValueChanged(v =>
            {
                if (!v.NewValue)
                {
                    // Ensure value can be reset before disabling the control.
                    alphaToolbarUse.Disabled = false;
                    alphaToolbarUse.Value = false;
                    alphaToolbarUse.Disabled = true;
                    alphaToolbarNote.Value = new SettingsNote.Data("Locked.", SettingsNote.Type.Warning);
                }
                else
                {
                    alphaToolbarUse.Disabled = false;
                    alphaToolbarNote.Value = new SettingsNote.Data("Unlocked. Re-open main menu after changing this.", SettingsNote.Type.Informational);
                }
            }, true);
        }

        private void applyCode(string rawCode)
        {
            string code = rawCode.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(code))
                return;

            string message;
            bool success = true;

            switch (code)
            {
                case "torii-alpha-navbar":
                case "alpha-navbar":
                    alphaToolbarUnlocked.Value = true;
                    message = "Alpha navbar unlocked.";
                    break;

                case "alpha-navbar-on":
                    alphaToolbarUnlocked.Value = true;
                    alphaToolbarUse.Value = true;
                    message = "Alpha navbar unlocked and enabled.";
                    break;

                case "alpha-navbar-off":
                    if (!alphaToolbarUse.Disabled)
                    {
                        alphaToolbarUse.Value = false;
                        message = "Alpha navbar disabled.";
                    }
                    else
                    {
                        message = "Alpha navbar is locked.";
                    }
                    break;

                case "torii-alpha-stableselect":
                case "alpha-stableselect":
                    alphaStableSongSelectEnabled.Value = true;
                    message = "Stable song select alpha enabled.";
                    break;

                case "alpha-stableselect-off":
                    alphaStableSongSelectEnabled.Value = false;
                    message = "Stable song select alpha disabled.";
                    break;

                case "torii-alpha-all":
                case "alpha-all":
                    alphaToolbarUnlocked.Value = true;
                    alphaToolbarUse.Value = true;
                    alphaStableSongSelectEnabled.Value = true;
                    message = "All alpha features enabled.";
                    break;

                default:
                    success = false;
                    message = "Invalid code.";
                    break;
            }

            statusText.Text = message;
            statusText.Colour = success ? new Color4(129, 242, 145, 255) : new Color4(255, 165, 120, 255);
            playCodeInputFeedback(success);
            codeBox.Current.Value = string.Empty;
        }

        private void playCodeInputFeedback(bool success)
        {
            codeInputContainer.ClearTransforms();
            codeInputContainer.MoveToX(-3, 25, Easing.OutSine)
                              .Then()
                              .MoveToX(3, 50, Easing.OutSine)
                              .Then()
                              .MoveToX(-2, 40, Easing.OutSine)
                              .Then()
                              .MoveToX(0, 35, Easing.OutQuint);

            codeInputGlow.ClearTransforms();
            codeInputGlow.Colour = success
                ? new Color4(129, 242, 145, 255)
                : new Color4(255, 165, 120, 255);
            codeInputGlow.FadeTo(0.28f, 70, Easing.OutQuint)
                         .Then()
                         .FadeOut(220, Easing.OutQuint);
        }

    }
}
