namespace IeeeVisRunOfShowWebApp.Models
{
    public class TrackViewModel
    {
        public string Track { get; init; }
        public string? RoomName { get; set; }
        public string? DiscordChannel { get; set; }
        public string? DiscordChannelID { get; set; }
        public string? DiscordURL { get; set; }
        public string? SlidoURL { get; set; }
        public string? StreamKeyID { get; set; }
        public string? StreamKeyTitle { get; set; }
        public string? StreamIngestionURL { get; set; }
        public string? CaptionsIngestionURL { get; set; }
        public string? StreamKey { get; set; }
    }
}