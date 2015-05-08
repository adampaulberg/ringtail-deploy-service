using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{
    public class Prioritizer
    {
        public IEnumerable<string> OrderCommands(IEnumerable<string> commands)
        {
            List<string> result = new List<string>();

            foreach (var command in commands)
            {
                if (command.Contains("Workers"))                
                    result.Insert(0, command);                
                else                
                    result.Add(command);                
            }
            return result;
        }
    }
}
