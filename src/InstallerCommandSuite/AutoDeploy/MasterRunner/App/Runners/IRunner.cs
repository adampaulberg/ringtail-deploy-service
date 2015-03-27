using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App.Runners
{
    public interface IRunner
    {
        int RunFile();
    }

    public class NoOp : IRunner
    {
        public int RunFile()
        {
            return 0;
        }
    }
}
