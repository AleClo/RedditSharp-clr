using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace RedditSharp
{
   public interface IWebAgent
   {
      
      CookieContainer Cookies { get; set; }
      string AuthCookie { get; set; }
      string AccessToken { get; set; }

      [Obsolete("This is going away",false)]
      HttpWebRequest CreateRequest(string url, string method);

      [Obsolete("This is going away", false)]
      HttpWebRequest CreateGet(string url);

      [Obsolete("This is going away", false)]
      HttpWebRequest CreatePost(string url);

      [Obsolete("This is going away", false)]
      string GetResponseString(Stream stream);

      [Obsolete("This is going away", false)]
      void WritePostBody(Stream stream, object data, params string[] additionalFields);

      [Obsolete("This is going away", false)]
      JToken CreateAndExecuteRequest(string url);

      [Obsolete("This is going away", false)]
      JToken ExecuteRequest(HttpWebRequest request);
   }
}