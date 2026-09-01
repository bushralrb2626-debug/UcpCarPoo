namespace UcpCarPool.ViewModels
{
    public class FeedItemViewModel
    {
        public string ActorName { get; set; } = string.Empty;
        public string? Batch { get; set; }
        public bool Verified { get; set; }
        public string Type { get; set; } = string.Empty; // "RidePosted", "RideCompleted", "NewMember"
        public string Headline { get; set; } = string.Empty;
        public string? SubText { get; set; }
        public DateTime When { get; set; }
        public int? RideId { get; set; }
    }

    public class HomeFeedViewModel
    {
        public List<string> AvailableBatches { get; set; } = new();
        public string? SelectedBatch { get; set; }
        public List<FeedItemViewModel> FeedItems { get; set; } = new();
        public int TotalActiveRides { get; set; }
        public int TotalMembers { get; set; }
        public int TotalCompletedRides { get; set; }
    }
}
