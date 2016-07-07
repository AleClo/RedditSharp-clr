namespace RedditSharp
{
   internal class LinkData
   {
      [RedditAPIName("extension")]
      internal string Extension { get; set; }

      [RedditAPIName("url")]
      internal string URL { get; set; }

      [RedditAPIName("api_type")]
      internal string APIType { get; set; }

      [RedditAPIName("kind")]
      internal string Kind { get; set; }

      [RedditAPIName("sr")]
      internal string Subreddit { get; set; }

      [RedditAPIName("uh")]
      internal string UserHash { get; set; }

      [RedditAPIName("title")]
      internal string Title { get; set; }

      [RedditAPIName("iden")]
      internal string Iden { get; set; }

      [RedditAPIName("captcha")]
      internal string Captcha { get; set; }

      [RedditAPIName("resubmit")]
      internal bool Resubmit { get; set; }

      internal LinkData()
      {
         APIType = "json";
         Extension = "json";
         Kind = "link";
      }

      

   }
}