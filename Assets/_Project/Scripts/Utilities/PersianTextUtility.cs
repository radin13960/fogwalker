using System.Collections.Generic;
using System.Text;

namespace FogWalker.Utilities
{
    /// <summary>
    /// اصلاحگر داخلی متن فارسی برای TextMeshPro بدون پلاگین:
    /// ۱) اتصال حروف (Reshape با گلیف‌های Presentation Forms)
    /// ۲) ترتیب بصری راست‌به‌چپ (معکوس‌سازی با حفظ ترتیب کلمات لاتین/اعداد)
    /// ۳) تبدیل ارقام لاتین به فارسی
    /// محدودیت‌ها: لیگچر لا (Lam-Alef) و اعراب پشتیبانی نمی‌شود؛ برای متن‌های طولانی RTLTMPro توصیه می‌شود.
    /// </summary>
    public static class PersianTextUtility
    {
        // [isolated, final, initial, medial] — حروف دوطرفه‌چسبان (initial != 0)
        private static readonly Dictionary<char, char[]> LetterForms = new Dictionary<char, char[]>
        {
            { 'ء', new[] { '\uFE80', '\0', '\0', '\0' } },
            { 'آ', new[] { '\uFE81', '\uFE82', '\0', '\0' } },
            { 'أ', new[] { '\uFE83', '\uFE84', '\0', '\0' } },
            { 'ؤ', new[] { '\uFE85', '\uFE86', '\0', '\0' } },
            { 'إ', new[] { '\uFE87', '\uFE88', '\0', '\0' } },
            { 'ئ', new[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' } },
            { 'ا', new[] { '\uFE8D', '\uFE8E', '\0', '\0' } },
            { 'ب', new[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' } },
            { 'ة', new[] { '\uFE93', '\uFE94', '\0', '\0' } },
            { 'ت', new[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' } },
            { 'ث', new[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' } },
            { 'ج', new[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' } },
            { 'ح', new[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' } },
            { 'خ', new[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' } },
            { 'د', new[] { '\uFEA9', '\uFEAA', '\0', '\0' } },
            { 'ذ', new[] { '\uFEAB', '\uFEAC', '\0', '\0' } },
            { 'ر', new[] { '\uFEAD', '\uFEAE', '\0', '\0' } },
            { 'ز', new[] { '\uFEAF', '\uFEB0', '\0', '\0' } },
            { 'س', new[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' } },
            { 'ش', new[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' } },
            { 'ص', new[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' } },
            { 'ض', new[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' } },
            { 'ط', new[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' } },
            { 'ظ', new[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' } },
            { 'ع', new[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' } },
            { 'غ', new[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' } },
            { 'ف', new[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' } },
            { 'ق', new[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' } },
            { 'ك', new[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' } }, // کاه عربی
            { 'ک', new[] { '\uFB8E', '\uFB8F', '\uFB90', '\uFB91' } }, // کاف فارسی
            { 'ل', new[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' } },
            { 'م', new[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' } },
            { 'ن', new[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' } },
            { 'ه', new[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' } },
            { 'و', new[] { '\uFEED', '\uFEEE', '\0', '\0' } },
            { 'ى', new[] { '\uFEEF', '\uFEF0', '\0', '\0' } },
            { 'ي', new[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' } }, // ی عربی
            { 'ی', new[] { '\uFBFC', '\uFBFD', '\uFBFE', '\uFBFF' } }, // ی فارسی
            { 'پ', new[] { '\uFB56', '\uFB57', '\uFB58', '\uFB59' } },
            { 'چ', new[] { '\uFB7A', '\uFB7B', '\uFB7C', '\uFB7D' } },
            { 'ژ', new[] { '\uFB8A', '\uFB8B', '\0', '\0' } },
            { 'گ', new[] { '\uFB92', '\uFB93', '\uFB94', '\uFB95' } },
        };

        /// <summary>آیا حرف از سمت چپ (به حرف بعدی) می‌چسبد؟</summary>
        private static bool JoinsToNext(char c)
        {
            return LetterForms.TryGetValue(c, out char[] forms) && forms[2] != '\0';
        }

        /// <summary>آیا حرف از سمت راست (به حرف قبلی) می‌چسبد؟</summary>
        private static bool JoinsToPrev(char c)
        {
            return LetterForms.TryGetValue(c, out char[] forms) && forms[1] != '\0';
        }

        /// <summary>اصلاح کامل متن (Reshape + ترتیب بصری)؛ خط به خط.</summary>
        public static string Fix(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string[] lines = input.Split('\n');
            var builder = new StringBuilder(input.Length + 8);
            for (int i = 0; i < lines.Length; i++)
            {
                builder.Append(FixLine(lines[i]));
                if (i < lines.Length - 1) builder.Append('\n');
            }
            return builder.ToString();
        }

        private static string FixLine(string line)
        {
            if (line.Length == 0) return line;

            // مرحله ۱: اتصال حروف
            char[] shaped = new char[line.Length];
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!LetterForms.TryGetValue(c, out char[] forms))
                {
                    shaped[i] = c;
                    continue;
                }

                char prev = PreviousLetter(line, i);
                char next = NextLetter(line, i);
                bool joinPrev = prev != '\0' && JoinsToNext(prev) && JoinsToPrev(c);
                bool joinNext = next != '\0' && JoinsToPrev(next) && JoinsToNext(c);

                if (joinPrev && joinNext && forms[3] != '\0') shaped[i] = forms[3];       // medial
                else if (joinPrev && forms[1] != '\0') shaped[i] = forms[1];               // final
                else if (joinNext && forms[2] != '\0') shaped[i] = forms[2];               // initial
                else shaped[i] = forms[0];                                                  // isolated
            }

            // مرحله ۲: ترتیب بصری — معکوس کل خط، سپس برگرداندن رشته‌های لاتین/عدد به ترتیب درست
            Reverse(shaped, 0, shaped.Length);
            RestoreLeftToRightRuns(shaped);
            return new string(shaped);
        }

        private static char PreviousLetter(string s, int index)
        {
            for (int i = index - 1; i >= 0; i--)
                if (LetterForms.ContainsKey(s[i])) return s[i];
                else if (s[i] == ' ') return '\0';
            return '\0';
        }

        private static char NextLetter(string s, int index)
        {
            for (int i = index + 1; i < s.Length; i++)
                if (LetterForms.ContainsKey(s[i])) return s[i];
                else if (s[i] == ' ') return '\0';
            return '\0';
        }

        private static void Reverse(char[] array, int start, int length)
        {
            for (int i = 0; i < length / 2; i++)
            {
                (array[start + i], array[start + length - 1 - i]) = (array[start + length - 1 - i], array[start + i]);
            }
        }

        /// <summary>رشته‌های LTR (لاتین + رقم + علائم هم‌خانواده) را پس از معکوس‌سازی خط دوباره به‌ترتیب می‌کند.</summary>
        private static void RestoreLeftToRightRuns(char[] shaped)
        {
            int runStart = -1;
            for (int i = 0; i <= shaped.Length; i++)
            {
                bool isLtr = i < shaped.Length && IsLatinOrDigit(shaped[i]);
                if (isLtr && runStart < 0) runStart = i;
                else if (!isLtr && runStart >= 0)
                {
                    Reverse(shaped, runStart, i - runStart);
                    runStart = -1;
                }
            }
        }

        private static bool IsLatinOrDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z');
        }

        /// <summary>آیا حرف از خانواده فارسی/عربی است؟ (برای ابزارهای تشخیص جهت)</summary>
        public static bool IsRtlChar(char c)
        {
            return (c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﻿');
        }

        /// <summary>تبدیل ارقام لاتین متن به ارقام فارسی (۰۱۲۳...).</summary>
        public static string ToPersianDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var builder = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                builder.Append(c >= '0' && c <= '9' ? (char)('۰' + (c - '0')) : c);
            }
            return builder.ToString();
        }
    }
}
