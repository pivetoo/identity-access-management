using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace IdentityManagement.Infrastructure.Tenancy
{
    public static class TenantNaming
    {
        private const int MaxIdentifier = 63;
        private static readonly Regex ValidIdentifier = new("^[a-z][a-z0-9_]{0,62}$", RegexOptions.Compiled);

        public static string Slugify(string name)
        {
            string firstWord = (name ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            string decomposed = firstWord.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();
            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }
                char lower = char.ToLowerInvariant(ch);
                if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                {
                    builder.Append(lower);
                }
            }
            string slug = builder.ToString();
            return slug.Length == 0 ? "tenant" : slug;
        }

        public static string DatabaseName(string audience, string slug, long companyId)
        {
            string prefix = (audience ?? string.Empty).Replace("-", string.Empty);
            string suffix = "_" + companyId.ToString(CultureInfo.InvariantCulture);
            int maxSlug = MaxIdentifier - prefix.Length - 1 - suffix.Length;
            string slugPart = maxSlug <= 0 ? string.Empty : slug[..Math.Min(slug.Length, maxSlug)];
            return slugPart.Length == 0 ? prefix + suffix : prefix + "_" + slugPart + suffix;
        }

        public static bool IsValidIdentifier(string name) => name is not null && ValidIdentifier.IsMatch(name);
    }
}
