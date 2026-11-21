using System;

namespace FfxivKoreanPatch.FFXIVPatchGenerator
{
    // 인게임 장식용 글꼴(Jupiter, TrumpGothic, MiedingerMid)의 한글 글리프가 원래 크기 AXIS 한글 셀을
    // 그대로 가리키게 하는 경로. 새 텍스처 공간을 쓰지 않고, 본문과 같은 글꼴·자간으로 번짐 없이 그린다.
    // 줄 높이와 숫자 위치가 AXIS 와 다르므로 세로 위치(offsetY)만 대상 글꼴의 숫자에 맞춰 옮긴다.
    internal static class SharedUi100PercentHangulGlyphs
    {
        public const string Jupiter16TargetFontPath = "common/font/Jupiter_16.fdt";
        public const string Jupiter20TargetFontPath = "common/font/Jupiter_20.fdt";
        public const string Jupiter23TargetFontPath = "common/font/Jupiter_23.fdt";
        public const string Jupiter16SourceFontPath = "common/font/AXIS_14.fdt";
        public const string Jupiter20SourceFontPath = "common/font/AXIS_14.fdt";
        public const string Jupiter23SourceFontPath = "common/font/AXIS_18.fdt";

        private enum VerticalAlignment
        {
            // 원본 offsetY 그대로 (기존 배치 유지).
            Source,
            // 한글 아랫선 = 숫자 아랫선. 한글이 숫자보다 클 때(Jupiter: 영문·숫자가 작은 글꼴).
            DigitBottom,
            // 한글 가운데 = 숫자 가운데. 한글과 숫자 크기가 크게 다를 때.
            DigitCenter
        }

        private struct Route
        {
            public readonly string TargetFontPath;
            public readonly string SourceFontPath;
            public readonly VerticalAlignment Alignment;

            public Route(string targetFontPath, string sourceFontPath, VerticalAlignment alignment)
            {
                TargetFontPath = targetFontPath;
                SourceFontPath = sourceFontPath;
                Alignment = alignment;
            }
        }

        // 2026-11-21 사용자 보고에 따른 경로:
        // - Jupiter_20/23: 자유부대·기술 목록 탭, 직업명이 주변보다 컸다. 한 단계 작은 AXIS 셀.
        // - TrumpGothic_23/34/68: 창 제목. 축소 보간한 한글이 번지고 자간이 들쭉날쭉했다. 원래 크기 AXIS 셀
        //   (UI 배율에 따라 제목 글꼴이 23 -> 34 -> 68 로 바뀐다). 한글이 영문 대문자보다 작아 가운데를 맞춘다.
        // - MiedingerMid: '링크셸1 :', '레벨 100' 의 숫자(9px)가 한글(12px)보다 작아 아래첨자처럼 보였다.
        //   같은 크기 AXIS 셀을 쓰고 숫자와 가운데를 맞춘다.
        private static readonly Route[] Routes = new Route[]
        {
            new Route(Jupiter16TargetFontPath, Jupiter16SourceFontPath, VerticalAlignment.Source),
            new Route(Jupiter20TargetFontPath, Jupiter20SourceFontPath, VerticalAlignment.DigitBottom),
            new Route(Jupiter23TargetFontPath, Jupiter23SourceFontPath, VerticalAlignment.DigitBottom),
            new Route("common/font/TrumpGothic_23.fdt", "common/font/AXIS_14.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/TrumpGothic_34.fdt", "common/font/AXIS_18.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/TrumpGothic_68.fdt", "common/font/AXIS_36.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/MiedingerMid_10.fdt", "common/font/AXIS_96.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/MiedingerMid_12.fdt", "common/font/AXIS_12.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/MiedingerMid_14.fdt", "common/font/AXIS_14.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/MiedingerMid_18.fdt", "common/font/AXIS_18.fdt", VerticalAlignment.DigitCenter),
            new Route("common/font/MiedingerMid_36.fdt", "common/font/AXIS_36.fdt", VerticalAlignment.DigitCenter)
        };

        public static readonly string[] TargetFontPaths = CreateTargetFontPaths();

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

        // 한글 아랫선을 대상 글꼴 숫자 아랫선에 맞추는 경로.
        public static bool AlignsBaselineToTargetDigits(string path)
        {
            Route route;
            return TryGetRoute(path, out route) && route.Alignment == VerticalAlignment.DigitBottom;
        }

        // 한글 세로 가운데를 대상 글꼴 숫자 세로 가운데에 맞추는 경로.
        public static bool CentersOnTargetDigits(string path)
        {
            Route route;
            return TryGetRoute(path, out route) && route.Alignment == VerticalAlignment.DigitCenter;
        }

        // 원본 셀·크기는 그대로 쓰고 offsetY 만 글꼴 전체에서 같은 값만큼 옮기는 경로.
        public static bool MovesVerticallyToTargetDigits(string path)
        {
            Route route;
            return TryGetRoute(path, out route) && route.Alignment != VerticalAlignment.Source;
        }

        public static bool TryGetSourceFontPath(string path, out string sourceFontPath)
        {
            Route route;
            if (TryGetRoute(path, out route))
            {
                sourceFontPath = route.SourceFontPath;
                return true;
            }

            sourceFontPath = null;
            return false;
        }

        private static bool TryGetRoute(string path, out Route route)
        {
            string normalized = Normalize(path);
            for (int i = 0; i < Routes.Length; i++)
            {
                if (string.Equals(normalized, Routes[i].TargetFontPath, StringComparison.OrdinalIgnoreCase))
                {
                    route = Routes[i];
                    return true;
                }
            }

            route = default(Route);
            return false;
        }

        private static string[] CreateTargetFontPaths()
        {
            string[] paths = new string[Routes.Length];
            for (int i = 0; i < Routes.Length; i++)
            {
                paths[i] = Routes[i].TargetFontPath;
            }

            return paths;
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }
    }
}
