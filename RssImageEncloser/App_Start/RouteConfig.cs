﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace RssImageEncloser
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "c/{*rssUrl}",
                defaults: new { controller = "Convert", action = "Index" }
            );

            routes.MapRoute(
                name: "RedditFeed",
                url: "r/{subredditName}",
                defaults: new { controller = "Convert", action = "RedditFeed" }
            );

            routes.MapRoute(
                name: "GalPhoto",
                url: "galphoto",
                defaults: new { controller = "Convert", action = "GalPhoto" }
            );
        }
    }
}
