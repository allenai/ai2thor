using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Barmetler
{
	public static class StringUtility
	{
		/// <summary>
		/// Convert a name to initials.
		/// <para>Name can be:</para>
		/// <list type="bullet">
		/// <item>multiple words</item>
		/// <item>PascalCase</item>
		/// <item>under_scores</item>
		/// <item>hyphens-between-words</item>
		/// </list>
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string GetInitials(string str)
		{
			if (str == null) return null;
			MatchCollection matches;

			if (Regex.IsMatch(str, @"^([A-Z][a-z0-9_]*)+$"))
				matches = Regex.Matches(str, @"([A-Z][^A-Z]*)");
			else
				matches = Regex.Matches(str, @"([A-Za-z][^ \-_]*)");

			return string.Join(
				"",
				from g
				in matches.Cast<Match>()
				where g.Value.Length > 0
				select (g.Value.ToCharArray()[0] + "").ToUpper()
			);
		}
	}
}
