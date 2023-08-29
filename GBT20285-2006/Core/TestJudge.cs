using System.Numerics;

namespace GBT20285_2006.Core
{
    public class TestJudge
    {
        /*
         * 功能: 执行试验结论判定
         * 参数:
         *       preavgweight - 试验前,一组小鼠的平均体重(单位:g)
         */
        public static bool Judge(float preavgweight)
        {

            return true;
        }

        /*
         * 功能: 试验结束后立即执行的判定(染毒30min结束后的立即判定)
         * 参数:
         *       allalive - 染毒30min结束后,一组小鼠是否都存活(true:都存活 | false:任意一只死亡 )
         * 返回:
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (true:合格 | false:不合格)
         */
        public static (bool, bool, bool) JudgePostTest(bool allalive)
        {
            return (true,true,true);
        }


    }
}
