using System;
using UnityEngine;

namespace Assets.Scripts.Data
{
	[Serializable]
	public struct Weapon
	{
		[SerializeField] private int _mt, _hit, _crit, _wt;
		[SerializeField] private WeaponRange _range;
		[SerializeField] private bool _isMagic;

		public int Mt => _mt;
		public int Hit => _hit;
		public int Crit => _crit;
		public int Wt => _wt;
		public WeaponRange Range => _range;
		public bool IsMagic => _isMagic;

		public Weapon(int mt, int hit, int crit, int wt, WeaponRange range, bool isMagic)
		{
			_mt = mt;
			_hit = hit;
			_crit = crit;
			_wt = wt;
			_range = range;
			_isMagic = isMagic;
		}

		[Serializable]
		public struct WeaponRange : IEquatable<WeaponRange>
		{
			[SerializeField] int _min, _max;

			public int Min => _min;
			public int Max => _max;

			public WeaponRange(int min, int max)
			{
				_min = min;
				_max = max;
			}
			public WeaponRange(int range)
			{
				_min = range;
				_max = range;
			}

			public static implicit operator WeaponRange(int range) => new WeaponRange(range);
			public static implicit operator WeaponRange(Range range) => new WeaponRange(range.Start.Value, range.End.Value);

			public bool Contains(int n) => n >= _min && n <= _max;

			public override bool Equals(object obj) => obj is WeaponRange other && Equals(other);
			public bool Equals(WeaponRange other) => _min == other._min && _max == other._max;
			public static bool operator ==(WeaponRange left, WeaponRange right) => left.Equals(right);
			public static bool operator !=(WeaponRange left, WeaponRange right) => !(left == right);

			public override int GetHashCode() => (_min, _max).GetHashCode();

			public override string ToString() => $"[{_min}, {_max}]";
		}
	}
}
