using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using RedditSharp.Things;
using System.Threading.Tasks;
using System.Web;


namespace RedditSharp
{
   /// <summary>
   /// Class to communicate with Reddit.com
   /// </summary>
   public class Reddit
   {
#region Constant Urls

      private const string SslLoginUrl = "https://ssl.reddit.com/api/login";
      private const string OAuthTokenUrl = "https://www.reddit.com/api/v1/access_token";
      private const string LoginUrl = "/api/login/username";
      private const string UserInfoUrl = "/user/{0}/about.json";
      private const string MeUrl = "/api/me.json";
      private const string OAuthMeUrl = "/api/v1/me.json";
      private const string SubredditAboutUrl = "/r/{0}/about.json";
      private const string ComposeMessageUrl = "/api/compose";
      private const string RegisterAccountUrl = "/api/register";
      private const string GetThingUrl = "/api/info.json?id={0}";
      private const string GetCommentUrl = "/r/{0}/comments/{1}/foo/{2}";
      private const string GetPostUrl = "{0}.json";
      private const string DomainUrl = "www.reddit.com";
      private const string OAuthDomainUrl = "oauth.reddit.com";
      private const string SearchUrl = "/search.json?q={0}&restrict_sr=off&sort={1}&t={2}";
      private const string UrlSearchPattern = "url:'{0}'";
      private const string NewSubredditsUrl = "/subreddits/new.json";
      private const string PopularSubredditsUrl = "/subreddits/popular.json";
      private const string GoldSubredditsUrl = "/subreddits/gold.json";
      private const string DefaultSubredditsUrl = "/subreddits/default.json";
      private const string SearchSubredditsUrl = "/subreddits/search.json?q={0}";


#endregion


      internal IAsyncWebAgent WebAgent { get; set; }

      /// <summary>
      /// Captcha solver instance to use when solving captchas.
      /// </summary>
      public ICaptchaSolver CaptchaSolver;

      /// <summary>
      /// The authenticated user for this instance.
      /// </summary>
      public AuthenticatedUser User { get; set; }

      /// <summary>
      /// Sets the Rate Limiting Mode of the underlying WebAgent
      /// </summary>
      public RateLimitMode RateLimit { get; set; }

      internal JsonSerializerSettings JsonSerializerSettings { get; set; }

      /// <summary>
      /// Gets the FrontPage using the current Reddit instance.
      /// </summary>
      public Subreddit FrontPage
      {
         get { return Subreddit.GetFrontPage(this); }
      }

      /// <summary>
      /// Gets /r/All using the current Reddit instance.
      /// </summary>
      public Subreddit RSlashAll
      {
         get { return Subreddit.GetRSlashAll(this); }
      }

      public Reddit()
      {
         WebAgent = new AsyncWebAgent();
         JsonSerializerSettings = new JsonSerializerSettings
         {
            CheckAdditionalContent = false,
            DefaultValueHandling = DefaultValueHandling.Ignore
         };
         CaptchaSolver = new ConsoleCaptchaSolver();
      }

      public Reddit(RateLimitMode limitMode)
         : this()
      {
         RateLimit = limitMode;
      }

      public Reddit(string accessToken)
         : this()
      {
         WebAgent.RootDomain = OAuthDomainUrl;
         WebAgent.AccessToken = accessToken;
         InitOrUpdateUser();
      }

      /// <summary>
      /// Creates a Reddit instance with the given WebAgent implementation
      /// </summary>
      /// <param name="agent">Implementation of IWebAgent interface. Used to generate requests.</param>
      public Reddit(IAsyncWebAgent agent)
      {
         WebAgent = agent;
         JsonSerializerSettings = new JsonSerializerSettings
         {
            CheckAdditionalContent = false,
            DefaultValueHandling = DefaultValueHandling.Ignore
         };
         CaptchaSolver = new ConsoleCaptchaSolver();
      }

      /// <summary>
      /// Creates a Reddit instance with the given WebAgent implementation
      /// </summary>
      /// <param name="agent">Implementation of IWebAgent interface. Used to generate requests.</param>
      /// <param name="initUser">Whether to run InitOrUpdateUser, requires <paramref name="agent"/> to have credentials first.</param>
      public Reddit(IAsyncWebAgent agent, bool initUser)
      {
         WebAgent = agent;
         JsonSerializerSettings = new JsonSerializerSettings
         {
            CheckAdditionalContent = false,
            DefaultValueHandling = DefaultValueHandling.Ignore
         };
         CaptchaSolver = new ConsoleCaptchaSolver();
         if (initUser) InitOrUpdateUser();
      }

      /// <summary>
      /// Logs in the current Reddit instance.
      /// </summary>
      /// <param name="username">The username of the user to log on to.</param>
      /// <param name="password">The password of the user to log on to.</param>
      /// <param name="useSsl">Whether to use SSL or not. (default: true)</param>
      /// <returns></returns>
      public AuthenticatedUser LogIn(string username, string password, string appId, string secret)
      {
         if (Type.GetType("Mono.Runtime") != null)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, ssl) => true;


         // get token
         var request = new HttpRequestMessage(HttpMethod.Post, OAuthTokenUrl);
         var auth = System.Text.Encoding.UTF8.GetBytes($"{appId}:{secret}");
         request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(auth));

         var content = new StringContent($"grant_type=password&username={HttpHelper.UrlEncode(username)}&password={HttpHelper.UrlEncode(password)}");
         request.Content = content;
         request.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded");

         var json = WebAgent.ExecuteRequest(request);
         if (json["error"] != null)
            throw new AuthenticationException("Incorrect login.",
               new Exception(json["error"].ToString()
               )
               );
         var token = json["access_token"].ToString();
         if (String.IsNullOrEmpty(token))
            throw new AuthenticationException("Couldn't get access token");

         WebAgent.AccessToken = token;
         InitOrUpdateUser();
         WebAgent.RootDomain = "oauth.reddit.com";
         return User;
      }

      /// <summary>
      /// Logs in the current Reddit instance using OAuth Implicit grant flow.
      /// </summary>
      /// <param name="clientId">The Client ID generated during app registration</param>
      /// <param name="redirectUri">The password of the user to log on to.</param>
      /// <param name="
      /// ">oauth scopes</param>
      /// <returns></returns>
      public AuthenticatedUser LogInImplicit(string clientId, string redirectUri, string username, string password, string[] scope)
      {

         throw new NotImplementedException("not done yet");
         if (Type.GetType("Mono.Runtime") != null)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, ssl) => true;



         var data = new
         {
            user = username,
            passwd = password,
            api_type = "json"
         };

         var json = WebAgent.Post(SslLoginUrl, data, null);
         // var json = JObject.Parse(result)["json"];
         if (json["errors"].Count() != 0)
            throw new AuthenticationException("Incorrect login.");

         InitOrUpdateUser();

         return User;
      }

      /// <summary>
      /// Logs in the current Reddit instance.
      /// </summary>
      /// <param name="username">The username of the user to log on to.</param>
      /// <param name="password">The password of the user to log on to.</param>
      /// <param name="useSsl">Whether to use SSL or not. (default: true)</param>
      /// <returns></returns>
      public async Task<AuthenticatedUser> LogInAsync(string username, string password)
      {
         if (Type.GetType("Mono.Runtime") != null)
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, ssl) => true;

         var data = new
         {
            user = username,
            passwd = password,
            api_type = "json"
         };

         var request = new HttpRequestMessage(HttpMethod.Post, SslLoginUrl);
         var content = new StringContent(JsonConvert.SerializeObject(data));
         content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

         request.Content = content;

         var json = await WebAgent.ExecuteRequestAsync(request);
         // var json = JObject.Parse(result)["json"];
         if (json["errors"].Count() != 0)
            throw new AuthenticationException("Incorrect login.");

         InitOrUpdateUser();

         return User;
      }

      public RedditUser GetUser(string name)
      {
         var json = WebAgent.Get(string.Format(UserInfoUrl, name));
         return new RedditUser().Init(this, json, WebAgent);
      }

      public Task<RedditUser> GetUserAsync(string name)
      {
         throw new NotImplementedException();
         /*
         var json = await WebAgent.GetAsync(string.Format(UserInfoUrl, name));
         return await new RedditUser().Init(this, json, WebAgent);
         */
      }

      /// <summary>
      /// Initializes the User property if it's null,
      /// otherwise replaces the existing user object
      /// with a new one fetched from reddit servers.
      /// </summary>
      public void InitOrUpdateUser()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://" + OAuthDomainUrl + OAuthMeUrl));
         var json = WebAgent.ExecuteRequest(request);
         User = new AuthenticatedUser().Init(this, json, WebAgent);
      }

#region Obsolete Getter Methods

      [Obsolete("Use User property instead")]
      public AuthenticatedUser GetMe()
      {
         return User;
      }

#endregion Obsolete Getter Methods

      public Subreddit GetSubreddit(string name)
      {
         if (name.StartsWith("r/"))
            name = name.Substring(2);
         if (name.StartsWith("/r/"))
            name = name.Substring(3);
         name = name.TrimEnd('/');
         return GetThing<Subreddit>(string.Format(SubredditAboutUrl, name));
      }

      /// <summary>
      /// Returns the subreddit. 
      /// </summary>
      /// <param name="name">The name of the subreddit</param>
      /// <returns>The Subreddit by given name</returns>
      public async Task<Subreddit> GetSubredditAsync(string name)
      {
         if (name.StartsWith("r/"))
            name = name.Substring(2);
         if (name.StartsWith("/r/"))
            name = name.Substring(3);
         name = name.TrimEnd('/');
         return await GetThingAsync<Subreddit>(string.Format(SubredditAboutUrl, name));
      }

      public Domain GetDomain(string domain)
      {
         if (!domain.StartsWith("http://") && !domain.StartsWith("https://"))
            domain = "http://" + domain;
         var uri = new Uri(domain);
         return new Domain(this, uri, WebAgent);
      }

      public JToken GetToken (Uri uri)
      {
         var url = uri.AbsoluteUri;
         if (url.EndsWith("/"))
            url = url.Remove(url.Length - 1);

         UriBuilder b = new UriBuilder("https", WebAgent.RootDomain);
         b.Path = uri.PathAndQuery;

         var json = WebAgent.Get(string.Format(GetPostUrl, b.Uri));

         return json[0]["data"]["children"].First;
      }

      public Post GetPost(Uri uri)
      {
         return new Post().Init(this, GetToken(uri), WebAgent);
      }

      public void ComposePrivateMessage(string subject, string body, string to, string captchaId = "",
         string captchaAnswer = "")
      {
         if (User == null)
            throw new Exception("User can not be null.");

         var data = new
         {
            api_type = "json",
            subject,
            text = body,
            to,
            uh = User.Modhash,
            iden = captchaId,
            captcha = captchaAnswer
         };

         var json = WebAgent.Post(ComposeMessageUrl, data);

         ICaptchaSolver solver = CaptchaSolver; // Prevent race condition

         if (json["json"]["errors"].Any() && json["json"]["errors"][0][0].ToString() == "BAD_CAPTCHA" && solver != null)
         {
            captchaId = json["json"]["captcha"].ToString();
            CaptchaResponse captchaResponse = solver.HandleCaptcha(new Captcha(captchaId));

            if (!captchaResponse.Cancel) // Keep trying until we are told to cancel
               ComposePrivateMessage(subject, body, to, captchaId, captchaResponse.Answer);
         }
      }

      public async Task ComposePrivateMessageAsync(string subject, string body, string to, string captchaId = "",
         string captchaAnswer = "")
      {
         if (User == null)
            throw new Exception("User can not be null.");

         var data = new
         {
            api_type = "json",
            subject,
            text = body,
            to,
            uh = User.Modhash,
            iden = captchaId,
            captcha = captchaAnswer
         };

         var json = await WebAgent.PostAsync(ComposeMessageUrl, data);

         ICaptchaSolver solver = CaptchaSolver; // Prevent race condition

         if (json["json"]["errors"].Any() && json["json"]["errors"][0][0].ToString() == "BAD_CAPTCHA" && solver != null)
         {
            captchaId = json["json"]["captcha"].ToString();
            CaptchaResponse captchaResponse = solver.HandleCaptcha(new Captcha(captchaId));

            if (!captchaResponse.Cancel) // Keep trying until we are told to cancel
               ComposePrivateMessage(subject, body, to, captchaId, captchaResponse.Answer);
         }
      }

      /// <summary>
      /// Registers a new Reddit user
      /// </summary>
      /// <param name="userName">The username for the new account.</param>
      /// <param name="passwd">The password for the new account.</param>
      /// <param name="email">The optional recovery email for the new account.</param>
      /// <returns>The newly created user account</returns>
      public AuthenticatedUser RegisterAccount(string userName, string passwd, string email = "")
      {
         var data = new
         {
            api_type = "json",
            email = email,
            passwd = passwd,
            passwd2 = passwd,
            user = userName
         };
         var json = WebAgent.Post(RegisterAccountUrl, data);

         return new AuthenticatedUser().Init(this, json, WebAgent);
         // TODO: Error
      }

      /// <summary>
      /// Registers a new Reddit user
      /// </summary>
      /// <param name="userName">The username for the new account.</param>
      /// <param name="passwd">The password for the new account.</param>
      /// <param name="email">The optional recovery email for the new account.</param>
      /// <returns>The newly created user account</returns>
      public async Task<AuthenticatedUser> RegisterAccountAsync(string userName, string passwd, string email = "")
      {
         var data = new
         {
            api_type = "json",
            email = email,
            passwd = passwd,
            passwd2 = passwd,
            user = userName
         };
         var json = await WebAgent.PostAsync(RegisterAccountUrl, data);

         return new AuthenticatedUser().Init(this, json, WebAgent);
         // TODO: Error
      }

      public Thing GetThingByFullname(string fullname)
      {
         var json = WebAgent.Get(string.Format(GetThingUrl, fullname));

         return Thing.Parse(this, json["data"]["children"][0], WebAgent);
      }

      public async Task<Thing> GetThingByFullnameAsync(string fullname)
      {
         var json = await WebAgent.GetAsync(string.Format(GetThingUrl, fullname));
         return Thing.Parse(this, json["data"]["children"][0], WebAgent);
      }

      public Comment GetComment(string subreddit, string name, string linkName)
      {
         try
         {
            if (linkName.StartsWith("t3_"))
               linkName = linkName.Substring(3);
            if (name.StartsWith("t1_"))
               name = name.Substring(3);

            var url = string.Format(GetCommentUrl, subreddit, linkName, name);
            return GetComment(new Uri(url));
         }
         catch (WebException)
         {
            return null;
         }
      }

      public async Task<Comment> GetCommentAsync(string subreddit, string name, string linkName)
      {
         try
         {
            if (linkName.StartsWith("t3_"))
               linkName = linkName.Substring(3);
            if (name.StartsWith("t1_"))
               name = name.Substring(3);

            var url = string.Format(GetCommentUrl, subreddit, linkName, name);
            return GetCommentAsync(new Uri(url)).Result;
         }
         catch (WebException)
         {
            return null;
         }
      }

      public Comment GetComment(Uri uri)
      {
         var url = string.Format(GetPostUrl, uri.AbsoluteUri);
         var json = WebAgent.Get(url);

         var sender = new Post().Init(this, json[0]["data"]["children"][0], WebAgent);
         return new Comment().Init(this, json[1]["data"]["children"][0], WebAgent, sender);
      }

      public async Task<Comment> GetCommentAsync(Uri uri)
      {
         var url = string.Format(GetPostUrl, uri.AbsoluteUri);
         var json = await WebAgent.GetAsync(url);

         var sender = new Post().Init(this, json[0]["data"]["children"][0], WebAgent);
         return new Comment().Init(this, json[1]["data"]["children"][0], WebAgent, sender);
      }

      public Listing<T> SearchByUrl<T>(string url) where T : Thing
      {
         var urlSearchQuery = string.Format(UrlSearchPattern, url);
         return Search<T>(urlSearchQuery);
      }

      public Listing<T> Search<T>(string query, Sorting sortE = Sorting.Relevance, TimeSorting timeE = TimeSorting.All)
         where T : Thing
      {
         string sort = sortE.ToString().ToLower();
         string time = timeE.ToString().ToLower();
         return new Listing<T>(this, string.Format(SearchUrl, query, sort, time), WebAgent);
      }

#region SubredditSearching

      /// <summary>
      /// Returns a Listing of newly created subreddits.
      /// </summary>
      /// <returns></returns>
      public Listing<Subreddit> GetNewSubreddits()
      {
         return new Listing<Subreddit>(this, NewSubredditsUrl, WebAgent);
      }

      /// <summary>
      /// Returns a Listing of the most popular subreddits.
      /// </summary>
      /// <returns></returns>
      public Listing<Subreddit> GetPopularSubreddits()
      {
         return new Listing<Subreddit>(this, PopularSubredditsUrl, WebAgent);
      }

      /// <summary>
      /// Returns a Listing of Gold-only subreddits. This endpoint will not return anything if the authenticated Reddit account does not currently have gold.
      /// </summary>
      /// <returns></returns>
      public Listing<Subreddit> GetGoldSubreddits()
      {
         return new Listing<Subreddit>(this, GoldSubredditsUrl, WebAgent);
      }

      /// <summary>
      /// Returns the Listing of default subreddits.
      /// </summary>
      /// <returns></returns>
      public Listing<Subreddit> GetDefaultSubreddits()
      {
         return new Listing<Subreddit>(this, DefaultSubredditsUrl, WebAgent);
      }

      /// <summary>
      /// Returns the Listing of subreddits related to a query.
      /// </summary>
      /// <returns></returns>
      public Listing<Subreddit> SearchSubreddits(string query)
      {
         return new Listing<Subreddit>(this, string.Format(SearchSubredditsUrl, query), WebAgent);
      }

#endregion SubredditSearching

#region Helpers

      protected async internal Task<T> GetThingAsync<T>(string url) where T : Thing
      {
         var json = await WebAgent.GetAsync(url);
         var ret = await Thing.ParseAsync(this, json, WebAgent);
         return (T)ret;
      }

      protected internal T GetThing<T>(string url) where T : Thing
      {
         var json = WebAgent.Get(url);
         return (T)Thing.Parse(this, json, WebAgent);
      }

#endregion
   }
}