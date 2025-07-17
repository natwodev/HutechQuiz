using System.Xml.Serialization;
using backend_manage.Entities;
using System.Text.RegularExpressions;

namespace backend_manage.wwwroot.Tool;

public class XmlReaderHelper
{
    public static OriginalExamPaper ReadDeThiFromFile(string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(OriginalExamPaper));
        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            return (OriginalExamPaper)serializer.Deserialize(fs);
        }
    }

    /// <summary>
    /// Đọc file XML, thay thế tất cả các GUID giống nhau trong toàn bộ file thành số nguyên tăng dần, đảm bảo các GUID giống nhau sẽ thành cùng một số. Trả về nội dung XML đã được thay thế.
    /// </summary>
    /// <returns>Nội dung XML đã thay thế GUID thành số nguyên</returns>
    static string filexml = "wwwroot/EPZ/c8839d97-d670-4771-9d19-b8ee445e6d44.xml";
    public static string ReadAndReplaceGuids()
    {
        string xmlContent = File.ReadAllText(filexml);
        // Regex tìm các cặp <Tag>...GUID...</Tag> cho phép khoảng trắng, xuống dòng giữa tag và giá trị
        var tagGuidRegex = new Regex(
            @"<(?<tag>\w+)>\s*([\r\n\t ]*)?(?<guid>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})([\r\n\t ]*)?\s*</\k<tag>>",
            RegexOptions.Multiline
        );
        string specialGuid = "00000000-0000-0000-0000-000000000000";
        var tagGuidMap = new Dictionary<string, Dictionary<string, int>>();
        var tagCurrentId = new Dictionary<string, int>();
        string replaced = tagGuidRegex.Replace(xmlContent, match => {
            string tag = match.Groups["tag"].Value;
            string guid = match.Groups["guid"].Value.ToLower();
            string leading = match.Value.Substring(0, match.Value.IndexOf(guid));
            string trailing = match.Value.Substring(match.Value.IndexOf(guid) + guid.Length);
            if (guid == specialGuid) return match.Value; // giữ nguyên
            if (!tagGuidMap.ContainsKey(tag))
            {
                tagGuidMap[tag] = new Dictionary<string, int>();
                tagCurrentId[tag] = 1;
            }
            if (!tagGuidMap[tag].ContainsKey(guid))
            {
                tagGuidMap[tag][guid] = tagCurrentId[tag]++;
            }
            // Giữ lại định dạng thụt lề/linebreak
            return $"<{tag}>{leading.Replace($"<{tag}>", string.Empty)}{tagGuidMap[tag][guid]}{trailing}";
        });
        File.WriteAllText(filexml, replaced);
        return replaced;
    }
}