using System.Xml.Serialization;

namespace LatestFilingsEventPublisher;

/// <summary>
/// Model reprensentation of the RSS feed for Latest Filings Received and Processed at the SEC.
/// Web: https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent
/// RSS: https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&CIK=&type=&company=&dateb=&owner=include&start=0&count=40&output=atom
/// </summary>
public class FeedModel
{
    [XmlRoot(ElementName = "feed", Namespace = "http://www.w3.org/2005/Atom")]
    public class Feed
    {
        [XmlElement(ElementName = "title")]
        public string? Title { get; set; }
        [XmlElement(ElementName = "link")]
        public required Link[] Links { get; set; }
        [XmlElement(ElementName = "id")]
        public string? Id { get; set; }
        [XmlElement(ElementName = "author")]
        public required Author Author { get; set; }
        [XmlElement(ElementName = "updated")]
        public DateTime Updated { get; set; }
        [XmlElement(ElementName = "entry")]
        public required Entry[] Entries { get; set; }
    }

    public class Author
    {
        [XmlElement(ElementName = "name")]
        public string? Name { get; set; }
        [XmlElement(ElementName = "email")]
        public string? Email { get; set; }
    }

    public class Entry
    {
        [XmlElement(ElementName = "title")]
        public string? Title { get; set; }
        [XmlElement(ElementName = "link")]
        public required Link Link { get; set; }
        [XmlElement(ElementName = "summary")]
        public required Summary Summary { get; set; }
        [XmlElement(ElementName = "updated")]
        public DateTime Updated { get; set; }
        [XmlElement(ElementName = "category")]
        public required Category Category { get; set; }
        [XmlElement(ElementName = "id")]
        public required string Id { get; set; }
    }

    public class Link
    {
        [XmlAttribute(AttributeName = "rel")]
        public string? Rel { get; set; }
        [XmlAttribute(AttributeName = "type")]
        public string? Type { get; set; }
        [XmlAttribute(AttributeName = "href")]
        public string? Href { get; set; }
    }

    public class Summary
    {
        [XmlAttribute(AttributeName = "type")]
        public string? Type { get; set; }
        [XmlText]
        public string? Value { get; set; }
    }

    public class Category
    {
        [XmlAttribute(AttributeName = "scheme")]
        public string? Scheme { get; set; }
        [XmlAttribute(AttributeName = "label")]
        public string? Label { get; set; }
        [XmlAttribute(AttributeName = "term")]
        public string? Term { get; set; }
    }
}