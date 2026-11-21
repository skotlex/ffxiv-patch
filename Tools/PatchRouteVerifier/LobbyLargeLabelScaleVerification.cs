using System;

namespace FfxivKoreanPatch.PatchRouteVerifier
{
    internal static partial class PatchRouteVerifier
    {
        private sealed partial class Verifier
        {
            private static readonly LobbyLargeLabelScaleCase[] LobbyLargeLabelScaleCases = new LobbyLargeLabelScaleCase[]
            {
                new LobbyLargeLabelScaleCase("start-system-config-title-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "시스템 설정", "システムコンフィグ"),
                new LobbyLargeLabelScaleCase("character-race-gender-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "로스가르 여", "ロスガル"),
                new LobbyLargeLabelScaleCase("character-tribe-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "떠도는 별", "ロスト"),
                new LobbyLargeLabelScaleCase("character-birthday-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "그림자 5월 11일", "霊5月11日"),
                new LobbyLargeLabelScaleCase("character-birthday-astral-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "별빛 1월 1일", "星1月1日"),
                new LobbyLargeLabelScaleCase("character-deity-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "니메이아", "ニメーヤ"),
                new LobbyLargeLabelScaleCase("character-class-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "레벨 100 암흑기사", "レベル100暗黒騎士"),
                new LobbyLargeLabelScaleCase("character-location-23", "common/font/TrumpGothic_23_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "지고천 거리", "ジゴテン街"),
                new LobbyLargeLabelScaleCase("start-system-config-title-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "시스템 설정", "システムコンフィグ"),
                new LobbyLargeLabelScaleCase("character-race-gender-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "로스가르 여", "ロスガル"),
                new LobbyLargeLabelScaleCase("character-tribe-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "떠도는 별", "ロスト"),
                new LobbyLargeLabelScaleCase("character-birthday-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "그림자 5월 11일", "霊5月11日"),
                new LobbyLargeLabelScaleCase("character-birthday-astral-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "별빛 1월 1일", "星1月1日"),
                new LobbyLargeLabelScaleCase("character-deity-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "니메이아", "ニメーヤ"),
                new LobbyLargeLabelScaleCase("character-class-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "레벨 100 암흑기사", "レベル100暗黒騎士"),
                new LobbyLargeLabelScaleCase("character-location-34", "common/font/TrumpGothic_34_lobby.fdt", "common/font/AXIS_18_lobby.fdt", "지고천 거리", "ジゴテン街")
            };

            private void VerifyLobbyLargeLabelScaleLayouts()
            {
                Console.WriteLine("[FDT] Lobby large-label scale layout");
                for (int i = 0; i < LobbyLargeLabelScaleCases.Length; i++)
                {
                    VerifyLobbyLargeLabelScaleCase(LobbyLargeLabelScaleCases[i]);
                }
            }

            private void VerifyLobbyLargeLabelScaleCase(LobbyLargeLabelScaleCase testCase)
            {
                PhraseVisualBounds korean;
                PhraseVisualBounds reference;
                string error;
                if (!TryMeasurePhraseVisualBounds(_patchedFont, testCase.FontPath, testCase.KoreanPhrase, true, out korean, out error))
                {
                    Fail("{0} lobby large-label Korean phrase [{1}] layout error: {2}", testCase.FontPath, Escape(testCase.KoreanPhrase), error);
                    return;
                }

                if (!TryMeasurePhraseVisualBounds(_patchedFont, testCase.ReferenceFontPath, testCase.KoreanPhrase, true, out reference, out error))
                {
                    Fail("{0} lobby large-label source phrase [{1}] layout error in {2}: {3}", testCase.FontPath, Escape(testCase.KoreanPhrase), testCase.ReferenceFontPath, error);
                    return;
                }

                double referenceHeight = reference.MeanHangulHeight;
                if (korean.MeanHangulHeight <= 0d || referenceHeight <= 0d)
                {
                    Fail(
                        "{0} lobby large-label [{1}] cannot compare visual scale: hangulHeight={2}, referenceHeight={3}",
                        testCase.FontPath,
                        testCase.Id,
                        FormatDouble(korean.MeanHangulHeight),
                        FormatDouble(referenceHeight));
                    return;
                }

                if (IsVisualScaledLobbyLargeLabelFont(testCase.FontPath))
                {
                    LobbyLargeLabelScaleExpectation expectation = GetLobbyLargeLabelScaleExpectation(testCase.FontPath);
                    VerifyLobbyLargeLabelRatio(testCase, "source-height", korean.MeanHangulHeight, referenceHeight, expectation.SourceMinRatio, expectation.SourceMaxRatio);
                    VerifyLobbyLargeLabelRatio(testCase, "source-width", korean.MeanHangulWidth, reference.MeanHangulWidth, expectation.SourceMinRatio, expectation.SourceMaxRatio);
                    VerifyLobbyLargeLabelRatio(testCase, "source-advance", korean.MeanHangulAdvance, reference.MeanHangulAdvance, expectation.AdvanceMinRatio, expectation.SourceMaxRatio);
                    VerifyLobbyLargeLabelFontPixelHeight(testCase, korean, expectation);
                    VerifyLobbyLargeLabelDigitBaseline(testCase, korean);
                }
                else
                {
                    VerifyLobbyLargeLabelRatio(testCase, "height", korean.MeanHangulHeight, referenceHeight, 0.96d, 1.04d);
                    VerifyLobbyLargeLabelRatio(testCase, "width", korean.MeanHangulWidth, reference.MeanHangulWidth, 0.96d, 1.04d);
                    VerifyLobbyLargeLabelRatio(testCase, "advance", korean.MeanHangulAdvance, reference.MeanHangulAdvance, 0.96d, 1.04d);
                }

                VerifyLobbyLargeLabelCleanReference(testCase, korean);
            }

            private void VerifyLobbyLargeLabelCleanReference(
                LobbyLargeLabelScaleCase testCase,
                PhraseVisualBounds korean)
            {
                PhraseVisualBounds cleanReference;
                string error;
                string cleanReferenceFontPath = testCase.FontPath;
                if (!TryMeasurePhraseVisualBounds(_cleanFont, cleanReferenceFontPath, testCase.CleanReferencePhrase, false, out cleanReference, out error))
                {
                    cleanReferenceFontPath = testCase.ReferenceFontPath;
                    if (!TryMeasurePhraseVisualBounds(_cleanFont, cleanReferenceFontPath, testCase.CleanReferencePhrase, false, out cleanReference, out error))
                    {
                        Warn("{0} lobby large-label clean reference [{1}] skipped: {2}", testCase.FontPath, Escape(testCase.CleanReferencePhrase), error);
                        return;
                    }
                }

                double referenceHeight = cleanReference.MeanReferenceHeight;
                if (referenceHeight <= 0d || korean.MeanHangulHeight <= 0d)
                {
                    Fail(
                        "{0} lobby large-label [{1}] cannot compare clean visual height: korean={2}, cleanReference=[{3}] {4}",
                        testCase.FontPath,
                        testCase.Id,
                        FormatDouble(korean.MeanHangulHeight),
                        Escape(testCase.CleanReferencePhrase),
                        FormatDouble(referenceHeight));
                    return;
                }

                LobbyLargeLabelScaleExpectation expectation = GetLobbyLargeLabelScaleExpectation(testCase.FontPath);
                VerifyLobbyLargeLabelRatio(testCase, "clean-reference-height", korean.MeanHangulHeight, referenceHeight, expectation.CleanReferenceMinRatio, expectation.CleanReferenceMaxRatio);
            }

            private static bool IsVisualScaledLobbyLargeLabelFont(string fontPath)
            {
                string normalized = (fontPath ?? string.Empty).Replace('\\', '/');
                return string.Equals(normalized, "common/font/TrumpGothic_23_lobby.fdt", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(normalized, "common/font/TrumpGothic_34_lobby.fdt", StringComparison.OrdinalIgnoreCase);
            }

            private static LobbyLargeLabelScaleExpectation GetLobbyLargeLabelScaleExpectation(string fontPath)
            {
                string normalized = (fontPath ?? string.Empty).Replace('\\', '/');
                if (string.Equals(normalized, "common/font/TrumpGothic_34_lobby.fdt", StringComparison.OrdinalIgnoreCase))
                {
                    // 캐릭터 선택 큰 라벨은 같은 창의 현재 위치 줄(AXIS_18_lobby 한글, 18px)과 같은 크기여야 한다.
                    // 예전 크기(약 28px, TrumpGothic_34.fdt 한글 x0.80)는 픽셀 상한으로 거부한다.
                    return new LobbyLargeLabelScaleExpectation(0.96d, 1.06d, 0.96d, 16.0d, 21.0d, 0.82d, 1.80d);
                }

                return new LobbyLargeLabelScaleExpectation(1.04d, 1.16d, 1.02d, 17.0d, 24.0d, 0.82d, 1.80d);
            }

            // 캐릭터 선택 큰 라벨('레벨 100', '1월 1일')은 한글과 원본 숫자가 한 줄에 섞인다.
            // 한글 원본(AXIS_18_lobby)의 offsetY 를 그대로 쓰면 한글이 14px 위로 떠 보였다(2026-11-21 사용자 화면).
            private void VerifyLobbyLargeLabelDigitBaseline(LobbyLargeLabelScaleCase testCase, PhraseVisualBounds korean)
            {
                string normalized = (testCase.FontPath ?? string.Empty).Replace('\\', '/');
                if (!string.Equals(normalized, "common/font/TrumpGothic_34_lobby.fdt", StringComparison.OrdinalIgnoreCase) ||
                    korean.MeanDigitHeight <= 0d ||
                    korean.MeanHangulHeight <= 0d)
                {
                    return;
                }

                double gap = korean.MeanDigitBottom - korean.MeanHangulBottom;
                if (Math.Abs(gap) > 2.0d)
                {
                    Fail(
                        "{0} lobby large-label [{1}] Hangul bottom {2} and digit bottom {3} differ by {4}px (max 2): korean=[{5}]",
                        testCase.FontPath,
                        testCase.Id,
                        FormatDouble(korean.MeanHangulBottom),
                        FormatDouble(korean.MeanDigitBottom),
                        FormatDouble(gap),
                        Escape(testCase.KoreanPhrase));
                    return;
                }

                Pass(
                    "{0} lobby large-label [{1}] Hangul/digit bottoms aligned: {2} / {3}",
                    testCase.FontPath,
                    testCase.Id,
                    FormatDouble(korean.MeanHangulBottom),
                    FormatDouble(korean.MeanDigitBottom));
            }

            private void VerifyLobbyLargeLabelFontPixelHeight(
                LobbyLargeLabelScaleCase testCase,
                PhraseVisualBounds korean,
                LobbyLargeLabelScaleExpectation expectation)
            {
                if (korean.MeanHangulHeight < expectation.MinimumPixelHeight ||
                    korean.MeanHangulHeight > expectation.MaximumPixelHeight)
                {
                    Fail(
                        "{0} lobby large-label [{1}] pixel height {2} outside {3}..{4}: korean=[{5}]",
                        testCase.FontPath,
                        testCase.Id,
                        FormatDouble(korean.MeanHangulHeight),
                        FormatDouble(expectation.MinimumPixelHeight),
                        FormatDouble(expectation.MaximumPixelHeight),
                        Escape(testCase.KoreanPhrase));
                    return;
                }

                Pass(
                    "{0} lobby large-label [{1}] pixel height={2} within {3}..{4}",
                    testCase.FontPath,
                    testCase.Id,
                    FormatDouble(korean.MeanHangulHeight),
                    FormatDouble(expectation.MinimumPixelHeight),
                    FormatDouble(expectation.MaximumPixelHeight));
            }

            private void VerifyLobbyLargeLabelRatio(
                LobbyLargeLabelScaleCase testCase,
                string axis,
                double koreanValue,
                double referenceValue,
                double minimum,
                double maximum)
            {
                if (referenceValue <= 0d)
                {
                    Warn(
                        "{0} lobby large-label [{1}] {2} ratio skipped: reference is empty",
                        testCase.FontPath,
                        testCase.Id,
                        axis);
                    return;
                }

                double ratio = SafeRatio(koreanValue, referenceValue);
                if (ratio < minimum || ratio > maximum)
                {
                    Fail(
                        "{0} lobby large-label [{1}] {2} ratio {3} outside {4}..{5}: korean=[{6}] {7}, reference=[{8}] {9}",
                        testCase.FontPath,
                        testCase.Id,
                        axis,
                        FormatRatio(ratio),
                        FormatRatio(minimum),
                        FormatRatio(maximum),
                        Escape(testCase.KoreanPhrase),
                        FormatDouble(koreanValue),
                        testCase.ReferenceFontPath,
                        FormatDouble(referenceValue));
                    return;
                }

                Pass(
                    "{0} lobby large-label [{1}] {2} ratio={3}: korean=[{4}] {5}, reference=[{6}] {7}",
                    testCase.FontPath,
                    testCase.Id,
                    axis,
                    FormatRatio(ratio),
                    Escape(testCase.KoreanPhrase),
                    FormatDouble(koreanValue),
                    testCase.ReferenceFontPath,
                    FormatDouble(referenceValue));
            }

            private struct LobbyLargeLabelScaleCase
            {
                public readonly string Id;
                public readonly string FontPath;
                public readonly string ReferenceFontPath;
                public readonly string KoreanPhrase;
                public readonly string CleanReferencePhrase;

                public LobbyLargeLabelScaleCase(string id, string fontPath, string referenceFontPath, string koreanPhrase, string cleanReferencePhrase)
                {
                    Id = id;
                    FontPath = fontPath;
                    ReferenceFontPath = referenceFontPath;
                    KoreanPhrase = koreanPhrase;
                    CleanReferencePhrase = cleanReferencePhrase;
                }
            }

            private struct LobbyLargeLabelScaleExpectation
            {
                public readonly double SourceMinRatio;
                public readonly double SourceMaxRatio;
                public readonly double AdvanceMinRatio;
                public readonly double MinimumPixelHeight;
                public readonly double MaximumPixelHeight;
                public readonly double CleanReferenceMinRatio;
                public readonly double CleanReferenceMaxRatio;

                public LobbyLargeLabelScaleExpectation(
                    double sourceMinRatio,
                    double sourceMaxRatio,
                    double advanceMinRatio,
                    double minimumPixelHeight,
                    double maximumPixelHeight,
                    double cleanReferenceMinRatio,
                    double cleanReferenceMaxRatio)
                {
                    SourceMinRatio = sourceMinRatio;
                    SourceMaxRatio = sourceMaxRatio;
                    AdvanceMinRatio = advanceMinRatio;
                    MinimumPixelHeight = minimumPixelHeight;
                    MaximumPixelHeight = maximumPixelHeight;
                    CleanReferenceMinRatio = cleanReferenceMinRatio;
                    CleanReferenceMaxRatio = cleanReferenceMaxRatio;
                }
            }
        }
    }
}
