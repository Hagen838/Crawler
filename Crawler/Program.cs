using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Extensions.Configuration;

namespace Crawler
{
    internal class Program
    {
        static CrawlerSettings RetrieveSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            CrawlerSettings settings = config.GetRequiredSection("Settings").Get<CrawlerSettings>();

            if (string.IsNullOrEmpty(settings.WebsiteUrl))
            {
                // Instead of throwing, wrong setting could be replaced by a default one
                throw new ArgumentException("Website must be provided");
            }

            if (settings.NumberOfWords < 1)
            {
                // Instead of throwing, wrong setting could be replaced by a default one
                throw new ArgumentException("Number of words must be 1 or greater");
            }

            return settings;
        }

        static async Task<string> RetrieveWebPage(string url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        static List<KeyValuePair<string, int>> ParseWebPage(string webpage, Dictionary<string, int> wordsToIgnore)
        {
            Dictionary<string, int> occurences = new Dictionary<string, int>();

            // We want to ignore all XML stuff so we don't have words such as "class" or "div" in the results
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            XmlReader reader = XmlReader.Create(new StringReader(webpage), settings);

            reader.MoveToContent();

            // The two follwing booleans are used to ignore script and style blocks
            bool ignoreScript = false;
            bool ignoreStyle = false;
            bool isHistory = false;

            // Parse the file and display each of the nodes.
            while (true) {
                try
                {
                    if (!reader.Read())
                        break;
                } catch (XmlException e)
                {
                    // Error while reading XML
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine("Stopping parsing because of XML reader exception - Rest of page is ignored");
                }
            
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        //Console.Write("<{0}>", reader.Name);
                        if (reader.Name == "script")
                            ignoreScript = true;
                        if (reader.Name == "style")
                            ignoreStyle = true;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        // Beginning and end for History section are found with two respective sentences. Maybe there is a better way to do so.
                        if (reader.Value.StartsWith("Further information")) // Start of section History
                            isHistory = true;
                        else if (reader.Value.StartsWith("US Federal Trade Commission"))    // End of section History
                            isHistory = false;

                        // Ignore script and style blocks
                        if (!ignoreScript && !ignoreStyle && isHistory)
                        {
                            // Comparison must be case insensitive
                            string value = reader.Value.ToLower();

                            // Split string into words
                            foreach (string word in value.Split(' ', ',', ';', ':', '.', '-', '+', '_', '(', ')', '[', ']', '&', '"', '@', '/', '*', '$', '€', '^'))
                            {
                                // Ignore empty words and numbers and excluded words
                                if (word.Length > 0 && !word.All(char.IsDigit) && !wordsToIgnore.ContainsKey(word))
                                {
                                    if (occurences.ContainsKey(word))
                                        occurences[word]++;
                                    else
                                        occurences[word] = 1;
                                }
                            }
                        }
                        break;
                    //    break;
                    case XmlNodeType.EndElement:
                        //Console.Write("</{0}>", reader.Name);
                        if (reader.Name == "script")
                            ignoreScript = false;
                        if (reader.Name == "style")
                            ignoreStyle = false;
                        break;
                }
            }

            // Sort edictionary by values
            List<KeyValuePair<string, int>> sortedList = occurences.ToList();
            sortedList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
            return sortedList;
        }

        static async Task Main(string[] args)
        {
            try
            {
                CrawlerSettings settings = RetrieveSettings();

                // Get the webpage body an parse it
                string webpage = await RetrieveWebPage(settings.WebsiteUrl);

                //Console.WriteLine(webpage);

                // Create collection (hash) of words to ignore, all lower case for comparision purpose
                // Only the key is important. Value is not used.Duplicates are accepted and ignored
                Dictionary<string, int> wordsToIgnore = new Dictionary<string, int>();
                foreach (string word in settings.ExcludedWords.Split(','))
                {
                    string lowerWord = word.ToLower();
                    if (wordsToIgnore.ContainsKey(lowerWord))
                        wordsToIgnore[lowerWord]++;
                    else
                        wordsToIgnore[lowerWord] = 1;
                }

                // Count all words from all text and get the results in a dictionary where string=word and int=count
                var sortedList = ParseWebPage(webpage, wordsToIgnore);

                // Display highest occurences
                int count = 0;
                foreach (var value in sortedList)
                {
                    if (count++ >= settings.NumberOfWords)
                    {
                        break;
                    }

                    Console.WriteLine(value);
                }
            }
            catch
            {
                // There is nothing to do beside exiting the program.
                // Hwever, in actual production code, logging/telemetry would most probably be generated before exiting.
            }
        }
    }
}