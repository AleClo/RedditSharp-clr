using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditSharp.Things
{
   public class Post : VotableThing
   {
      private const string CommentUrl = "/api/comment";
      private const string RemoveUrl = "/api/remove";
      private const string DelUrl = "/api/del";
      private const string GetCommentsUrl = "/comments/{0}.json";
      private const string ApproveUrl = "/api/approve";
      private const string EditUserTextUrl = "/api/editusertext";
      private const string HideUrl = "/api/hide";
      private const string UnhideUrl = "/api/unhide";
      private const string SetFlairUrl = "/r/{0}/api/flair";
      private const string MarkNSFWUrl = "/api/marknsfw";
      private const string UnmarkNSFWUrl = "/api/unmarknsfw";
      private const string ContestModeUrl = "/api/set_contest_mode";

      [JsonIgnore]
      private Reddit Reddit { get; set; }

      [JsonIgnore]
      private IWegAgent WebAgent { get; set; }

      public Post Init(Reddit reddit, JToken post, IWegAgent webAgent)
      {
         CommonInit(reddit, post, webAgent);
         JsonConvert.PopulateObject(post["data"].ToString(), this, reddit.JsonSerializerSettings);
         return this;
      }

      public async Task<Post> InitAsync(Reddit reddit, JToken post, IWegAgent webAgent)
      {
         CommonInit(reddit, post, webAgent);
         await
            Task.Factory.StartNew(
               () => JsonConvert.PopulateObject(post["data"].ToString(), this, reddit.JsonSerializerSettings));
         return this;
      }

      private void CommonInit(Reddit reddit, JToken post, IWegAgent webAgent)
      {
         base.Init(reddit, webAgent, post);
         Reddit = reddit;
         WebAgent = webAgent;
      }

      [JsonProperty("author")]
      public string AuthorName { get; set; }

      [JsonIgnore]
      public RedditUser Author
      {
         get { return Reddit.GetUser(AuthorName); }
      }

      public Comment[] Comments
      {
         get { return ListComments().ToArray(); }
      }

      [JsonProperty("approved_by")]
      public string ApprovedBy { get; set; }

      [JsonProperty("author_flair_css_class")]
      public string AuthorFlairCssClass { get; set; }

      [JsonProperty("author_flair_text")]
      public string AuthorFlairText { get; set; }

      [JsonProperty("banned_by")]
      public string BannedBy { get; set; }

      [JsonProperty("domain")]
      public string Domain { get; set; }

      [JsonProperty("edited")]
      public bool Edited { get; set; }

      [JsonProperty("is_self")]
      public bool IsSelfPost { get; set; }

      [JsonProperty("link_flair_css_class")]
      public string LinkFlairCssClass { get; set; }

      [JsonProperty("link_flair_text")]
      public string LinkFlairText { get; set; }

      [JsonProperty("num_comments")]
      public int CommentCount { get; set; }

      [JsonProperty("over_18")]
      public bool NSFW { get; set; }

      [JsonProperty("permalink")]
      [JsonConverter(typeof (UrlParser))]
      public Uri Permalink { get; set; }

      [JsonProperty("score")]
      public int Score { get; set; }

      [JsonProperty("selftext")]
      public string SelfText { get; set; }

      [JsonProperty("selftext_html")]
      public string SelfTextHtml { get; set; }

      [JsonProperty("thumbnail")]
      [JsonConverter(typeof (UrlParser))]
      public Uri Thumbnail { get; set; }

      [JsonProperty("title")]
      public string Title { get; set; }

      [JsonProperty("subreddit")]
      public string SubredditName { get; set; }

      [JsonIgnore]
      public Subreddit Subreddit
      {
         get { return Reddit.GetSubreddit("/r/" + SubredditName); }
      }

      [JsonProperty("url")]
      [JsonConverter(typeof (UrlParser))]
      public Uri Url { get; set; }

      [JsonProperty("num_reports")]
      public int? Reports { get; set; }

      public Comment Comment(string message)
      {
         if (Reddit.User == null)
            throw new AuthenticationException("No user logged in.");
         var data = new
         {
            text = message,
            thing_id = FullName,
            uh = Reddit.User.Modhash,
            api_type = "json"
         };

         var json = WebAgent.Post(CommentUrl, data);

         if (json["json"]["ratelimit"] != null)
            throw new RateLimitException(TimeSpan.FromSeconds(json["json"]["ratelimit"].ValueOrDefault<double>()));
         return new Comment().Init(Reddit, json["json"]["data"]["things"][0], WebAgent, this);
      }

      private void SimpleAction(string endpoint)
      {
         if (Reddit.User == null)
            throw new AuthenticationException("No user logged in.");
         var data = new
         {
            id = FullName,
            uh = Reddit.User.Modhash
         };

         WebAgent.Post(endpoint, data);
      }

      private void SimpleActionToggle(string endpoint, bool value)
      {
         if (Reddit.User == null)
            throw new AuthenticationException("No user logged in.");
         var data = new
         {
            id = FullName,
            state = value,
            uh = Reddit.User.Modhash
         };

         WebAgent.Post(endpoint, data);
      }

      public void Approve()
      {
         SimpleAction(ApproveUrl);
      }

      public void Remove()
      {
         RemoveImpl(false);
      }

      public void RemoveSpam()
      {
         RemoveImpl(true);
      }

      private void RemoveImpl(bool spam)
      {
         var data = new
         {
            id = FullName,
            spam = spam,
            uh = Reddit.User.Modhash
         };

         WebAgent.Post(RemoveUrl, data);
      }

      public void Del()
      {
         SimpleAction(DelUrl);
      }

      public void Hide()
      {
         SimpleAction(HideUrl);
      }

      public void Unhide()
      {
         SimpleAction(UnhideUrl);
      }

      public void MarkNSFW()
      {
         SimpleAction(MarkNSFWUrl);
      }

      public void UnmarkNSFW()
      {
         SimpleAction(UnmarkNSFWUrl);
      }

      public void ContestMode(bool state)
      {
         SimpleActionToggle(ContestModeUrl, state);
      }

      #region Obsolete Getter Methods

      [Obsolete("Use Comments property instead")]
      public Comment[] GetComments()
      {
         return Comments;
      }

      #endregion Obsolete Getter Methods

      /// <summary>
      /// Replaces the text in this post with the input text.
      /// </summary>
      /// <param name="newText">The text to replace the post's contents</param>
      public void EditText(string newText)
      {
         if (Reddit.User == null)
            throw new Exception("No user logged in.");
         if (!IsSelfPost)
            throw new Exception("Submission to edit is not a self-post.");

         var data = new
         {
            api_type = "json",
            text = newText,
            thing_id = FullName,
            uh = Reddit.User.Modhash
         };
    
         JToken json = WebAgent.Post(EditUserTextUrl, data);
         if (json["json"].ToString().Contains("\"errors\": []"))
            SelfText = newText;
         else
            throw new Exception("Error editing text.");
      }

      public void Update()
      {
         JToken post = Reddit.GetToken(this.Url);
         JsonConvert.PopulateObject(post["data"].ToString(), this, Reddit.JsonSerializerSettings);
      }

      public void SetFlair(string flairText, string flairClass)
      {
         if (Reddit.User == null)
            throw new Exception("No user logged in.");

         var data = new
         {
            api_type = "json",
            css_class = flairClass,
            link = FullName,
            name = Reddit.User.Name,
            text = flairText,
            uh = Reddit.User.Modhash
         };

         var json = WebAgent.Post(SetFlairUrl, data);
         LinkFlairText = flairText;
      }

      public List<Comment> ListComments(int? limit = null)
      {
         //todo: I think i broke this method
         var url = string.Format(GetCommentsUrl, Id);

         if (limit.HasValue)
         {
            var query = HttpHelper.ParseQueryString(string.Empty);
            query.Add("limit", limit.Value.ToString());
            url = string.Format("{0}?{1}", url, query);
         }

         var json = JArray.FromObject(WebAgent.Get(url));

         var postJson = json[1]["data"]["children"];

         var comments = new List<Comment>();
         foreach (var comment in postJson)
         {
            comments.Add(new Comment().Init(Reddit, comment, WebAgent, this));
         }

         return comments;
      }
   }
}