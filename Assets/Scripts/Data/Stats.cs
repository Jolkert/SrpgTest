using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts.Data
{
	[Serializable]
	public struct Stats
	{
		[SerializeField] private int _maxHp, _str, _mag, _dex, _spd, _lck, _def, _res, _con, _mov;

		public int MaxHp => _maxHp;
		public int Str => _str;
		public int Mag => _mag;
		public int Dex => _dex;
		public int Spd => _spd;
		public int Lck => _lck;
		public int Def => _def;
		public int Res => _res;
		public int Con => _con;
		public int Mov => _mov;

		public Stats(int maxHp, int str, int mag, int dex, int spd, int lck, int def, int res, int con, int mov)
		{
			_maxHp = maxHp;
			_str = str;
			_mag = mag;
			_dex = dex;
			_spd = spd;
			_lck = lck;
			_def = def;
			_res = res;
			_con = con;
			_mov = mov;
		}
	}
}
