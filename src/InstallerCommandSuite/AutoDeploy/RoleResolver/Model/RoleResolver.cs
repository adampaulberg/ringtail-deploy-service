using RoleResolverUtility.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoleResolverUtility
{
    public class RoleResolver
    {
        public Logger Logger { get; set; }
        private Options options;


        public RoleResolver(Options opts, Logger logger)
        {
            this.options = opts;
            Logger = logger;
        }

        public static List<string> FilterCommandsByRoles(ValuesForRole roles, List<CommandBlock> commandsByHeading)
        {
            var filteredCommands = new List<string>();

            foreach(var x in commandsByHeading)
            {
                bool include = x.CommandHeader == "|ALWAYS";


                foreach (var value in roles)
                {
                    if (x.CommandHeader == "|" + value)
                    {

                        include = true;
                    }
                }

                if (include)
                {
                    filteredCommands.AddRange(x.Commands);
                }
            }

            return filteredCommands;
        }

        public List<string> FilterMasterCommandsByRole(List<string> allCommands, List<string> allRoles)
        {
            var commands = CommandBlock.BuildCommandBlockList(allCommands);
            var roleFile = RoleFileReader.GetRoleFile(options.Role);
            var values = ValuesForRole.BuildValuesForRole(options.Role, allRoles);
            var result = RoleResolver.FilterCommandsByRoles(values, commands);

            Logger.AddToLog("Found the following values for role " + options.Role);
            Logger.AddToLog(string.Join("\r\n", values));

            return result;
        }
    }

    public class CommandBlock
    {
        public string CommandHeader { get; private set; }
        public List<string> Commands { get; private set; }

        public CommandBlock(string commandHeader)
        {
            this.CommandHeader = commandHeader;
            this.Commands = new List<string>();
        }

        public void AddCommand(string command)
        {
            this.Commands.Add(command);
        }


        public static List<CommandBlock> BuildCommandBlockList(IEnumerable<string> masterCommands)
        {
            var commandsByHeading = new List<CommandBlock>();

            foreach (var command in masterCommands)
            {
                if (command.StartsWith("|"))
                {
                    commandsByHeading.Add(new CommandBlock(command));
                }
                else
                {
                    commandsByHeading[commandsByHeading.Count - 1].AddCommand(command);
                }
            }

            return commandsByHeading;
        }
    }
}
