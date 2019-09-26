using System.Configuration;

namespace RssImageEncloser
{
    public class MyAppSettings : ConfigurationSection
    {
        [ConfigurationProperty("reddit")]
        public RedditConfigElement Reddit => base["reddit"] as RedditConfigElement;

        [ConfigurationProperty("discord")]
        public DiscordConfigElement Discord => base["discord"] as DiscordConfigElement;
    }

    public class RedditConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password => (string)this["password"];
    }

    public class DiscordConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("clientId", IsRequired = true)]
        public string ClientId => (string)this["clientId"];

        [ConfigurationProperty("clientSecret", IsRequired = true)]
        public string ClientSecret => (string)this["clientSecret"];
    }
}