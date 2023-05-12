
using System.Text.RegularExpressions;

namespace CIB.Core.Utils
{
    public class ReqEx
    {
        public readonly Regex AlphabetOnly = new("^[a-zA-Z]*$");
        public readonly Regex NumberOnly = new("^[0-9 +]*$");
        public readonly Regex AlphaNumeric = new("^[a-zA-Z0-9 .&-_]*$");
    }
}