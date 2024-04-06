namespace LatestFilingsEventPublisher.UnitTests;

public static class TestFeedData
{
    public const string FeedWithTwoEntries = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" ?>\r\n" +
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

    public const string FeedWithNoEntries = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" ?>\r\n" +
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
    "</feed>";
}
