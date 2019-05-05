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

        public static string Base64Representation(this string str)
        {
            return Convert.ToBase64String(Encoding.Default.GetBytes(str));
        }

        //thanks to yck1509
        public static OpCode InverseBranch(this OpCode opCode)
        {
            switch (opCode.Code)
            {
                case Code.Bge:
                    return OpCodes.Blt;
                case Code.Bge_Un:
                    return OpCodes.Blt_Un;
                case Code.Blt:
                    return OpCodes.Bge;
                case Code.Blt_Un:
                    return OpCodes.Bge_Un;
                case Code.Bgt:
                    return OpCodes.Ble;
                case Code.Bgt_Un:
                    return OpCodes.Ble_Un;
                case Code.Ble:
                    return OpCodes.Bgt;
                case Code.Ble_Un:
                    return OpCodes.Bgt_Un;
                case Code.Brfalse:
                    return OpCodes.Brtrue;
                case Code.Brtrue:
                    return OpCodes.Brfalse;
                case Code.Beq:
                    return OpCodes.Bne_Un;
                case Code.Bne_Un:
                    return OpCodes.Beq;
            }
            throw new NotSupportedException();
        }


        public static List<IMemberDef> InjectInto(this TypeDef type, TypeDef target, bool rename)
        {
            List<IMemberDef> defs = new List<IMemberDef>();
            foreach (MethodDef method in type.Methods)
            {
                method.DeclaringType = null;
                target.Methods.Add(method);
                defs.Add(method);
            }

            foreach (FieldDef field in type.Fields)
            {
                field.DeclaringType = null;
                target.Fields.Add(field);
                defs.Add(field);
            }
            return defs;
        }
    }
}
