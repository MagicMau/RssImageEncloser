using RedditSharp;
using RedditSharp.Things;
using RssImageEncloser.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace RssImageEncloser.Controllers
{
    [NoCache]
    public class ConvertController : Controller
    {
        private const string xslt = @"<?xml version=""1.0"" encoding=""UTF-8""?><xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:fo=""http://www.w3.org/1999/XSL/Format"" xmlns:media=""http://search.yahoo.com/mrss/""><xsl:template match=""@*|node()""><xsl:copy><xsl:apply-templates select=""@*|node()""/></xsl:copy></xsl:template><xsl:template match=""media:content""><enclosure x=""{@fileSize}"" type=""{@type}"" url=""{@url}""></enclosure></xsl:template></xsl:stylesheet>";

        private const string redditXslt = @"<?xml version=""1.0"" encoding=""UTF-8""?> <xsl:stylesheet version=""1.0"" xmlns:fo=""http://www.w3.org/1999/XSL/Format""  xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:media=""http://search.yahoo.com/mrss/""> <xsl:template match=""items""> <rss version=""2.0""> <channel> <title>FEED_TITLE</title> <xsl:apply-templates select=""item"" /> </channel> </rss> </xsl:template>  <xsl:template match=""item""> <item> <title><xsl:value-of select=""@title"" /></title> <description><xsl:value-of select=""@description"" /></description> <link><xsl:value-of select=""@link"" /></link> <guid isPermaLink=""true""><xsl:value-of select=""@link"" /></guid> <pubDate><xsl:value-of select=""@date"" /></pubDate> <media:title><xsl:value-of select=""@title"" /></media:title> <media:thumbnail url=""{@thumbnail}"" /> <enclosure type=""{@type}"" url=""{@image}"" /> </item> </xsl:template>  </xsl:stylesheet>";

        // GET: Convert
        public ActionResult Index(string rssUrl)
        {
            // for some reason url is passed in as a single http:/, not http://
            rssUrl = rssUrl.Replace(":/", "://");

            // Grab a webclient, download the original rss feed
            var wc = new WebClient();
            string feed = wc.DownloadString(rssUrl);

            // transform the xml
            // transform media:content tags to enclosure tags for better compatibility with Windows Themes

            // provide the new rss feed as a download
            return Content(Transform(feed, xslt), "application/rss+xml", System.Text.Encoding.UTF8);
        }

        private string Transform(string xml, string xslt)
        {
            StringWriter sw = new StringWriter();
            using (XmlReader xrt = XmlReader.Create(new StringReader(xslt)))
            using (XmlReader xri = XmlReader.Create(new StringReader(xml)))
            using (XmlWriter xwo = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                var xct = new XslCompiledTransform();
                xct.Load(xrt);
                xct.Transform(xri, xwo);
            }
            string output = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + sw.ToString();
            return output;
        }


        public ActionResult RedditFeed(string subredditName)
        {
            List<RssItem> items = new List<RssItem>();

            // First use Reddit's API to get the last 20 posts in the subreddit
            // RedditSharp: https://github.com/SirCmpwn/RedditSharp

            // get username/password from config
            var myAppSettings = ConfigurationManager.GetSection("myAppSettings") as MyAppSettings;
            // login
            var reddit = new Reddit(myAppSettings.Reddit.Name, myAppSettings.Reddit.Password);
            // reddit base uri
            Uri redditUri = new Uri("http://www.reddit.com");

            string[] subreddits = subredditName.Split('+');
            Subreddit subreddit = null;

            // grab 25 images from each feed

            foreach (var sr in subreddits)
            {
                // get the subreddit
                subreddit = reddit.GetSubreddit(sr);
                
                foreach (var post in subreddit.New.Take(50))
                {
                    // for now, only grab the single images, not the albums from imgur
                    if (post.Domain == "i.imgur.com" && post.Score > 0)
                    {
                        string type = ConvertImageType(post.Url);
                        if (type != null)
                        {
                            items.Add(new RssItem
                            {
                                Title = post.Title,
                                Description = post.SelfTextHtml,
                                Link = new Uri(redditUri, post.Permalink),
                                Date = post.Created,
                                Image = post.Url,
                                Type = type,
                                Thumbnail = post.Thumbnail
                            });
                        }
                    }
                }
            }

            var feed = ToRssFeed(items, subreddit);
            return Content(feed, "application/rss+xml", System.Text.Encoding.UTF8);
        }

        public string ConvertImageType(Uri url)
        {
            string path = url.GetLeftPart(UriPartial.Path);
            int lastDot = path.LastIndexOf('.');
            if (lastDot > 0)
            {
                string ext = path.Substring(lastDot);
                switch (ext)
                {
                    case ".jpg": return "image/jpeg";
                    case ".png":
                    case ".bmp":
                        return "image/" + ext.Substring(1);
                }
            }
            return null;
        }

        public string ToRssFeed(IEnumerable<RssItem> items, Subreddit subreddit)
        {
            StringBuilder sb = new StringBuilder("<items>");
            foreach (var item in items)
            {
                sb.Append(item.ToXml());
            }

            string xslt = redditXslt.Replace("FEED_TITLE", subreddit.Title);
            return Transform(sb.ToString() + "</items>", xslt);
        }
    }
}