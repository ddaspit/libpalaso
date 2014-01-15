﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Palaso.Data;
using Palaso.DictionaryServices.Model;
using Palaso.WritingSystems;

namespace Palaso.DictionaryServices.Queries
{
	public class HeadwordQuery:IQuery<LexEntry>
	{
		private WritingSystemDefinition _writingSystemDefinition;
		private IComparer _comparer;

		public HeadwordQuery(Comparer<string> guidComparer, WritingSystemDefinition writingSystemDefinition)
		{
			_writingSystemDefinition = writingSystemDefinition;
			_comparer = guidComparer;
		}

		public HeadwordQuery(WritingSystemDefinition writingSystemDefinition)
		{
			_writingSystemDefinition = writingSystemDefinition;
			_comparer = writingSystemDefinition.Collator;
		}

		public override IEnumerable<IDictionary<string, object>> GetResults(LexEntry entryToQuery)
		{
			IDictionary<string, object> tokenFieldsAndValues = new Dictionary<string, object>();
			string headWord = entryToQuery.VirtualHeadWord[_writingSystemDefinition.Id];
			if (String.IsNullOrEmpty(headWord))
			{
				headWord = null;
			}
			tokenFieldsAndValues.Add("Form", headWord);
			return new[] { tokenFieldsAndValues };
		}

		public override IEnumerable<SortDefinition> SortDefinitions
		{
			get
			{
				var sortOrder = new SortDefinition[1];
				sortOrder[0] = new SortDefinition("Form", _comparer);
				return sortOrder;
			}
		}

		public override string UniqueLabel
		{
			get { return "HeadwordQuery" + _writingSystemDefinition.Id; }
		}

		public override bool IsUnpopulated(IDictionary<string, object> entryToCheckAgainst)
		{
			throw new NotImplementedException();
		}
	}
}