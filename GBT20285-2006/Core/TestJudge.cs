using System.Numerics;

namespace GBT20285_2006.Core
{
    /* 定义试验结论判定逻辑的执行类型,依据《材料产烟毒性危险分级试验结论判定决策表》 */
    public class TestJudge
    {
        /*
         * 功能: 染毒30分钟后立即执行的判定(染毒30min结束后的立即判定)
         * 参数:
         *       allalive - 染毒30min结束后,试验小鼠是否都存活(true:都存活 | false:任意一只死亡 )
         * 返回:
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (-1:不合格 | 0:待观察 | 1:合格)
         */
        public static (int, int, int) JudgeWithin30Min(bool allalive)
        {
            int narcotic;
            int irritation;
            int final = 0;
            // 麻醉性结论判定
            narcotic = allalive ? 0 : -1;
            // 刺激性结论判定
            irritation = allalive ? 0 : -1;
            // 综合结论
            final = irritation;

            return (narcotic, irritation, final);
        }

        /*
         * 功能: 染毒30分钟后至染毒1小时内执行的判定
         * 参数:
         *       allalive      - 判定时间范围内,试验小鼠是否都存活(true:都存活 | false:任意一只死亡 )
         *       preavgweight  - 试验前,小鼠的平均体重
         *       postavgweight - 执行该判定时,小鼠的平均体重
         */
        public static (int, int, int) JudgeWithin1Hour(bool allalive, float preavgweight, float postavgweight)
        {
            int narcotic;
            int irritation;
            int final;
            // 麻醉性结论判定
            narcotic = allalive ? 0 : -1;
            // 刺激性结论判定
            if (!allalive)
            {
                irritation = -1;
            }
            else
            {
                if ((int)(postavgweight * 10) >= (int)(preavgweight * 10))
                {
                    irritation = 1;
                }
                else
                {
                    irritation = 0;
                }
            }
            // 综合结论
            final = irritation;

            return (narcotic, irritation, final);
        }

        /*
         * 功能: 染毒后第n天执行的判定
         * 参数:
         *       nday - 试验后第n天(n = 1,2,3)
         *       allalive      - 判定时间范围内,试验小鼠是否都存活(true:都存活 | false:任意一只死亡 )
         *       preavgweight  - 试验前,小鼠的平均体重
         *       postavgweight - 执行该判定时,小鼠的平均体重
         */
        public static (int, int, int) JudgeWithinPostDays(int nday, bool allalive, float preavgweight, float postavgweight)
        {
            int narcotic = 1;  // 麻醉性结论默认判定为[合格]
            int irritation = 0;
            int final;
            // 刺激性结论
            if (!allalive)
            {
                irritation = -1;
            }
            else
            {
                if ((int)(postavgweight * 10) >= (int)(preavgweight * 10))
                {
                    irritation = 1;
                }
                else
                {
                    if(nday == 1 || nday == 2)
                    {
                        irritation = 0;
                    } 
                    else if(nday == 3)
                    {
                        irritation = -1;
                    }                    
                }
            }
            // 综合结论
            final = irritation;

            return (narcotic, irritation, final);
        }
    }
}
