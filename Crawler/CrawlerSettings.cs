using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    internal class CrawlerSettings
    {
        public string WebsiteUrl { get; set; }
        public int NumberOfWords { get; set; }
        public string ExcludedWords { get; set; }
    }
}
