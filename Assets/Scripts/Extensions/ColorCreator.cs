using System;
using UnityEngine;

namespace Assets.Scripts.Extensions
{
	public static class ColorCreator
	{
		public static Color FromRgb(byte r, byte g, byte b, byte a = byte.MaxValue) => new Color(r / 255f, g / 255f, b / 255f, a / 255f);
		public static Color FromRawInt(uint raw)
		{

			byte r, g, b, a;
			if (raw <= 0xffffff)
			{// no alpha
				r = (byte)((raw & 0xff0000) >> 16);
				g = (byte)((raw & 0x00ff00) >> 8);
				b = (byte)(raw & 0x0000ff);
				a = byte.MaxValue;
			}
			else
			{// alpha
				r = (byte)((raw & 0xff000000) >> 24);
				g = (byte)((raw & 0x00ff0000) >> 16);
				b = (byte)((raw & 0x0000ff00) >> 8);
				a = (byte)(raw & 0x000000ff);
			}

			return FromRgb(r, g, b, a);
		}
		public static Color FromHexString(string hex)
		{
			hex = hex.TrimStart('#');
			if (hex.Length != 6 && hex.Length != 8)
				throw new ArgumentException("Invalid hex string!");

			return FromRawInt(Convert.ToUInt32(hex, 16));
		}
	}
}
