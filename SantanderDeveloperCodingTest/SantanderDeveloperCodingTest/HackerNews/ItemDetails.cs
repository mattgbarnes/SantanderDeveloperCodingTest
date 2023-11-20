namespace SantanderDeveloperCodingTest.HackerNews
{
    public struct ItemDetails
    {
        public int id { get; set; }
        public bool deleted { get; set; } // guess
        public string type { get; set; }
        public string by { get; set; }
        public long time { get; set; }
        public string text { get; set; }
        public bool dead { get; set; } // guess
        public int parent { get; set; }
        public int poll { get; set; } // guess
        public int[] kids { get; set; }
        public string url { get; set; }
        public int score { get; set; }
        public string title { get; set; }
        public int[] parts { get; set; } // guess
        public int descendants { get; set; }
    }
}
