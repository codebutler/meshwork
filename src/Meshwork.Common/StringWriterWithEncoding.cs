//
// StringWriterWithEncoding.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System.IO;
using System.Text;

namespace Meshwork.Common
{
	public class StringWriterWithEncoding : StringWriter
	{
		private Encoding m_encoding;

		public StringWriterWithEncoding(StringBuilder sb, Encoding encoding) : base (sb)
		{
			m_encoding = encoding;
		}

		public override Encoding Encoding {
			get {
				return m_encoding;
			}
		}
	}
}
