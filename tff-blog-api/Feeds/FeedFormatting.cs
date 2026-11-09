#nullable enable annotations

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Tff.Blog.Api.Feeds;

/// <summary>
/// Date and XML formatting shared by both feed builders.
/// </summary>
internal static class FeedFormatting
{
    private const string Rfc3339Format = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>
    /// RFC 822 date, the format RSS 2.0 requires for <c>pubDate</c> and <c>lastBuildDate</c>.
    /// </summary>
    internal static string Rfc822(DateTime value) => ToUtc(value).ToString("r", CultureInfo.InvariantCulture);

    /// <summary>
    /// RFC 3339 date, the format Atom requires for <c>updated</c> and <c>published</c>.
    /// </summary>
    internal static string Rfc3339(DateTime value) => ToUtc(value).ToString(Rfc3339Format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Storyblok dates are UTC but <see cref="DateTime.Kind"/> may come back unspecified, so treat
    /// those as UTC instead of letting <see cref="DateTime.ToUniversalTime"/> shift them by the
    /// machine's local offset.
    /// </summary>
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    /// <summary>
    /// Serializes a document as UTF-8 XML. <see cref="XDocument.ToString()"/> drops the XML
    /// declaration and a plain <see cref="StringWriter"/> claims to be UTF-16, so the declaration
    /// is written through a writer that reports UTF-8.
    /// </summary>
    internal static string Serialize(XDocument document)
    {
        using var stringWriter = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
        }))
        {
            document.Save(xmlWriter);
        }

        return stringWriter.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
