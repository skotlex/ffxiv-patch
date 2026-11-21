using System;
using System.Threading;

namespace FfxivKoreanPatch.FFXIVPatchGenerator
{
    // 창별 ULD 보정(글자 크기·위치)은 게임 업데이트로 창 구조가 바뀌면 검사에서 실패한다.
    // 이 보정이 없어도 한글은 표시되므로, 실패한 보정 묶음만 건너뛰고 해당 ULD 는 글로벌 원본으로 둔다.
    // 텍스트·폰트 패치 생성은 그대로 계속한다.
    internal static class OptionalUldPatch
    {
        // 묶음의 ULD 를 모두 고칠 수 있을 때만 고친 내용을 돌려준다.
        // 하나라도 실패하면 null 을 돌려주고 warning 에 이유를 적는다. 일반/고난도 창처럼 짝을 이루는 ULD 가
        // 한쪽만 바뀌지 않게 하려는 것이다. 읽기와 검사만 여기서 하고, dat/index 쓰기는 호출하는 쪽에서 한다.
        public static byte[][] TryApplyAll(
            string patchName,
            string[] uldPaths,
            Func<string, byte[]> readSource,
            Func<string, byte[], byte[]> apply,
            out string warning)
        {
            warning = null;
            byte[][] patched = new byte[uldPaths.Length][];
            for (int i = 0; i < uldPaths.Length; i++)
            {
                string uldPath = uldPaths[i];
                try
                {
                    patched[i] = apply(uldPath, readSource(uldPath));
                }
                catch (Exception ex)
                {
                    if (IsUnrecoverable(ex))
                    {
                        throw;
                    }

                    warning = FormatSkipWarning(patchName, uldPaths, uldPath, ex);
                    return null;
                }
            }

            return patched;
        }

        private static string FormatSkipWarning(string patchName, string[] uldPaths, string failedPath, Exception ex)
        {
            return patchName + " was skipped; " + string.Join(", ", uldPaths) +
                " kept as global original. " + failedPath + ": " + ex.GetType().Name + ": " + ex.Message;
        }

        private static bool IsUnrecoverable(Exception ex)
        {
            return ex is OutOfMemoryException || ex is ThreadAbortException;
        }
    }
}
