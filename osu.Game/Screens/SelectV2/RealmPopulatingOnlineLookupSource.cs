// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using Realms;

namespace osu.Game.Screens.SelectV2
{
    /// <summary>
    /// This component is designed to perform lookups of online data
    /// and store portions of it for later local use to the realm database.
    /// </summary>
    /// <example>
    /// This component is designed to locally persist potentially-volatile online information such as:
    /// <list type="bullet">
    /// <item>user tags assigned to difficulties of a beatmap,</item>
    /// <item>the beatmap's <see cref="BeatmapInfo.Status"/>,</item>
    /// <item>guest mappers assigned to difficulties of a beatmap,</item>
    /// <item>the local user's best score on a given beatmap.</item>
    /// </list>
    /// </example>
    public partial class RealmPopulatingOnlineLookupSource : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        public Task<APIBeatmapSet?> GetBeatmapSetAsync(int id, CancellationToken token = default)
        {
            var request = new GetBeatmapSetRequest(id);
            var tcs = new TaskCompletionSource<APIBeatmapSet?>();

            token.Register(request.Cancel);

            // async request success callback is a bit of a dangerous game, but there's some reasoning for it.
            // - don't really want to use `IAPIAccess.PerformAsync()` because we still want to respect request queueing & online status checks
            // - we want the realm write here to be async because it is known to be slow for some users with large beatmap collections
            // - at the time of writing `RealmAccess.WriteAsync()` can only be safely called from update thread,
            //   and API request completion callbacks are automatically marshaled onto update thread scheduler,
            //   so calling `WriteAsync()` within the callback is a somewhat "nice" way of guaranteeing that the call is safe
            //   (rather than having to enforce that `GetBeatmapSetAsync()` can only be called from update thread, or locally scheduling)
            request.Success += async onlineBeatmapSet =>
            {
                if (token.IsCancellationRequested)
                {
                    tcs.SetCanceled(token);
                    return;
                }

                await realm.WriteAsync(r => updateRealmBeatmapSet(r, onlineBeatmapSet)).ConfigureAwait(true);
                tcs.SetResult(onlineBeatmapSet);
            };
            request.Failure += tcs.SetException;
            api.Queue(request);
            return tcs.Task;
        }

        private static void updateRealmBeatmapSet(Realm r, APIBeatmapSet onlineBeatmapSet)
        {
            var tagsById = (onlineBeatmapSet.RelatedTags ?? []).ToDictionary(t => t.Id);
            var onlineBeatmaps = onlineBeatmapSet.Beatmaps.ToDictionary(b => b.OnlineID);

            var dbBeatmapSets = r.All<BeatmapSetInfo>().Where(b => b.OnlineID == onlineBeatmapSet.OnlineID);

            foreach (var dbBeatmapSet in dbBeatmapSets)
            {
                // note that every single write to realm models is preceded by a guard, even if it technically would write the same value back.
                // the reason this matters is that doing so avoids triggering realm subscription callbacks.
                // unfortunately in terms of subscriptions realm treats *every* write to any realm object as a modification,
                // even if the write was redundant and had no observable effect.

                // notably, `LocallyModified` status is preserved on the set until the user performs an explicit action to get rid of it
                // (be it updating the set or deciding to discard their changes, removing the set and re-downloading it, etc.)
                if (dbBeatmapSet.Status != onlineBeatmapSet.Status && dbBeatmapSet.Status != BeatmapOnlineStatus.LocallyModified)
                    dbBeatmapSet.Status = onlineBeatmapSet.Status;

                foreach (var dbBeatmap in dbBeatmapSet.Beatmaps)
                {
                    if (onlineBeatmaps.TryGetValue(dbBeatmap.OnlineID, out var onlineBeatmap))
                    {
                        // compare `BeatmapUpdaterMetadataLookup`
                        //
                        // Only adopt the upstream MD5 / LastUpdated when this is the first
                        // lookup we've ever performed for this beatmap, OR when upstream's
                        // LastUpdated has actually advanced (i.e. the map was genuinely
                        // re-uploaded). This prevents spurious "update available" prompts
                        // for maps whose local file came from a re-packing mirror
                        // (BeatConnect, Gatari, osu.direct) — those mirrors produce a
                        // different .osu byte layout and therefore a different MD5 even
                        // though the map plays identically. The Torii server already trusts
                        // the client's MD5 on `/beatmaps/lookup`, so the *first* lookup
                        // stores a matching pair; we just need to avoid clobbering that
                        // here on subsequent set-level lookups that don't carry the client
                        // md5 hint.
                        bool isFirstOnlineLookup = dbBeatmap.LastOnlineUpdate == null;
                        bool upstreamReuploaded = dbBeatmap.LastOnlineUpdate != onlineBeatmap.LastUpdated;

                        if ((isFirstOnlineLookup || upstreamReuploaded) && dbBeatmap.OnlineMD5Hash != onlineBeatmap.MD5Hash)
                            dbBeatmap.OnlineMD5Hash = onlineBeatmap.MD5Hash;

                        if (dbBeatmap.LastOnlineUpdate != onlineBeatmap.LastUpdated)
                            dbBeatmap.LastOnlineUpdate = onlineBeatmap.LastUpdated;

                        // Status is decoupled from the MD5 gate intentionally: a server-side
                        // status promotion (e.g. Torii overriding bancho's graveyard with
                        // approved) must reach the client even when the local file's md5
                        // diverges from upstream's. `LocallyModified` is preserved at the
                        // set level above, which is enough to keep editor-modified maps from
                        // having their explicit local-modification status overwritten.
                        if (dbBeatmap.Status != BeatmapOnlineStatus.LocallyModified && dbBeatmap.Status != onlineBeatmap.Status)
                            dbBeatmap.Status = onlineBeatmap.Status;

                        HashSet<string> userTags = onlineBeatmap.TopTags?
                                                                .Select(t => (topTag: t, relatedTag: tagsById.GetValueOrDefault(t.TagId)))
                                                                .Where(t => t.relatedTag != null)
                                                                .Select(t => t.relatedTag!.Name)
                                                                .ToHashSet() ?? [];

                        if (!userTags.SetEquals(dbBeatmap.Metadata.UserTags))
                        {
                            dbBeatmap.Metadata.UserTags.Clear();
                            dbBeatmap.Metadata.UserTags.AddRange(userTags);
                        }
                    }
                }
            }
        }
    }
}
