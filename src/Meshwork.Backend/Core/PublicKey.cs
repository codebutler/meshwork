//
// PublicKey.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Meshwork.Library.CRC;

namespace Meshwork.Backend.Core
{
	public class PublicKey
	{
		public const string BEGIN_LINE = "-----BEGIN MESHWORK PUBLIC KEY BLOCK-----";
		public const string END_LINE = "-----END MESHWORK PUBLIC KEY BLOCK-----";

		static CRC s_CRC24 = new CRC(CRCParameters.GetParameters(CRCStandard.CRC24));

		public static PublicKey Parse (string armoredText)
		{
			var headers = new Dictionary<string, string>();
			string line = null;
			string crc = null;
			var data = new StringBuilder();
			var state = ParseState.Start;
			using (var reader = new StringReader(armoredText)) {
				while ((line = reader.ReadLine()) != null) {
					line = line.Trim();
					switch (state) {
					case ParseState.Start:
						if (line != BEGIN_LINE)
							goto done;
						state = ParseState.Header;
						break;
					case ParseState.Header:
						if (line == string.Empty)
							state = ParseState.Body;
						else {
							var i = line.IndexOf(": ");
							if (i <= 0)
								goto done;
							var name = line.Substring(0, i).Trim();
							var val = line.Substring(i + 2).Trim();
							headers.Add(name, val);
						}
						break;
					case ParseState.Body:
						var match = Regex.Match(line, "^=(....)$");
						if (match.Success) {
							crc = match.Groups[1].Captures[0].Value;
							state = ParseState.End;
						} else if (line == END_LINE) {
							state = ParseState.End;
						} else {
							data.Append(line);
						}
						break;
					case ParseState.End:
						goto done;
					}
				}
			}
			done:
			
			
			if (state != ParseState.End)
				throw new Exception($"Malformed/missing {Enum.GetName(typeof(ParseState), state).ToLower()}");
			
			if (string.IsNullOrEmpty(crc))
				throw new Exception("Missing checksum");
			
			byte[] dataBytes = null;
			string dataString = null;
			try {
				dataBytes = Convert.FromBase64String(data.ToString());
				dataString = Encoding.UTF8.GetString(dataBytes);
				dataBytes = Encoding.UTF8.GetBytes(dataString);
			} catch (Exception) {
				throw new Exception("Invalid key data");
			}
			
			byte[] expectedHash = null;
			byte[] actualHash = null;
			try {
				expectedHash = Convert.FromBase64String(crc);
				actualHash = s_CRC24.ComputeHash(dataBytes);
			} catch (Exception) {
				throw new Exception("Invalid checksum");
			}

		    if (!expectedHash.SequenceEqual(actualHash))
		    {
				throw new Exception("Checksum does not match");
		    }

		    return new PublicKey(headers["Nickname"], dataString);
		}

		public PublicKey (string nickName, string key)
		{
			Nickname = nickName;
			Key = key;
		}

		public string Nickname { get; set; }

		public string Key { get; set; }

	    public string Fingerprint => Common.Utils.SHA512Str(Key);

	    public string ToArmoredString ()
		{
			var keyBytes = Encoding.UTF8.GetBytes(Key);
			var builder = new StringBuilder();
			builder.AppendLine(BEGIN_LINE);
			builder.AppendLine($"Nickname: {Nickname}");
			builder.AppendLine();
			builder.AppendLine(Common.Utils.AddLineBreaks(Convert.ToBase64String(keyBytes)));
			builder.Append("=");
			builder.AppendLine(Convert.ToBase64String(s_CRC24.ComputeHash(keyBytes)));
			builder.AppendLine(END_LINE);
			return builder.ToString();
		}

	    private enum ParseState
		{
			Start,
			Header,
			Body,
			End
		}
	}
}
