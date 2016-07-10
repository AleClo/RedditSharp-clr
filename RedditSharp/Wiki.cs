using System.Collections.Generic;
using RedditSharp.Things;

namespace RedditSharp
{
   using System;

   public class Wiki
   {
      private Reddit Reddit { get; set; }
      private Subreddit Subreddit { get; set; }
      private IAsyncWebAgent WebAgent { get; set; }

      private const string GetWikiPageUrl = "/r/{0}/wiki/{1}.json?v={2}";
      private const string GetWikiPagesUrl = "/r/{0}/wiki/pages.json";
      private const string WikiPageEditUrl = "/r/{0}/api/wiki/edit";
      private const string HideWikiPageUrl = "/r/{0}/api/wiki/hide";
      private const string RevertWikiPageUrl = "/r/{0}/api/wiki/revert";
      private const string WikiPageAllowEditorAddUrl = "/r/{0}/api/wiki/alloweditor/add";
      private const string WikiPageAllowEditorDelUrl = "/r/{0}/api/wiki/alloweditor/del";
      private const string WikiPageSettingsUrl = "/r/{0}/wiki/settings/{1}.json";
      private const string WikiRevisionsUrl = "/r/{0}/wiki/revisions.json";
      private const string WikiPageRevisionsUrl = "/r/{0}/wiki/revisions/{1}.json";
      private const string WikiPageDiscussionsUrl = "/r/{0}/wiki/discussions/{1}.json";

      public IEnumerable<string> PageNames
      {
         get
         {
            var json = WebAgent.Get(string.Format(GetWikiPagesUrl, Subreddit.Name));
            return json["data"].Values<string>();
         }
      }

      public Listing<WikiPageRevision> Revisions
      {
         get
         {
            return new Listing<WikiPageRevision>(Reddit, string.Format(WikiRevisionsUrl, Subreddit.Name), WebAgent);
         }
      }

      protected internal Wiki(Reddit reddit, Subreddit subreddit, IAsyncWebAgent webAgent)
      {
         Reddit = reddit;
         Subreddit = subreddit;
         WebAgent = webAgent;
      }

      public WikiPage GetPage(string page, string version = null)
      {
         var json = WebAgent.Get(string.Format(GetWikiPageUrl, Subreddit.Name, page, version));
         var result = new WikiPage(Reddit, json["data"], WebAgent);
         return result;
      }

      #region Settings

      public WikiPageSettings GetPageSettings(string name)
      {
         var json = WebAgent.Get(string.Format(WikiPageSettingsUrl, Subreddit.Name, name));

         var result = new WikiPageSettings(Reddit, json["data"], WebAgent);
         return result;
      }

      public void SetPageSettings(string name, WikiPageSettings settings)
      {
         var data = new
         {
            page = name,
            permlevel = settings.PermLevel,
            listed = settings.Listed,
            uh = Reddit.User.Modhash
         };

         var response = WebAgent.Post(string.Format(WikiPageSettingsUrl, Subreddit.Name, name), data);
      }

      #endregion

      #region Revisions

      public Listing<WikiPageRevision> GetPageRevisions(string page)
      {
         return new Listing<WikiPageRevision>(Reddit, string.Format(WikiPageRevisionsUrl, Subreddit.Name, page),
            WebAgent);
      }

      #endregion

      #region Discussions

      public Listing<Post> GetPageDiscussions(string page)
      {
         return new Listing<Post>(Reddit, string.Format(WikiPageDiscussionsUrl, Subreddit.Name, page), WebAgent);
      }

      #endregion

      public void EditPage(string page, string content, string previous = null, string reason = null)
      {
         dynamic param = new
         {
            content = content,
            page = page,
            uh = Reddit.User.Modhash
         };
         List<string> addParams = new List<string>();
         if (previous != null)
         {
            addParams.Add("previous");
            addParams.Add(previous);
         }
         if (reason != null)
         {
            addParams.Add("reason");
            addParams.Add(reason);
         }

         var response = WebAgent.Post(string.Format(WikiPageEditUrl, Subreddit.Name), param, addParams.ToArray());


      }

      public void HidePage(string page, string revision)
      {
         var data = new
         {
            page = page,
            revision = revision,
            uh = Reddit.User.Modhash
         };
         var response = WebAgent.Post(string.Format(HideWikiPageUrl, Subreddit.Name), data);
      }

      public void RevertPage(string page, string revision)
      {
         var data = new
         {
            page = page,
            revision = revision,
            uh = Reddit.User.Modhash
         };
         var response = WebAgent.Post(string.Format(RevertWikiPageUrl, Subreddit.Name), data);
      }

      public void SetPageEditor(string page, string username, bool allow)
      {
         var data =new
         {
            page = page,
            username = username,
            uh = Reddit.User.Modhash
         };

         var response = WebAgent.Post(string.Format(allow ? WikiPageAllowEditorAddUrl : WikiPageAllowEditorDelUrl,
            Subreddit.Name), data);
      }

      #region Obsolete Getter Methods

      [Obsolete("Use PageNames property instead")]
      public IEnumerable<string> GetPageNames()
      {
         return PageNames;
      }

      [Obsolete("Use Revisions property instead")]
      public Listing<WikiPageRevision> GetRevisions()
      {
         return Revisions;
      }

      #endregion Obsolete Getter Methods
   }
}