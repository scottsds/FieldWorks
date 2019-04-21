﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcPatternSda : DomainDataByFlidDecoratorBase
	{
		public const int ktagChildren = -1000;

		private readonly ComplexConcPatternNode m_root;
		private readonly Dictionary<int, ComplexConcPatternNode> m_nodes;

		public ComplexConcPatternSda(ISilDataAccessManaged domainDataByFlid, ComplexConcPatternNode root)
			: base(domainDataByFlid)
		{
			SetOverrideMdc(new ComplexConcordancePatternMdc((IFwMetaDataCacheManaged) domainDataByFlid.MetaDataCache));
			m_nodes = new Dictionary<int, ComplexConcPatternNode>();
			m_root = root;
			m_root.Sda = this;
		}

		public ComplexConcPatternNode Root
		{
			get { return m_root; }
		}

		public IDictionary<int, ComplexConcPatternNode> Nodes
		{
			get { return m_nodes; }
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			switch (tag)
			{
				case ktagChildren:
					return m_nodes[hvo].Children[index].Hvo;

				default:
					return base.get_VecItem(hvo, tag, index);
			}
		}

		public override int get_VecSize(int hvo, int tag)
		{
			switch (tag)
			{
				case ktagChildren:
					ComplexConcPatternNode node = m_nodes[hvo];
					if (node.IsLeaf)
						return 0;
					return node.Children.Count;

				default:
					return base.get_VecSize(hvo, tag);
			}
		}

		public override int[] VecProp(int hvo, int tag)
		{
			switch (tag)
			{
				case ktagChildren:
					ComplexConcPatternNode node = m_nodes[hvo];
					if (node.IsLeaf)
						return new int[0];
					return node.Children.Select(n => n.Hvo).ToArray();

				default:
					return base.VecProp(hvo, tag);
			}
		}

		public override bool get_IsValidObject(int hvo)
		{
			if (m_nodes.ContainsKey(hvo))
				return true;
			return base.get_IsValidObject(hvo);
		}

		private class ComplexConcordancePatternMdc : LcmMetaDataCacheDecoratorBase
		{
			public ComplexConcordancePatternMdc(IFwMetaDataCacheManaged metaDataCache)
				: base(metaDataCache)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			public override int GetFieldType(int luFlid)
			{
				switch (luFlid)
				{
					case ktagChildren:
						return (int) CellarPropertyType.OwningSequence;

					default:
						return base.GetFieldType(luFlid);
				}
			}
		}
	}
}
