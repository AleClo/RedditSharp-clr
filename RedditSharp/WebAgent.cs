using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RedditSharp
{
   public class WegAgent : IWegAgent, IDisposable
   {
      private HttpClient client;
      private HttpClientHandler handler;

      private string accessToken;

      public string AccessToken
      {
         get { return this.accessToken; }

         set
         {
            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
         }
      }

      public string AuthCookie { get; set; }
      public CookieContainer Cookies { get; set; }
      // backward compatibility - this will probably go away
      public string Protocol => "https";
      public RateLimitMode RateLimit { get; set; }
      public string RootDomain { get; set; }

      private int[] throttleValues = new int[3];
      private DateTime lastRequest;

      private string userAgent;
      public string UserAgent
      {
         get { return userAgent; }
         set
         {
            userAgent = value;
            if (client != null)
            {
               client.DefaultRequestHeaders.UserAgent.Clear();
               client.DefaultRequestHeaders.UserAgent.ParseAdd("Pimabot/1.0");
            }
         }
      }

      RedditSharp.RateLimitMode IWegAgent.RateLimit
      {
         get
         {
            throw new NotImplementedException();
         }

         set
         {
            throw new NotImplementedException();
         }
      }

      private ApiThrottler throttle;


      public WegAgent()
      {
         throttle = new ApiThrottler();
         client = new HttpClient();
         Cookies = new CookieContainer();
         UserAgent = "";
         RootDomain = "www.reddit.com";
         handler = new HttpClientHandler
         {
            CookieContainer = this.Cookies,
         };
         client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
         client.DefaultRequestHeaders.UserAgent.ParseAdd($"{UserAgent} - with RedditSharp by sircmpwn - mods by pimanac/1.0");

      }

      public WegAgent(string oauthToken) : this()
      {
         if (!String.IsNullOrEmpty(AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
      }

      ~WegAgent()
      {
         Dispose(true);
      }

      public JToken Post(string url, object data = null, params string[] additionalFields)
      {
         using (var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(url)))
         {
            var content = new StringContent(GetPostBody(data, additionalFields));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Content = content;

            return ExecuteRequest(request);
         }
      }

      public async Task<JToken> PostAsync(string url, object data, params string[] additionalFields)
      {
         using (var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(url)))
         {
            var content = new StringContent(GetPostBody(HttpMethod.Post, url));
            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded");
            request.Content = content;

            return await ExecuteRequestAsync(request);
         }
      }

      public JToken Get(string url)
      {
         using (var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(url)))
         {
            return ExecuteRequest(request);
         }
      }

      public async Task<JToken> GetAsync(string url)
      {
         using (var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(url)))
         {
            return await ExecuteRequestAsync(request);
         }
      }

      public JToken ExecuteRequest(HttpRequestMessage request)
      {
         // hack for the comment url
         var url = request.RequestUri.OriginalString;
         if (!request.RequestUri.IsAbsoluteUri)
            request.RequestUri = BuildUri(url);


         throttle.DoThrottle();

         var response = client.SendAsync(request).Result;
         if (response.IsSuccessStatusCode)
         {
            GetThrottleHeaders(response);
            var strJson = response.Content.ReadAsStringAsync().Result;
            return GetJson(strJson);
         }
         else
         {
            //todo: sane exception handling
            throw new Exception("There was a problem");
         }
      }

      public async Task<JToken> ExecuteRequestAsync(HttpRequestMessage request)
      {
         // hack for the comment url
         var url = request.RequestUri.OriginalString;
         if (request.RequestUri.Scheme != Uri.UriSchemeHttp && request.RequestUri.Scheme != Uri.UriSchemeHttps)
            request.RequestUri = BuildUri(url);

         lock (throttle)
         {
            throttle.DoThrottle();
         }

         var response = await client.SendAsync(request);
         if (response.IsSuccessStatusCode)
         {
            GetThrottleHeaders(response);
            var strJson = await response.Content.ReadAsStringAsync();
            return GetJson(strJson);
         }
         else
         {
            //todo: sane exception handling
            throw new Exception("There was a problem");
         }
      }

      //todo: this is going ot give me threading problems methinks
      private void GetThrottleHeaders(HttpResponseMessage response)
      {
         /*
          * Clients connecting via OAuth2 may make up to 60 requests per minute. Monitor the following response headers to ensure that you're not exceeding the limits:
            X-Ratelimit-Used: Approximate number of requests used in this period
            X-Ratelimit-Remaining: Approximate number of requests left to use
            X-Ratelimit-Reset: Approximate number of seconds to end of period
          */

         try
         {
            throttle.XRateLimitUsed = Int32.Parse(response.Headers.GetValues("x-ratelimit-used").FirstOrDefault());
            throttle.XRemaining = (int)Decimal.Parse(response.Headers.GetValues("x-ratelimit-remaining").FirstOrDefault());
            throttle.XReset = (int)Decimal.Parse(response.Headers.GetValues("x-ratelimit-reset").FirstOrDefault());

            System.Diagnostics.Debug.WriteLine($"XUsed: {throttle.XRateLimitUsed} XRemain: {throttle.XRemaining} XReset {throttle.XReset}");

         }
         catch (Exception ex)
         {
            // dont care
         }
      }

      private string GetPostBody(object data, params string[] additionalFields)
      {
         var type = data.GetType();
         var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
         string value = "";
         foreach (var property in properties)
         {
            var attr =
               property.GetCustomAttributes(typeof(RedditAPINameAttribute), false).FirstOrDefault() as
                  RedditAPINameAttribute;
            string name = attr == null ? property.Name : attr.Name;
            var entry = Convert.ToString(property.GetValue(data, null));
            value += name + "=" + HttpHelper.UrlEncode(entry).Replace(";", "%3B").Replace("&", "%26") + "&";
         }

         if (additionalFields == null)
            return value;

         for (int i = 0; i < additionalFields.Length; i += 2)
         {
            var entry = Convert.ToString(additionalFields[i + 1]) ?? string.Empty;
            value += additionalFields[i] + "=" + HttpHelper.UrlEncode(entry).Replace(";", "%3B").Replace("&", "%26") +
                     "&";
         }
         value = value.Remove(value.Length - 1); // Remove trailing &

         return value;
      }

      private JToken GetJson(string result)
      {
         JToken json;
         if (!string.IsNullOrEmpty(result))
         {
            json = JToken.Parse(result);

            if (json is JArray)
               return json as JArray;

            else
            {
               json = json;
            }
            try
            {
               if (json["json"] != null)
               {
                  json = json["json"]; //get json object if there is a root node
               }
               if (json["error"] != null)
               {
                  switch (json["error"].ToString())
                  {
                     case "404":
                        throw new Exception("File Not Found");
                     case "403":
                        throw new Exception("Restricted");
                     case "invalid_grant":
                        //Refresh authtoken
                        //AccessToken = authProvider.GetRefreshToken();
                        //ExecuteRequest(request);
                        break;
                  }
               }
            }
            catch (Exception ex)
            {
               System.Diagnostics.Debug.WriteLine(ex.Message);
            }
         }
         else
         {
            throw new NotImplementedException("Soemthignn must be done here");
            /*
            json =
               JToken.Parse("{'method':'" + response.Method + "','uri':'" + response.ResponseUri.AbsoluteUri +
                            "','status':'" + response.StatusCode.ToString() + "'}");

   */
         }
         return json;
      }

      public Uri BuildUri(string url)
      {
         Uri uri;
         if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
         {
            if (!Uri.TryCreate(String.Format("{0}://{1}{2}", Protocol, RootDomain, url), UriKind.Absolute, out uri))
               throw new Exception("Could not parse Uri");
         }

         return uri;
      }

      /*   
        [MethodImpl(MethodImplOptions.Synchronized)]
       protected virtual void EnforceRateLimit()
        {
           switch (RateLimit)
           {
              case WebAgent.RateLimitMode.Pace:
                 while ((DateTime.UtcNow - _lastRequest).TotalSeconds < 2) // Rate limiting
                    Thread.Sleep(250);
                 _lastRequest = DateTime.UtcNow;
                 break;
              case WebAgent.RateLimitMode.SmallBurst:
                 if (_requestsThisBurst == 0 || (DateTime.UtcNow - _burstStart).TotalSeconds >= 10)
                 //this is first request OR the burst expired
                 {
                    _burstStart = DateTime.UtcNow;
                    _requestsThisBurst = 0;
                 }
                 if (_requestsThisBurst >= 5) //limit has been reached
                 {
                    while ((DateTime.UtcNow - _burstStart).TotalSeconds < 10)
                       Thread.Sleep(250);
                    _burstStart = DateTime.UtcNow;
                    _requestsThisBurst = 0;
                 }
                 _lastRequest = DateTime.UtcNow;
                 _requestsThisBurst++;
                 break;
              case WebAgent.RateLimitMode.Burst:
                 if (_requestsThisBurst == 0 || (DateTime.UtcNow - _burstStart).TotalSeconds >= 60)
                 //this is first request OR the burst expired
                 {
                    _burstStart = DateTime.UtcNow;
                    _requestsThisBurst = 0;
                 }
                 if (_requestsThisBurst >= 30) //limit has been reached
                 {
                    while ((DateTime.UtcNow - _burstStart).TotalSeconds < 60)
                       Thread.Sleep(250);
                    _burstStart = DateTime.UtcNow;
                    _requestsThisBurst = 0;
                 }
                 _lastRequest = DateTime.UtcNow;
                 _requestsThisBurst++;
                 break;
           }
        }
        */


      // IDisposable
      public void Dispose()
      {
         Dispose(true);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (client != null)
               client.Dispose();
         }
      }
   }

   /// <summary>
   /// This class conforms to the reddit api.
   /// </summary>
   sealed class ApiThrottler : IDisposable
   {
      /// <summary>
      /// It is strongly advised that you leave this set to Burst or Pace. Reddit bans excessive
      /// requests with extreme predjudice.
      /// </summary>
      public RateLimitMode RateLimit { get; set; }

      /// <summary>
      /// The method by which the WebAgent will limit request rate
      /// </summary>
      public enum RateLimitMode
      {
         /// <summary>
         /// Limits requests to one per second
         /// </summary>
         Pace,

         /// <summary>
         /// Restricts requests to five per ten seconds
         /// </summary>
         SmallBurst,

         /// <summary>
         /// Restricts requests to sixty per minute
         /// </summary>
         Burst,

         /// <summary>
         /// Does not restrict request rate. ***NOT RECOMMENDED***
         /// </summary>
         None
      }


      public int? XRateLimitUsed;
      public int? XRemaining;
      public int? XReset = null;
      private Timer timer;

      private AutoResetEvent throttle = new AutoResetEvent(false);
      private int requestsThisPeriod = 0;
      private int requestsThisBurst = 0;

      private DateTime periodEnd;
      private DateTime lastRequest;

      public ApiThrottler()
      {
         timer = null;
      }

      [MethodImpl(MethodImplOptions.Synchronized)]
      public void DoThrottle()
      {
         // enforce the standard throttling

         TimeSpan ts;
         switch (RateLimit)
         {
            case RateLimitMode.Pace:
               ts = DateTime.Now.Subtract(lastRequest);
               if (ts.Milliseconds < 1000)
                  Thread.Sleep(ts.Milliseconds);
               break;
            case RateLimitMode.SmallBurst:
               throw new NotImplementedException();
               break;
            case RateLimitMode.Burst:
               throw new NotImplementedException();
               break;
            case RateLimitMode.None:
            default:
               // do nothing, rely on the reddit api throttle alone
               break;
         }

         if (XRateLimitUsed == null || XRemaining == null || XReset == null)
            // we probably aren't on outh
            return;

         if (timer == null)
         {
            // when is the expected end of the period?  with a little wiggle.
            periodEnd = DateTime.Now.AddSeconds((double)XReset.Value + 5);

            var ms = (XReset.Value + 5) * 1000;

            // start the timer
            timer = new Timer((state) =>
            {
#if DEBUG
               System.Diagnostics.Debug.WriteLine("throttle.Set()");
#endif
               // reset the period and allow the threads to continue
               throttle.Set();
               timer.Dispose();
               timer = null;
            }, null, ms, Timeout.Infinite);

#if DEBUG
            System.Diagnostics.Debug.WriteLine("Started timer for " + ms + " ms");
            System.Diagnostics.Debug.WriteLine("end time : " + periodEnd.ToString());
#endif
         }

         if (XRateLimitUsed > requestsThisPeriod)
            requestsThisPeriod = XRateLimitUsed.Value;

         // no reason to toe the line
         if (XRemaining <= 2)
         {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("throttle.WaitOne()");
#endif
            if (timer != null)
               throttle.WaitOne();
         }
         requestsThisPeriod++;
         requestsThisBurst++;
         lastRequest = DateTime.Now;
      }


      // IDisposable
      public void Dispose()
      {
         Dispose(true);
      }

      public void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (timer != null)
               timer.Dispose();
         }
      }

   }
}
