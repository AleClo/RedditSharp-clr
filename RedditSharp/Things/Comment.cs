using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedditSharp.Things
{
   public class Comment : VotableThing
   {
      private const string CommentUrl = "/api/comment";
      private const string EditUserTextUrl = "/api/editusertext";
      private const string RemoveUrl = "/api/remove";
      private const string DelUrl = "/api/del";
      private const string SetAsReadUrl = "/api/read_message";

      [JsonIgnore]
      private Reddit Reddit { get; set; }

      [JsonIgnore]
      private IAsyncWebAgent WebAgent { get; set; }

      public Comment Init(Reddit reddit, JToken json, IAsyncWebAgent webAgent, Thing sender)
      {
         var data = CommonInit(reddit, json, webAgent, sender);
         ParseComments(reddit, json, webAgent, sender);
         JsonConvert.PopulateObject(data.ToString(), this, reddit.JsonSerializerSettings);
         return this;
      }

      public async Task<Comment> InitAsync(Reddit reddit, JToken json, IAsyncWebAgent webAgent, Thing sender)
      {
         var data = CommonInit(reddit, json, webAgent, sender);
         await ParseCommentsAsync(reddit, json, webAgent, sender);
         await
            Task.Factory.StartNew(() => JsonConvert.PopulateObject(data.ToString(), this, reddit.JsonSerializerSettings));
         return this;
      }

      private JToken CommonInit(Reddit reddit, JToken json, IAsyncWebAgent webAgent, Thing sender)
      {
         base.Init(reddit, webAgent, json);
         var data = json["data"];
         Reddit = reddit;
         WebAgent = webAgent;
         this.Parent = sender;

         // Handle Reddit's API being horrible
         if (data["context"] != null)
         {
            var context = data["context"].Value<string>();
            LinkId = context.Split('/')[4];
         }

         return data;
      }

      private void ParseComments(Reddit reddit, JToken data, IAsyncWebAgent webAgent, Thing sender)
      {
         // Parse sub comments
         var replies = data["data"]["replies"];
         var subComments = new List<Comment>();
         if (replies != null && replies.Count() > 0)
         {
            foreach (var comment in replies["data"]["children"])
               subComments.Add(new Comment().Init(reddit, comment, webAgent, sender));
         }
         Comments = subComments.ToArray();
      }

      private async Task ParseCommentsAsync(Reddit reddit, JToken data, IAsyncWebAgent webAgent, Thing sender)
      {
         // Parse sub comments
         var replies = data["data"]["replies"];
         var subComments = new List<Comment>();
         if (replies != null && replies.Count() > 0)
         {
            foreach (var comment in replies["data"]["children"])
               subComments.Add(await new Comment().InitAsync(reddit, comment, webAgent, sender));
         }
         Comments = subComments.ToArray();
      }

      [JsonProperty("author")]
      public string Author { get; set; }

      [JsonProperty("banned_by")]
      public string BannedBy { get; set; }

      [JsonProperty("body")]
      public string Body { get; set; }

      [JsonProperty("body_html")]
      public string BodyHtml { get; set; }

      [JsonProperty("parent_id")]
      public string ParentId { get; set; }

      [JsonProperty("subreddit")]
      public string Subreddit { get; set; }

      [JsonProperty("approved_by")]
      public string ApprovedBy { get; set; }

      [JsonProperty("author_flair_css_class")]
      public string AuthorFlairCssClass { get; set; }

      [JsonProperty("author_flair_text")]
      public string AuthorFlairText { get; set; }

      [JsonProperty("gilded")]
      public int Gilded { get; set; }

      [JsonProperty("link_id")]
      public string LinkId { get; set; }

      [JsonProperty("link_title")]
      public string LinkTitle { get; set; }

      [JsonProperty("num_reports")]
      public int? NumReports { get; set; }

      [JsonIgnore]
      public IList<Comment> Comments { get; private set; }

      [JsonIgnore]
      public Thing Parent { get; internal set; }

      public override string Shortlink
      {
         get
         {
            // Not really a "short" link, but you can't actually use short links for comments
            string linkId = "";
            int index = this.LinkId.IndexOf('_');
            if (index > -1)
            {
               linkId = this.LinkId.Substring(index + 1);
            }

            return String.Format("https://{0}/r/{1}/comments/{2}/_/{3}",
               "www.reddit.com",
               this.Subreddit, this.Parent != null ? this.Parent.Id : linkId, this.Id);
         }
      }

      public Comment Reply(string message)
      {
         if (Reddit.User == null)
            throw new AuthenticationException("No user logged in.");

         var data = new
         {
            text = message,
            thing_id = FullName,
            uh = Reddit.User.Modhash,
            api_type = "json"
            //r = Subreddit
         };
         var json = WebAgent.Post(CommentUrl, data);
         return new Comment().Init(Reddit, json["json"]["data"]["things"][0], WebAgent, this);
      }

      /// <summary>
      /// Replaces the text in this comment with the input text.
      /// </summary>
      /// <param name="newText">The text to replace the comment's contents</param>        
      public void EditText(string newText)
      {
         if (Reddit.User == null)
            throw new Exception("No user logged in.");

         var data = new
         {
            api_type = "json",
            text = newText,
            thing_id = FullName,
            uh = Reddit.User.Modhash
         };

         JToken json = WebAgent.Post(EditUserTextUrl, data);

         if (json["json"].ToString().Contains("\"errors\": []"))
            Body = newText;
         else
            throw new Exception("Error editing text.");
      }

      private string SimpleAction(string endpoint)
      {
         // todo: why is this method here?

         if (Reddit.User == null)
            throw new AuthenticationException("No user logged in.");

         var data = new
         {
            id = FullName,
            uh = Reddit.User.Modhash
         };

         var json = WebAgent.Post(endpoint, data);
         return data.ToString();
      }

      [Obsolete()]
      public void Del()
      {
         var data = SimpleAction(DelUrl);
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

      public void SetAsRead()
      {
         var data = new
         {
            id = FullName,
            uh = Reddit.User.Modhash,
            api_type = "json"
         };
         WebAgent.Post(SetAsReadUrl, data);
      }
   }
}