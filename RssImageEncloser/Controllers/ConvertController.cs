using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Xsl;
using Newtonsoft.Json.Linq;
using RedditSharp;
using RedditSharp.Things;
using RssImageEncloser.Models;

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
            var sw = new StringWriter();
            using (var xrt = XmlReader.Create(new StringReader(xslt)))
            using (var xri = XmlReader.Create(new StringReader(xml)))
            using (var xwo = XmlWriter.Create(sw, new XmlWriterSettings { OmitXmlDeclaration = true }))
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
            var items = new List<RssItem>();

            // First use Reddit's API to get the last 20 posts in the subreddit
            // RedditSharp: https://github.com/SirCmpwn/RedditSharp

            // get username/password from config
            var myAppSettings = ConfigurationManager.GetSection("myAppSettings") as MyAppSettings;
            // login
#pragma warning disable CS0618 // Type or member is obsolete
            var reddit = new Reddit(myAppSettings.Reddit.Name, myAppSettings.Reddit.Password);
#pragma warning restore CS0618 // Type or member is obsolete
            // reddit base uri
            var redditUri = new Uri("http://www.reddit.com");

            string[] subreddits = subredditName.Split('+');
            Subreddit subreddit = null;

            // grab 25 images from each feed

            foreach (string sr in subreddits)
            {
                // get the subreddit
                subreddit = reddit.GetSubreddit(sr);

                foreach (var post in subreddit.New.Take(50))
                {
                    // for now, only grab the single images, not the albums from imgur
                    if ((post.Domain == "i.imgur.com" || post.Domain == "i.redd.it") && post.Score > 0)
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

            string feed = ToRssFeed(items, subreddit);
            return Content(feed, "application/rss+xml", System.Text.Encoding.UTF8);
        }

        public async Task<ActionResult> GalPhoto()
        {
            // retrieve data from Discord
            // first, authenticate to get a token
            var myAppSettings = ConfigurationManager.GetSection("myAppSettings") as MyAppSettings;

            var request = WebRequest.CreateHttp("https://discordapp.com/api/v6/oauth2/token");
            request.Method = "POST";
            request.UserAgent = "EliteG19s";

            request.ContentType = "application/x-www-form-urlencoded";

            string encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                .GetBytes(myAppSettings.Discord.ClientId + ":" + myAppSettings.Discord.ClientSecret));
            request.Headers.Add("Authorization", "Basic " + encoded);

            var data = HttpUtility.ParseQueryString(string.Empty);
            data["grant_type"] = "client_credentials";
            data["scope"] = "identify connections messages.read";

            byte[] dataBytes = Encoding.ASCII.GetBytes(data.ToString());
            request.ContentLength = dataBytes.Length;
            var dataStream = await request.GetRequestStreamAsync();
            await dataStream.WriteAsync(dataBytes, 0, dataBytes.Length);
            dataStream.Close();

            string accessToken = null;

            try
            {
                using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new HttpStatusCodeResult(response.StatusCode, response.StatusDescription);
                    }

                    string responseBody;
                    using (var stream = new StreamReader(response.GetResponseStream()))
                    {
                        responseBody = await stream.ReadToEndAsync();
                    }

                    var cc = JObject.Parse(responseBody);
                    accessToken = cc.Value<string>("access_token");
                }
            }
            catch (WebException e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, new StreamReader(e.Response.GetResponseStream()).ReadToEnd());
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "No access token");
            }

            // now get messages from channel 338216104310603777 (EliteG19s - General)
            request = WebRequest.CreateHttp("https://discordapp.com/api/v6/channels/338216104310603777/messages");
            request.UserAgent = "EliteG19s";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;

            using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new HttpStatusCodeResult(response.StatusCode, response.StatusDescription);
                }

                string responseBody;
                using (var stream = new StreamReader(response.GetResponseStream()))
                {
                    responseBody = await stream.ReadToEndAsync();
                }

                var items = new List<RssItem>();

                var messages = JArray.Parse(responseBody);
                foreach (var message in messages)
                {

                }
            }

            return Content("OK");
        }

        public static string ConvertImageType(Uri url)
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
            var sb = new StringBuilder("<items>");
            foreach (var item in items)
            {
                sb.Append(item.ToXml());
            }

            string xslt = redditXslt.Replace("FEED_TITLE", subreddit.Title);
            return Transform(sb.ToString() + "</items>", xslt);
        }
    }
}