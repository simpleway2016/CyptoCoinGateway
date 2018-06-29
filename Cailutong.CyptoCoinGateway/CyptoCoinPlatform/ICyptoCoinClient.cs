using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway.CyptoCoinPlatform
{
    interface ICyptoCoinClient
    {
        /// <summary>
        /// 创建加密货币平台使用的密钥
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        void CreatePlatformKey(out byte[] key1, out byte[] key2);
        /// <summary>
        /// 获取未使用的收款地址
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        string GetUnusedAddress(byte[] key1, byte[] key2);
    }
}
