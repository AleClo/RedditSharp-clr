using System;
using System.Collections.Generic;
using System.Linq;
using RedditSharp;
using System.Security.Authentication;
using RedditSharp.Things;

namespace TestRedditSharp
{
   class Program
   {
      static void Main(string[] args)
      {
         Reddit reddit = null;
         var authenticated = false;
         while (!authenticated)
         {
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = ReadPassword();
            Console.Write("App Id  : ");
            var appId = ReadPassword();
            Console.Write("Secret  : ");
            var secret = ReadPassword();
            try
            {
               Console.WriteLine("Logging in...");
               reddit = new Reddit();
               reddit.LogIn(username, password, appId, secret);
               authenticated = reddit.User != null;
            }
            catch (AuthenticationException)
            {
               Console.WriteLine("Incorrect login.");
               authenticated = false;
            }
         }
         /*Console.Write("Create post? (y/n) [n]: ");
            var choice = Console.ReadLine();
            if (!string.IsNullOrEmpty(choice) && choice.ToLower()[0] == 'y')
            {
                Console.Write("Type a subreddit name: ");
                var subname = Console.ReadLine();
                var sub = reddit.GetSubreddit(subname);
                Console.WriteLine("Making test post");
                var post = sub.SubmitTextPost("RedditSharp test", "This is a test post sent from RedditSharp");
                Console.WriteLine("Submitted: {0}", post.Url);
            }
            else
            {
                Console.Write("Type a subreddit name: ");
                var subname = Console.ReadLine();
                var sub = reddit.GetSubreddit(subname);
                foreach (var post in sub.GetTop(FromTime.Week).Take(10))
                    Console.WriteLine("\"{0}\" by {1}", post.Title, post.Author);
            }*/
         Post post = (Post)reddit.GetThingByFullname("t3_434h6c");

         Console.WriteLine(post.SelfText);
         Console.WriteLine();
         Console.WriteLine();

         Console.WriteLine("Press any key to exit");
         Console.ReadKey(true);
      }

      public static string ReadPassword()
      {
         var passbits = new Stack<string>();
         //keep reading
         for (ConsoleKeyInfo cki = Console.ReadKey(true); cki.Key != ConsoleKey.Enter; cki = Console.ReadKey(true))
         {
            if (cki.Key == ConsoleKey.Backspace)
            {
               if (passbits.Count() > 0)
               {
                  //rollback the cursor and write a space so it looks backspaced to the user
                  Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                  Console.Write(" ");
                  Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                  passbits.Pop();
               }
            }
            else
            {
               Console.Write("*");
               passbits.Push(cki.KeyChar.ToString());
            }
         }
         string[] pass = passbits.ToArray();
         Array.Reverse(pass);
         Console.Write(Environment.NewLine);
         return string.Join(string.Empty, pass);
      }
   }
}