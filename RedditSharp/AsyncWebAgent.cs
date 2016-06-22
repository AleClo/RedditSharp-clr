using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace RedditSharp
{
   public class AsyncWebAgent : WebAgent, IAsyncWebAgent, IDisposable
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",value);
         }
      }

      public string AuthCookie { get; set; }
      public CookieContainer Cookies { get; set; }
      // backward compatibility - this will probably go away
      public string Protocol => "https";
      public RateLimitMode RateLimit { get; set; }
      public string RootDomain { get; set; }

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

      RedditSharp.RateLimitMode IAsyncWebAgent.RateLimit
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


      public AsyncWebAgent()
      {
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

      public AsyncWebAgent(string oauthToken) : this()
      {
         if (!String.IsNullOrEmpty(AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
      }





      ~AsyncWebAgent()
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
         var response = client.SendAsync(request).Result;
         if (response.IsSuccessStatusCode)
         {
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
         var response = await client.SendAsync(request);
         if (response.IsSuccessStatusCode)
         {
            var strJson = await response.Content.ReadAsStringAsync();
            return GetJson(strJson);
         }
         else
         {
            //todo: sane exception handling
            throw new Exception("There was a problem");
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
            value += name + "=" + HttpUtility.UrlEncode(entry).Replace(";", "%3B").Replace("&", "%26") + "&";
         }

         if (additionalFields == null)
            return value;

         for (int i = 0; i < additionalFields.Length; i += 2)
         {
            var entry = Convert.ToString(additionalFields[i + 1]) ?? string.Empty;
            value += additionalFields[i] + "=" + HttpUtility.UrlEncode(entry).Replace(";", "%3B").Replace("&", "%26") +
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
            catch
            {
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

      private Uri BuildUri(string url)
      {
         Uri uri;
         if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
         {
            if (!Uri.TryCreate(String.Format("{0}://{1}{2}", Protocol, RootDomain, url), UriKind.Absolute, out uri))
               throw new Exception("Could not parse Uri");
         }

         return uri;
      }

      
      [MethodImpl(MethodImplOptions.Synchronized)]
   /*   protected virtual void EnforceRateLimit()
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
}
