using System;

namespace RedditSharp
{
   public class SubredditImage
   {
      private const string DeleteImageUrl = "/api/delete_sr_img";

      private Reddit Reddit { get; set; }
      private IAsyncWebAgent WebAgent { get; set; }

      public SubredditImage(Reddit reddit, SubredditStyle subredditStyle,
         string cssLink, string name, IAsyncWebAgent webAgent)
      {
         Reddit = reddit;
         WebAgent = webAgent;
         SubredditStyle = subredditStyle;
         Name = name;
         CssLink = cssLink;
      }

      public SubredditImage(Reddit reddit, SubredditStyle subreddit,
         string cssLink, string name, string url, IAsyncWebAgent webAgent)
         : this(reddit, subreddit, cssLink, name, webAgent)
      {
         Url = new Uri(url);
         // Handle legacy image urls
         // http://thumbs.reddit.com/FULLNAME_NUMBER.png
         int discarded;
         if (int.TryParse(url, out discarded))
            Url = new Uri(string.Format("http://thumbs.reddit.com/{0}_{1}.png", subreddit.Subreddit.FullName, url),
               UriKind.Absolute);
      }

      public string CssLink { get; set; }
      public string Name { get; set; }
      public Uri Url { get; set; }
      public SubredditStyle SubredditStyle { get; set; }

      public void Delete()
      {
         var data = new
         {
            img_name = Name,
            uh = Reddit.User.Modhash,
            r = SubredditStyle.Subreddit.Name
         };
         var response = WebAgent.Post(DeleteImageUrl, data);

         SubredditStyle.Images.Remove(this);
      }
   }
}