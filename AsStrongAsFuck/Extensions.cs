using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    public static class Extensions
    {
        public static void AddListEntry<TKey, TValue>(this IDictionary<TKey, List<TValue>> self, TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            List<TValue> list;
            if (!self.TryGetValue(key, out list))
                list = self[key] = new List<TValue>();
            list.Add(value);
        }
    }
}
