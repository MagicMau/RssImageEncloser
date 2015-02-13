using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RssImageEncloser.Models
{
    public class RssItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Uri Link { get; set; }
        public DateTime Date { get; set; }
        public Uri Image { get; set; }
        public string Type { get; set; }
        public Uri Thumbnail { get; set; }


        public string ToXml()
        {
            return string.Format(
                @"<item title=""{0}"" link=""{1}"" description=""{2}"" date=""{3}"" image=""{4}"" type=""{5}"" thumbnail=""{6}"" />",
                Title, Link, Description, Date, Image, Type, Thumbnail
                );
        }
    }
}