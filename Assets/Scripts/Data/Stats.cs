using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
	[Serializable]
	public struct Stats
	{
		[SerializeField] private int _maxHp, _str, _mag, _dex, _spd, _def, _res, _lck, _con, _mov;

		public int MaxHp => _maxHp;
		public int Str => _str;
		public int Mag => _mag;
		public int Dex => _dex;
		public int Spd => _spd;
		public int Def => _def;
		public int Res => _res;
		public int Lck => _lck;
		public int Con => _con;
		public int Mov => _mov;

		public Stats(int maxHp, int str, int mag, int dex, int spd, int def, int res, int lck, int con, int mov)
		{
			_maxHp = maxHp;
			_str = str;
			_mag = mag;
			_dex = dex;
			_spd = spd;
			_def = def;
			_res = res;
			_lck = lck;
			_con = con;
			_mov = mov;
		}

		public string PrettyString()
		{
			return $"Str: {_str}\nMag: {_mag}\nDex: {_dex}\nSpd: {_spd}\nDef: {_def}\nRes: {_res}\nLck: {_lck}\nCon: {_con}\nMov: {_mov}";
		}
	}
}
