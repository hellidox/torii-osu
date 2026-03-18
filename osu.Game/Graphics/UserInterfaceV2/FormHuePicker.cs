// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserInterfaceV2
{
    public partial class FormHuePicker : CompositeDrawable, IHasCurrentValue<float>, IFormControl
    {
        public Bindable<float> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly BindableNumberWithCurrent<float> current = new BindableNumberWithCurrent<float>
        {
            MinValue = 0,
            MaxValue = 359,
            Precision = 1,
            Default = 300,
        };

        public LocalisableString Caption { get; init; }
        public LocalisableString HintText { get; init; }

        private readonly Bindable<Colour4> pickerColour = new Bindable<Colour4>();

        private bool syncingFromPicker;
        private bool syncingFromCurrent;
        private float pickerSaturation = 1f;
        private float pickerValue = 1f;

        private FormControlBackground background = null!;
        private FormFieldCaption captionText = null!;
        private HueSwatchButton swatchButton = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                background = new FormControlBackground(),
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding(9),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Right = 120 },
                            Child = captionText = new FormFieldCaption
                            {
                                Caption = Caption,
                                TooltipText = HintText,
                            },
                        },
                        swatchButton = new HueSwatchButton
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            CurrentColour = { BindTarget = pickerColour },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            colourProvider.ColoursChanged += updateState;

            current.BindValueChanged(_ =>
            {
                if (!syncingFromPicker)
                {
                    syncingFromCurrent = true;
                    pickerColour.Value = hueToColour(current.Value);
                    syncingFromCurrent = false;
                }

                updateState();
                background.Flash();
                ValueChanged?.Invoke();
            }, true);

            current.BindDisabledChanged(_ =>
            {
                pickerColour.Disabled = current.Disabled;
                updateState();
            }, true);

            pickerColour.BindValueChanged(change =>
            {
                if (syncingFromCurrent)
                    return;

                syncingFromPicker = true;

                var hsv = new Colour4(change.NewValue.R, change.NewValue.G, change.NewValue.B, 1).ToHSV();
                pickerSaturation = Math.Clamp(hsv.Y, 0f, 1f);
                pickerValue = Math.Clamp(hsv.Z, 0f, 1f);

                float hueDegrees = normaliseHue(hsv.X * 360f);

                if (MathF.Abs(current.Value - hueDegrees) > 0.01f)
                    current.Value = hueDegrees;

                syncingFromPicker = false;
                updateState();
            }, true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            base.OnHoverLost(e);
            updateState();
        }

        private void updateState()
        {
            captionText.Colour = current.Disabled ? colourProvider.Background1 : colourProvider.Content2;
            swatchButton.CurrentHue = normaliseHue(current.Value);

            if (current.Disabled)
                background.VisualStyle = VisualStyle.Disabled;
            else if (IsHovered)
                background.VisualStyle = VisualStyle.Hovered;
            else
                background.VisualStyle = VisualStyle.Normal;
        }

        private Colour4 hueToColour(float hue) => Colour4.FromHSV(normaliseHue(hue) / 360f, pickerSaturation, pickerValue);

        private static float normaliseHue(float hue)
        {
            float normalised = hue % 360f;

            if (normalised < 0)
                normalised += 360f;

            return normalised;
        }

        public IEnumerable<LocalisableString> FilterTerms => new[] { Caption, HintText };

        public event Action? ValueChanged;

        public bool IsDefault => Current.IsDefault;

        public void SetDefault() => Current.SetDefault();

        public bool IsDisabled => Current.Disabled;

        public float MainDrawHeight => DrawHeight;

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
                colourProvider.ColoursChanged -= updateState;

            base.Dispose(isDisposing);
        }

        private partial class HueSwatchButton : OsuClickableContainer, IHasPopover
        {
            public Bindable<Colour4> CurrentColour { get; } = new Bindable<Colour4>();

            public float CurrentHue
            {
                set
                {
                    if (IsLoaded)
                        label.Text = $"{value:0}°";
                }
            }

            private Box fill = null!;
            private OsuSpriteText label = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                Size = new Vector2(110, 36);
                Action = this.ShowPopover;
                CornerRadius = 8;
                Masking = true;

                Children = new Drawable[]
                {
                    fill = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    label = new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                CurrentColour.BindValueChanged(_ => updateState(), true);
            }

            private void updateState()
            {
                fill.Colour = CurrentColour.Value;
                label.Colour = OsuColour.ForegroundTextColourFor(CurrentColour.Value);
            }

            public Popover GetPopover() => new HuePickerPopover
            {
                Current = { BindTarget = CurrentColour },
            };
        }

        private partial class HuePickerPopover : OsuPopover, IHasCurrentValue<Colour4>
        {
            public Bindable<Colour4> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

            public HuePickerPopover()
                : base(false)
            {
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Child = new OsuColourPicker
                {
                    Current = { BindTarget = Current },
                };

                Body.BorderThickness = 2;
                Body.BorderColour = colourProvider.Highlight1;
                Content.Padding = new MarginPadding(2);
            }
        }
    }
}
