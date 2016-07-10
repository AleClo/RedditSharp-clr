using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using System.Net.Http;

namespace RedditSharp
{
   public interface IWegAgent
   {
      CookieContainer Cookies { get; set; }

      string AuthCookie { get; set; }
      string AccessToken { get; set; }
      string UserAgent { get; set; }
      string RootDomain { get; set; }

      RateLimitMode RateLimit { get; set; }


      JToken Post(string url, object data = null, params string[] additionalFields);
      Task<JToken> PostAsync(string url, object data, params string[] additionalFields);

      JToken Get(string url);
      Task<JToken> GetAsync(string url);

      JToken ExecuteRequest(HttpRequestMessage request);
      Task<JToken> ExecuteRequestAsync(HttpRequestMessage request);
   }

   public enum RateLimitMode
   {
      /// <summary>
      /// Limits requests to one every two seconds
      /// </summary>
      Pace,

      /// <summary>
      /// Restricts requests to five per ten seconds
      /// </summary>
      SmallBurst,

      /// <summary>
      /// Restricts requests to thirty per minute
      /// </summary>
      Burst,

      /// <summary>
      /// Does not restrict request rate. ***NOT RECOMMENDED***
      /// </summary>
      None
   }
}
