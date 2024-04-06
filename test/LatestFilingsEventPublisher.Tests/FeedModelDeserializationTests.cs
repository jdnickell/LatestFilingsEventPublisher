using LatestFilingsEventPublisher.UnitTests;
using System.Xml.Serialization;
using Xunit;

namespace LatestFilingsEventPublisher.Tests;

public class FeedModelDeserializationTests
{
    /// <summary>
    /// Tests a sample RSS feed deserializes indicating the <see cref="FeedModel"/> class is correctly structured and annotated for XML serialization.
    /// </summary>
    [Fact]
    public void Deserialize_ValidInputWithEntries_ValidResult()
    {
        var serializer = new XmlSerializer(typeof(FeedModel.Feed));
        using TextReader reader = new StringReader(TestFeedData.FeedWithTwoEntries);

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

    /// <summary>
    /// Tests a sample RSS feed deserializes when there are NO new filings indicating the <see cref="FeedModel"/> class is correctly structured and annotated for XML serialization.
    /// </summary>
    [Fact]
    public void Deserialize_ValidInputWithNoEntries_ValidResult()
    {
        var serializer = new XmlSerializer(typeof(FeedModel.Feed));
        using TextReader reader = new StringReader(TestFeedData.FeedWithNoEntries);

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
        Assert.Null(deserializedFeed.Entries);
    }
}
