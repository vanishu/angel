using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace NameUniquenessCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fullContactAPI = "https://api.fullcontact.com/v2/name/stats.json?name={0}&apiKey=76ae3732151391f3";
            const string whitePagesAPI = "http://names.whitepages.com/{0}/{1}";

            Console.WriteLine("Full Name? ");
            String name = Console.ReadLine();
            String firstName = null, lastName = null;

            try
            {
                if (name.Contains(" "))
                {
                    var tokens = name.Split(new char[] { ' ' });
                    firstName = tokens[0];
                    lastName = tokens[tokens.Length - 1];
                    using (WebClient wc = new WebClient())
                    {
                        String response = wc.DownloadString(String.Format(whitePagesAPI, WebUtility.UrlEncode(firstName), WebUtility.UrlEncode(lastName)));
                        int target = response.IndexOf("head_count");
                        String[] targetTokens = response.Substring(target, 100).Split(new char[] { '<', '>' });
                        Console.WriteLine("\n-> " + targetTokens[3].Trim() + " people in the U.S. have this name");
                    }
                }

                else
                {
                    using (WebClient wc = new WebClient())
                    {
                        String response = wc.DownloadString(String.Format(fullContactAPI, name.Trim()));
                        JObject jobject = JObject.Parse(response);
                        Console.WriteLine("Given Name - Rank: " + jobject["name"]["given"]["rank"] + " - Count: " + jobject["name"]["given"]["count"]);
                        Console.WriteLine("Family Name - Rank: " + jobject["name"]["family"]["rank"] + " - Count: " + jobject["name"]["family"]["count"]);
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine("\nERROR - Cannot determine uniqueness");
            }

            Console.ReadLine();
        }
    }
}
