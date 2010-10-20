using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Palaso.BuildTasks.MakePot
{
	public class MakePot: Task
	{
		readonly Dictionary<string, List<string>> _entries = new Dictionary<string, List<string>>();

		public ITaskItem[] CSharpFiles { get; set; }

		public ITaskItem[] XmlFiles { get; set; }

		[Required]
		public string ProjectId { get; set; }

		public string MsdIdBugsTo { get; set; }

		[Required]
		public string OutputFile { get; set; }

		public string XpathToStrings { get; set; }

		private readonly Regex _pattern;

		public MakePot()
		{
			_pattern = new Regex(@"(Text\s*=\s*""(~)?|StringCatalog\.Get(Formatted)?\(""(~)?|""~)(?<key>([^""\\]|\\.)*)""(\s*,\s*""(?<note>([^""\\]|\\.)*)"")?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		}

		public override bool Execute()
		{
			using (StreamWriter writer = File.CreateText(OutputFile))
			{
				writer.NewLine = "\n";
				if (XmlFiles != null)
				{
					foreach (ITaskItem file in XmlFiles)
					{
						ProcessXmlFile(file);
					}
				}
				if (CSharpFiles != null)
				{
					foreach (ITaskItem file in CSharpFiles)
					{
						ProcessSrcFile(file.ItemSpec);
					}
				}

				WritePotFile(writer);

				LogMessage(MessageImportance.High, "MakePot wrote " + _entries.Count + " strings to " + OutputFile);
			}
			return true;
		}

		internal void WritePotFile(TextWriter writer)
		{
			WritePotHeader(writer);

			foreach (KeyValuePair<string, List<string>> pair in _entries)
			{
				WriteEntry(pair.Key, pair.Value, writer);
			}
		}

		private void WritePotHeader(TextWriter writer)
		{
			/* Note:
			 * The header output by POEdit on Windows looks like the following.  However, POEdit can't
			 * read this in as a pot, though it could as a po.  Therefore we prefer to write out our
			 * info as a comment which at least is preserved by POEdit.  This allows the pot file to
			 * be merged in to any already existing po file.
			 */
			//writer.WriteLine(@"msgid """"");
			//writer.WriteLine(@"msgstr """"");
			//writer.WriteLine(@"""Project-Id-Version: " + ProjectId + @"\n""");
			//writer.WriteLine(@"""POT-Creation-Date: " + DateTime.UtcNow.ToString("s") + @"\n""");
			//writer.WriteLine(@"""PO-Revision-Date: \n""");
			//writer.WriteLine(@"""MIME-Version: 1.0\n""");
			//writer.WriteLine(@"""Content-Type: text/plain; charset=UTF-8\n""");
			//writer.WriteLine(@"""Content-Transfer-Encoding: 8bit\n""");

			/* As noted above the commented version below isn't read by POEdit, however it is preserved */
			writer.WriteLine("# Project-Id-Version: {0}", ProjectId);
			writer.WriteLine("# Report-Msgid-Bugs-To: {0}", MsdIdBugsTo);
			writer.WriteLine("# POT-Creation-Date: {0}", DateTime.UtcNow.ToString("s"));
			writer.WriteLine("# Content-Type: text/plain; charset=UTF-8");
			writer.WriteLine();

			//writer.WriteLine("# PO-Revision-Date: ");
			//writer.WriteLine("# Last-Translator: ");
			//writer.WriteLine("# Language-Team: ");
			//writer.WriteLine("# MIME-Version: 1.0");
			//writer.WriteLine("# Content-Transfer-Encoding: 8bit");
		}

		private void ProcessXmlFile(ITaskItem  fileSpec)
		{
			if (string.IsNullOrEmpty(XpathToStrings))
			{
				LogError("You must define XPathToStrings if you include anything in XPathFiles");
				return;
			}
			LogMessage("Processing {0}", fileSpec.ItemSpec);
			var doc = new XmlDocument();
			doc.Load(fileSpec.ItemSpec);
			foreach (XmlNode node in doc.SelectNodes(XpathToStrings))
			{
				AddStringInstance(node.InnerText, String.Empty);
			}
		}

		private void LogMessage(string message, params object[] args)
		{
			LogMessage(MessageImportance.Normal, message, args);
		}

		private void LogMessage(MessageImportance importance, string message, params object[] args)
		{
			try
			{
				Log.LogMessage(importance, message, args);
			}
			catch (InvalidOperationException)
			{
				// Swallow exceptions for testing
			}
		}

		private void LogError(string message, params object[] args)
		{
			try
			{
				Log.LogError(message, args);
			}
			catch (InvalidOperationException)
			{
				// Swallow exceptions for testing
			}
		}

		private void AddStringInstance(string stringToTranslate, string commentsForTranslator)
		{
			if (!_entries.ContainsKey(stringToTranslate)) //first time we've encountered this string?
			{
				LogMessage(MessageImportance.Low, "Found '{0}'", stringToTranslate);
				_entries.Add(stringToTranslate, new List<string>());
			}
			_entries[stringToTranslate].Add(commentsForTranslator);//add this reference
		}

		internal void ProcessSrcFile(string filePath)
		{
			LogMessage("Processing {0}", filePath);
			string contents = File.ReadAllText(filePath);

			foreach (Match match in MatchesInCSharpString(contents))
			{
				string str = UnescapeString(match.Groups["key"].Value);
				if (!_entries.ContainsKey(str)) //first time we've encountered this string?
				{
					this.LogMessage(MessageImportance.Low, "Found '{0}'", str);
					_entries.Add(str, new List<string>());
				}
				string comments = "#: " + filePath;

				//catch the second parameter from calls like this:
				//            StringCataGet("~Note", "The label for the field showing a note.");

				if (!String.IsNullOrEmpty(match.Groups["note"].Value))
				{
					string comment = match.Groups["note"].Value;
					this.LogMessage(MessageImportance.Low, "  with comment '{0}'", comment);
					comments += System.Environment.NewLine + "#. " + comment;
				}
				_entries[str].Add(comments);//add this reference
			}
		}

		internal MatchCollection MatchesInCSharpString(string contents)
		{
			return _pattern.Matches(contents);
		}

		private static void WriteEntry(string key, IEnumerable<string> comments, TextWriter writer)
		{
			writer.WriteLine("");
			foreach (var s in comments)
			{
				writer.WriteLine(s);
			}
			key = EscapeString(key);
			writer.WriteLine("msgid \"" + key + "\"");
			writer.WriteLine("msgstr \"\"");
		}

		public static string EscapeString(string s)
		{
			string result = s.Replace("\\", "\\\\"); // This must be first
			result = result.Replace("\"", "\\\"");
			return result;
		}

		public static string UnescapeString(string s)
		{
			string result = s.Replace("\\'", "'");
			result = result.Replace("\\\"", "\"");
			result = result.Replace("\\\\", "\\");
			return result;
		}
	}
}