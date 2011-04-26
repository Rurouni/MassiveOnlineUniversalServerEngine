 
using MOUSE.Core;
using System.IO;
using System.Collections.Generic;
using RakNetWrapper;
namespace Protocol
{
	
	public static class AccountDataSerializer	
	{
		public static void Serialize(AccountData x, OutPacket w)
		{
			w.WriteInt64(x.Id);
			w.WriteString(x.Name);
			w.WriteInt32(12);
			foreach(var element in x.Characters)
				CharacterDataSerializer.Serialize(element, w);
		}
		
		public static AccountData Deserialize(InPacket r)
		{
			var ret = new AccountData();
			ret.Id = r.ReadInt64();
			ret.Name = r.ReadString();
			{
				int lenght = r.ReadInt32();
				var list = new List< Protocol.CharacterData >(lenght);
				for(int i = 0; i < lenght; i++)
				{
					var x = CharacterDataSerializer.Deserialize(r);
					list.Add(x);
				}
				ret.Characters = list;
			}
			return ret;
		}
	}
	
	public static class CharacterDataSerializer	
	{
		public static void Serialize(CharacterData x, OutPacket w)
		{
			w.WriteInt64(x.Id);
			w.WriteString(x.Name);
		}
		
		public static CharacterData Deserialize(InPacket r)
		{
			var ret = new CharacterData();
			ret.Id = r.ReadInt64();
			ret.Name = r.ReadString();
			return ret;
		}
	}
}


