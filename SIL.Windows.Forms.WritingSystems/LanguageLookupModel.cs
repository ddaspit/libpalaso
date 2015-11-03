﻿using System;
using System.Collections.Generic;
using System.Linq;
using L10NSharp;
using SIL.Extensions;
using SIL.WritingSystems;

namespace SIL.Windows.Forms.WritingSystems
{
	public class LanguageLookupModel
	{
		private static readonly string UnlistedLanguageName = LocalizationManager.GetString("LanguageLookup.UnlistedLanguage", "Unlisted Language");

		private LanguageLookup _languageLookup;
		private string _searchText;
		private LanguageInfo _selectedLanguage;
		private string _desiredLanguageName;

		/// <summary>
		/// Return a full language subtag given its official iso639 code. Note that this means you must use the 2-letter code
		/// where it exists; 'ara' and 'eng' will not match.
		/// </summary>
		/// <remarks>
		/// This method may be obsolete...originally for Bloom but now so trivial Bloom just uses StandardSubtags.RegisteredLanguages.
		/// Leaving it here in case some other client has started using it.
		/// </remarks>
		/// <param name="iso639Code"></param>
		/// <returns></returns>
		public LanguageSubtag GetExactLanguageMatch(string iso639Code)
		{
			LanguageSubtag language;
			if (StandardSubtags.RegisteredLanguages.TryGet(iso639Code.ToLowerInvariant(), out language))
				return language;
			return null;
		}

		public Func<LanguageInfo, bool> MatchingLanguageFilter { get; set; }

		public string SearchText
		{
			get { return _searchText; }
			set { _searchText = value.Trim(); }
		}

		public bool HaveSufficientInformation
		{
			get
			{
				return _desiredLanguageName != null && SelectedLanguage != null && 
					_desiredLanguageName != UnlistedLanguageName && _desiredLanguageName.Length > 0;
			}
		}

		public string DesiredLanguageName
		{
			get
			{
				return _desiredLanguageName ?? string.Empty;
			}
			set { _desiredLanguageName = value == null ? null : value.Trim(); }
		}

		public void LoadLanguages()
		{
			_languageLookup = new LanguageLookup();
		}

		public bool AreLanguagesLoaded
		{
			get { return _languageLookup != null; }
		}

		public IEnumerable<LanguageInfo> MatchingLanguages
		{
			get
			{
				if (_searchText == "?")
				{
					yield return new LanguageInfo {LanguageTag = "qaa", Names = {UnlistedLanguageName}};
					yield break;
				}

				foreach (LanguageInfo li in _languageLookup.SuggestLanguages(_searchText).Where(li => MatchingLanguageFilter == null || MatchingLanguageFilter(li)))
					yield return li;
			}
		}

		public LanguageInfo SelectedLanguage
		{
			get { return _selectedLanguage; }
			set
			{
				_selectedLanguage = value;
				if (_selectedLanguage == null)
					return;

				if (LanguageTag == "qaa")
				{
					if (_searchText != "?")
					{
						string failedSearchText = _searchText.ToUpperFirstLetter();
						_desiredLanguageName = failedSearchText;
						_selectedLanguage.Names.Insert(0, failedSearchText);
					}
				}
				else
				{
					IList<string> names = _selectedLanguage.Names;
					if (names.Count == 0)
					{
						_desiredLanguageName = _selectedLanguage.LanguageTag; // best we can do
					}
					else
					{
						//now if they were typing another form, well then that form makes a better default "Desired Name" than the official primary name
						_desiredLanguageName = names.FirstOrDefault(n => n.StartsWith(_searchText, StringComparison.InvariantCultureIgnoreCase)) ?? names[0];
					}
				}
			}
		}

		public string LanguageTag
		{
			get
			{
				if (_selectedLanguage == null)
					return string.Empty;
				return _selectedLanguage.LanguageTag;
			}
		}
	}
}