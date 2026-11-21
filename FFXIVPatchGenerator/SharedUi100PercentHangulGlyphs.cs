using System;

namespace FfxivKoreanPatch.FFXIVPatchGenerator
{
    internal static class SharedUi100PercentHangulGlyphs
    {
        public const string Jupiter16TargetFontPath = "common/font/Jupiter_16.fdt";
        public const string Jupiter20TargetFontPath = "common/font/Jupiter_20.fdt";
        public const string Jupiter23TargetFontPath = "common/font/Jupiter_23.fdt";
        public const string Jupiter16SourceFontPath = "common/font/AXIS_14.fdt";
        // Jupiter 는 같은 크기 번호여도 영문·숫자가 작다(Jupiter_20 숫자 11.8px, Jupiter_23 13.6px).
        // 한글을 본문 AXIS 와 같은 한글/숫자 비율(약 1.18)에 가깝게 맞추려고 한 단계 작은 AXIS 셀을 쓴다.
        // 예전 경로(Jupiter_20 -> Jupiter_16 셀 17.9px, Jupiter_23 -> TTMP 원본 22.4px)는
        // 자유부대·기술 목록 탭과 직업명이 주변보다 크게 보였다 (2026-11-21 사용자 보고).
        public const string Jupiter20SourceFontPath = "common/font/AXIS_14.fdt";
        public const string Jupiter23SourceFontPath = "common/font/AXIS_18.fdt";

        public static readonly string[] TargetFontPaths = new string[]
        {
            Jupiter16TargetFontPath,
            Jupiter20TargetFontPath,
            Jupiter23TargetFontPath
        };

        public static bool IsTargetFontPath(string path)
        {
            string ignored;
            return TryGetSourceFontPath(path, out ignored);
        }

        public static bool IsTargetCodepoint(uint codepoint)
        {
            // These two invisible composition fillers use font-specific cells.
            if (codepoint == 0x115Fu || codepoint == 0x1160u)
            {
                return false;
            }

            return (codepoint >= 0xAC00u && codepoint <= 0xD7A3u) ||
                   (codepoint >= 0x1100u && codepoint <= 0x11FFu) ||
                   (codepoint >= 0x3130u && codepoint <= 0x318Fu) ||
                   (codepoint >= 0xA960u && codepoint <= 0xA97Fu) ||
                   (codepoint >= 0xD7B0u && codepoint <= 0xD7FFu);
        }

        // 원본 AXIS 셀의 offsetY 를 그대로 쓰면 줄 높이가 더 큰 Jupiter 에서 한글이 위로 뜬다.
        // true 인 경로는 대상·원본 글꼴(clean)의 숫자 아랫선 차이만큼 한글 offsetY 를 옮긴다.
        // Jupiter_16 은 기존 배치를 유지한다.
        public static bool AlignsBaselineToTargetDigits(string path)
        {
            string normalized = Normalize(path);
            return string.Equals(normalized, Jupiter20TargetFontPath, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, Jupiter23TargetFontPath, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetSourceFontPath(string path, out string sourceFontPath)
        {
            string normalized = Normalize(path);
            if (string.Equals(normalized, Jupiter16TargetFontPath, StringComparison.OrdinalIgnoreCase))
            {
                sourceFontPath = Jupiter16SourceFontPath;
                return true;
            }

            if (string.Equals(normalized, Jupiter20TargetFontPath, StringComparison.OrdinalIgnoreCase))
            {
                sourceFontPath = Jupiter20SourceFontPath;
                return true;
            }

            if (string.Equals(normalized, Jupiter23TargetFontPath, StringComparison.OrdinalIgnoreCase))
            {
                sourceFontPath = Jupiter23SourceFontPath;
                return true;
            }

            sourceFontPath = null;
            return false;
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }
    }
}
