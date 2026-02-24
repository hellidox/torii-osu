#nullable enable
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Configuration;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Rulesets.Space.Beatmaps;
using osu.Game.Rulesets.Space.Beatmaps.Formats;
using osu.Game.Rulesets.Space.Configuration;
using osu.Game.Rulesets.Space.Difficulty;
using osu.Game.Rulesets.Space.Mods;
using osu.Game.Rulesets.Space.Replays;
using osu.Game.Rulesets.Space.Scoring;
using osu.Game.Rulesets.Space.Skinning.Argon;
using osu.Game.Rulesets.Space.Skinning.Legacy;
using osu.Game.Rulesets.Space.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Space.Edit;
using osu.Game.Skinning;


namespace osu.Game.Rulesets.Space
{
    public partial class SpaceRuleset : Ruleset, ILegacyRuleset
    {
        public override string Description => "osu!space";
        public override string ShortName => "osuspaceruleset";
        public const string VERSION_STRING = "2026.106.1";
        public SpaceRuleset()
        {
            SpaceLegacyBeatmapDecoder.Register();

            // lol who fkin knows
            RulesetInfo.OnlineID = 727;
        }

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null) =>
            new DrawableSpaceRuleset(this, beatmap, mods);

        public override HealthProcessor CreateHealthProcessor(double drainStartTime) => new SpaceHealthProcessor();

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) =>
            new SpaceBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) =>
            new SpaceDifficultyCalculator(RulesetInfo, beatmap);

        public override PerformanceCalculator? CreatePerformanceCalculator() => new SpacePerformanceCalculator(this);

        public int LegacyID => 215;

        public override IConvertibleReplayFrame CreateConvertibleReplayFrame() => new SpaceReplayFrame();



        public ILegacyScoreSimulator CreateLegacyScoreSimulator() => new SpaceLegacyScoreSimulator();

        public override IRulesetConfigManager CreateConfig(SettingsStore? settings) => new SpaceRulesetConfigManager(settings, RulesetInfo);

        public override RulesetSettingsSubsection CreateSettings() => new SpaceSettingsSubsection(this);

        public override HitObjectComposer CreateHitObjectComposer() => new SpaceHitObjectComposer(this);

        public override IEnumerable<HitResult> GetValidHitResults()
        {
            return
            [
                HitResult.Perfect,
                HitResult.Miss
            ];
        }

        public override ISkin? CreateSkinTransformer(ISkin skin, IBeatmap beatmap)
        {
            switch (skin)
            {
                case LegacySkin:
                    return new SpaceLegacySkinTransformer(skin);

                case ArgonSkin:
                    return new SpaceArgonSkinTransformer(skin);
            }

            return null;
        }

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.DifficultyReduction:
                    return
                    [
                        new SpaceModNoFail(),
                        new MultiMod(new SpaceModHalfTime(), new SpaceModDaycore()),
                    ];

                case ModType.DifficultyIncrease:
                    return
                    [
                        new MultiMod(new SpaceModPerfect()),
                        new MultiMod(new SpaceModDoubleTime(), new SpaceModNightcore()),
                        new SpaceModFullGhost(),
                        new ModAccuracyChallenge(),
                    ];

                case ModType.Conversion:
                    return
                    [
                        new SpaceModMirror(),
                    ];

                case ModType.Automation:
                    return
                    [
                        new MultiMod(new SpaceModAutoplay(), new SpaceModCinema()),
                    ];

                case ModType.Fun:
                    return
                    [
                        new MultiMod(new ModWindUp(), new ModWindDown()),
                        new SpaceModMuted(),
                        new ModAdaptiveSpeed(),
                        new SpaceModNoScope(),
                    ];
                // case ModType.System:
                //     return new Mod[]
                //     {
                //     };

                default:
                    return [];
            }
        }

        public override IEnumerable<Mod> ConvertFromLegacyMods(LegacyMods mods)
        {
            if (mods.HasFlag(LegacyMods.Nightcore))
                yield return new SpaceModNightcore();
            else if (mods.HasFlag(LegacyMods.DoubleTime))
                yield return new SpaceModDoubleTime();

            if (mods.HasFlag(LegacyMods.Autoplay))
                yield return new SpaceModAutoplay();

            if (mods.HasFlag(LegacyMods.NoFail))
                yield return new SpaceModNoFail();

            if (mods.HasFlag(LegacyMods.HalfTime))
                yield return new SpaceModHalfTime();

            if (mods.HasFlag(LegacyMods.Perfect))
                yield return new SpaceModPerfect();

            if (mods.HasFlag(LegacyMods.Hidden))
                yield return new SpaceModFullGhost();

            if (mods.HasFlag(LegacyMods.Cinema))
                yield return new SpaceModCinema();
        }

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0) => [];

        public override Drawable CreateIcon() => new SpaceRulesetIcon(this);

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}
