using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway
{
    public class MainDB : DBModels.DB.CyptoCoinPayDB
    {
        public MainDB() : base(Config.Instance.ConnectString, Config.Instance.DatabaseType)
        {

        }
    }
}
