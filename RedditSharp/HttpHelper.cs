using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;

namespace RedditSharp
{
   internal static class HttpHelper
   {
      public static string UrlEncode(byte[] bytes)
      {
         return HttpUtility.UrlEncode(bytes);
      }

      public static string UrlEncode(byte[] bytes, int offset, int count)
      {
         return HttpUtility.UrlEncode(bytes,offset,count);
      }

      public static string UrlEncode(string s)
      {
         return HttpUtility.UrlEncode(s);
      }

      public static string UrlEncode(string s, Encoding e)
      {
         return HttpUtility.UrlEncode(s, e);
      }

      public static string UrlDecode(string s)
      {
         return HttpUtility.UrlDecode(s);
      }

      public static string UrlDecode(string s,Encoding e)
      {
         return HttpUtility.UrlDecode(s, e);
      }

      public static string UrlDecode(byte[] bytes, int offset, int count,Encoding e)
      {
         return HttpUtility.UrlDecode(bytes, offset, count, e);
      }

      public static string UrlDecode(byte[] bytes, Encoding e)
      {
         return HttpUtility.UrlDecode(bytes, e);
      }

      public static string HtmlEncode(string s)
      {
         return HttpUtility.HtmlEncode(s);
      }

      public static void HtmlEncode(string s,TextWriter output)
      {
         HttpUtility.HtmlEncode(s,output);
      }

      public static string HtmlDecode(string s)
      {
         return HttpUtility.HtmlDecode(s);
      }

      public static void HtmlDecode(string s, TextWriter output)
      {
         HttpUtility.HtmlDecode(s, output);
      }

      public static NameValueCollection ParseQueryString(string s)
      {
         return HttpUtility.ParseQueryString(s);
      }

      public static NameValueCollection ParseQueryString(string s,Encoding e)
      {
         return HttpUtility.ParseQueryString(s,e);
      }


   }
}
