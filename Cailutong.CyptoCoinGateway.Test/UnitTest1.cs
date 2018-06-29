using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBXplorer;

namespace Cailutong.CyptoCoinGateway.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var network = new NBXplorerNetworkProvider(ChainType.Main).GetBTC();
            ExplorerClient client = new ExplorerClient(network , new System.Uri("http://47.75.159.73:24444") );


            //获取一个没有使用的收款地址
            var info = client.GetFeeRate(2);
        }
    }
}
