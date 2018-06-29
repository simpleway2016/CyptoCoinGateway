using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cailutong.CyptoCoinGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //run background thread , check transactions
            Task.Run(() => BackgroundTask.BTCTransactionTask.Run());

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls($"http://*:{Config.Instance.Listen}")
                .UseStartup<Startup>()
                .Build();
    }
}
