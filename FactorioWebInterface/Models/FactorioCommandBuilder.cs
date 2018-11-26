using System.Text;

namespace FactorioWebInterface.Models
{
    public class FactorioCommandBuilder
    {
        private StringBuilder sb = new StringBuilder();
        private bool appendBracket;

        private FactorioCommandBuilder()
        {
        }

        public static FactorioCommandBuilder ServerCommand(string functionName)
        {
            var cb = new FactorioCommandBuilder();
            var sb = cb.sb;

            sb.Append("/silent-command local s = ServerCommands s = s and s.");
            sb.Append(functionName);
            sb.Append('(');

            cb.appendBracket = true;

            return cb;
        }

        public static FactorioCommandBuilder SilentCommand()
        {
            var cb = new FactorioCommandBuilder();
            var sb = cb.sb;

            sb.Append("/silent-command ");

            return cb;
        }

        public FactorioCommandBuilder Add(string s)
        {
            sb.Append(s);
            return this;
        }

        public FactorioCommandBuilder AddQuotedString(string s)
        {
            sb.Append('\'');
            sb.Append(s);
            sb.Append('\'');

            return this;
        }

        public FactorioCommandBuilder AddDoubleQuotedString(string s)
        {
            sb.Append('"');
            sb.Append(s);
            sb.Append('"');

            return this;
        }

        public FactorioCommandBuilder RemoveLast(int characters)
        {
            int start = sb.Length - characters;
            sb.Remove(start, characters);

            return this;
        }

        public string Build()
        {
            if (appendBracket)
            {
                sb.Append(')');
            }

            return sb.ToString();
        }

    }
}
