using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cailutong.CyptoCoinGateway.Models
{
    public class NullableSortedDict <TKey,TValue>: SortedDictionary<TKey, TValue>
    {
        public new TValue this[TKey key]
        {
            get
            {
                if (base.ContainsKey(key) == false)
                    return default(TValue);
                return base[key];
            }
            set
            {
                base[key] = value;
            }
        }

        public T GetValue<T>(TKey key)
        {
            if (base.ContainsKey(key) == false || base[key] == null)
                return default(T);
            var type = typeof(T);
            if(type.IsGenericType)
            {
                return (T)Convert.ChangeType(base[key], type.GetGenericArguments()[0]);                
            }
            return (T)Convert.ChangeType( base[key] , type);
        }
    }
}
