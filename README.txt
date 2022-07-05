*** CRAWLER ***

1) Building and running the solution
- Solution "Crawler" was built using Visual Studio Community 2022 with .NET 6.
- Settinsg can be modified by editing file appsettings.json
- Excluded words, if any, are comma separated

2) Assumptions
All XML tags are ignored in word count, as well as cript and style blocks.

3) Known issue(?)
When reading thwebpage, the XML reader throws an exception, complaining about bad tags.
XmlException: The 'input' start tag on line 1956 position 6 does not match the end tag of 'div'. Line 1958, position 6.
The whole webpage response body is in file webpage.txt (root of solution).

