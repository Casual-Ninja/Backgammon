using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe_MCTS_NEAT
{
    public class LockingClass<T>
    {
        public T variable;

        public LockingClass(T var)
        {
            this.variable = var;
        }
    }
}
