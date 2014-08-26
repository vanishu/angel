using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Simon
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                Task runTask = RunRest(arg);
                runTask.Wait();
            }
        }


        private static async Task RunRest(string args)
        {
            string[] userSearchTerms = ConfigurationManager.AppSettings["searchValues"].Split(',');

            var auth = new ApplicationOnlyAuthorizer
            {
                CredentialStore = new InMemoryCredentialStore
                {
                    ConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"],
                    ConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"]
                }
            };

            await auth.AuthorizeAsync();

            var ctx = new TwitterContext(auth);

            string[] searchTerms = args.Split(',');

            for (int i = 0; i < searchTerms.Length; i++)
                searchTerms[i] = searchTerms[i].Trim();

            string query =
                string.Join(" OR ",
                    (from term in searchTerms
                     select "\"" + term.Trim() + "\"")
                    .ToArray());

            var search =
                await
                (from srch in ctx.Search
                 where srch.Type == SearchType.Search &&
                       srch.Query == query
                 select srch)
                .SingleOrDefaultAsync();

            var exactResults =
                (from tweet in search.Statuses
                 where searchTerms.Contains(tweet.Text.ToLower().Trim(), StringComparer.OrdinalIgnoreCase)
                 select tweet)
                .ToList();

            exactResults.ForEach(tweet =>
                {
                    var now = DateTime.Now;
                    var tweetedTime = tweet.CreatedAt.ToLocalTime();

                    if ((now - tweetedTime).Minutes > Int32.Parse(ConfigurationManager.AppSettings["tweetedInTheLastXMinutes"])) return;

                    string coordinates = "not available";
                    if (tweet.Coordinates != null)
                    {
                        coordinates = tweet.Coordinates.Latitude + "," + tweet.Coordinates.Longitude;
                    }

                    string place = "not available";
                    if (tweet.Place != null)
                    {
                        place = tweet.Place.FullName + "," + tweet.Place.Country + "," + tweet.Place.PlaceType;
                    }

                    Console.WriteLine(
                        "User Name: {0}, \r\nScreen Name: {1}, \r\nTweet: {2}, \r\nProfile Image URL: {3}, \r\nCreated At: {4}, \r\n" +
                        "Lat/Lon: {5}, \r\nBio: {6}, \r\nGeo Enabled: {7}, \r\nLocation: {8} \r\nPlace: {9}",
                        tweet.User.Name,
                        tweet.User.ScreenNameResponse,
                        tweet.Text,
                        tweet.User.ProfileImageUrl,
                        tweet.CreatedAt.ToLocalTime(),
                        coordinates,
                        tweet.User.Description,
                        tweet.User.GeoEnabled,
                        tweet.User.Location,
                        place);


                    // Get tweets that the user cares about
                    List<String> interestingTweets = new List<String>();
                    var tweets = (from myTweet in ctx.Status
                                  where myTweet.Type == StatusType.User &&
                                        myTweet.ScreenName == tweet.User.ScreenNameResponse
                                  select myTweet).ToList();

                    foreach (var myTweet in tweets)
                    {
                        if (userSearchTerms.Any(myTweet.Text.ToLower().Trim().Contains))
                        {
                            Console.WriteLine("++ Found: " + myTweet.Text);
                            interestingTweets.Add(myTweet.Text.Trim());
                        }
                    }

                    Console.WriteLine("\r\n\r\n");


                    // Write the HTML file
                    String htmlOutputFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "\\tweetdata.html";

                    if (!File.Exists(htmlOutputFile))
                    {
                        using (StreamWriter sw = new StreamWriter(htmlOutputFile, true))
                        {
                            sw.WriteLine(
                            @"<input style='height:70px; font-size:40px' type='button' value='Reload' onClick=""window.location = '/svupload/uploads/tweetdata.html?t=' + Math.random();"">
                              <br>" + DateTime.Now + "<br>");
                        }
                    }

                    using (StreamWriter sw = new StreamWriter(htmlOutputFile, true))
                    {
                        sw.WriteLine(
                        @"<h1>" + tweet.User.Name + @", @" + tweet.User.ScreenNameResponse + @"<br>
                        <img src='" + tweet.User.ProfileImageUrl.Replace("_normal", "_400x400") + @"' width=150 height=150><br>
                        </h1>
                        <div style='font-size:20px;'>
                        Tweet: <b>" + tweet.Text + @"</b><br>
                        Location: <b>" + tweet.User.Location + @"</b><br>
                        Bio: " + tweet.User.Description + @"<br>
                        Place: " + tweet.User.Location + @"<br><br>");

                        foreach (String str in interestingTweets)
                        {
                            sw.WriteLine("+<b><i>Tweet</i></b>: " + str + "<br>");
                        }

                        sw.WriteLine("</div>");
                    }

                });
        }
    }
}
