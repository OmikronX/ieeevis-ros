﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace IeeeVisRunOfShowWebApp.Models
{
    public class DataSourceModel
    {

        private static readonly Lazy<DataSourceModel> _default = new(() => new DataSourceModel());
        public static DataSourceModel Default => _default.Value;


        public DateTime LastRetrievalTimeUtc { get; set; }
        private DateTime _lastCheckTimeUtc;
        private EventViewModel[] _events = Array.Empty<EventViewModel>();
        private TrackViewModel[] _tracks = Array.Empty<TrackViewModel>();
        private readonly object _lck = new();
        private readonly GoogleSheetsHelper _sheetsHelper;
        private readonly string _privateKey;
        private readonly ZoomApi? _zoomApi;
        private readonly Dictionary<long, (ZoomMeetingInfo info, DateTime expiry)> _zoomDict = new();
        private readonly AuthHelper _authHelper;

        public bool HasZoomApi => _zoomApi != null;

        public string AdminKey { get; }

        public DataSourceModel()
        {
            var settings = new ConfigurationBuilder().AddJsonFile("appsettings.json")
                .Build().GetSection("CustomSettings");
            var dataUrl = settings["SheetsLink"];
            _privateKey = settings["PrivateKey"];
            _authHelper = new AuthHelper(_privateKey);
            AdminKey = settings["AdminKey"];
            _sheetsHelper = new GoogleSheetsHelper(dataUrl);
            // try
            // {
            //     var jwtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "zoom_jwt_token.txt");
            //     if (File.Exists(jwtPath))
            //     {
            //         var jwt = File.ReadAllText(jwtPath);
            //         if (!string.IsNullOrWhiteSpace(jwt))
            //             _zoomApi = new ZoomApi(jwt);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Trace.WriteLine(e);
            // }
            try
            {
                var cache = SheetsCache.LoadOrCreate();
                if (cache.Events is { Length: > 0 })
                {
                    _lastCheckTimeUtc = cache.LastCheckTimeUtc;
                    Refresh(cache);
                }
            }
            catch (Exception)
            {
            }
        }

        public async Task<ZoomMeetingInfo?> GetMeetingInfo(long id)
        {
            lock (_zoomDict)
            {
                if (_zoomApi == null) return null;
                if (_zoomDict.TryGetValue(id, out var field))
                {
                    if (field.expiry > DateTime.UtcNow.AddMinutes(5))
                        return field.info;
                    _zoomDict.Remove(id);
                }
            }


            var expiry = DateTime.UtcNow.AddHours(2);
            var info = await _zoomApi.GetMeetingInfo(id.ToString(CultureInfo.InvariantCulture));
            if (info == null)
                return info;
            lock (_zoomDict)
            {
                if (!_zoomDict.ContainsKey(id))
                    _zoomDict.Add(id, (info, expiry));
            }

            return info;


        }


        public (EventViewModel[] events, TrackViewModel[] tracks) GetData(bool forceRefresh = false)
        {
            lock (_lck)
            {
                if (forceRefresh || (DateTime.UtcNow - _lastCheckTimeUtc).TotalHours >= 1)
                {
                    Refresh();
                }
                return (_events, _tracks);
            }
        }

        public void Refresh()
        {
            _lastCheckTimeUtc = DateTime.UtcNow;
            var eventsRaw = _sheetsHelper.GetEvents();
            var sessionsRaw = _sheetsHelper.GetSessions();
            var itemsRaw = _sheetsHelper.GetItems();
            var tracksRaw = _sheetsHelper.GetTracks();
            var ffVideosRaw = _sheetsHelper.GetFfVideos();
            var videosRaw = _sheetsHelper.GetVideos();
            var papersRaw = _sheetsHelper.GetPapers();
            var bunnyRaw = _sheetsHelper.GetBunnyContent();
            var cache = new SheetsCache
            {
                Events = eventsRaw,
                Sessions = sessionsRaw,
                Items = itemsRaw,
                Tracks = tracksRaw,
                FfVideos = ffVideosRaw,
                Videos = videosRaw,
                Papers = papersRaw,
                Bunny = bunnyRaw,
                LastCheckTimeUtc = _lastCheckTimeUtc
            };
            Refresh(cache);
            try
            {
                cache.Save();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        private void Refresh(SheetsCache cache)
        {
            var ffVideos = cache.FfVideos == null
                ? new Dictionary<string, string?>()
                : cache.FfVideos.Where(k => !string.IsNullOrWhiteSpace(k["FF Source ID"]))
                    .ToDictionary(k => k["FF Source ID"]!, v => v["FF Link"]);
            var videos = cache.Videos == null
                ? new Dictionary<string, string?>()
                : cache.Videos.Where(k => !string.IsNullOrWhiteSpace(k["Video Source ID"]))
                    .ToDictionary(k => k["Video Source ID"]!, v => v["Video Link"]);

            var papers = cache.Papers == null
                ? new Dictionary<string, Dictionary<string, string?>>()
                : cache.Papers.Where(k => !string.IsNullOrWhiteSpace(k["UID"]))
                    .ToDictionary(k => k["UID"]!, v => v);
            var bunny = cache.Bunny == null
                ? new Dictionary<string, Dictionary<string, string?>>()
                : cache.Bunny.Where(k => !string.IsNullOrWhiteSpace(k["UID"]))
                    .ToDictionary(k => k["UID"]!, v => v);


            var tracks = cache.Tracks.Select(dict =>
                new TrackViewModel
                {
                    Track = dict.GetValueOrDefault("Track") ?? "",
                    RoomName = dict.GetValueOrDefault("Room Name") ?? "",
                    SlidoURL = dict.GetValueOrDefault("Slido URL") ?? "",
                    DiscordChannel = dict.GetValueOrDefault("Discord Channel") ?? "",
                    DiscordChannelID = dict.GetValueOrDefault("Discord Channel ID") ?? "",
                    DiscordURL = dict.GetValueOrDefault("Discord URL") ?? "",
                    StreamKeyID = dict.GetValueOrDefault("Stream Key ID") ?? "",
                    StreamKeyTitle = dict.GetValueOrDefault("Stream Key Title") ?? "",
                    StreamIngestionURL = dict.GetValueOrDefault("Stream Ingestion URL") ?? "",
                    CaptionsIngestionURL = dict.GetValueOrDefault("Captions Ingestion URL") ?? "",
                    StreamKey = dict.GetValueOrDefault("Stream Key") ?? "",
                }).ToArray();

            var events = cache.Events.Select(dict => new EventViewModel
            {
                Event = dict.GetValueOrDefault("Event") ?? "",
                EventDescription = dict.GetValueOrDefault("Event Description") ?? "",
                EventPrefix = dict.GetValueOrDefault("Event Prefix") ?? "",
                EventType = dict.GetValueOrDefault("Event Type") ?? "",
                EventUrl = dict.GetValueOrDefault("Event URL") ?? "",
                OrganizerEmails = (dict.GetValueOrDefault("Organizer Emails") ?? "")
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Organizers = (dict.GetValueOrDefault("Organizers") ?? "")
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            })
                .ToArray();

            foreach (var eventVm in events)
            {
                var sessions = cache.Sessions
                    .Where(dict => dict.GetValueOrDefault("Event Prefix") == eventVm.EventPrefix)
                    .Select(dict =>
                    {
                        var trackSrc = dict.GetValueOrDefault("Track");
                        var track = tracks.FirstOrDefault(t => t.Track == trackSrc);
                        if (track == null)
                            return null;
                        var vm = new SessionViewModel
                        {
                            Event = eventVm,
                            SessionID = dict.GetValueOrDefault("Session ID") ?? "",
                            SessionTitle = dict.GetValueOrDefault("Session Title") ?? "",
                            DateTimeStart = ParseDateTime(dict.GetValueOrDefault("DateTime Start") ?? ""),
                            DateTimeEnd = ParseDateTime(dict.GetValueOrDefault("DateTime End") ?? ""),
                            Track = track,
                            LivestreamID = dict.GetValueOrDefault("Livestream ID") ?? "",
                            SessionImage = dict.GetValueOrDefault("Session Image") ?? "",
                            SessionChairs = (dict.GetValueOrDefault("Session Chairs") ?? "")
                                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                            SessionChairsEmails = (dict.GetValueOrDefault("Session Chairs Emails") ?? "")
                                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                            SessionYouTubeURL = dict.GetValueOrDefault("Session YouTube URL") ?? "",
                            SessionFFPlaylistURL = dict.GetValueOrDefault("Session FF Playlist URL") ?? "",
                            SessionFFURL = dict.GetValueOrDefault("Session FF URL") ?? "",
                            SlidoURL = dict.GetValueOrDefault("Slido URL") ?? track.SlidoURL ?? "",
                            DiscordChannel = dict.GetValueOrDefault("Discord Channel") ?? track.DiscordChannel ?? "",
                            DiscordChannelID = dict.GetValueOrDefault("Discord Channel ID") ?? track.DiscordChannelID ?? "",
                            DiscordURL = dict.GetValueOrDefault("Discord URL") ?? track.DiscordURL ?? "",
                            ZoomMeetingID = dict.GetValueOrDefault("Zoom Meeting ID") ?? "",
                            ZoomPassword = dict.GetValueOrDefault("Zoom Password") ?? "",
                            ZoomURL = dict.GetValueOrDefault("Zoom URL") ?? "",
                            ZoomHostStartURL = dict.GetValueOrDefault("Zoom Host Start URL") ?? "",
                            ZoomHostUsername = dict.GetValueOrDefault("Zoom Host Username") ?? "",
                            AdditionalPresenters = dict.GetValueOrDefault("Additional Presenters") ?? "",
                            AdditionalPresentersEmails = dict.GetValueOrDefault("Additional Presenters Emails") ?? "",
                            SVZoomHost = dict.GetValueOrDefault("SVs Zoom Host") ?? "",
                            SVZoomHostEmail = dict.GetValueOrDefault("SVs ZH Emails") ?? "",
                            SVChannelMonitor = dict.GetValueOrDefault("SVs Channel Monitor") ?? "",
                            SVChannelMonitorEmail = dict.GetValueOrDefault("SVs CM emails") ?? ""
                        };
                        if (string.IsNullOrWhiteSpace(vm.SessionFFURL))
                            vm.SessionFFURL = ffVideos.GetValueOrDefault(vm.SessionID) ?? "";
                        vm.SessionKey = _authHelper.GetKey(vm.SessionID);
                        return vm;
                    }).Where(it => it != null && it.DateTimeStart != null && it.DateTimeEnd != null).ToArray();
                eventVm.Sessions = sessions;
                foreach (var session in sessions)
                {
                    var items = cache.Items
                        .Where(dict => dict.GetValueOrDefault("Session ID") == session.SessionID)
                        .Select(dict =>
                        {
                            var contributors = dict.GetValueOrDefault("Slot Contributors") ?? "";
                            var contributorsEmails = dict.GetValueOrDefault("Slot Contributors Emails") ?? "";

                            var presentersChanged = dict.GetValueOrDefault("Slot Presenters Changed") ?? "";
                            var presentersChangedEmails = dict.GetValueOrDefault("Slot Presenters Emails Changed") ?? "";

                            var contributorsFinal = string.IsNullOrEmpty(presentersChanged) ? contributors : presentersChanged;
                            var contributorsEmailsFinal = string.IsNullOrEmpty(presentersChangedEmails) ? contributorsEmails : presentersChangedEmails;
                            var svm = new SlotViewModel
                            {
                                Session = session,
                                ItemId = dict.GetValueOrDefault("Item ID") ?? "",
                                Title = dict.GetValueOrDefault("Slot Title") ?? "",
                                Start = ParseDateTime(dict.GetValueOrDefault("Slot DateTime Start") ?? ""),
                                End = ParseDateTime(dict.GetValueOrDefault("Slot DateTime End") ?? ""),
                                SlotType = dict.GetValueOrDefault("Slot Type") ?? "",
                                Contributors = contributorsFinal.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                                ContributorEmails = contributorsEmailsFinal.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                                AttendanceMode = dict.GetValueOrDefault("Attendance Mode") ?? "",
                                ParsedAttendanceMode = ParseAttendanceMode(dict.GetValueOrDefault("Attendance Mode") ?? ""),
                                PaperUid = dict.GetValueOrDefault("Paper UID") ?? "",
                                FileName = dict.GetValueOrDefault("File Name") ?? "",
                                Authors = (dict.GetValueOrDefault("Authors") ?? "")
                                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                                Abstract = dict.GetValueOrDefault("Abstract") ?? "",
                                SpeakerAffiliation = dict.GetValueOrDefault("Speaker Affiliation") ?? "",
                                SpeakerBio = dict.GetValueOrDefault("Speaker Bio") ?? "",
                                SpecialNotes = dict.GetValueOrDefault("Special Notes") ?? "",
                            };
                            var declinedRaw = dict.GetValueOrDefault("Recording Declined")?.Trim()?.ToLowerInvariant();
                            svm.RecordingDeclined = declinedRaw == "1" || declinedRaw == "y";
                            svm.FastForwardLink = ffVideos.GetValueOrDefault(svm.PaperUid) ?? "";
                            if (string.IsNullOrEmpty(svm.FastForwardLink))
                            {
                                svm.FastForwardLink = bunny.GetValueOrDefault(svm.PaperUid)
                                    ?.GetValueOrDefault("FF Video Bunny URL") ?? "";
                            }
                            svm.VideoLink = videos.GetValueOrDefault(svm.PaperUid) ?? "";
                            if (string.IsNullOrEmpty(svm.VideoLink))
                            {
                                svm.VideoLink = bunny.GetValueOrDefault(svm.PaperUid)
                                    ?.GetValueOrDefault("Video Bunny URL") ?? "";
                            }
                            var paper = papers.GetValueOrDefault(svm.PaperUid);
                            var doi = paper?.GetValueOrDefault("DOI")?.Trim();
                            var fno = paper?.GetValueOrDefault("FNO")?.Trim();
                            var hasPdf = paper?.GetValueOrDefault("Has PDF")?.Trim()?.ToLowerInvariant();
                            var hasVideo = paper?.GetValueOrDefault("Has Video")?.Trim()?.ToLowerInvariant();
                            if (!string.IsNullOrWhiteSpace(fno))
                            {
                                svm.IeeeUrl = "https://ieeexplore.ieee.org/document/" + fno;
                            }
                            else if (!string.IsNullOrWhiteSpace(doi))
                            {
                                svm.DoiUrl = "https://doi.org/" + doi;
                            }

                            svm.HasPdf = hasPdf is "y" or "1";
                            svm.HasVideo = hasVideo is "y" or "1";
                            if (svm.HasPdf)
                                svm.PdfUrl = $"https://ieeevis.b-cdn.net/vis_2024/pdfs/{svm.PaperUid}.pdf";
                            EnrichHints(svm);
                            return svm;
                        }).Where(it => it is { Start: not null, End: not null }).ToArray();
                    session.Slots = items;
                }
            }

            _events = events;
            _tracks = tracks;
            LastRetrievalTimeUtc = cache.LastCheckTimeUtc;
        }

        private static void EnrichHints(SlotViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.AttendanceMode))
                vm.AttendanceMode = "Unknown";
        }

        private static AttendanceModeEnum ParseAttendanceMode(string? mode)
        {
            return mode switch
            {
                "Virtual" => AttendanceModeEnum.Virtual,
                "Mixed" => AttendanceModeEnum.Mixed,
                "In Person" => AttendanceModeEnum.InPerson,
                _ => AttendanceModeEnum.Unknown
            };
        }

        private static DateTime? ParseDateTime(string? dt)
        {
            if (string.IsNullOrWhiteSpace(dt))
                return null;
            var utc = DateTime.Parse(dt.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            //return time parsed
            return utc;
        }

    }
}
