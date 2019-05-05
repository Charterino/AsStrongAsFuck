using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.ControlFlow
{
    public class BlockParser
    {
        public static List<Block> ParseMethod(MethodDef method)
        {
            List<Block> blocks = new List<Block>();
            List<Instruction> body = new List<Instruction>(method.Body.Instructions);

            //splitting into blocks
            while (body.Count > 0)
            {
                for (int i = 1; i < body.Count; i++)
                {
                    var instr = body[i - 1];
                    if (instr.IsConditionalBranch())
                    {
                        blocks.Add(new Block(body.GetRange(0, i + 1), blocks.Count));
                        body.RemoveRange(0, i + 1);
                        break;
                    }
                    if (i == body.Count - 1)
                    {
                        blocks.Add(new Block(body.GetRange(0, i + 1), blocks.Count));
                        body.RemoveRange(0, i + 1);
                    }
                }
            }
            return blocks;
        }
    }
}
