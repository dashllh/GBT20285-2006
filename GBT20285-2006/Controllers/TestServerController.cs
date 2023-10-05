using GBT20285_2006.DBContext;
using GBT20285_2006.Models;
using GBT20285_2006.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GBT20285_2006.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestServerController : ControllerBase
    {
        static bool IsInitialized { get; set; } = false;

        private readonly IDbContextFactory<GB20285DBContext> _dbContextFactory;

        private readonly TestServerContainer _serverContainer;

        public TestServerController(TestServerContainer container, IDbContextFactory<GB20285DBContext> context)
        {
            _serverContainer = container;
            _dbContextFactory = context;
            if (!IsInitialized)
            {
                foreach (var server in _serverContainer.Servers)
                {
                    server.Value.Initialize();
                }
                IsInitialized = true;
            }
        }

        /*
         * 功能: 试验服务器登录验证
         * 参数:
         *       userid - 用户Id/用户名
         *       passwd - 密码
         * 返回:
         *       Message - 试验服务器消息对象
         */
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var response = new ServerResponseMessage();
            response.Command = "login";
            // 从数据库查找指定用户的密码
            using var ctx = _dbContextFactory.CreateDbContext();
            try
            {
                var loginuser = await ctx.Users.Where(x => x.Userid == user.Userid).FirstAsync();
                if (loginuser != null)
                {
                    // 登录用户存在,验证登录密码
                    using (SHA256 sha256Hash = SHA256.Create())
                    {
                        // Convert the input string to a byte array and compute the hash.
                        byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(user.Passwd != null ? user.Passwd : string.Empty));
                        // Create a new Stringbuilder to collect the bytes and create a string.
                        var sBuilder = new StringBuilder();
                        // Loop through each byte of the hashed data and format each one as a hexadecimal string.
                        for (int i = 0; i < data.Length; i++)
                        {
                            sBuilder.Append(data[i].ToString("x2"));
                        }
                        // Return the hexadecimal string.
                        var passwdhash = sBuilder.ToString();
                        // Create a StringComparer an compare the hashes.
                        StringComparer comparer = StringComparer.OrdinalIgnoreCase;

                        if (comparer.Compare(passwdhash, loginuser.Passwd) == 0)
                        {
                            response.Result = true;
                            response.Time = DateTime.Now.ToString("HH:mm:ss");
                            response.Message = "登录成功";
                            response.Parameters.Add("dispname", loginuser.Dispname != null ? loginuser.Dispname : string.Empty);
                            response.Parameters.Add("type", loginuser.Type != null ? loginuser.Type : string.Empty);
                        }
                        else
                        {
                            response.Result = false;
                            response.Time = DateTime.Now.ToString("HH:mm:ss");
                            response.Message = "密码验证失败";
                        }
                    }
                }
                else
                {
                    response.Result = false;
                    response.Time = DateTime.Now.ToString("HH:mm:ss");
                    response.Message = "该用户不存在";
                }
                return new JsonResult(response);
            }
            catch (Exception e)
            {
                await ctx.DisposeAsync();
                response.Result = false;
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                response.Message = e.Message;
                return new JsonResult(response);
            }
        }

        /*
         * 功能: 变更当前用户的登录密码
         * 参数:
         *       passwd - 当前密码
         *       newpasswd - 新密码
         * 返回:
         *       Message - 试验服务器消息对象
         */
        [HttpPut("changepasswd")]
        //public async Task<IActionResult> ChangePasswd(string passwd, string newpasswd)
        //{

        //    return Ok();
        //}

        /*
         * 功能: 获取试验服务器当前信息
         */
        [HttpGet("getserverinfo")]
        public async Task<IActionResult> GetTestServerInformation()
        {
            var response = new ServerResponseMessage();
            response.Command = "getserverinfo";
            using var ctx = _dbContextFactory.CreateDbContext();
            try
            {
                var serverlist = await ctx.Apparatuses.ToListAsync();
                response.Result = true;
                response.Message = "成功获取试验服务器信息。";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                response.Parameters.Add("result",serverlist);

                return new JsonResult(response);
            }
            catch (Exception e)
            {
                await ctx.DisposeAsync();
                response.Result = false;
                response.Message= e.Message;
                response.Time = DateTime.Now.ToString("HH:mm:ss");
                
                return new JsonResult(response);
            }            
        }

        /*
         * 功能: 开始试验数据记录,若当前未达到试验条件则返回提示信息
         */
        [HttpGet("startrecording/{id}")]
        public IActionResult StartRecording(int id)
        {
            var response = new ServerResponseMessage();
            response.Command = "startrecording";

            if (_serverContainer.Servers[id].Status == MasterStatus.Ready)
            {
                _serverContainer.Servers[id].StartRecording();
                response.Result = true;
                response.Message = "OK";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
            }
            else
            {
                response.Result = false;
                response.Message = "尚未满足试验条件,是否仍然开始数据记录?";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
            }

            return new JsonResult(response);
        }

        /* 功能: 开始试验数据记录,若当前未达到试验条件,仍然开始记录数据 */
        [HttpGet("startrecordinganyway/{id}")]
        public IActionResult StartRecordingAnyWay(int id)
        {
            var response = new ServerResponseMessage();
            response.Command = "startrecording";

            _serverContainer.Servers[id].StartRecording();

            response.Result = true;
            response.Message = "OK";
            response.Time = DateTime.Now.ToString("HH:mm:ss");            

            return new JsonResult(response);
        }

        /*
         * 功能: 设置试验现象
         * 参数:
         *       id        - 试验服务器Id
         *       phenocode - 试验现象编码
         *       memo      - 其他现象描述
         */
        [HttpGet("setphenomenon/{id}/{phenocode}/{memo}")]
        public async Task<IActionResult> SetPhenomenon(int id, string phenocode, string memo)
        {
            var response = new ServerResponseMessage();
            response.Command = "setphenomenon";
            var ret = await _serverContainer.Servers[id].SetPhenomenon(phenocode, memo);
            if (ret)
            {
                response.Result = true;
                response.Message = "成功记录试验现象.";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
            }
            else
            {
                response.Result = false;
                response.Message = "尚未创建试验任务数据,请先录入试验数据。";
                response.Time = DateTime.Now.ToString("HH:mm:ss");
            }

            return new JsonResult(response);
        }

        /*
         * 功能: 设置服务器端试验数据缓存
         * 参数:
         *       id   - 试验服务器Id
         *       data - 上传的样品数据及试验数据
         * 返回:
         *       Message对象
         */
        [HttpPost("createnewtest/{id}")]
        public async Task<IActionResult> CreateNewTest(int id, NewTestDataModel data)
        {
            var response = new ServerResponseMessage();
            response.Command = "createnewtest";

            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                try
                {
                    // 保存首次试验的产品数据至数据库
                    var exist = await ctx.Products.AnyAsync(x => x.Productid == data.Product.Productid);
                    if (!exist)
                    {
                        ctx.Products.Add(data.Product);
                        await ctx.SaveChangesAsync();
                    }
                    // 判断试验编号是否重复
                    exist = await ctx.Tests.AnyAsync(record => record.Specimenid == data.Product.Productid && record.Testid == data.Test.Testid);
                    if (exist)
                    {
                        response.Result = false;
                        response.Message = "样品标识号(试验编号重复)";
                        response.Time = DateTime.Now.ToString("HH:mm:ss");
                        return new JsonResult(response);
                    }
                    // 设置服务器端样品数据及试验数据缓存
                    _serverContainer.Servers[id].SetProductData(data.Product);
                    _serverContainer.Servers[id].SetTestData(data.Test);
                    response.Result = true;
                    response.Message = $"新建试验成功。样品编号:[ {data.Product.Productid} ], 试验编号:[ {data.Test.Testid} ]";
                    response.Time = DateTime.Now.ToString("HH:mm:ss");
                }
                catch (Exception e)
                {
                    response.Result = false;
                    response.Message = e.Message;
                    response.Time = DateTime.Now.ToString("HH:mm:ss");
                    await ctx.DisposeAsync();

                    return new JsonResult(response);
                }
            }

            return new JsonResult(response);
        }
    }

    public class NewTestDataModel
    {
        public Product Product { get; set; } = null!;
        public Test Test { get; set; } = null!;
    }
}
