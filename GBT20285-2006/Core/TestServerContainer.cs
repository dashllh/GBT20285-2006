using GBT20285_2006.DBContext;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GBT20285_2006.Core
{
    public class TestServerContainer
    {
        public Dictionary<int, TestServer> Servers { get; set; }
        /* IHubContext对象,用于发送实时数据广播 */
        private readonly IHubContext<BroadcastHub> _broadcast;
        /* 本次试验的产品数据及试样数据缓存 */
        private readonly IDbContextFactory<GB20285DBContext> _dbFactory;
        public TestServerContainer(IDbContextFactory<GB20285DBContext> context, IHubContext<BroadcastHub> broadcast)
        {
            _broadcast = broadcast;
            _dbFactory = context;
            Servers = new Dictionary<int, TestServer>()
            {
                { 0, new TestServer(0, _dbFactory, _broadcast) }
            };
        }
    }
}