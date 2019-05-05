using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck.ControlFlow
{
    public class Block
    {
        public Block(List<Instruction> instrs, int number)
        {
            Instructions = instrs;
            Number = number;
            Condition = Instructions.Last();
            FirstInstruction = Instructions.First();
        }

        public List<Instruction> Instructions { get; set; }

        public Instruction Condition { get; set; }

        public Instruction FirstInstruction { get; set; }

        public int Number { get; set; }
    }
}
