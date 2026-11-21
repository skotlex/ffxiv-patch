using System;
using System.Collections.Generic;
using FfxivKoreanPatch.FFXIVPatchGenerator;

namespace FfxivKoreanPatch.PatchRouteVerifier
{
    internal static partial class PatchRouteVerifier
    {
        private sealed partial class Verifier
        {
            private const double SharedUiMaxRelativeGlyphScaleSpread = 1.25d;
            // Jupiter_20 한글은 본문(AXIS_14) 셀을 쓰므로 짧은 문구에서 한글이 Jupiter 숫자와 거의 같은 높이가 된다
            // ('PvP 프로필' 1.045). 1.10 은 예전 Jupiter_16 셀(17.9px) 기준이었다.
            private const double SharedUiMinHangulToDigitRatio = 1.00d;
            private const double SharedUiMaxHangulToDigitRatio = 1.55d;

            private static readonly string[] SharedJupiterFontPaths =
                SharedUi100PercentHangulGlyphs.TargetFontPaths;

            private static readonly string[] SharedUiScaleProbeFontPaths = new string[]
            {
                "common/font/Jupiter_16.fdt",
                "common/font/Jupiter_20.fdt",
                "common/font/TrumpGothic_184.fdt",
                "common/font/TrumpGothic_23.fdt",
                "common/font/TrumpGothic_34.fdt",
                "common/font/TrumpGothic_68.fdt"
            };

            private static readonly SharedUiRouteGroup[] SharedUiRouteGroups = new SharedUiRouteGroup[]
            {
                new SharedUiRouteGroup(
                    "action",
                    new string[]
                    {
                        "ui/uld/ActionMenu.uld",
                        "ui/uld/ActionDetail.uld"
                    },
                    new string[]
                    {
                        "\uC561\uC158",
                        ActionDetailHighScaleHangulGlyphs.ActionsAndTraitsPhrase,
                        ActionDetailHighScaleHangulGlyphs.InstantCastPhrase,
                        ActionDetailHighScaleHangulGlyphs.RecastTimePhrase
                    }),
                new SharedUiRouteGroup(
                    "pvp",
                    new string[]
                    {
                        "ui/uld/PvPProfile.uld",
                        "ui/uld/PvPCharacter.uld",
                        "ui/uld/PvPAction.uld",
                        "ui/uld/PvPActions.uld",
                        "ui/uld/PvPTeam.uld",
                        "ui/uld/PvPTeamBoard.uld",
                        "ui/uld/PvPSchedule.uld"
                    },
                    PvpProfileVisualScaleGlyphs.FallbackPhrases),
                new SharedUiRouteGroup(
                    "free-company",
                    new string[]
                    {
                        "ui/uld/FreeCompany.uld",
                        "ui/uld/FreeCompanyProfile.uld",
                        "ui/uld/FreeCompanyMember.uld"
                    },
                    new string[]
                    {
                        "\uC0C8 \uC18C\uC2DD",
                        "\uBD80\uB300\uC6D0",
                        "\uACC4\uAE09",
                        "\uBD80\uB300 \uD61C\uD0DD",
                        "\uD65C\uB3D9",
                        "\uC0C1\uD0DC"
                    })
            };

            private void VerifySharedUiFontConsistency()
            {
                Console.WriteLine("[ULD/FDT] Action, PvP, and Free Company shared font consistency");
                if (_ttmpFont == null)
                {
                    Fail("Shared UI font consistency requires the TTMP source font package");
                    return;
                }

                bool retiredPvpTransformIsEnabled = false;
                for (int i = 0; i < SharedJupiterFontPaths.Length; i++)
                {
                    if (PvpProfileVisualScaleGlyphs.IsTargetFontPath(SharedJupiterFontPaths[i]))
                    {
                        retiredPvpTransformIsEnabled = true;
                        Fail("{0} must not use the retired phrase-subset PvP transform", SharedJupiterFontPaths[i]);
                    }
                }

                HashSet<uint> reportedCodepoints = new HashSet<uint>();
                int routedGroups = 0;
                int measuredPhrases = 0;
                int attemptedPhrases = 0;
                for (int groupIndex = 0; groupIndex < SharedUiRouteGroups.Length; groupIndex++)
                {
                    SharedUiRouteGroup group = SharedUiRouteGroups[groupIndex];
                    AddHangulCodepoints(reportedCodepoints, group.Phrases);

                    HashSet<string> routedFonts;
                    int foundUlds;
                    if (!TryCollectSharedUiGroupRoutes(group, out routedFonts, out foundUlds))
                    {
                        continue;
                    }

                    routedGroups++;
                    int groupAttemptedPhrases;
                    measuredPhrases += VerifySharedUiGroupGlyphScale(group, routedFonts, out groupAttemptedPhrases);
                    attemptedPhrases += groupAttemptedPhrases;
                    Pass(
                        "{0} shared UI routes preserved: ulds={1}, fonts={2}",
                        group.Name,
                        foundUlds,
                        routedFonts.Count);
                }

                if (routedGroups != SharedUiRouteGroups.Length)
                {
                    Fail(
                        "Shared UI route coverage incomplete: groups={0}/{1}",
                        routedGroups,
                        SharedUiRouteGroups.Length);
                }

                VerifySharedJupiterDigitBaselines();
                VerifyDigitCenteredHangulFonts();

                int expectedMappedGlyphs;
                int mappedProbeGlyphs;
                int expectedProbeGlyphs;
                int mappedGlyphs = VerifySharedJupiterMappedGlyphs(
                    reportedCodepoints,
                    out expectedMappedGlyphs,
                    out mappedProbeGlyphs,
                    out expectedProbeGlyphs);
                if (mappedGlyphs != expectedMappedGlyphs || mappedProbeGlyphs != expectedProbeGlyphs)
                {
                    Fail(
                        "Shared Jupiter low-scale mapping coverage incomplete: routes={0}/{1}, probes={2}/{3}",
                        mappedGlyphs,
                        expectedMappedGlyphs,
                        mappedProbeGlyphs,
                        expectedProbeGlyphs);
                    return;
                }

                if (retiredPvpTransformIsEnabled ||
                    routedGroups != SharedUiRouteGroups.Length ||
                    measuredPhrases != attemptedPhrases)
                {
                    Fail(
                        "Shared UI consistency summary incomplete: routes={0}/{1}, phrases={2}/{3}",
                        routedGroups,
                        SharedUiRouteGroups.Length,
                        measuredPhrases,
                        attemptedPhrases);
                    return;
                }

                Pass(
                    "Shared UI 100% font sizing verified: groups={0}, phrases={1}/{2}, Jupiter routes={3}, probes={4}",
                    routedGroups,
                    measuredPhrases,
                    attemptedPhrases,
                    mappedGlyphs,
                    mappedProbeGlyphs);
            }

            private bool TryCollectSharedUiGroupRoutes(
                SharedUiRouteGroup group,
                out HashSet<string> routedFonts,
                out int foundUlds)
            {
                routedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foundUlds = 0;
                for (int i = 0; i < group.UldPaths.Length; i++)
                {
                    HashSet<string> uldFonts;
                    if (!TryCollectOptionalPreservedUldFontRoutes(
                        group.UldPaths[i],
                        group.Name,
                        false,
                        out uldFonts))
                    {
                        continue;
                    }

                    foundUlds++;
                    foreach (string fontPath in uldFonts)
                    {
                        routedFonts.Add(fontPath);
                    }
                }

                if (foundUlds == 0)
                {
                    Fail(
                        "No {0} ULD candidate was found; shared font verification does not cover this UI",
                        group.Name);
                    return false;
                }

                int sharedJupiterRoutes = 0;
                for (int i = 0; i < SharedJupiterFontPaths.Length; i++)
                {
                    if (routedFonts.Contains(SharedJupiterFontPaths[i]))
                    {
                        sharedJupiterRoutes++;
                    }
                }

                if (sharedJupiterRoutes == 0)
                {
                    Fail(
                        "{0} ULDs do not route to Jupiter_16/20; reported shared-font route is not covered",
                        group.Name);
                    return false;
                }

                return true;
            }

            private int VerifySharedJupiterMappedGlyphs(
                HashSet<uint> probeCodepoints,
                out int expectedMappedGlyphs,
                out int mappedProbeGlyphs,
                out int expectedProbeGlyphs)
            {
                int mapped = 0;
                expectedMappedGlyphs = 0;
                mappedProbeGlyphs = 0;
                expectedProbeGlyphs = probeCodepoints.Count * SharedJupiterFontPaths.Length;
                uint[] sortedProbeCodepoints = ToSortedArray(probeCodepoints);
                foreach (string fontPath in SharedJupiterFontPaths)
                {
                    string sourceFontPath;
                    if (!SharedUi100PercentHangulGlyphs.TryGetSourceFontPath(fontPath, out sourceFontPath))
                    {
                        Fail("{0} has no shared UI 100% source mapping", fontPath);
                        continue;
                    }

                    if (!_ttmpFont.ContainsPath(fontPath) || !_ttmpFont.ContainsPath(sourceFontPath))
                    {
                        Fail("{0} -> {1} is missing from the TTMP source package", fontPath, sourceFontPath);
                        continue;
                    }

                    byte[] sourceFdt;
                    byte[] originalTargetFdt;
                    byte[] targetFdt;
                    try
                    {
                        sourceFdt = _ttmpFont.ReadFile(sourceFontPath);
                        originalTargetFdt = _ttmpFont.ReadFile(fontPath);
                        targetFdt = _patchedFont.ReadFile(fontPath);
                    }
                    catch (Exception ex)
                    {
                        Fail("{0} shared Jupiter read error: {1}", fontPath, ex.Message);
                        continue;
                    }

                    Dictionary<uint, FdtGlyphEntry> sourceGlyphs = ReadHangulGlyphEntries(sourceFdt);
                    Dictionary<uint, FdtGlyphEntry> originalTargetGlyphs = ReadHangulGlyphEntries(originalTargetFdt);
                    Dictionary<uint, FdtGlyphEntry> targetGlyphs = ReadHangulGlyphEntries(targetFdt);
                    int failuresForFont = 0;
                    // 기준선을 맞추는 경로는 원본 셀·크기를 그대로 쓰고 offsetY 만 글꼴 전체에서 같은 값만큼 옮긴다.
                    bool alignsBaseline = SharedUi100PercentHangulGlyphs.MovesVerticallyToTargetDigits(fontPath);
                    int? baselineShift = null;
                    foreach (KeyValuePair<uint, FdtGlyphEntry> pair in originalTargetGlyphs)
                    {
                        uint codepoint = pair.Key;
                        if (!SharedUi100PercentHangulGlyphs.IsTargetCodepoint(codepoint))
                        {
                            continue;
                        }

                        expectedMappedGlyphs++;
                        FdtGlyphEntry sourceGlyph;
                        FdtGlyphEntry targetGlyph;
                        if (!sourceGlyphs.TryGetValue(codepoint, out sourceGlyph))
                        {
                            if (failuresForFont < MaxTexturePaddingFailuresPerFont)
                            {
                                Fail("{0} mapped source {1} is missing U+{2:X4}", fontPath, sourceFontPath, codepoint);
                            }

                            failuresForFont++;
                            continue;
                        }

                        if (!targetGlyphs.TryGetValue(codepoint, out targetGlyph))
                        {
                            if (failuresForFont < MaxTexturePaddingFailuresPerFont)
                            {
                                Fail("{0} patched font is missing mapped U+{1:X4}", fontPath, codepoint);
                            }

                            failuresForFont++;
                            continue;
                        }

                        FdtGlyphEntry comparedSource = sourceGlyph;
                        if (alignsBaseline)
                        {
                            int delta = targetGlyph.OffsetY - sourceGlyph.OffsetY;
                            if (!baselineShift.HasValue)
                            {
                                baselineShift = delta;
                            }

                            if (delta == baselineShift.Value)
                            {
                                comparedSource.OffsetY = targetGlyph.OffsetY;
                            }
                        }

                        bool routeMatches =
                            GlyphSpacingMetricsMatch(comparedSource, targetGlyph) &&
                            sourceGlyph.ImageIndex == targetGlyph.ImageIndex &&
                            sourceGlyph.X == targetGlyph.X &&
                            sourceGlyph.Y == targetGlyph.Y;
                        if (!routeMatches)
                        {
                            if (failuresForFont < MaxTexturePaddingFailuresPerFont)
                            {
                                Fail(
                                    "{0} U+{1:X4} does not route to {2}: patched={3}, expected={4}",
                                    fontPath,
                                    codepoint,
                                    sourceFontPath,
                                    FormatGlyphRoute(targetGlyph),
                                    FormatGlyphRoute(sourceGlyph));
                            }

                            failuresForFont++;
                            continue;
                        }

                        mapped++;
                    }

                    if (failuresForFont > MaxTexturePaddingFailuresPerFont)
                    {
                        Warn(
                            "{0} low-scale route scan suppressed {1} additional failures",
                            fontPath,
                            failuresForFont - MaxTexturePaddingFailuresPerFont);
                    }

                    for (int codepointIndex = 0; codepointIndex < sortedProbeCodepoints.Length; codepointIndex++)
                    {
                        uint codepoint = sortedProbeCodepoints[codepointIndex];
                        GlyphCanvas sourceCanvas;
                        GlyphCanvas targetCanvas;
                        try
                        {
                            sourceCanvas = RenderGlyph(_ttmpFont, sourceFontPath, codepoint);
                            targetCanvas = RenderGlyph(_patchedFont, fontPath, codepoint);
                            if (alignsBaseline && sourceCanvas.Glyph.OffsetY != targetCanvas.Glyph.OffsetY)
                            {
                                // 세로 위치만 옮긴 경로는 원본을 같은 offsetY 로 그려 모양만 비교한다.
                                FdtGlyphEntry shiftedSource = sourceCanvas.Glyph;
                                shiftedSource.OffsetY = targetCanvas.Glyph.OffsetY;
                                sourceCanvas.Alpha = RenderGlyphAlpha(ReadFontTexture(_ttmpFont, sourceCanvas.TexturePath), shiftedSource);
                            }
                        }
                        catch (Exception ex)
                        {
                            Fail(
                                "{0} U+{1:X4} mapped probe render error from {2}: {3}",
                                fontPath,
                                codepoint,
                                sourceFontPath,
                                ex.Message);
                            continue;
                        }

                        long pixelDiff = Diff(sourceCanvas.Alpha, targetCanvas.Alpha);
                        if (pixelDiff != 0 || targetCanvas.VisiblePixels < 10)
                        {
                            Fail(
                                "{0} U+{1:X4} differs from mapped source {2}: score={3}, visible={4}/{5}",
                                fontPath,
                                codepoint,
                                sourceFontPath,
                                pixelDiff,
                                targetCanvas.VisiblePixels,
                                sourceCanvas.VisiblePixels);
                            continue;
                        }

                        mappedProbeGlyphs++;
                    }
                }

                return mapped;
            }

            // 기준선을 맞추는 Jupiter 경로: 한글(원본 AXIS 셀)과 Jupiter 숫자의 아랫선이 같은 줄에서 맞아야 한다.
            // 예: 기술 목록·캐릭터 창의 '레벨 100 사무라이' 줄, 자유부대 탭.
            private void VerifySharedJupiterDigitBaselines()
            {
                const string phrase = "부대원 100 사무라이";
                for (int i = 0; i < SharedJupiterFontPaths.Length; i++)
                {
                    string fontPath = SharedJupiterFontPaths[i];
                    if (!SharedUi100PercentHangulGlyphs.AlignsBaselineToTargetDigits(fontPath))
                    {
                        continue;
                    }

                    PhraseVisualBounds bounds;
                    string error;
                    if (!TryMeasurePhraseVisualBounds(_patchedFont, fontPath, phrase, true, out bounds, out error))
                    {
                        Fail("{0} shared Jupiter baseline phrase [{1}] layout error: {2}", fontPath, Escape(phrase), error);
                        continue;
                    }

                    double gap = bounds.MeanDigitBottom - bounds.MeanHangulBottom;
                    if (bounds.MeanDigitHeight <= 0d || bounds.MeanHangulHeight <= 0d || Math.Abs(gap) > 2.0d)
                    {
                        Fail(
                            "{0} shared Jupiter Hangul bottom {1} and digit bottom {2} differ by {3}px (max 2)",
                            fontPath,
                            FormatDouble(bounds.MeanHangulBottom),
                            FormatDouble(bounds.MeanDigitBottom),
                            FormatDouble(gap));
                        continue;
                    }

                    Pass(
                        "{0} shared Jupiter Hangul/digit bottoms aligned: {1} / {2}, Hangul/digit height {3}",
                        fontPath,
                        FormatDouble(bounds.MeanHangulBottom),
                        FormatDouble(bounds.MeanDigitBottom),
                        FormatRatio(SafeRatio(bounds.MeanHangulHeight, bounds.MeanDigitHeight)));
                }
            }

            // 가운데 맞춤 경로(창 제목 TrumpGothic, MiedingerMid): 한글과 숫자의 세로 가운데가 맞아야 한다.
            // 예: 링크셸 목록 '링크셸1 :', 기술 목록 '레벨 100'. 한글은 원래 크기 AXIS 셀이라 비율은 글꼴마다 다르다.
            private void VerifyDigitCenteredHangulFonts()
            {
                const string phrase = "링크셸 1 : 레벨 100";
                for (int i = 0; i < SharedJupiterFontPaths.Length; i++)
                {
                    string fontPath = SharedJupiterFontPaths[i];
                    if (!SharedUi100PercentHangulGlyphs.CentersOnTargetDigits(fontPath))
                    {
                        continue;
                    }

                    PhraseVisualBounds bounds;
                    string error;
                    if (!TryMeasurePhraseVisualBounds(_patchedFont, fontPath, phrase, true, out bounds, out error))
                    {
                        Fail("{0} digit-centered phrase [{1}] layout error: {2}", fontPath, Escape(phrase), error);
                        continue;
                    }

                    double ratio = SafeRatio(bounds.MeanHangulHeight, bounds.MeanDigitHeight);
                    double hangulCenter = bounds.MeanHangulTop + bounds.MeanHangulHeight / 2d;
                    double digitCenter = bounds.MeanDigitTop + bounds.MeanDigitHeight / 2d;
                    double centerGap = hangulCenter - digitCenter;
                    double tolerance = Math.Max(1.0d, bounds.MeanDigitHeight * 0.12d);
                    if (bounds.MeanDigitHeight <= 0d || ratio < 0.60d || ratio > 1.60d || Math.Abs(centerGap) > tolerance)
                    {
                        Fail(
                            "{0} digit-centered Hangul: Hangul/digit height {1} (0.60..1.60), center gap {2}px (max {3})",
                            fontPath,
                            FormatRatio(ratio),
                            FormatDouble(centerGap),
                            FormatDouble(tolerance));
                        continue;
                    }

                    Pass(
                        "{0} digit-centered Hangul: Hangul/digit height {1}, center gap {2}px",
                        fontPath,
                        FormatRatio(ratio),
                        FormatDouble(centerGap));
                }
            }

            private int VerifySharedUiGroupGlyphScale(
                SharedUiRouteGroup group,
                HashSet<string> routedFonts,
                out int attempted)
            {
                int measured = 0;
                attempted = 0;
                for (int fontIndex = 0; fontIndex < SharedUiScaleProbeFontPaths.Length; fontIndex++)
                {
                    string fontPath = SharedUiScaleProbeFontPaths[fontIndex];
                    if (!routedFonts.Contains(fontPath))
                    {
                        continue;
                    }

                    for (int phraseIndex = 0; phraseIndex < group.Phrases.Length; phraseIndex++)
                    {
                        attempted++;
                        if (VerifySharedUiPhraseGlyphScale(group.Name, fontPath, group.Phrases[phraseIndex]))
                        {
                            measured++;
                        }
                    }
                }

                if (attempted == 0)
                {
                    Fail("{0} routed fonts did not render any shared UI scale probe phrase", group.Name);
                }
                else if (measured != attempted)
                {
                    Fail(
                        "{0} shared UI scale probes incomplete: passed={1}/{2}",
                        group.Name,
                        measured,
                        attempted);
                }

                return measured;
            }

            private bool VerifySharedUiPhraseGlyphScale(string area, string fontPath, string phrase)
            {
                string sourceFontPath = fontPath;
                SharedUi100PercentHangulGlyphs.TryGetSourceFontPath(fontPath, out sourceFontPath);
                if (string.IsNullOrEmpty(sourceFontPath))
                {
                    sourceFontPath = fontPath;
                }

                if (!_ttmpFont.ContainsPath(sourceFontPath))
                {
                    Fail("{0} {1} scale probe requires TTMP source font {2}", area, fontPath, sourceFontPath);
                    return false;
                }

                HashSet<uint> codepoints = new HashSet<uint>();
                AddHangulCodepoints(codepoints, new string[] { phrase });
                if (codepoints.Count == 0)
                {
                    return false;
                }

                double minScale = double.MaxValue;
                double maxScale = 0d;
                foreach (uint codepoint in codepoints)
                {
                    GlyphCanvas sourceCanvas;
                    GlyphCanvas targetCanvas;
                    try
                    {
                        sourceCanvas = RenderGlyph(_ttmpFont, sourceFontPath, codepoint);
                        targetCanvas = RenderGlyph(_patchedFont, fontPath, codepoint);
                    }
                    catch (Exception ex)
                    {
                        Fail(
                            "{0} {1} phrase [{2}] U+{3:X4} scale render error: {4}",
                            area,
                            fontPath,
                            Escape(phrase),
                            codepoint,
                            ex.Message);
                        return false;
                    }

                    int sourceHeight = GetVisibleGlyphHeight(sourceCanvas);
                    int targetHeight = GetVisibleGlyphHeight(targetCanvas);
                    if (sourceHeight <= 0 || targetHeight <= 0)
                    {
                        Fail(
                            "{0} {1} phrase [{2}] U+{3:X4} has blank source/target height {4}/{5}",
                            area,
                            fontPath,
                            Escape(phrase),
                            codepoint,
                            sourceHeight,
                            targetHeight);
                        return false;
                    }

                    double scale = (double)targetHeight / sourceHeight;
                    if (scale < minScale)
                    {
                        minScale = scale;
                    }

                    if (scale > maxScale)
                    {
                        maxScale = scale;
                    }
                }

                double spread = minScale > 0d ? maxScale / minScale : double.MaxValue;
                if (spread > SharedUiMaxRelativeGlyphScaleSpread)
                {
                    Fail(
                        "{0} {1} phrase [{2}] mixes Hangul glyph scales: min={3}, max={4}, spread={5}, limit={6}",
                        area,
                        fontPath,
                        Escape(phrase),
                        FormatRatio(minScale),
                        FormatRatio(maxScale),
                        FormatRatio(spread),
                        FormatRatio(SharedUiMaxRelativeGlyphScaleSpread));
                    return false;
                }

                if (SharedUi100PercentHangulGlyphs.IsTargetFontPath(fontPath) &&
                    !VerifySharedUiPhraseVisualSize(area, fontPath, phrase))
                {
                    return false;
                }

                Pass(
                    "{0} {1} phrase [{2}] keeps a uniform Hangul scale: min={3}, max={4}, spread={5}",
                    area,
                    fontPath,
                    Escape(phrase),
                    FormatRatio(minScale),
                    FormatRatio(maxScale),
                    FormatRatio(spread));
                return true;
            }

            // 가운데 맞춤 경로(창 제목 TrumpGothic, MiedingerMid)는 원래 크기 AXIS 셀을 쓰므로 한글/숫자 비율이
            // 글꼴마다 다르다(TrumpGothic_23 0.72, MiedingerMid_12 1.31). 위치는 VerifyDigitCenteredHangulFonts 가 본다.
            private static void SelectHangulToDigitRatioWindow(string fontPath, double defaultMin, double defaultMax, out double min, out double max)
            {
                if (SharedUi100PercentHangulGlyphs.CentersOnTargetDigits(fontPath))
                {
                    min = 0.60d;
                    max = 1.60d;
                    return;
                }

                min = defaultMin;
                max = defaultMax;
            }

            private bool VerifySharedUiPhraseVisualSize(string area, string fontPath, string phrase)
            {
                double minRatio;
                double maxRatio;
                SelectHangulToDigitRatioWindow(fontPath, SharedUiMinHangulToDigitRatio, SharedUiMaxHangulToDigitRatio, out minRatio, out maxRatio);
                PhraseVisualBounds korean;
                PhraseVisualBounds numeric;
                string error;
                if (!TryMeasurePhraseVisualBounds(_patchedFont, fontPath, phrase, true, out korean, out error) ||
                    !TryMeasurePhraseVisualBounds(_patchedFont, fontPath, ActionDetailNumericBaselinePhrase, false, out numeric, out error))
                {
                    Fail("{0} {1} phrase [{2}] visual-size probe failed: {3}", area, fontPath, Escape(phrase), error);
                    return false;
                }

                double ratio = SafeRatio(korean.MeanHangulHeight, numeric.MeanDigitHeight);
                if (ratio < minRatio || ratio > maxRatio)
                {
                    Fail(
                        "{0} {1} phrase [{2}] 100% Hangul/digit ratio {3} outside {4}..{5}: hangul={6}, digit={7}",
                        area,
                        fontPath,
                        Escape(phrase),
                        FormatRatio(ratio),
                        FormatRatio(minRatio),
                        FormatRatio(maxRatio),
                        FormatDouble(korean.MeanHangulHeight),
                        FormatDouble(numeric.MeanDigitHeight));
                    return false;
                }

                return true;
            }

            private static int GetVisibleGlyphHeight(GlyphCanvas canvas)
            {
                GlyphStats stats = AnalyzeGlyph(canvas);
                return stats.MinY <= stats.MaxY ? stats.MaxY - stats.MinY + 1 : 0;
            }

            private sealed class SharedUiRouteGroup
            {
                public readonly string Name;
                public readonly string[] UldPaths;
                public readonly string[] Phrases;

                public SharedUiRouteGroup(string name, string[] uldPaths, string[] phrases)
                {
                    Name = name;
                    UldPaths = uldPaths;
                    Phrases = phrases;
                }
            }
        }
    }
}
