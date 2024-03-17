using System.Xml.Serialization;
using Xunit;

namespace LatestFilingsEventPublisher.Tests;

public class FeedDeserializationTests
{
    public static string ExampleFeed => "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" ?>\r\n" +
        "<feed xmlns=\"http://www.w3.org/2005/Atom\">\r\n" +
            "<title>Latest Filings - Sun, 17 Mar 2024 13:35:35 EDT</title>\r\n" +
            "<link rel=\"alternate\" href=\"/cgi-bin/browse-edgar?action=getcurrent\"/>\r\n" +
            "<link rel=\"self\" href=\"/cgi-bin/browse-edgar?action=getcurrent\"/>\r\n" +
            "<id>https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent</id>\r\n" +
            "<author>" +
                "<name>Webmaster</name>" +
                "<email>webmaster@sec.gov</email>" +
            "</author>\r\n" +
            "<updated>2024-03-17T13:35:35-04:00</updated>\r\n" +
            "<entry>\r\n" +
                "<title>4 - ESGEN LLC (0001865514) (Reporting)</title>\r\n" +
                "<link rel=\"alternate\" type=\"text/html\" href=\"https://www.sec.gov/Archives/edgar/data/1865514/000095017024032448/0000950170-24-032448-index.htm\"/>\r\n" +
                "<summary type=\"html\">\r\n &lt;b&gt;Filed:&lt;/b&gt; 2024-03-15 &lt;b&gt;AccNo:&lt;/b&gt; 0000950170-24-032448 &lt;b&gt;Size:&lt;/b&gt; 9 KB\r\n</summary>\r\n" +
                "<updated>2024-03-15T21:45:18-04:00</updated>\r\n" +
                "<category scheme=\"https://www.sec.gov/\" label=\"form type\" term=\"4\"/>\r\n" +
                "<id>urn:tag:sec.gov,2008:accession-number=0000950170-24-032448</id>\r\n" +
            "</entry>\r\n" +
            "<entry>\r\n" +
                "<title>4 - Zeo Energy Corp. (0001865506) (Issuer)</title>\r\n" +
                "<link rel=\"alternate\" type=\"text/html\" href=\"https://www.sec.gov/Archives/edgar/data/1865506/000095017024032448/0000950170-24-032448-index.htm\"/>\r\n" +
                "<summary type=\"html\">\r\n &lt;b&gt;Filed:&lt;/b&gt; 2024-03-15 &lt;b&gt;AccNo:&lt;/b&gt; 0000950170-24-032448 &lt;b&gt;Size:&lt;/b&gt; 9 KB\r\n</summary>\r\n" +
                "<updated>2024-03-15T21:45:18-04:00</updated>\r\n" +
                "<category scheme=\"https://www.sec.gov/\" label=\"form type\" term=\"4\"/>\r\n" +
            "<id>urn:tag:sec.gov,2008:accession-number=0000950170-24-032448</id>\r\n" +
            "</entry>" +
        "</feed>";

    /// <summary>
    /// Tests a samlple RSS feed deserializes indicating the <see cref="FeedModel"/> class is correctly structured and annotated for XML serialization.
    /// </summary>
    [Fact]
    public void Deserialize_ValidInput_ValidResult()
    {
        var serializer = new XmlSerializer(typeof(FeedModel.Feed));
        using TextReader reader = new StringReader(ExampleFeed);

        var deserializedFeed = serializer.Deserialize(reader) as FeedModel.Feed;

        Assert.NotNull(deserializedFeed);
        Assert.Equal("Latest Filings - Sun, 17 Mar 2024 13:35:35 EDT", deserializedFeed.Title);
        Assert.NotNull(deserializedFeed.Links);
        Assert.Equal(2, deserializedFeed.Links.Length);
        Assert.Equal("alternate", deserializedFeed.Links[0].Rel);
        Assert.Equal("self", deserializedFeed.Links[1].Rel);
        Assert.Equal("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent", deserializedFeed.Id);
        Assert.NotNull(deserializedFeed.Author);
        Assert.Equal("Webmaster", deserializedFeed.Author.Name);
        Assert.Equal("webmaster@sec.gov", deserializedFeed.Author.Email);
        Assert.True(deserializedFeed.Updated > DateTime.MinValue);
        Assert.NotNull(deserializedFeed.Entries);
        Assert.Equal(2, deserializedFeed.Entries.Length);
        Assert.Equal("4 - ESGEN LLC (0001865514) (Reporting)", deserializedFeed.Entries[0].Title);
        Assert.Equal("4 - Zeo Energy Corp. (0001865506) (Issuer)", deserializedFeed.Entries[1].Title);
        Assert.Equal("https://www.sec.gov/Archives/edgar/data/1865514/000095017024032448/0000950170-24-032448-index.htm", deserializedFeed.Entries[0].Link.Href);
        Assert.Equal("https://www.sec.gov/Archives/edgar/data/1865506/000095017024032448/0000950170-24-032448-index.htm", deserializedFeed.Entries[1].Link.Href);
        Assert.True(deserializedFeed.Entries[0].Summary.Type == "html");
        Assert.Equal("\n <b>Filed:</b> 2024-03-15 <b>AccNo:</b> 0000950170-24-032448 <b>Size:</b> 9 KB\n", deserializedFeed.Entries[0].Summary.Value);
        Assert.True(deserializedFeed.Entries[1].Summary.Type == "html");
        Assert.Contains("\n <b>Filed:</b> 2024-03-15 <b>AccNo:</b> 0000950170-24-032448 <b>Size:</b> 9 KB\n", deserializedFeed.Entries[1].Summary.Value);
        Assert.True(deserializedFeed.Entries[0].Updated > DateTime.MinValue);
        Assert.True(deserializedFeed.Entries[1].Updated > DateTime.MinValue);
        Assert.Equal("form type", deserializedFeed.Entries[0].Category.Label);
        Assert.Equal("form type", deserializedFeed.Entries[1].Category.Label);
        Assert.Equal("https://www.sec.gov/", deserializedFeed.Entries[0].Category.Scheme);
        Assert.Equal("https://www.sec.gov/", deserializedFeed.Entries[1].Category.Scheme);
        Assert.Equal("4", deserializedFeed.Entries[0].Category.Term);
        Assert.Equal("4", deserializedFeed.Entries[1].Category.Term);
        Assert.Equal("urn:tag:sec.gov,2008:accession-number=0000950170-24-032448", deserializedFeed.Entries[0].Id);
        Assert.Equal("urn:tag:sec.gov,2008:accession-number=0000950170-24-032448", deserializedFeed.Entries[1].Id);
    }
}
