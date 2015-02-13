using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RssImageEncloser
{
    public class MyAppSettings : ConfigurationSection
    {
        [ConfigurationProperty("reddit")]
        public RedditConfigElement Reddit {
            get { return base["reddit"] as RedditConfigElement; }
        }
    }

    public class RedditConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string)this["password"]; }
        }

    }
}