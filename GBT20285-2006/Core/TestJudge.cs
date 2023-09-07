using GBT20285_2006.Models;
using System.Numerics;
using System.Runtime.CompilerServices;

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
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (false:不合格 | true:合格 | null:待观察)
         */
        public static (bool?, bool?, bool?) JudgeWithin30Min(bool allalive)
        {
            bool? narcotic;
            bool? irritation;
            bool? final;
            // 麻醉性结论判定
            narcotic = allalive ? null : false;
            // 刺激性结论判定
            irritation = allalive ? null : false;
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
         * 返回:
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (false:不合格 | true:合格 | null:待观察)
         */
        public static (bool?, bool?, bool?) JudgeWithin1Hour(bool allalive, float preavgweight, float postavgweight)
        {
            bool? narcotic;
            bool? irritation;
            bool? final;
            // 麻醉性结论判定
            narcotic = allalive;
            // 刺激性结论判定
            if (!allalive)
            {
                irritation = false;
            }
            else
            {
                if ((int)(postavgweight * 10) >= (int)(preavgweight * 10))
                {
                    irritation = true;
                }
                else
                {
                    irritation = null;
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
         *       postavgweight - 执行该判定时,小鼠的平均体重.即第 nday 天的平均体重
         * 返回:
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (false:不合格 | true:合格 | null:待观察)
         */
        public static (bool?, bool?, bool?) JudgeWithinPostDays(int nday, bool allalive, float preavgweight, float postavgweight)
        {
            bool? narcotic = true;  // 麻醉性结论默认判定为[合格]
            bool? irritation = null;
            bool? final;
            // 刺激性结论
            if (!allalive)
            {
                irritation = false;
            }
            else
            {
                if ((int)(postavgweight * 10) >= (int)(preavgweight * 10))
                {
                    irritation = true;
                }
                else
                {
                    if (nday == 1 || nday == 2)
                    {
                        irritation = null;
                    }
                    else if (nday == 3)
                    {
                        irritation = false;
                    }
                }
            }
            // 综合结论
            final = irritation;

            return (narcotic, irritation, final);
        }

        /*
         * 功能: 指定产品编号及试验编号的结论判定
         * 参数:
         *       records - 小鼠体重列表
         * 返回:
         *       (narcotic,irritation,final) - 麻醉性结论,刺激性结论,最终结论 (false:不合格 | true:合格 | null:待观察)
         */
        public static (bool?, bool?, bool?) JudgeFinalResult(IList<MouseWeight> records)
        {
            bool? narcotic = null;
            bool? irritation = null;
            bool? final = null;

            // 第1步: 
            foreach (var item in records)
            {

            }

            // 第2步: 计算试验当日平均体重

            return (narcotic, irritation, final);
        }
    }
}