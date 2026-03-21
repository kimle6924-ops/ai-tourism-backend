using System.Globalization;
using System.Text;

namespace BE_AI_Tourism.Shared.Utils;

public static class StringUtils
{
    /// <summary>
    /// Bỏ dấu tiếng Việt và chuyển về lowercase.
    /// VD: "Bản Cát Cát" → "ban cat cat", "Đà Nẵng" → "da nang"
    /// </summary>
    public static string RemoveDiacritics(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Xử lý đặc biệt cho chữ Đ/đ trước khi normalize
        text = text.Replace("Đ", "D").Replace("đ", "d");

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLower();
    }
}
