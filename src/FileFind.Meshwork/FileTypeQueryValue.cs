
using System;
using Mono.Unix;
using Hyena.Query;

namespace FileFind.Meshwork
{
	public class FileTypeQueryValue : ExactStringQueryValue
	{
		// Don't do substring comparisons on type field since it's currently only 'D' or 'F',
		// but make the ":", "!:" and "=" operators still work.
		public static readonly Operator Equal    = new Operator ("equals", Catalog.GetString ("is"), "= '{0}'", ":", "=");
		public static readonly Operator NotEqual = new Operator ("notEqual", Catalog.GetString ("is not"), "!= '{0}'", true, "!:");
		
		public override AliasedObjectSet<Operator> OperatorSet {
			get {
				return new AliasedObjectSet<Operator>(Equal, NotEqual, StringQueryValue.Equal, StringQueryValue.NotEqual);
			}
		}

		public override void ParseUserQuery (string input)
		{			
			if (input == "directory" || input == "dir" || input == "d" || input == "folder") {
				input = "D";
			} else {
				input = "F";
			}
			base.ParseUserQuery(input);
		}
		
        public override string ToSql (Operator op)
        {
            return String.IsNullOrEmpty (value) ? null : EscapeString (op, value);
        }
	}
}
