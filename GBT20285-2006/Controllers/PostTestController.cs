using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using GBT20285_2006.Models;
using GBT20285_2006.DBContext;
using GBT20285_2006.Core;
using GBT20285_2006.Utility;
using OfficeOpenXml;
using DevExpress.XtraPrinting;
using DevExpress.Spreadsheet;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
         * 功能: 新增指定样品的指定试验编号的小鼠体重数据(客户端新建样品试验任务时调用)
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
            var response = new ServerResponseMessage();
            response.Command = "judgefinalresult";
            var ctx = _dbContextFactory.CreateDbContext();
            try
            {
                // 获取指定试验数据
                var testrecord = await ctx.Tests.Where(x => x.Specimenid == productid && x.Testid == testid).FirstOrDefaultAsync();
                /* Step 1 - 根据 染毒期间30Min内试验现象 进行判定 */
                if (testrecord == null)
                {
                    response.Result = false;
                    response.Message = "未查询到指定编号的试验记录";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");
                    return new JsonResult(response);
                }

                (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult) =
                        TestJudge.JudgeWithin30Min(testrecord.Phenocode?[0] == '0');
                // 如果已得出结论,则设置处理标志并终止继续判定
                if (testrecord.Testresult != null)
                {
                    if (testrecord.Flag != null)
                    {
                        char[] array = testrecord.Flag.ToCharArray();
                        array[0] = '1'; // 设置处理标志为[已得出结论]
                        testrecord.Flag = new string(array);
                    }
                    response.Result = true;
                    response.Message = "得出最终判定结论";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");
                    response.Parameters.Add("result", new SingleTestResult()
                    {
                        Nounresult = testrecord.Nounresult,
                        Irriresult = testrecord.Irriresult,
                        Testresult = testrecord.Testresult
                    });

                    // 保存最新判定结论至数据库
                    await ctx.SaveChangesAsync();

                    return new JsonResult(response);
                }

                /* Step 2 - 根据 染毒后1h内小鼠状态 进行判定 (该步骤根据实际检验业务需求定制)*/

                /* Step 3 - 根据 染毒后第n天体重数据 进行判定 */
                // 获取指定试验的小鼠体重列表数据
                var weightrecords = await ctx.MouseWeights.Where(x => x.ProductId == productid && x.TestId == testid).ToListAsync();
                // 将体重数据转换为按列操作的内存数据结构(第一列为:试验当天所有小鼠的体重数据,以此类推...)
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
                for (int i = 1; i < 4; i++)
                {
                    // 确认当前判定日体重值的有效性,即:若有null值,则认为数据不完整,不纳入判定
                    if (weights[i].Any(x => x == null))
                    {
                        response.Result = true;
                        response.Message = "未得出最终判定结论";
                        response.Time = DateTime.Now.ToString("HH:mm:ss");
                        response.Parameters.Add("result", new SingleTestResult()
                        {
                            Nounresult = testrecord.Nounresult,
                            Irriresult = testrecord.Irriresult,
                            Testresult = testrecord.Testresult
                        });

                        break;
                    }
                    // 判断是否有小鼠死亡
                    if (weights[i].Any(x => x == 0))
                    {
                        alllive = false;
                    }
                    // 计算判定日的小鼠平均体重
                    avgPostWeight = weights[i].Average();
                    // 执行 试验后第i日 的结论判定
                    if (avgTestDayWeight.HasValue && avgPostWeight.HasValue)
                    {
                        (testrecord.Nounresult, testrecord.Irriresult, testrecord.Testresult) = TestJudge.JudgeWithinPostDays(i, alllive, (float)avgTestDayWeight, (float)avgPostWeight);
                    }
                    // 确认是否已经得出有效判定结论,即:结论不包含"待观察"
                    if (testrecord.Testresult.HasValue)
                    {
                        response.Result = true;
                        response.Message = "得出最终判定结论";
                        response.Time = DateTime.Now.ToString("HH:mm:ss");
                        response.Parameters.Add("result", new SingleTestResult()
                        {
                            Nounresult = testrecord.Nounresult,
                            Irriresult = testrecord.Irriresult,
                            Testresult = testrecord.Testresult
                        });
                        // 分析并给出判定依据
                        if (testrecord.Testresult == true)
                        { // 最终结论为[合格]
                            //设置平均体重恢复天数
                            testrecord.Recoveryday = Convert.ToByte(i);
                            response.Parameters.Add("noun", "试验动物在染毒期内(含染毒后1小时)无死亡");
                            response.Parameters.Add("irri", $"试验动物体重在试验后第 {i} 天恢复");
                        }
                        else if (testrecord.Testresult == false)
                        {  // 最终结论为[不合格]  
                            response.Parameters.Add("noun", "试验动物在染毒期内(含染毒后1小时)无死亡");
                            if (!alllive)
                            {  // 出现小鼠死亡 导致判定不合格的情况
                                response.Parameters.Add("irri", $"试验动物在试验后第 {i} 天死亡");
                            }
                            else
                            {  // 试验后3天内体重未恢复 导致判定不合格的情况
                                response.Parameters.Add("irri", $"试验动物平均体重在试验后3天内未恢复");
                            }
                        }
                        // 设置处理标志
                        if (testrecord.Flag != null)
                        {
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
                response.Result = false;
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                response.Message = e.Message;
                throw;
            }

            return new JsonResult(response);
        }

        /*
         * 功能: 生成试验报表
         * 参数:
         *       specimenid - 样品编号
         *       testid     - 试验编号
         * 返回:
         *       ServerResponseMessage
         */
        [HttpGet("gettestreport/{productid}/{testid}")]
        public async Task<IActionResult> GenerateTestReport(string productid, string testid, [FromQuery] double postweight, [FromQuery] string? smokeoption)
        {
            var response = new ServerResponseMessage();
            response.Command = "gettestreport";

            /* 试验数据存储目录 */
            string prodpath = $"D:\\GB20285\\{productid}";
            string smppath = $"{prodpath}\\{testid}";
            string datapath = $"{smppath}\\data";
            string rptpath = $"{smppath}\\report";

            try
            {
                // 从数据库获取指定样品编号及试验编号的试验数据
                var ctx = _dbContextFactory.CreateDbContext();
                var productrecord = ctx.Products.AsNoTracking().Where(x => x.Productid == productid).First();
                if (productrecord == null)
                {
                    response.Result = false;
                    response.Message = "未找到指定样品编号的试验记录,请检查样品编号输入。";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");

                    return new JsonResult(response);
                }
                var testrecord = ctx.Tests.Where(x => x.Specimenid == productid && x.Testid == testid).First();
                if (testrecord == null)
                {
                    response.Result = false;
                    response.Message = "未找到指定试验编号(样品标识号)的试验记录,请检查试验编号输入。";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");

                    return new JsonResult(response);
                }

                /* 生成本次试验的报表 */
                //设置EPPlus license版本为非商用版本
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                //设置CSV文件格式
                var format = new ExcelTextFormat()
                {
                    Delimiter = ',',
                    EOL = "\r"    // 修改行尾结束符,默认为 "\r\n" ("\r"为回车符 "\n"为换行符);
                                  // 字符类型引用符 format.TextQualifier = '"';
                };
                //填充试验报表模板文件
                using (ExcelPackage package = new ExcelPackage(new FileInfo($"D:\\GB20285\\template_report.xlsx")))
                {
                    //取得传感器数据页面
                    ExcelWorksheet sheet_sensordata = package.Workbook.Worksheets.ElementAt(1);
                    //填充传感器原始数据(含首行标题)
                    sheet_sensordata.Cells["A1"].LoadFromText(new FileInfo($"{datapath}\\sensordata.csv"), format, null, true);
                    //取得小鼠体重页面
                    ExcelWorksheet sheet_mouseweight = package.Workbook.Worksheets.ElementAt(2);
                    //将小鼠体重页面拷贝至试验报表的 [小鼠体重] 页面
                    if (testrecord != null)
                    {
                        var records = ctx.MouseWeights.AsNoTracking()
                            .Where(x => x.ProductId == testrecord.Specimenid && x.TestId == testrecord.Testid)
                            .OrderBy(x => x.MouseId)
                            .ToList();
                        sheet_mouseweight.Cells["A1"].LoadFromDataTable(Utils.ToDataTable(records));
                    }

                    /* 设置报表首页部分数据 */
                    //取得报表首页页面(页面索引从 0 开始)
                    ExcelWorksheet sheet_main = package.Workbook.Worksheets.ElementAt(0);
                    if (testrecord != null)
                    {
                        //报告编号
                        sheet_main.Cells["AB2"].Value = testrecord.Specimenid;
                        //实验室温度
                        sheet_main.Cells["G9"].Value = testrecord.Ambtemp;
                        //环境湿度
                        sheet_main.Cells["AC9"].Value = testrecord.Ambhumi;
                        //产品名称
                        sheet_main.Cells["G10"].Value = productrecord.Productname;
                        //样品编号
                        sheet_main.Cells["AA10"].Value = testrecord.Specimenid;
                        //试验编号(样品标识号)
                        sheet_main.Cells["AA11"].Value = testrecord.Testid;
                        //规格型号
                        sheet_main.Cells["G12"].Value = productrecord.Specification;
                        //试件尺寸
                        sheet_main.Cells["AA12"].Value = testrecord.Specilength;
                        //样品形态
                        sheet_main.Cells["G13"].Value = productrecord.Shape;
                        //加热温度
                        sheet_main.Cells["AA13"].Value = testrecord.Heattemp;

                        //状态调节T
                        //状态调节H
                        //状态调节S

                        //设备编号
                        sheet_main.Cells["G15"].Value = testrecord.Apparatusid;
                        //设备检定日期From
                        sheet_main.Cells["AA14"].Value = testrecord.Checkdatef?.ToString("d");
                        //设备检定日期To
                        sheet_main.Cells["AA15"].Value = testrecord.Checkdatet?.ToString("d");

                        //实验动物来源
                        //实验动物品种
                        //实验动物级别

                        //检验依据
                        sheet_main.Cells["AA17"].Value = testrecord.According;
                        //材料产烟浓度等级
                        switch (testrecord.Safetylevel)
                        {
                            case "AQ1":
                                sheet_main.Cells["G18"].Value = "■";
                                sheet_main.Cells["G45"].Value = "■";
                                break;
                            case "AQ2":
                                sheet_main.Cells["L18"].Value = "■";
                                sheet_main.Cells["K45"].Value = "■";
                                break;
                            case "ZA1":
                                sheet_main.Cells["Q18"].Value = "■";
                                sheet_main.Cells["O45"].Value = "■";
                                break;
                            case "ZA2":
                                sheet_main.Cells["V18"].Value = "■";
                                sheet_main.Cells["S45"].Value = "■";
                                break;
                            case "ZA3":
                                sheet_main.Cells["AA18"].Value = "■";
                                sheet_main.Cells["W45"].Value = "■";
                                break;
                            case "WX":
                                sheet_main.Cells["AA45"].Value = "■";
                                break;
                            default:
                                break;
                        }
                        //试件质量
                        sheet_main.Cells["G19"].Value = testrecord.Speciweight;
                        //残余质量
                        testrecord.Speciweightpost = postweight;
                        sheet_main.Cells["R19"].Value = postweight;
                        //产烟率
                        testrecord.Smokerate = (testrecord.Speciweight - postweight) / testrecord.Speciweight;
                        sheet_main.Cells["AA19"].Value = testrecord.Smokerate;
                        //充分产烟率的确定方法
                        if (testrecord.Smokerate.HasValue)
                        {
                            var tempvalue = (int)(testrecord.Smokerate * 10);
                            if (tempvalue > 950)
                            {
                                sheet_main.Cells["G21"].Value = "■";
                            }
                            else if (smokeoption == "1")
                            {
                                sheet_main.Cells["G20"].Value = "■";
                            }
                            else if (smokeoption == "2")
                            {
                                sheet_main.Cells["G22"].Value = "■";
                            }
                        }
                        //载气流量
                        sheet_main.Cells["G23"].Value = testrecord.Cgasflow;
                        //稀释气流量
                        sheet_main.Cells["AA23"].Value = testrecord.Dgasflow;
                        //麻醉性结论
                        if (testrecord.Nounresult == true)
                        {
                            sheet_main.Cells["K43"].Value = "合格";
                        }
                        else if (testrecord.Nounresult == false)
                        {
                            sheet_main.Cells["K43"].Value = "不合格";
                        }
                        else
                        {
                            sheet_main.Cells["K43"].Value = "待观察";
                        }
                        //刺激性结论
                        if (testrecord.Irriresult == true)
                        {
                            sheet_main.Cells["S43"].Value = "合格";
                        }
                        else if (testrecord.Irriresult == false)
                        {
                            sheet_main.Cells["S43"].Value = "不合格";
                        }
                        else
                        {
                            sheet_main.Cells["S43"].Value = "待观察";
                        }
                        //综合结论
                        if (testrecord.Testresult == true)
                        {
                            sheet_main.Cells["G46"].Value = "■";
                            sheet_main.Cells["K46"].Value = "";
                        }
                        else if (testrecord.Testresult == false)
                        {
                            sheet_main.Cells["G46"].Value = "";
                            sheet_main.Cells["K46"].Value = "■";
                        }
                        else
                        {
                            sheet_main.Cells["G46"].Value = "";
                            sheet_main.Cells["K46"].Value = "";
                        }
                        //备注
                        sheet_main.Cells["G47"].Value = testrecord.Comment;
                        //检验人员
                        sheet_main.Cells["E50"].Value = testrecord.Operator;
                        //试验日期
                        sheet_main.Cells["AB50"].Value = testrecord.Testdate?.ToString("d");
                        //小鼠体重恢复情况
                        if (testrecord.Testresult == true)
                        {
                            if (testrecord.Recoveryday.HasValue)
                            {
                                sheet_main.Cells["G34"].Value = "■";
                                sheet_main.Cells["L34"].Value = testrecord.Recoveryday;
                            }
                        }
                        else if (testrecord.Testresult == false)
                        {
                            sheet_main.Cells["G35"].Value = "■";
                        }
                        //试验现象
                        if (testrecord.Phenocode != null)
                        {
                            var pheno = new StringBuilder();
                            if (testrecord.Phenocode[1] == '1') pheno.Append("欲跑不能,");
                            if (testrecord.Phenocode[2] == '1') pheno.Append("呼吸变化,");
                            if (testrecord.Phenocode[3] == '1') pheno.Append("惊跳,");
                            if (testrecord.Phenocode[4] == '1') pheno.Append("挣扎,");
                            if (testrecord.Phenocode[5] == '1') pheno.Append("不能翻身,");
                            if (testrecord.Phenocode[6] == '1') pheno.Append("昏迷,");
                            if (testrecord.Phenocode[7] == '1') pheno.Append("痉挛,");
                            if (testrecord.Phenocode[8] == '1') pheno.Append("视力丧失,");
                            if (testrecord.Phenocode[9] == '1') pheno.Append("流泪,");
                            if (testrecord.Phenocode[10] == '1') pheno.Append("肿胀,");
                            if (testrecord.Phenocode[11] == '1') pheno.Append("闭目,");
                            //去掉末尾","
                            pheno.Length--;
                            //输出现象描述
                            sheet_main.Cells["G36"].Value = pheno.ToString();
                        }
                        // 保存试验结果补录数据至数据库
                        await ctx.SaveChangesAsync();
                        //保存本次试验报表
                        await package.SaveAsAsync($"{rptpath}\\report.xlsx");
                    }
                }
                /* 使用DevExpress Office API打开报表文件、执行公式计算并导出报表首页PDF文件 */
                using (var workbook = new Workbook())
                {
                    // 加载报表文件
                    workbook.LoadDocument($"{rptpath}\\report.xlsx", DocumentFormat.Xlsx);
                    // 计算报表中所有公式
                    workbook.CalculateFull();
                    // 导出报表首页为PDF格式
                    PdfExportOptions options = new PdfExportOptions();
                    options.DocumentOptions.Author = testrecord?.Operator;
                    workbook.ExportToPdf($"{rptpath}\\report.pdf", options, "main");
                }

                response.Result = true;
                response.Message = $"生成报表成功。样品编号: [ {testrecord?.Specimenid} ],试验编号:[ {testrecord?.Testid} ]";
                response.Time = DateTime.Now.ToString("HH:mm:ss");

                return new JsonResult(response);
            }
            catch (Exception e)
            {
                response.Result = false;
                response.Message = "报表生成过程发生异常,报表生成失败。";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                response.Parameters.Add("error", e.Message);

                return new JsonResult(response);
            }
        }

        /*
         * 功能: 试验数据检索,用于生成试验报告
         * 参数:
         *       specimenid - 样品编号
         *       testid     - 试验编号
         * 返回:
         *       ServerResponseMessage
        */
        [HttpGet("searchtestinfo/{productid}/{testid}")]
        public async Task<IActionResult> SearchTestInformation(string productid, string? testid)
        {
            var response = new ServerResponseMessage();
            response.Command = "searchtestinfo";

            var result = new ReportSearchDataModel();
            var ctx = _dbContextFactory.CreateDbContext();
            try
            {

                // 获取样品数据
                var productinfo = await ctx.Products.Where(x => x.Productid == productid).FirstAsync();
                if (productinfo == null)
                {
                    response.Result = false;
                    response.Message = "未查询到指定样品信息,请检查样品编号。";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");

                    return new JsonResult(response);
                }
                // 获取指定样品编号及试验编号的试验数据
                if (!string.IsNullOrEmpty(testid))
                {
                    var testinfo = await ctx.Tests.Where(x => x.Specimenid == productid && x.Testid == testid).FirstAsync();
                    if (testinfo == null)
                    {
                        response.Result = false;
                        response.Message = "未查询到指定试验信息,请检查试验编号。";
                        response.Time = DateTime.Now.ToString("HH:mm:ss");

                        return new JsonResult(response);
                    }
                    response.Result = true;
                    response.Message = "获取数据成功。";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");

                    result.Product = productinfo;
                    result.Tests.Add(testinfo);
                    response.Parameters.Add("result", result);

                    return new JsonResult(response);
                }
                // 获取指定样品编号的所有试验数据
                var testinfolist = await ctx.Tests.Where(x => x.Specimenid == productid).ToListAsync();
                if (testinfolist.Count == 0)
                {
                    response.Result = false;
                    response.Message = "未找到指定样品编号的试验信息。";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");

                    return new JsonResult(response);
                }
                result.Product = productinfo;
                foreach (var item in testinfolist)
                {
                    result.Tests.Add(item);
                }
                response.Parameters.Add("result", result);

                return new JsonResult(response);
            }
            catch (Exception e)
            {
                response.Result = false;
                response.Message = "检索过程发生异常。";
                response.Parameters.Add("error", e.Message);
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                return new JsonResult(response);
            }
        }
    }

    // 单次试验结论(麻醉性、刺激性、综合结论)对象
    internal class SingleTestResult
    {
        public bool? Nounresult { get; set; }
        public bool? Irriresult { get; set; }
        public bool? Testresult { get; set; }
    }

    // 报表检索返回数据对象
    internal class ReportSearchDataModel
    {
        public Product? Product { get; set; }
        public List<Test> Tests { get; set; }
        public ReportSearchDataModel()
        {
            Tests = new List<Test>();
        }
    }

    //控制器函数返回消息对象
    internal class ServerResponseMessage
    {
        //操作命令
        public string Command { get; set; } = string.Empty;
        //返回结果(true:执行成功 | false:执行失败)
        public bool Result { get; set; } = true;
        public string Time { get; set; } = string.Empty;
        //返回消息内容
        public string Message { get; set; } = string.Empty;
        //返回的附加参数
        public Dictionary<string, object> Parameters { get; set; }

        public ServerResponseMessage()
        {
            Parameters = new Dictionary<string, object>();
        }
    }
}
