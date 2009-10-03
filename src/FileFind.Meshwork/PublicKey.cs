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
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
using Classless.Hasher;

namespace FileFind.Meshwork
{
	public class PublicKey
	{
		public const string BEGIN_LINE = "-----BEGIN MESHWORK PUBLIC KEY BLOCK-----";
		public const string END_LINE = "-----END MESHWORK PUBLIC KEY BLOCK-----";

		static CRC s_CRC24 = new CRC(CRCParameters.GetParameters(CRCStandard.CRC24));

		public static PublicKey Parse (string armoredText)
		{
			PublicKey result = new PublicKey();
			var headers = new Dictionary<string, string>();
			string line = null;
			string crc = null;
			StringBuilder data = new StringBuilder();
			ParseState state = ParseState.Start;
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
						if (line == String.Empty)
							state = ParseState.Body;
						else {
							int i = line.IndexOf(": ");
							if (i <= 0)
								goto done;
							string name = line.Substring(0, i).Trim();
							string val = line.Substring(i + 2).Trim();
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
				throw new Exception(String.Format("Malformed/missing {0}", Enum.GetName(typeof(ParseState), state).ToLower()));
			
			if (String.IsNullOrEmpty(crc))
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
				throw new Exception("Checksum does not match");
			
			if (headers.ContainsKey("Nickname"))
				result.Nickname = headers["Nickname"];
			
			result.Key = dataString;
			
			return result;
		}

		public PublicKey ()
		{
			this.Nickname = "Unknown";
			this.Key = "Unknown";
		}

		public PublicKey (string key)
		{
			this.Nickname = "Unknown";
			this.Key = key;
		}

		public PublicKey (string nickName, string key)
		{
			this.Nickname = nickName;
			this.Key = key;
		}

		public string Nickname { get; set; }

		public string Key { get; set; }

		public string ToArmoredString ()
		{
			byte[] keyBytes = Encoding.UTF8.GetBytes(this.Key);
			var builder = new StringBuilder();
			builder.AppendLine(BEGIN_LINE);
			builder.AppendLine(String.Format("Nickname: {0}", this.Nickname));
			builder.AppendLine();
			builder.AppendLine(FileFind.Common.AddLineBreaks(Convert.ToBase64String(keyBytes)));
			builder.Append("=");
			builder.AppendLine(Convert.ToBase64String(s_CRC24.ComputeHash(keyBytes)));
			builder.AppendLine(END_LINE);
			return builder.ToString();
		}

		enum ParseState
		{
			Start,
			Header,
			Body,
			End
		}
	}
}
