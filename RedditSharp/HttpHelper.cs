using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;


namespace RedditSharp
{
   internal static class HttpHelper
   {
      //public static string UrlEncode(byte[] bytes)
      //{
      //   return WebUtility.UrlEncode(bytes);
      //}

      //public static string UrlEncode(byte[] bytes, int offset, int count)
      //{
      //   return WebUtility.UrlEncode(bytes,offset,count);
      //}

      public static string UrlEncode(string s)
      {
         return WebUtility.UrlEncode(s);
      }

      //public static string UrlEncode(string s, Encoding e)
      //{
      //   return WebUtility.UrlEncode(s, e);
      //}

      public static string UrlDecode(string s)
      {
         return WebUtility.UrlDecode(s);
      }

      //public static string UrlDecode(string s,Encoding e)
      //{
      //   return WebUtility.UrlDecode(s, e);
      //}

      //public static string UrlDecode(byte[] bytes, int offset, int count,Encoding e)
      //{
      //   return WebUtility.UrlDecode(bytes, offset, count, e);
      //}

      //public static string UrlDecode(byte[] bytes, Encoding e)
      //{
      //   return WebUtility.UrlDecode(bytes, e);
      //}

      public static string HtmlEncode(string s)
      {
         return WebUtility.HtmlEncode(s);
      }

      //public static void HtmlEncode(string s,TextWriter output)
      //{
      //   WebUtility.HtmlEncode(s,output);
      //}

      public static string HtmlDecode(string s)
      {
         return WebUtility.HtmlDecode(s);
      }

      //public static void HtmlDecode(string s, TextWriter output)
      //{
      //   WebUtility.HtmlDecode(s, output);
      //}

      public static Dictionary<string, StringValues> ParseQueryString(string s)
      {
            return QueryHelpers.ParseQuery(s);
        // return WebUtility.ParseQueryString(s);
      }

      //public static NameValueCollection ParseQueryString(string s,Encoding e)
      //{
      //   return WebUtility.ParseQueryString(s,e);
      //}


   }
}
