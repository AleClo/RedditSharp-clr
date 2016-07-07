namespace RedditSharp
{
   internal class TextData
   {
      [RedditAPIName("text")]
      internal string Text { get; set; }

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

      internal TextData()
      {
         Kind = "self";
         APIType = "json";
      }
   }
}