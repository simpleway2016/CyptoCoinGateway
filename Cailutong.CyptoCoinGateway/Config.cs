using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway
{
    public class Config
    {
        public string ConnectString;
        public Way.EntityDB.DatabaseType DatabaseType;
        public static Config Instance;
        public string Listen;

        public NBXplorerSetting NBXplorer;
        static Config()
        {
            var filepath = $"{AppDomain.CurrentDomain.BaseDirectory}config.json";
            var content = System.IO.File.ReadAllText(filepath, System.Text.Encoding.UTF8);
            Instance = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(content);

            Instance.ConnectString = Instance.ConnectString.Replace("./", $"{AppDomain.CurrentDomain.BaseDirectory}");
        }
        private Config()
        {

        }
    }

    public class NBXplorerSetting
    {
        public string ServerUrl;
    }
}
