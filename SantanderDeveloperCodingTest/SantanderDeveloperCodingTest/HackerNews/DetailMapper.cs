namespace SantanderDeveloperCodingTest.HackerNews
{
    public static class DetailMapper
    {
        public static StoryDetails ToStoryDetails(this ItemDetails itemDetails)
        {
            var storyDetails = new StoryDetails
            {
                Title = itemDetails.title,
                Uri = itemDetails.url,
                PostedBy = itemDetails.by,
                Time = DateTimeOffset.FromUnixTimeSeconds(itemDetails.time).DateTime.ToString("O"),
                Score = itemDetails.score,
                CommentCount = itemDetails.descendants
            };
            return storyDetails;
        }
    }
}
