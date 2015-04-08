using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace MiniBlogFormatter
{
    public class BlogEngineFormatter
    {
        private Regex rxFiles = new Regex("(href|src)=\"(([^\"]+)?(file|image)\\.axd\\?(file|picture)=([^\"]+))\"", RegexOptions.IgnoreCase);
        private Regex rxAggBug = new Regex("<img (.*) src=(.*(aggbug.ashx).*) />", RegexOptions.IgnoreCase);

        private readonly Regex rxCode = new Regex("(?<all><font face=\"Courier New\">(?<code>[^<]+)</font>)", RegexOptions.IgnoreCase);
        private readonly Regex rxCode2 = new Regex("(?<all><span style=\"font-family:\\s*Courier New;*\">(?<code>[^<]+)</span>)", RegexOptions.IgnoreCase);
        private readonly Regex rxCode3 = new Regex("(?<all><span style=\"font-family:\\s*Courier New,\\s*courier;*\">(?<code>[^<]+)</span>)", RegexOptions.IgnoreCase);

        public void Format(string fileName, string targetFolderPath, string categoriesFileName)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            XmlNode isDeletedNode = doc.SelectSingleNode("post/isdeleted");

            bool isDeleted = isDeletedNode != null ? isDeletedNode.InnerText == "True" : false;

            if (!isDeleted)
            {
                FormatSlug(doc);
                FormatFileReferences(doc);
                FormatCode(doc);
                ReplaceTeaserMarker(doc);
                RemoveAggBug(doc);
                RemoveSpamComments(doc);

                XmlDocument categories = new XmlDocument();
                categories.Load(categoriesFileName);
                FormatCategories(doc, categories);

                string newFileName = Path.Combine(targetFolderPath, Path.GetFileName(fileName));
                doc.Save(newFileName);
            }
        }

        private void FormatFileReferences(XmlDocument doc)
        {
            XmlNode content = doc.SelectSingleNode("post/content");

            if (content != null)
            {
                foreach (Match match in rxFiles.Matches(content.InnerText))
                {
                    content.InnerText = content.InnerText.Replace(match.Groups[2].Value, "/posts/files/" + match.Groups[6].Value);
                }
            }
        }

        private void FormatCode(XmlDocument doc)
        {
            DoFormatCode(doc, rxCode);
            DoFormatCode(doc, rxCode2);
            DoFormatCode(doc, rxCode3);
        }

        private static void DoFormatCode(XmlDocument doc, Regex regex)
        {
            XmlNode content = doc.SelectSingleNode("post/content");

            if (content != null)
            {
                foreach (Match match in regex.Matches(content.InnerText))
                {
                    content.InnerText = content.InnerText.Replace(match.Groups["all"].Value, 
                        "<code>" + match.Groups["code"].Value + "</code>");
                }
            }
        }

        private const string OldTeaserMarker = "<p>[more]</p>";
        private const string NewTeaserMarker = "<!--more-->";

        private static void ReplaceTeaserMarker(XmlDocument doc)
        {
            XmlNode content = doc.SelectSingleNode("post/content");

            if (content != null)
            {
                content.InnerText = content.InnerText.Replace(OldTeaserMarker, NewTeaserMarker);
            }
        }

        private void FormatSlug(XmlDocument doc)
        {
            XmlNode slug = doc.SelectSingleNode("//slug");

            if (slug != null)
            {
                slug.InnerText = FormatterHelpers.FormatSlug(slug.InnerText);
            }
        }

        private void RemoveAggBug(XmlDocument doc)
        {
            XmlNode content = doc.SelectSingleNode("post/content");

            if (content != null)
            {
                content.InnerText = rxAggBug.Replace(content.InnerText, string.Empty);
            }
        }

        private void RemoveSpamComments(XmlDocument doc)
        {
            XmlNodeList comments = doc.SelectNodes("//comment");

            for (int i = comments.Count - 1; i > -1; i--)
            {
                XmlNode comment = comments[i];
                bool approved = comment.Attributes["approved"] != null ? comment.Attributes["approved"].InnerText == "True" : true;
                bool deleted = comment.Attributes["deleted"] != null ? comment.Attributes["deleted"].InnerText == "True" : true;

                if (!approved || deleted)
                {
                    comment.ParentNode.RemoveChild(comment);
                }
            }
        }

        private void FormatCategories(XmlDocument doc, XmlDocument categoriesDoc)
        {
            XmlNodeList categories = doc.SelectNodes("//category");

            foreach (XmlNode category in categories)
            {
                string id = category.InnerText;
                XmlNode name = categoriesDoc.SelectSingleNode("//category[@id='" + id + "']");

                if (name != null)
                {
                    category.InnerText = name.InnerText;
                }
            }
        }
    }
}
