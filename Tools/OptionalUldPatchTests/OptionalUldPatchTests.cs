using System;
using System.IO;

namespace FfxivKoreanPatch.FFXIVPatchGenerator
{
    // OptionalUldPatch 회귀 테스트. 게임 데이터 없이 망가진 ULD 와 가짜 보정 함수로 돌린다.
    // Scripts\test-optional-uld-patch.ps1 로 실행한다.
    internal static class OptionalUldPatchTests
    {
        private static int _failures;
        private static int _passed;

        private static int Main()
        {
            // 실제 보정 3종: 구조가 바뀐(여기서는 망가진) ULD 를 받으면 생성을 멈추지 않고 건너뛰어야 한다.
            byte[][] brokenUlds = new byte[][]
            {
                new byte[0],
                new byte[] { (byte)'u', (byte)'l', (byte)'d', (byte)'h' },
                UldHeaderWithBadAtkOffset()
            };
            for (int i = 0; i < brokenUlds.Length; i++)
            {
                byte[] broken = brokenUlds[i];
                string label = "broken ULD #" + i.ToString();
                ExpectSkipped("party list: " + label,
                    new string[] { PartyBonusRoleFontPatch.UldPath },
                    delegate(string path, byte[] source) { return PartyBonusRoleFontPatch.Apply(source); },
                    broken);
                ExpectSkipped("duty finder: " + label,
                    DutyFinderRoleFontPatch.UldPaths, DutyFinderRoleFontPatch.Apply, broken);
                ExpectSkipped("inventory: " + label,
                    InventorySubtitleSpacingPatch.UldPaths, InventorySubtitleSpacingPatch.Apply, broken);
            }

            string[] pair = new string[] { "ui/uld/A.uld", "ui/uld/B.uld" };

            // 모두 성공하면 경로 순서대로 고친 내용을 돌려준다.
            {
                string warning;
                byte[][] result = OptionalUldPatch.TryApplyAll("all ok", pair, ReadPathBytes, MarkPatched, out warning);
                Check("all succeed: returns every ULD",
                    result != null && result.Length == 2 && warning == null &&
                    result[0][0] == 0xFF && result[1][0] == 0xFF &&
                    result[0][1] == (byte)'A' && result[1][1] == (byte)'B',
                    "result=" + Describe(result) + ", warning=" + (warning ?? "(null)"));
            }

            // 짝 중 하나만 실패해도 묶음 전체를 건너뛴다 (한쪽 창만 바뀌지 않게).
            {
                string warning;
                byte[][] result = OptionalUldPatch.TryApplyAll(
                    "pair",
                    pair,
                    ReadPathBytes,
                    delegate(string path, byte[] source)
                    {
                        if (path == pair[1])
                        {
                            throw new InvalidDataException("node changed");
                        }

                        return MarkPatched(path, source);
                    },
                    out warning);
                Check("second of pair fails: nothing patched", result == null, "result=" + Describe(result));
                Check("second of pair fails: warning names both ULDs and the failure",
                    warning != null && warning.Contains(pair[0]) && warning.Contains(pair[1]) &&
                    warning.Contains("InvalidDataException") && warning.Contains("node changed"),
                    "warning=" + (warning ?? "(null)"));
            }

            // 게임 업데이트로 ULD 가 없어진 경우.
            ExpectSkippedBy("ULD removed by an update",
                delegate(string path) { throw new FileNotFoundException("SqPack file was not found: " + path); },
                MarkPatched,
                "FileNotFoundException");

            // 검사 코드의 checked 산술이 넘친 경우도 데이터 문제로 본다.
            ExpectSkippedBy("offset arithmetic overflow",
                ReadPathBytes,
                delegate(string path, byte[] source) { throw new OverflowException(); },
                "OverflowException");

            // 메모리 부족은 건너뛰지 않고 그대로 올린다.
            {
                bool rethrown = false;
                try
                {
                    string warning;
                    OptionalUldPatch.TryApplyAll(
                        "oom",
                        pair,
                        ReadPathBytes,
                        delegate(string path, byte[] source) { throw new OutOfMemoryException(); },
                        out warning);
                }
                catch (OutOfMemoryException)
                {
                    rethrown = true;
                }

                Check("out of memory is rethrown", rethrown, "exception was swallowed");
            }

            Console.WriteLine("{0} passed, {1} failed", _passed, _failures);
            return _failures == 0 ? 0 : 1;
        }

        private static void ExpectSkipped(string name, string[] uldPaths, Func<string, byte[], byte[]> apply, byte[] source)
        {
            string warning = null;
            byte[][] result = null;
            try
            {
                result = OptionalUldPatch.TryApplyAll(
                    name,
                    uldPaths,
                    delegate(string path) { return (byte[])source.Clone(); },
                    apply,
                    out warning);
            }
            catch (Exception ex)
            {
                Fail(name, "threw " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            Check(name, result == null && warning != null && warning.Contains("kept as global original"),
                "result=" + Describe(result) + ", warning=" + (warning ?? "(null)"));
        }

        private static void ExpectSkippedBy(
            string name,
            Func<string, byte[]> read,
            Func<string, byte[], byte[]> apply,
            string exceptionName)
        {
            string warning = null;
            byte[][] result = null;
            try
            {
                result = OptionalUldPatch.TryApplyAll(name, new string[] { "ui/uld/A.uld" }, read, apply, out warning);
            }
            catch (Exception ex)
            {
                Fail(name, "threw " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            Check(name, result == null && warning != null && warning.Contains(exceptionName),
                "result=" + Describe(result) + ", warning=" + (warning ?? "(null)"));
        }

        private static byte[] ReadPathBytes(string path)
        {
            // "ui/uld/A.uld" -> { 0, 'A' }
            return new byte[] { 0, (byte)path[7] };
        }

        private static byte[] MarkPatched(string path, byte[] source)
        {
            byte[] patched = (byte[])source.Clone();
            patched[0] = 0xFF;
            return patched;
        }

        private static byte[] UldHeaderWithBadAtkOffset()
        {
            // 올바른 uldh/atkh 시그니처에 범위를 벗어난 ATK 오프셋.
            byte[] uld = new byte[64];
            WriteAscii(uld, 0, "uldh0100");
            WriteAscii(uld, 16, "atkh0100");
            uld[8] = 0xF0;
            uld[9] = 0xFF;
            uld[12] = 0xF0;
            uld[13] = 0xFF;
            return uld;
        }

        private static void WriteAscii(byte[] data, int offset, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                data[offset + i] = (byte)text[i];
            }
        }

        private static string Describe(byte[][] result)
        {
            return result == null ? "(null)" : result.Length.ToString() + " ULD(s)";
        }

        private static void Check(string name, bool ok, string detail)
        {
            if (ok)
            {
                _passed++;
                return;
            }

            Fail(name, detail);
        }

        private static void Fail(string name, string detail)
        {
            _failures++;
            Console.WriteLine("FAIL: {0}: {1}", name, detail);
        }
    }
}
