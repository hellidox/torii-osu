// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Threading;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Users;

namespace osu.Game.Online
{
    /// <summary>
    /// A component that keeps track of the latest statistics for the local user.
    /// </summary>
    public partial class LocalUserStatisticsProvider : Component
    {
        /// <summary>
        /// Invoked whenever a change occured to the statistics of any ruleset,
        /// either due to change in local user (log out and log in) or as a result of score submission.
        /// </summary>
        /// <remarks>
        /// This does not guarantee the presence of the old statistics,
        /// specifically in the case of initial population or change in local user.
        /// </remarks>
        public event Action<UserStatisticsUpdate>? StatisticsUpdated;

        [Resolved]
        private RulesetStore rulesets { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        private readonly IBindable<APIUser> localUser = new Bindable<APIUser>();

        private readonly Dictionary<string, UserStatistics> statisticsCache = new Dictionary<string, UserStatistics>();

        /// <summary>
        /// Returns the <see cref="UserStatistics"/> currently available for the given ruleset.
        /// This may return null if the requested statistics has not been fetched before yet.
        /// </summary>
        /// <param name="ruleset">The ruleset to return the corresponding <see cref="UserStatistics"/> for.</param>
        public UserStatistics? GetStatisticsFor(RulesetInfo ruleset) => statisticsCache.GetValueOrDefault(ruleset.ShortName);

        protected override void LoadComplete()
        {
            base.LoadComplete();

            localUser.BindTo(api.LocalUser);
            localUser.BindValueChanged(_ =>
            {
                // queuing up requests directly on user change is unsafe, as the API status may have not been updated yet.
                // schedule a frame to allow the API to be in its correct state sending requests.
                Schedule(initialiseStatistics);
            }, true);

            IBindable<bool> ppVariantBindable = ToriiPpVariantState.UsePpDevVariantBindable;

            ppVariantBindable.BindValueChanged(_ =>
            {
                if (api.LocalUser.Value != null && api.LocalUser.Value.Id > 1)
                    Schedule(initialiseStatistics);
            });
        }

        private ScheduledDelegate? deferredSecondaryStatsFetch;

        private void initialiseStatistics()
        {
            statisticsCache.Clear();
            deferredSecondaryStatsFetch?.Cancel();

            if (api.LocalUser.Value == null || api.LocalUser.Value.Id <= 1)
                return;

            // Fetch the user's preferred ruleset stats first so the toolbar profile card,
            // briefing, and any "open my profile" interaction have data right away.
            // The other 7 ruleset/special-ruleset stats requests used to fire serially in
            // the same tick — each one a full GetUserRequest against the API — and that
            // backed up the queue behind the briefing's requests on login. Defer them so
            // login feels instant and the secondary caches still warm up shortly after.
            var preferred = getPreferredRuleset();
            if (preferred != null)
                RefetchStatistics(preferred);

            deferredSecondaryStatsFetch = Scheduler.AddDelayed(fetchAllRulesetStats, 1500);
        }

        private RulesetInfo? getPreferredRuleset()
        {
            var user = api.LocalUser.Value;
            if (user == null || user.Id <= 1)
                return null;

            if (!string.IsNullOrEmpty(user.PlayMode))
            {
                var preferred = rulesets.AvailableRulesets.FirstOrDefault(r => r.ShortName == user.PlayMode && r.IsLegacyRuleset());
                if (preferred != null)
                    return preferred;
            }

            return rulesets.AvailableRulesets.FirstOrDefault(r => r.IsLegacyRuleset());
        }

        private void fetchAllRulesetStats()
        {
            if (api.LocalUser.Value == null || api.LocalUser.Value.Id <= 1)
                return;

            foreach (var ruleset in rulesets.AvailableRulesets.Where(r => r.IsLegacyRuleset()))
            {
                if (!statisticsCache.ContainsKey(ruleset.ShortName))
                    RefetchStatistics(ruleset);

                switch (ruleset.ShortName)
                {
                    case RulesetInfo.OSU_MODE_SHORTNAME:
                        RefetchStatistics(ruleset.CreateSpecialRuleset(RulesetInfo.OSU_RELAX_MODE_SHORTNAME, RulesetInfo.OSU_RELAX_ONLINE_ID));
                        RefetchStatistics(ruleset.CreateSpecialRuleset(RulesetInfo.OSU_AUTOPILOT_MODE_SHORTNAME, RulesetInfo.OSU_AUTOPILOT_ONLINE_ID));
                        break;

                    case RulesetInfo.TAIKO_MODE_SHORTNAME:
                        RefetchStatistics(ruleset.CreateSpecialRuleset(RulesetInfo.TAIKO_RELAX_MODE_SHORTNAME, RulesetInfo.TAIKO_RELAX_ONLINE_ID));
                        break;

                    case RulesetInfo.CATCH_MODE_SHORTNAME:
                        RefetchStatistics(ruleset.CreateSpecialRuleset(RulesetInfo.CATCH_MODE_SHORTNAME, RulesetInfo.CATCH_RELAX_ONLINE_ID));
                        break;
                }
            }
        }

        public void RefetchStatistics(RulesetInfo ruleset, Action<UserStatisticsUpdate>? callback = null)
        {
            if (!ruleset.IsLegacyRuleset())
                throw new InvalidOperationException($@"Retrieving statistics is not supported for ruleset {ruleset.ShortName}");

            var request = new GetUserRequest(api.LocalUser.Value.Id, ruleset);
            request.Success += u => UpdateStatistics(u.Statistics, ruleset, callback);
            api.Queue(request);
        }
        public void RefetchStatistics(ScoreInfo score, Action<UserStatisticsUpdate>? callback = null)
        {
            var specialRuleset = score.Ruleset.CreateSpecialRulesetByScore(score);
            RefetchStatistics(specialRuleset ?? score.Ruleset, callback);
        }

        protected void UpdateStatistics(UserStatistics newStatistics, RulesetInfo ruleset, Action<UserStatisticsUpdate>? callback = null)
        {
            var oldStatistics = statisticsCache.GetValueOrDefault(ruleset.ShortName);
            statisticsCache[ruleset.ShortName] = newStatistics;

            var update = new UserStatisticsUpdate(ruleset, oldStatistics, newStatistics);
            callback?.Invoke(update);
            StatisticsUpdated?.Invoke(update);
        }
    }

    public record UserStatisticsUpdate(RulesetInfo Ruleset, UserStatistics? OldStatistics, UserStatistics NewStatistics);
}
