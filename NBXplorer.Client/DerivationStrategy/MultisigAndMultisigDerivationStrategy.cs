﻿using NBitcoin;
using System.Linq;
using NBXplorer.DerivationStrategy;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using NBitcoin.Crypto;
using System.Threading.Tasks;

namespace NBXplorer.DerivationStrategy
{
	public class MultisigAndMultisigDerivationStrategy : DerivationStrategyBase
	{
		internal MultisigDerivationStrategy Multisig1
		{
			get; set;
		}
		internal MultisigDerivationStrategy Multisig2
		{
			get; set;
		}
		public bool LexicographicOrder
		{
			get; set;
		}

		protected override string StringValue
		{
			get
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(Multisig1.ToString() + "-and-" + Multisig2.ToString());
				if(IsLegacy)
				{
					builder.Append("-[legacy]");
				}
				if(!LexicographicOrder)
				{
					builder.Append("-[keeporder]");
				}
				return builder.ToString();
			}
		}

		internal MultisigAndMultisigDerivationStrategy(MultisigDerivationStrategy multisig1, MultisigDerivationStrategy multisig2, bool isLegacy)
		{
			Multisig1 = Clone(multisig1);
			Multisig2 = Clone(multisig2);
			LexicographicOrder = true;
			IsLegacy = isLegacy;
		}

		static MultisigDerivationStrategy Clone(MultisigDerivationStrategy multisig)
		{
			return new MultisigDerivationStrategy(multisig.RequiredSignatures, multisig.Keys, false) { LexicographicOrder = true };
		}

		public bool IsLegacy
		{
			get; private set;
		}

		public override Derivation GetDerivation()
		{
			var pubKeys1 = new PubKey[this.Multisig1.Keys.Length];
			Parallel.For(0, pubKeys1.Length, i =>
			{
				pubKeys1[i] = this.Multisig1.Keys[i].ExtPubKey.PubKey;
			});
			if (LexicographicOrder)
			{
				Array.Sort(pubKeys1, MultisigDerivationStrategy.LexicographicComparer);
			}

			var pubKeys2 = new PubKey[this.Multisig2.Keys.Length];
			Parallel.For(0, pubKeys2.Length, i =>
			{
				pubKeys2[i] = this.Multisig2.Keys[i].ExtPubKey.PubKey;
			});
			if (LexicographicOrder)
			{
				Array.Sort(pubKeys2, MultisigDerivationStrategy.LexicographicComparer);
			}
			List<Op> ops = new List<Op>();
			ops.Add(Op.GetPushOp(Multisig1.RequiredSignatures));
			foreach(var keys in pubKeys1)
			{
				ops.Add(Op.GetPushOp(keys.ToBytes()));
			}
			ops.Add(Op.GetPushOp(pubKeys1.Length));
			ops.Add(OpcodeType.OP_CHECKMULTISIGVERIFY);
			ops.Add(Op.GetPushOp(Multisig2.RequiredSignatures));
			foreach(var keys in pubKeys2)
			{
				ops.Add(Op.GetPushOp(keys.ToBytes()));
			}
			ops.Add(Op.GetPushOp(pubKeys2.Length));
			ops.Add(OpcodeType.OP_CHECKMULTISIG);

			return new Derivation() { ScriptPubKey = new Script(ops.ToList()) };
		}

		public override IEnumerable<ExtPubKey> GetExtPubKeys()
		{
			return Multisig1.GetExtPubKeys().Concat(Multisig2.GetExtPubKeys());
		}

		public override DerivationStrategyBase GetChild(KeyPath keyPath)
		{
			return new MultisigAndMultisigDerivationStrategy((MultisigDerivationStrategy)Multisig1.GetChild(keyPath), (MultisigDerivationStrategy)Multisig2.GetChild(keyPath), IsLegacy)
			{
				LexicographicOrder = LexicographicOrder
			};
		}
	}
}