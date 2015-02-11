using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace RssImageEncloser.Controllers
{
    public class ConvertController : Controller
    {
        private const string xslt = @"<?xml version=""1.0"" encoding=""UTF-8""?><xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:fo=""http://www.w3.org/1999/XSL/Format"" xmlns:media=""http://search.yahoo.com/mrss/""><xsl:template match=""@*|node()""><xsl:copy><xsl:apply-templates select=""@*|node()""/></xsl:copy></xsl:template><xsl:template match=""media:content""><enclosure x=""{@fileSize}"" type=""{@type}"" url=""{@url}""></enclosure></xsl:template></xsl:stylesheet>";

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
            StringWriter sw = new StringWriter();
            using (XmlReader xrt = XmlReader.Create(new StringReader(xslt)))
            using (XmlReader xri = XmlReader.Create(new StringReader(feed)))
            using (XmlWriter xwo = XmlWriter.Create(sw))
            {
                var xct = new XslCompiledTransform();
                xct.Load(xrt);
                xct.Transform(xri, xwo);
            }
            string output = sw.ToString();

            // provide the new rss feed as a download
            return Content(output, "application/rss+xml", System.Text.Encoding.Unicode);
        }
    }
}