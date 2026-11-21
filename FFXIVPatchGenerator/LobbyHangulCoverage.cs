using System;
using System.Collections.Generic;

namespace FfxivKoreanPatch.FFXIVPatchGenerator
{
    internal static class LobbyHangulCoverage
    {
        public static readonly string[] TargetFontPaths = new string[]
        {
            "common/font/AXIS_12_lobby.fdt",
            "common/font/AXIS_14_lobby.fdt",
            "common/font/AXIS_18_lobby.fdt",
            "common/font/AXIS_36_lobby.fdt",
            "common/font/Jupiter_46_lobby.fdt",
            "common/font/Jupiter_90_lobby.fdt",
            "common/font/Meidinger_40_lobby.fdt",
            "common/font/MiedingerMid_12_lobby.fdt",
            "common/font/MiedingerMid_14_lobby.fdt",
            "common/font/MiedingerMid_18_lobby.fdt",
            "common/font/MiedingerMid_36_lobby.fdt",
            "common/font/TrumpGothic_23_lobby.fdt",
            "common/font/TrumpGothic_34_lobby.fdt",
            "common/font/TrumpGothic_68_lobby.fdt"
        };

        public static readonly string[] HighScaleTargetFontPaths = new string[]
        {
            "common/font/AXIS_36_lobby.fdt",
            "common/font/Jupiter_46_lobby.fdt",
            "common/font/Jupiter_90_lobby.fdt",
            "common/font/Meidinger_40_lobby.fdt",
            "common/font/MiedingerMid_36_lobby.fdt",
            "common/font/TrumpGothic_68_lobby.fdt"
        };

        // 캐릭터 선택 정보 창의 현재 위치(예: 안갯빛 마을). TerritoryType 이 가리키는 영역 이름만 쓴다.
        // PlaceName 전체(약 5200행)는 HD/FHD 로비 페이지 예산을 넘는다 (AXIS_14_lobby 등에서 할당 실패).
        // Rows(보이는 한자 조사·회수 보호)에는 넣지 않는다. 지명을 원문으로 둘 때 지명 한자까지 보호하면
        // 회수 셀이 줄어 AXIS_14_lobby 등에서 할당이 실패한다.
        private static readonly LobbyHangulCoverageRowSpec CharacterLocationRows = new LobbyHangulCoverageRowSpec(
            "PlaceName",
            0,
            uint.MaxValue,
            0,
            new LobbyHangulCoverageRowReference("TerritoryType", TerritoryTypePlaceNameColumnOffset, ExcelUInt16ColumnType));

        private const ushort TerritoryTypePlaceNameColumnOffset = 32;
        private const ushort ExcelUInt16ColumnType = 5;

        public static readonly LobbyHangulCoverageRowSpec[] Rows = CreateFullSheetRows(
            "Lobby",
            "Error",
            "Addon",
            "ClassJob",
            "Race",
            "Tribe",
            "GuardianDeity");

        public static readonly LobbyHangulCoverageRowSpec[] StartMainMenuRows = CreateFullSheetRows(
            "Lobby",
            "Error",
            "Addon",
            "ClassJob",
            "Race",
            "Tribe",
            "GuardianDeity");

        public static readonly LobbyHangulCoverageRowSpec[] SystemSettingsRows = CreateFullSheetRows(
            "Lobby",
            "Error",
            "Addon",
            "ClassJob",
            "Race",
            "Tribe",
            "GuardianDeity");

        public static readonly LobbyHangulCoverageRowSpec[] CharacterSelectRows = Append(CreateFullSheetRows(
            "Lobby",
            "Error",
            "Addon",
            "ClassJob",
            "Race",
            "Tribe",
            "GuardianDeity"),
            CharacterLocationRows);

        public static readonly LobbyHangulCoverageRowSpec[] LargeLabelRows = new LobbyHangulCoverageRowSpec[]
        {
            new LobbyHangulCoverageRowSpec("ClassJob", 0, 43),
            new LobbyHangulCoverageRowSpec("Race", 1, 8),
            new LobbyHangulCoverageRowSpec("Tribe", 1, 16),
            new LobbyHangulCoverageRowSpec("GuardianDeity", 1, 12, 0),
            // 캐릭터 선택 생일의 달 이름(별빛1월~그림자6월). 없으면 '별빛'의 '빛'이 큰 라벨에서 '-'로 보인다.
            new LobbyHangulCoverageRowSpec("Lobby", 40, 51, 0)
        };

        public static bool IsTargetFontPath(string path)
        {
            string normalized = Normalize(path);
            for (int i = 0; i < TargetFontPaths.Length; i++)
            {
                if (string.Equals(normalized, TargetFontPaths[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsMainMenuOnlyTargetFontPath(string path)
        {
            return string.Equals(
                Normalize(path),
                "common/font/MiedingerMid_18_lobby.fdt",
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHighScaleTargetFontPath(string path)
        {
            string normalized = Normalize(path);
            for (int i = 0; i < HighScaleTargetFontPaths.Length; i++)
            {
                if (string.Equals(normalized, HighScaleTargetFontPaths[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim();
        }

        // 다른 시트가 가리키는 row 만 쓰는 범위(Reference)를 실제 row 집합으로 바꾼다.
        // 이 함수를 거치지 않은 범위는 Contains 가 false 라서, 해석하지 않는 쪽은 그 범위를 쓰지 않는다.
        // 참조 시트나 열을 찾지 못하면 warnings 에 남기고 그 범위는 비워 둔다 (게임 업데이트로 열이 바뀐 경우).
        public static LobbyHangulCoverageRowSpec[] ResolveReferences(
            LobbyHangulCoverageRowSpec[] rows,
            Func<string, byte[]> readFile,
            List<string> warnings)
        {
            if (rows == null)
            {
                return null;
            }

            LobbyHangulCoverageRowSpec[] resolved = (LobbyHangulCoverageRowSpec[])rows.Clone();
            for (int i = 0; i < resolved.Length; i++)
            {
                LobbyHangulCoverageRowReference reference = resolved[i].Reference;
                if (reference == null)
                {
                    continue;
                }

                string error;
                HashSet<uint> rowIds = ReadReferencedRowIds(reference, readFile, out error);
                if (rowIds == null)
                {
                    if (warnings != null)
                    {
                        warnings.Add(resolved[i].Sheet + " rows referenced by " + reference.Sheet + " were skipped for lobby glyph coverage: " + error);
                    }

                    rowIds = new HashSet<uint>();
                }

                resolved[i] = resolved[i].WithReferencedRowIds(rowIds);
            }

            return resolved;
        }

        private static HashSet<uint> ReadReferencedRowIds(
            LobbyHangulCoverageRowReference reference,
            Func<string, byte[]> readFile,
            out string error)
        {
            error = null;
            ExcelHeader header;
            try
            {
                header = ExcelHeader.Parse(readFile("exd/" + reference.Sheet + ".exh"));
            }
            catch (Exception ex)
            {
                error = "header could not be read: " + ex.Message;
                return null;
            }

            if (header.Variant != ExcelVariant.Default)
            {
                error = "unsupported variant " + header.Variant.ToString();
                return null;
            }

            if (header.Languages.Count > 0 && !header.HasLanguage(0))
            {
                error = "sheet is language-specific";
                return null;
            }

            bool columnFound = false;
            for (int i = 0; i < header.Columns.Count; i++)
            {
                if (header.Columns[i].Offset == reference.ColumnOffset && header.Columns[i].Type == reference.ColumnType)
                {
                    columnFound = true;
                    break;
                }
            }

            if (!columnFound || reference.ColumnType != 5)
            {
                error = "uint16 column at offset " + reference.ColumnOffset.ToString() + " was not found";
                return null;
            }

            HashSet<uint> rowIds = new HashSet<uint>();
            for (int pageIndex = 0; pageIndex < header.Pages.Count; pageIndex++)
            {
                string exdPath = "exd/" + reference.Sheet + "_" + header.Pages[pageIndex].StartId.ToString() + ".exd";
                ExcelDataFile file;
                try
                {
                    file = ExcelDataFile.Parse(readFile(exdPath));
                }
                catch (Exception ex)
                {
                    error = exdPath + " could not be read: " + ex.Message;
                    return null;
                }

                for (int rowIndex = 0; rowIndex < file.Rows.Count; rowIndex++)
                {
                    int rowOffset = checked((int)file.Rows[rowIndex].Offset);
                    if (rowOffset < 0 || rowOffset + 6 > file.Data.Length)
                    {
                        continue;
                    }

                    int rowDataOffset = rowOffset + 6;
                    int rowEnd = rowDataOffset + checked((int)Endian.ReadUInt32BE(file.Data, rowOffset));
                    int fieldOffset = rowDataOffset + reference.ColumnOffset;
                    if (rowEnd > file.Data.Length || fieldOffset + 2 > rowEnd)
                    {
                        continue;
                    }

                    uint value = Endian.ReadUInt16BE(file.Data, fieldOffset);
                    if (value != 0)
                    {
                        rowIds.Add(value);
                    }
                }
            }

            return rowIds;
        }

        private static LobbyHangulCoverageRowSpec[] Append(LobbyHangulCoverageRowSpec[] rows, params LobbyHangulCoverageRowSpec[] extra)
        {
            LobbyHangulCoverageRowSpec[] result = new LobbyHangulCoverageRowSpec[rows.Length + extra.Length];
            Array.Copy(rows, result, rows.Length);
            Array.Copy(extra, 0, result, rows.Length, extra.Length);
            return result;
        }

        private static LobbyHangulCoverageRowSpec[] CreateFullSheetRows(params string[] sheets)
        {
            LobbyHangulCoverageRowSpec[] rows = new LobbyHangulCoverageRowSpec[sheets.Length];
            for (int i = 0; i < sheets.Length; i++)
            {
                rows[i] = new LobbyHangulCoverageRowSpec(sheets[i], 0, uint.MaxValue);
            }

            return rows;
        }
    }

    internal struct LobbyHangulCoverageRowSpec
    {
        public readonly string Sheet;
        public readonly uint StartId;
        public readonly uint EndId;
        public readonly ushort? ColumnOffset;
        public readonly LobbyHangulCoverageRowReference Reference;
        private readonly HashSet<uint> _referencedRowIds;

        public LobbyHangulCoverageRowSpec(string sheet, uint startId, uint endId)
            : this(sheet, startId, endId, null)
        {
        }

        public LobbyHangulCoverageRowSpec(string sheet, uint startId, uint endId, ushort? columnOffset)
            : this(sheet, startId, endId, columnOffset, null)
        {
        }

        public LobbyHangulCoverageRowSpec(
            string sheet,
            uint startId,
            uint endId,
            ushort? columnOffset,
            LobbyHangulCoverageRowReference reference)
            : this(sheet, startId, endId, columnOffset, reference, null)
        {
        }

        private LobbyHangulCoverageRowSpec(
            string sheet,
            uint startId,
            uint endId,
            ushort? columnOffset,
            LobbyHangulCoverageRowReference reference,
            HashSet<uint> referencedRowIds)
        {
            Sheet = sheet;
            StartId = startId;
            EndId = endId;
            ColumnOffset = columnOffset;
            Reference = reference;
            _referencedRowIds = referencedRowIds;
        }

        public bool Contains(uint rowId)
        {
            if (rowId < StartId || rowId > EndId)
            {
                return false;
            }

            if (Reference == null)
            {
                return true;
            }

            // 참조 범위는 LobbyHangulCoverage.ResolveReferences 를 거친 뒤에만 row 를 포함한다.
            return _referencedRowIds != null && _referencedRowIds.Contains(rowId);
        }

        public LobbyHangulCoverageRowSpec WithReferencedRowIds(HashSet<uint> rowIds)
        {
            return new LobbyHangulCoverageRowSpec(Sheet, StartId, EndId, ColumnOffset, Reference, rowIds);
        }
    }

    // 범위를 "Sheet 의 uint16 열(ColumnOffset)이 가리키는 row" 로 좁힌다.
    internal sealed class LobbyHangulCoverageRowReference
    {
        public readonly string Sheet;
        public readonly ushort ColumnOffset;
        public readonly ushort ColumnType;

        public LobbyHangulCoverageRowReference(string sheet, ushort columnOffset, ushort columnType)
        {
            Sheet = sheet;
            ColumnOffset = columnOffset;
            ColumnType = columnType;
        }
    }
}
