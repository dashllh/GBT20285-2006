using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GBT20285_2006.Models;
using Microsoft.EntityFrameworkCore;
using GBT20285_2006.DBContext;
using Microsoft.VisualBasic;
using GBT20285_2006.Core;
using Azure;
using System.Text.Json;

namespace GBT20285_2006.Controllers
{
    /*
     * 类型说明: 包含试验后期(即:30分钟染毒后)的所有业务操作
     * 包含:           
     *      1. 新增指定样品的指定试验的小鼠体重信息
     *      2. 获取指定样品的指定试验的小鼠体重数据
     *      3. 更新指定样品的指定试验的小鼠体重数据
     *      4. 判定试验结论
     */
    [Route("[controller]")]
    [ApiController]
    public class PostTestController : ControllerBase
    {
        private readonly IDbContextFactory<GB20285DBContext> _dbContextFactory;

        public PostTestController(IDbContextFactory<GB20285DBContext> context)
        {
            _dbContextFactory = context;
        }

        /*
         * 功能: 获取指定样品的指定试验的小鼠体重数据
         * 参数:         
         *       productid - 样品编号
         *       testid    - 试验编号
         * 返回:
         *       weights   - 小鼠体重数据列表
         */
        [HttpGet("getmouseweight/{productid}/{testid}")]
        public async Task<IList<MouseWeightRecord>> GetMouseWeightAsync(string productid, string testid)
        {
            var ctx = _dbContextFactory.CreateDbContext();
            var records = await ctx.MouseWeights.Where(x => x.ProductId == productid && x.TestId == testid).ToListAsync();
            var values = new List<MouseWeightRecord>();
            foreach (var record in records)
            {
                values.Add(new MouseWeightRecord()
                {
                    Status = record.Status,
                    MouseId = record.MouseId,
                    PreWeight1 = record.PreWeight1,
                    PreWeight2 = record.PreWeight2,
                    ExpWeight = record.ExpWeight,
                    PostWeight1 = record.PostWeight1,
                    PostWeight2 = record.PostWeight2,
                    PostWeight3 = record.PostWeight3
                });
            }
            return values;
        }

        /*
         * 功能: 新增指定样品的指定试验编号的小鼠体重数据
         * 参数:         
         *       productid - 样品编号
         *       testid    - 试验编号
         *       weights   - 小鼠体重列表
         */
        [HttpPost("appendmouseinfo/{productid}/{testid}")]
        public async Task<IActionResult> AppendWeight(string productid, string testid, [FromBody] IList<MouseWeightRecord> weights)
        {
            var ctx = _dbContextFactory.CreateDbContext();
            var records = new List<MouseWeight>();
            foreach (var item in weights)
            {
                records.Add(new MouseWeight()
                {
                    ProductId = productid,
                    TestId = testid,
                    MouseId = item.MouseId,
                    Status = item.Status,
                    PreWeight1 = item.PreWeight1,
                    PreWeight2 = item.PreWeight2,
                    ExpWeight = item.ExpWeight,
                    PostWeight1 = item.PostWeight1,
                    PostWeight2 = item.PostWeight2,
                    PostWeight3 = item.PostWeight3,
                });
            }
            ctx.MouseWeights.AddRange(records);
            await ctx.SaveChangesAsync();

            return Ok(0);
        }

        /*
         * 功能: 更新指定样品的指定试验编号的小鼠体重数据
         * 参数:
         *       productid - 样品编号
         *       testid    - 试验编号
         *       weights   - 小鼠体重列表
         */
        [HttpPut("updatemouseweight/{productid}/{testid}")]
        public async Task<IActionResult> UpdateWeight(string productid, string testid, [FromBody] IList<MouseWeightRecord> weights)
        {
            var ctx = _dbContextFactory.CreateDbContext();
            foreach (var item in weights)
            {
                var record = ctx.MouseWeights.Where(x => x.ProductId == productid && x.TestId == testid && x.MouseId == item.MouseId).FirstOrDefault();
                if (record != null)
                {
                    // 更新小鼠体重及状态数据
                    record.PreWeight1 = item.PreWeight1;
                    record.PreWeight2 = item.PreWeight2;
                    record.ExpWeight = item.ExpWeight;
                    record.PostWeight1 = item.PostWeight1;
                    record.PostWeight2 = item.PostWeight2;
                    record.PostWeight3 = item.PostWeight3;
                    record.Status = item.Status;
                }
            }
            await ctx.SaveChangesAsync();
            return Ok(0);
        }

        /*
         * 功能: 判定指定试验样品的指定试验的最终结论并更新试验记录业务标志
         * 参数:
         *       productid - 样品编号
         *       testid    - 试验编号
         * 返回:
         *       
         */
        [HttpGet("judgefinalresult/{productid}/{testid}")]
        public async Task<IActionResult> JudgeFinalResult(string productid, string testid)
        {
            var respone = new Message();            
            respone.Cmd = "judgefinalresult";
            var ctx = _dbContextFactory.CreateDbContext();
            try
            {
                // 获取指定试验数据
                var testrecord = await ctx.Tests.Where(x => x.Specimenid == productid && x.Testid == testid).FirstOrDefaultAsync();
                /* Step 1 - 根据 染毒期间30Min内试验现象 进行判定 */
                if (testrecord == null) {
                    respone.Ret = "-1";
                    respone.Msg = "未查询到指定编号的试验记录";
                    return new JsonResult(respone);
                } else {
                    (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult) =
                        TestJudge.JudgeWithin30Min(testrecord.Phenocode?[0] == '0');
                    // 如果已得出结论,则设置处理标志并终止继续判定
                    if (testrecord.Testresult != null)
                    {
                        if(testrecord.Flag != null)
                        {
                            char[] array = testrecord.Flag.ToCharArray();
                            array[0] = '1'; // 设置处理标志为[已得出结论]
                            testrecord.Flag = new string(array);
                        }                        
                        respone.Ret = "0";
                        respone.Msg = "得出最终判定结论";
                        respone.Param.Add("result", (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult).ToString());

                        // 保存最新判定结论至数据库
                        await ctx.SaveChangesAsync();

                        return new JsonResult(respone);
                    }
                }

                /* Step 2 - 根据 染毒后1h内小鼠状态 进行判定 */

                /* Step 3 - 根据 染毒后第n天观察数据 进行判定 */
                // 获取指定试验的小鼠体重列表数据
                var weightrecords = await ctx.MouseWeights.Where(x => x.ProductId == productid && x.TestId == testid).ToListAsync();
                List<List<float?>> weights = new List<List<float?>>
                {
                    new List<float?>(),
                    new List<float?>(),
                    new List<float?>(),
                    new List<float?>()
                };
                foreach (var record in weightrecords)
                {
                    weights[0].Add((float?)record.ExpWeight);
                    weights[1].Add((float?)record.PostWeight1);
                    weights[2].Add((float?)record.PostWeight2);
                    weights[3].Add((float?)record.PostWeight3);
                }
                // 计算试验当日小鼠的平均体重
                float? avgTestDayWeight = weights[0].Average();
                float? avgPostWeight;
                bool alllive = true;
                // 从试验后第一日开始执行判定
                for (int i = 1; i < 4; i++) {
                    // 确认当前判定日体重值的有效性,即:若有null值,则认为数据不完整,不纳入判定
                    if (weights[i].Any(x => x == null)) {
                        respone.Ret = "0";
                        respone.Msg = "未得出最终判定结论";
                        respone.Param.Add("result", (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult).ToString());

                        break;
                    }
                    // 判断是否有小鼠死亡
                    if (weights[i].Any(x => x == 0)) {
                        alllive = false;
                    }
                    // 计算判定日的小鼠平均体重
                    avgPostWeight = weights[i].Average();
                    // 执行 试验后第i日 的结论判定
                    if(avgTestDayWeight != null && avgPostWeight != null)
                    {
                        (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult) = TestJudge.JudgeWithinPostDays(i, alllive, (float)avgTestDayWeight, (float)avgPostWeight);
                    }                    
                    // 确认是否已经得出有效判定结论,即:结论不包含"待观察"
                    if (testrecord.Testresult != null) {
                        respone.Ret = "0";
                        respone.Msg = "得出最终判定结论";                        
                        respone.Param.Add("result", (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult).ToString());
                        // 分析并给出判定依据
                        if ((bool)testrecord.Testresult) { // 最终结论为[合格]
                            respone.Param.Add("noun","试验动物在染毒期内(含染毒后1小时)无死亡");
                            respone.Param.Add("irri", $"试验动物体重在试验后第 {i} 天恢复");
                        } else {  // 最终结论为[不合格]  
                            respone.Param.Add("noun", "试验动物在染毒期内(含染毒后1小时)无死亡");
                            if(!alllive) {  // 出现小鼠死亡 导致判定不合格的情况
                                respone.Param.Add("irri", $"试验动物在试验后第 {i} 天死亡");
                            } else {  // 试验后3天内体重未恢复 导致判定不合格的情况
                                respone.Param.Add("irri", $"试验动物平均体重在试验后3天内未恢复");
                            }
                        }
                        // 设置处理标志
                        if (testrecord.Flag != null) {
                            char[] array = testrecord.Flag.ToCharArray();
                            array[0] = '1'; // 设置处理标志为[已得出结论]
                            testrecord.Flag = new string(array);
                        }
                        break;
                    }
                }                
                // 保存最新判定结论至数据库
                await ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                respone.Ret = "-1";
                respone.Msg = e.Message;
                throw;
            }

            return new JsonResult(respone);
        }
    }

    //控制器函数返回消息对象
    internal class Message
    {
        //操作命令
        public string Cmd { get; set; } = string.Empty;
        //返回结果("0":执行成功 | "-1":执行失败)
        public string Ret { get; set; } = string.Empty;
        //返回消息内容
        public string Msg { get; set; } = string.Empty;
        //返回的附加参数
        public Dictionary<string, object> Param { get; set; }

        public Message()
        {            
            Param = new Dictionary<string, object>();
        }
    }
}
