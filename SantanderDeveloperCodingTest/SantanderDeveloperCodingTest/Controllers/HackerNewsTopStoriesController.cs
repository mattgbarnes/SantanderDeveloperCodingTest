using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SantanderDeveloperCodingTest.HackerNews;

namespace SantanderDeveloperCodingTest.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HackerNewsTopStoriesController : ControllerBase
    {
        private readonly IHackerNewsService _hackerNewsService;

        public HackerNewsTopStoriesController(IHackerNewsService hackerNewsService)
        {
            _hackerNewsService = hackerNewsService;
        }

        [HttpGet(Name = "GetTopStories")]
        public async Task<IEnumerable<StoryDetails>> Get(int count)
        {
            var bestStoryDetails = await _hackerNewsService.GetBestStories(count);
            return bestStoryDetails.ToArray();
            //return new List<string>() { $"you asked for {count} items" };


        }
    }

}
