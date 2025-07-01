//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

/// <summary>
/// Helper class containing functionality related to using dynamic fonts.
/// </summary>

static public class NGUIText
{
	[DoNotObfuscateNGUI] public enum Alignment
	{
		Automatic,
		Left,
		Center,
		Right,
		Justified,
	}

	[DoNotObfuscateNGUI] public enum SymbolStyle
	{
		None,
		Normal,
		Colored,
		NoOutline,
	}

	public class GlyphInfo
	{
		public Vector2 v0;
		public Vector2 v1;
		public Vector2 u0;
		public Vector2 u1;
		public Vector2 u2;
		public Vector2 u3;
		public float advance = 0f;
		public int channel = 0;
	}

	/// <summary>
	/// When printing text, a lot of additional data must be passed in. In order to save allocations,
	/// this data is not passed at all, but is rather set in a single place before calling the functions that use it.
	/// </summary>

	static public INGUIFont nguiFont;

	static public Font dynamicFont;
	static public GlyphInfo glyph = new GlyphInfo();

	static public int spaceWidth = 0;
	static public int fontSize = 16;
	static public float fontScale = 1f;
	static public float pixelDensity = 1f;
	static public FontStyle fontStyle = FontStyle.Normal;
	static public Alignment alignment = Alignment.Left;
	static public Color tint = Color.white;

	static public int rectWidth = 1000000;
	static public int rectHeight = 1000000;
	static public int regionWidth = 1000000;
	static public int regionHeight = 1000000;
	static public int maxLines = 0;

	static public bool gradient = false;
	static public Color gradientBottom = Color.white;
	static public Color gradientTop = Color.white;

	static public bool encoding = false;
	static public float spacingX = 0f;
	static public float spacingY = 0f;
	static public bool premultiply = false;
	static public SymbolStyle symbolStyle;

	static public int finalSize = 0;
	static public float finalSpacingX = 0f;
	static public float finalLineHeight = 0f;
	static public float baseline = 0f;
	static public bool useSymbols = false;

	/// <summary>
	/// Recalculate the 'final' values.
	/// </summary>

	static public void Update () { Update(true); }

	/// <summary>
	/// Recalculate the 'final' values.
	/// </summary>

	static public void Update (bool request)
	{
		finalSize = Mathf.RoundToInt(fontSize / pixelDensity);
		finalSpacingX = spacingX * fontScale;
		finalLineHeight = (fontSize + spacingY) * fontScale;
		useSymbols = (nguiFont != null) && encoding && (symbolStyle != SymbolStyle.None);

		var font = dynamicFont;

		if (font != null && request)
		{
			font.RequestCharactersInTexture(")_-.", finalSize, fontStyle);

			if (!font.GetCharacterInfo(')', out mTempChar, finalSize, fontStyle) || mTempChar.maxY == 0f)
			{
				font.RequestCharactersInTexture("A", finalSize, fontStyle);
				{
					if (!font.GetCharacterInfo('A', out mTempChar, finalSize, fontStyle))
					{
						baseline = 0f;
						return;
					}
				}
			}

			float y0 = mTempChar.maxY;
			float y1 = mTempChar.minY;

			baseline = Mathf.Round(y0 + (finalSize - y0 + y1) * 0.5f);
		}
	}

	[System.NonSerialized] static StringBuilder mTempSB;

	/// <summary>
	/// Prepare to use the specified text.
	/// </summary>

	static public void Prepare (string text)
	{
		var font = dynamicFont;

		if (font != null)
		{
			if (!encoding || symbolStyle == SymbolStyle.None)
			{
				font.RequestCharactersInTexture(text, finalSize, fontStyle);
				return;
			}

			if (mTempSB == null) mTempSB = new StringBuilder();
			else mTempSB.Length = 0;

			var bold = false;
			var italic = false;
			var underline = false;
			var strikethrough = false;
			var ignoreColor = false;
			var forceSpriteColor = false;
			var currentStyle = fontStyle;
			var sub = 0;
			var fontScaleMult = 0f;

			for (int i = 0, imax = text.Length; i < imax; ++i)
			{
				if (ParseSymbol(text, ref i, null, false, ref sub, ref fontScaleMult, ref bold, ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
				{
					--i;
					continue;
				}

				var styleNow = fontStyle;
				if (bold && italic) styleNow = FontStyle.BoldAndItalic;
				else if (bold) styleNow = FontStyle.Bold;
				else if (italic) styleNow = FontStyle.Italic;

				if (currentStyle != styleNow)
				{
					if (mTempSB.Length != 0) font.RequestCharactersInTexture(mTempSB.ToString(), finalSize, currentStyle);
					currentStyle = styleNow;
#if UNITY_5
					mTempSB.Length = 0;
#else
					mTempSB.Clear();
#endif
				}

				mTempSB.Append(text[i]);
			}

			if (mTempSB.Length != 0)
			{
				var s = mTempSB.ToString();
				font.RequestCharactersInTexture(s, finalSize, currentStyle);
#if UNITY_5
				mTempSB.Length = 0;
#else
				mTempSB.Clear();
#endif
			}
		}
	}

	/// <summary>
	/// Get the specified symbol.
	/// </summary>

	static public BMSymbol GetSymbol (ref string text, int index, int textLength)
	{
		if (nguiFont != null) return nguiFont.MatchSymbol(ref text, index, textLength);
		return null;
	}

	/// <summary>
	/// Get the width of the specified glyph. Returns zero if the glyph could not be retrieved.
	/// </summary>

	static public float GetGlyphWidth (int ch, int prev, float fontScale, bool bold, bool italic)
	{
		var font = dynamicFont;

		if (font != null)
		{
			if (spaceWidth != 0 && ch == ' ') return Mathf.RoundToInt(spaceWidth * fontScale * pixelDensity * ((float)finalSize / font.fontSize));

			var fs = fontStyle;
			if (bold && italic) fs = FontStyle.BoldAndItalic;
			else if (italic) fs = FontStyle.Italic;
			else if (bold) fs = FontStyle.Bold;

			if (font.GetCharacterInfo((char)ch, out mTempChar, finalSize, fs))
			return mTempChar.advance * fontScale * pixelDensity;
		}
		else if (nguiFont != null)
		{
			bool thinSpace = false;

			if (ch == '\u2009')
			{
				thinSpace = true;
				ch = ' ';
			}

			BMGlyph bmg = null;
			if (nguiFont != null) bmg = nguiFont.bmFont.GetGlyph(ch);

			if (bmg != null)
			{
				int adv = bmg.advance;
				if (thinSpace) adv >>= 1;
				return fontScale * ((prev != 0) ? adv + bmg.GetKerning(prev) : bmg.advance);
			}
		}
		return 0f;
	}

	/// <summary>
	/// Get the specified glyph.
	/// </summary>

	static public GlyphInfo GetGlyph (int ch, int prev, bool bold, bool italic, float fontScale = 1f)
	{
		var font = dynamicFont;

		if (font != null)
		{
			var fs = fontStyle;
			if (bold && italic) fs = FontStyle.BoldAndItalic;
			else if (italic) fs = FontStyle.Italic;
			else if (bold) fs = FontStyle.Bold;

			if (font.GetCharacterInfo((char)ch, out mTempChar, finalSize, fs))
			{
				var kern = 0;
				var nf = nguiFont as NGUIFont;

				if (nf != null)
				{
					kern = nf.GetKerning(prev, ch);
					if (kern != 0) kern = Mathf.RoundToInt(kern * ((float)finalSize / font.fontSize));
				}


				glyph.v0.x = mTempChar.minX + kern;
				glyph.v1.x = mTempChar.maxX + kern;

				glyph.v0.y = mTempChar.maxY - baseline;
				glyph.v1.y = mTempChar.minY - baseline;

				glyph.u0 = mTempChar.uvTopLeft;
				glyph.u1 = mTempChar.uvBottomLeft;
				glyph.u2 = mTempChar.uvBottomRight;
				glyph.u3 = mTempChar.uvTopRight;

				glyph.advance = mTempChar.advance + kern;
				glyph.channel = 0;

				glyph.v0.x = Mathf.Round(glyph.v0.x);
				glyph.v0.y = Mathf.Round(glyph.v0.y);
				glyph.v1.x = Mathf.Round(glyph.v1.x);
				glyph.v1.y = Mathf.Round(glyph.v1.y);

				if (ch == ' ' && spaceWidth != 0)
				{
					glyph.advance = Mathf.RoundToInt(spaceWidth * ((float)finalSize / font.fontSize));
				}

				float pd = fontScale * pixelDensity;

				if (pd != 1f)
				{
					glyph.v0 *= pd;
					glyph.v1 *= pd;
					glyph.advance *= pd;
				}
				return glyph;
			}
		}
		else if (nguiFont != null && nguiFont.bmFont != null)
		{
			bool thinSpace = false;

			if (ch == '\u2009')
			{
				thinSpace = true;
				ch = ' ';
			}

			var bmg = nguiFont.bmFont.GetGlyph(ch);

			if (bmg != null)
			{
				int kern = (prev != 0) ? bmg.GetKerning(prev) : 0;
				glyph.v0.x = bmg.offsetX + kern;
				glyph.v1.y = -bmg.offsetY;

				glyph.v1.x = glyph.v0.x + bmg.width;
				glyph.v0.y = glyph.v1.y - bmg.height;

				glyph.u0.x = bmg.x;
				glyph.u0.y = bmg.y + bmg.height;

				glyph.u2.x = bmg.x + bmg.width;
				glyph.u2.y = bmg.y;

				glyph.u1.x = glyph.u0.x;
				glyph.u1.y = glyph.u2.y;

				glyph.u3.x = glyph.u2.x;
				glyph.u3.y = glyph.u0.y;

				int adv = bmg.advance;
				if (ch == ' ' && spaceWidth != 0) adv = spaceWidth;

				if (thinSpace) adv >>= 1;
				glyph.advance = adv + kern;
				glyph.channel = bmg.channel;

				if (fontScale != 1f)
				{
					glyph.v0 *= fontScale;
					glyph.v1 *= fontScale;
					glyph.advance *= fontScale;
				}
				return glyph;
			}
		}
		return null;
	}

	static Color mInvisible = new Color(0f, 0f, 0f, 0f);
	static BetterList<Color> mColors = new BetterList<Color>();
	static float mAlpha = 1f;
	static CharacterInfo mTempChar;

	/// <summary>
	/// Parse Aa syntax alpha encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public float ParseAlpha (string text, int index)
	{
		int a = (NGUIMath.HexToDecimal(text[index + 1]) << 4) | NGUIMath.HexToDecimal(text[index + 2]);
		return Mathf.Clamp01(a / 255f);
	}

	/// <summary>
	/// Parse a RrGgBb color encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public Color ParseColor (string text, int offset = 0) { return ParseColor24(text, offset); }

	/// <summary>
	/// Parse a RrGgBb color encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public Color ParseColor24 (string text, int offset = 0)
	{
		int r = (NGUIMath.HexToDecimal(text[offset])     << 4) | NGUIMath.HexToDecimal(text[offset + 1]);
		int g = (NGUIMath.HexToDecimal(text[offset + 2]) << 4) | NGUIMath.HexToDecimal(text[offset + 3]);
		int b = (NGUIMath.HexToDecimal(text[offset + 4]) << 4) | NGUIMath.HexToDecimal(text[offset + 5]);
		float f = 1f / 255f;
		return new Color(f * r, f * g, f * b);
	}

	/// <summary>
	/// Parse a RrGgBb color encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool ParseColor24 (ref string text, int offset, out Color c)
	{
		var d0 = NGUIMath.HexToDecimal(text[offset], -1);
		var d1 = NGUIMath.HexToDecimal(text[offset + 1], -1);
		var d2 = NGUIMath.HexToDecimal(text[offset + 2], -1);

		var d3 = NGUIMath.HexToDecimal(text[offset + 3], -1);
		var d4 = NGUIMath.HexToDecimal(text[offset + 4], -1);
		var d5 = NGUIMath.HexToDecimal(text[offset + 5], -1);

		var dec = (d0 | d1 | d2 | d3 | d4 | d5);
		if (dec == -1) { c = Color.white; return false; }

		int r = (d0 << 4) | d1;
		int g = (d2 << 4) | d3;
		int b = (d4 << 4) | d5;
		float f = 1f / 255f;
		c = new Color(f * r, f * g, f * b);
		return true;
	}

	/// <summary>
	/// Parse a RrGgBbAa color encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public Color ParseColor32 (string text, int offset)
	{
		int r = (NGUIMath.HexToDecimal(text[offset]) << 4) | NGUIMath.HexToDecimal(text[offset + 1]);
		int g = (NGUIMath.HexToDecimal(text[offset + 2]) << 4) | NGUIMath.HexToDecimal(text[offset + 3]);
		int b = (NGUIMath.HexToDecimal(text[offset + 4]) << 4) | NGUIMath.HexToDecimal(text[offset + 5]);
		int a = (NGUIMath.HexToDecimal(text[offset + 6]) << 4) | NGUIMath.HexToDecimal(text[offset + 7]);
		float f = 1f / 255f;
		return new Color(f * r, f * g, f * b, f * a);
	}

	/// <summary>
	/// Parse a RrGgBbAa color encoded in the string.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool ParseColor32 (ref string text, int offset, out Color c)
	{
		var d0 = NGUIMath.HexToDecimal(text[offset], -1);
		var d1 = NGUIMath.HexToDecimal(text[offset + 1], -1);
		var d2 = NGUIMath.HexToDecimal(text[offset + 2], -1);
		var d3 = NGUIMath.HexToDecimal(text[offset + 3], -1);

		var d4 = NGUIMath.HexToDecimal(text[offset + 4], -1);
		var d5 = NGUIMath.HexToDecimal(text[offset + 5], -1);
		var d6 = NGUIMath.HexToDecimal(text[offset + 6], -1);
		var d7 = NGUIMath.HexToDecimal(text[offset + 7], -1);

		var dec = (d0 | d1 | d2 | d3 | d4 | d5 | d6 | d7);
		if (dec == -1) { c = Color.white; return false; }

		int r = (d0 << 4) | d1;
		int g = (d2 << 4) | d3;
		int b = (d4 << 4) | d5;
		int a = (d6 << 4) | d7;
		float f = 1f / 255f;
		c = new Color(f * r, f * g, f * b, f * a);
		return true;
	}

	/// <summary>
	/// The reverse of ParseColor -- encodes a color in RrGgBb format.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public string EncodeColor (Color c) { return EncodeColor24(c); }

	/// <summary>
	/// Convenience function that wraps the specified text block in a color tag.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public string EncodeColor (string text, Color c) { return "[c][" + EncodeColor24(c) + "]" + text + "[-][/c]"; }

	/// <summary>
	/// The reverse of ParseAlpha -- encodes a color in Aa format.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public string EncodeAlpha (float a)
	{
		int i = Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
		return NGUIMath.DecimalToHex8(i);
	}

	/// <summary>
	/// The reverse of ParseColor24 -- encodes a color in RrGgBb format.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public string EncodeColor24 (Color c)
	{
		int i = 0xFFFFFF & (NGUIMath.ColorToInt(c) >> 8);
		return NGUIMath.DecimalToHex24(i);
	}

	/// <summary>
	/// The reverse of ParseColor32 -- encodes a color in RrGgBb format.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public string EncodeColor32 (Color c)
	{
		int i = NGUIMath.ColorToInt(c);
		return NGUIMath.DecimalToHex32(i);
	}

	/// <summary>
	/// Parse an embedded symbol, such as [FFAA00] (set color) or [-] (undo color change). Returns whether the index was adjusted.
	/// </summary>

	static public bool ParseSymbol (string text, ref int index)
	{
		int n = 0;
		bool bold = false;
		bool italic = false;
		bool underline = false;
		bool strikethrough = false;
		bool ignoreColor = false;
		bool forceSpriteColor = false;
		float fontScaleMult = 0f;
		return ParseSymbol(text, ref index, null, false, ref n, ref fontScaleMult, ref bold, ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor);
	}

	/// <summary>
	/// Whether the specified character falls under the 'hex' character category (0-9, A-F).
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool IsHex (char ch)
	{
		return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
	}

	/// <summary>
	/// Parse the symbol, if possible. Returns 'true' if the 'index' was adjusted.
	/// Advanced symbol support originally contributed by Rudy Pangestu.
	/// </summary>

	static public bool ParseSymbol (string text, ref int index, BetterList<Color> colors, bool premultiply,
		ref int sub, ref float fontScaleMult, ref bool bold, ref bool italic, ref bool underline, ref bool strike, ref bool ignoreColor, ref bool forceSpriteColor)
	{
		int length = text.Length;

		if (index + 3 > length || text[index] != '[') return false;

		char ch0 = text[index + 1];
		char ch1 = text[index + 2];

		if (ch1 == ']')
		{
			if (ch0 == '-')
			{
				if (colors != null && colors.size > 1)
					colors.RemoveAt(colors.size - 1);
				index += 3;
				return true;
			}

			if (ch0 == 'b' || ch0 == 'B') { index += 3; bold = true; return true; }
			if (ch0 == 'i' || ch0 == 'I') { index += 3; italic = true; return true; }
			if (ch0 == 'u' || ch0 == 'U') { index += 3; underline = true; return true; }
			if (ch0 == 's' || ch0 == 'S') { index += 3; strike = true; return true; }
			if (ch0 == 'c' || ch0 == 'C') { index += 3; ignoreColor = true; return true; }
			if (ch0 == 't' || ch0 == 'T') { index += 3; forceSpriteColor = true; return true; }
		}
		else if (ch1 == '=')
		{
			// [y=0.75] syntax to scale the font size
			if (ch0 == 'y' || ch0 == 'Y')
			{
				var closing = text.IndexOf(']', index + 4);

				if (closing != -1 && float.TryParse(text.Substring(index + 3, closing - (index + 3)), out fontScaleMult))
				{
					sub = 0;
					index = closing + 1;
					return true;
				}
			}
		}

		if (index + 4 > length) return false;

		char ch2 = text[index + 3];

		if (ch2 == ']')
		{
			if (ch0 == '/')
			{
				if (ch1 == 'b' || ch1 == 'B') { index += 4; bold = false; return true; }
				if (ch1 == 'i' || ch1 == 'I') { index += 4; italic = false; return true; }
				if (ch1 == 'u' || ch1 == 'U') { index += 4; underline = false; return true; }
				if (ch1 == 's' || ch1 == 'S') { index += 4; strike = false; return true; }
				if (ch1 == 'c' || ch1 == 'C') { index += 4; ignoreColor = false; return true; }
				if (ch1 == 't' || ch1 == 'T') { index += 4; forceSpriteColor = false; return true; }
				if (ch1 == 'y' || ch1 == 'Y') { index += 4; sub = 0; fontScaleMult = 0f; return true; }
			}

			if (IsHex(ch0) && IsHex(ch1))
			{
				int a = (NGUIMath.HexToDecimal(ch0) << 4) | NGUIMath.HexToDecimal(ch1);
				mAlpha = a / 255f;
				index += 4;
				return true;
			}
		}

		if (index + 5 > length) return false;

		char ch3 = text[index + 4];

		// [sub], [sup] and [sub=0.5] / [sup=0.5] style syntax
		if ((ch0 == 's' || ch0 == 'S') && (ch1 == 'u' || ch1 == 'U'))
		{
			if (ch2 == 'b' || ch2 == 'B')
			{
				if (ch3 == ']')
				{
					sub = 1;
					fontScaleMult = 0.75f;
					index += 5;
					return true;
				}
				else if (ch3 == '=')
				{
					var closing = text.IndexOf(']', index + 4);

					if (closing != -1 && float.TryParse(text.Substring(index + 5, closing - (index + 5)), out fontScaleMult))
					{
						sub = 1;
						index = closing + 1;
						return true;
					}
				}
			}
			else if (ch2 == 'p' || ch2 == 'P')
			{
				if (ch3 == ']')
				{
					sub = 2;
					fontScaleMult = 0.75f;
					index += 5;
					return true;
				}
				else if (ch3 == '=')
				{
					var closing = text.IndexOf(']', index + 4);

					if (closing != -1 && float.TryParse(text.Substring(index + 5, closing - (index + 5)), out fontScaleMult))
					{
						sub = 2;
						index = closing + 1;
						return true;
					}
				}
			}
		}

		if (index + 6 > length) return false;

		char ch4 = text[index + 5];

		if (ch4 == ']')
		{
			if (ch0 == '/')
			{
				if ((ch1 == 's' || ch1 == 'S') && (ch2 == 'u' || ch2 == 'U'))
				{
					if (ch3 == 'b' || ch3 == 'B') { sub = 0; fontScaleMult = 0f; index += 6; return true; }
					if (ch3 == 'p' || ch3 == 'P') { sub = 0; fontScaleMult = 0f; index += 6; return true; }
				}
				else if ((ch1 == 'u' || ch1 == 'U') && (ch2 == 'r' || ch2 == 'R'))
				{
					if (ch3 == 'l' || ch3 == 'L') { index += 6; return true; }
				}
			}
		}

		if ((ch3 == '=') && (ch0 == 'u' && ch1 == 'r' && ch2 == 'l') || (ch0 == 'U' && ch1 == 'R' && ch2 == 'L'))
		{
			int closingBracket = text.IndexOf(']', index + 4);

			if (closingBracket != -1)
			{
				index = closingBracket + 1;
				return true;
			}
			else
			{
				index = text.Length;
				return true;
			}
		}

		if (index + 8 > length) return false;

		if (text[index + 7] == ']')
		{
			Color c;
			if (!ParseColor24(ref text, index + 1, out c)) return false;

			if (colors != null && colors.size > 0)
			{
				c.a = colors.buffer[colors.size - 1].a;
				if (premultiply && c.a != 1f) c = Color.Lerp(mInvisible, c, c.a);
				colors.Add(c);
			}

			index += 8;
			return true;
		}

		if (index + 10 > length) return false;

		if (text[index + 9] == ']')
		{
			Color c;
			if (!ParseColor32(ref text, index + 1, out c)) return false;

			if (colors != null)
			{
				if (premultiply && c.a != 1f) c = Color.Lerp(mInvisible, c, c.a);
				colors.Add(c);
			}

			index += 10;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Runs through the specified string and removes all symbols.
	/// </summary>

	static public string StripSymbols (string text)
	{
		if (text != null)
		{
			for (int i = 0, imax = text.Length; i < imax; )
			{
				char c = text[i];

				if (c == '[')
				{
					int sub = 0;
					var bold = false;
					var italic = false;
					var underline = false;
					var strikethrough = false;
					var ignoreColor = false;
					var forceSpriteColor = false;
					int retVal = i;
					var fontScaleMult = 0f;

					if (ParseSymbol(text, ref retVal, null, false, ref sub, ref fontScaleMult, ref bold, ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
					{
						text = text.Remove(i, retVal - i);
						imax = text.Length;
						continue;
					}
				}
				++i;
			}
		}
		return text;
	}

	/// <summary>
	/// Align the vertices to be right or center-aligned given the line width specified by NGUIText.lineWidth.
	/// </summary>

	static public void Align (List<Vector3> verts, int indexOffset, float printedWidth, int elements = 4)
	{
		switch (alignment)
		{
			case Alignment.Right:
			{
				float padding = rectWidth - printedWidth;
				if (padding < 0f) return;

				for (int i = indexOffset, imax = verts.Count; i < imax; ++i)
				{
					var v = verts[i];
					v.x += padding;
					verts[i] = v;
				}
				break;
			}

			case Alignment.Center:
			{
				float padding = (rectWidth - printedWidth) * 0.5f;
				if (padding < 0f) return;

				// Keep it pixel-perfect
				int diff = Mathf.RoundToInt(rectWidth - printedWidth);
				int intWidth = Mathf.RoundToInt(rectWidth);

				bool oddDiff = (diff & 1) == 1;
				bool oddWidth = (intWidth & 1) == 1;
				if ((oddDiff && !oddWidth) || (!oddDiff && oddWidth))
					padding += 0.5f * fontScale;

				for (int i = indexOffset, imax = verts.Count; i < imax; ++i)
				{
					var v = verts[i];
					v.x += padding;
					verts[i] = v;
				}
				break;
			}

			case Alignment.Justified:
			{
				// Printed text needs to reach at least 65% of the width in order to be justified
				if (printedWidth < rectWidth * 0.65f) return;

				// There must be some padding involved
				float padding = (rectWidth - printedWidth) * 0.5f;
				if (padding < 1f) return;

				// There must be at least two characters
				int chars = (verts.Count - indexOffset) / elements;
				if (chars < 1) return;

				float progressPerChar = 1f / (chars - 1);
				float scale = rectWidth / printedWidth;
				Vector3 v;

				for (int i = indexOffset + elements, charIndex = 1, imax = verts.Count; i < imax; ++charIndex)
				{
					float x0 = verts[i].x;
					float x1 = verts[i + elements / 2].x;
					float w = x1 - x0;
					float x0a = x0 * scale;
					float x1a = x0a + w;
					float x1b = x1 * scale;
					float x0b = x1b - w;
					float progress = charIndex * progressPerChar;

					x1 = Mathf.Lerp(x1a, x1b, progress);
					x0 = Mathf.Lerp(x0a, x0b, progress);
					x0 = Mathf.Round(x0);
					x1 = Mathf.Round(x1);

					if (elements == 4)
					{
						v = verts[i]; v.x = x0; verts[i++] = v;
						v = verts[i]; v.x = x0; verts[i++] = v;
						v = verts[i]; v.x = x1; verts[i++] = v;
						v = verts[i]; v.x = x1; verts[i++] = v;
					}
					else if (elements == 2)
					{
						v = verts[i]; v.x = x0; verts[i++] = v;
						v = verts[i]; v.x = x1; verts[i++] = v;
					}
					else if (elements == 1)
					{
						v = verts[i]; v.x = x0; verts[i++] = v;
					}
				}
				break;
			}
		}
	}

	/// <summary>
	/// Get the index of the closest character within the provided list of values.
	/// Meant to be used with the arrays created by PrintExactCharacterPositions().
	/// </summary>

	static public int GetExactCharacterIndex (List<Vector3> verts, List<int> indices, Vector2 pos)
	{
		for (int i = 0, imax = indices.Count; i < imax; ++i)
		{
			int i0 = (i << 1);
			int i1 = i0 + 1;

			float x0 = verts[i0].x;
			if (pos.x < x0) continue;

			float x1 = verts[i1].x;
			if (pos.x > x1) continue;

			float y0 = verts[i0].y;
			if (pos.y < y0) continue;

			float y1 = verts[i1].y;
			if (pos.y > y1) continue;

			return indices[i];
		}
		return 0;
	}

	/// <summary>
	/// Get the index of the closest vertex within the provided list of values.
	/// This function first sorts by Y, and only then by X.
	/// Meant to be used with the arrays created by PrintApproximateCharacterPositions().
	/// </summary>

	static public int GetApproximateCharacterIndex (List<Vector3> verts, List<int> indices, Vector2 pos)
	{
		// First sort by Y, and only then by X
		float bestX = float.MaxValue;
		float bestY = float.MaxValue;
		int bestIndex = 0;

		for (int i = 0, imax = verts.Count; i < imax; ++i)
		{
			float diffY = Mathf.Abs(pos.y - verts[i].y);
			if (diffY > bestY) continue;

			float diffX = Mathf.Abs(pos.x - verts[i].x);

			if (diffY < bestY)
			{
				bestY = diffY;
				bestX = diffX;
				bestIndex = i;
			}
			else if (diffX < bestX)
			{
				bestX = diffX;
				bestIndex = i;
			}
		}
		return indices[bestIndex];
	}

	/// <summary>
	/// Whether the specified character is a space.
	/// </summary>

	[DebuggerHidden]
	[DebuggerStepThrough]
	static public bool IsSpace (int ch) { return (ch == ' ' || ch == 0x200a || ch == 0x200b || ch == '\u2009'); }

	/// <summary>
	/// Convenience function that ends the line by either appending a new line character or replacing a space with one.
	/// </summary>

	[DebuggerHidden]
	[DebuggerStepThrough]
	static public void EndLine (ref StringBuilder s)
	{
		int i = s.Length - 1;
		if (i > 0 && IsSpace(s[i])) s[i] = '\n';
		else s.Append('\n');
	}

	/// <summary>
	/// Convenience function that ends the line by replacing a space with a newline character.
	/// </summary>

	[DebuggerHidden]
	[DebuggerStepThrough]
	static void ReplaceSpaceWithNewline (ref StringBuilder s)
	{
		int i = s.Length - 1;
		if (i > 0 && IsSpace(s[i])) s[i] = '\n';
	}

	static float symbolScale
	{
		get
		{
			var font = nguiFont as NGUIFont;
			if (font == null) return 1f;
			return font.symbolScale * fontSize / font.defaultSize;
		}
	}

	static float symbolOffset
	{
		get
		{
			var font = nguiFont as NGUIFont;
			if (font == null) return 1f;
			return font.symbolOffset;
		}
	}

	static int symbolMaxHeight
	{
		get
		{
			var font = nguiFont as NGUIFont;
			if (font == null) return 0;
			return font.symbolMaxHeight;
		}
	}

	static bool symbolCentered
	{
		get
		{
			var font = nguiFont as NGUIFont;
			if (font == null) return false;
			return font.symbolCentered;
		}
	}

	/// <summary>
	/// Get the printed size of the specified string. The returned value is in pixels.
	/// </summary>

	static public Vector2 CalculatePrintedSize (string text, bool prepare = true)
	{
		var v = Vector2.zero;

		if (!string.IsNullOrEmpty(text))
		{
			if (prepare) Prepare(text);
			mColors.Clear();

			int ch = 0, prev = 0;
			float x = 0f, maxX = 0f, maxWidth = regionWidth + 0.01f;
			var yOffset = Mathf.Round(spacingY * fontScale * 0.5f);
			var y = yOffset;
			int textLength = text.Length;
			int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
			var bold = false;
			var italic = false;
			var underline = false;
			var strikethrough = false;
			var ignoreColor = false;
			var forceSpriteColor = false;
			var symbolScale = NGUIText.symbolScale;
			var symbolMaxHeight = NGUIText.symbolMaxHeight;
			var fontScaleMult = 0f;

			for (int i = 0; i < textLength; ++i)
			{
				ch = text[i];

				// Color changing symbol
				if (encoding && ParseSymbol(text, ref i, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
					ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
				{
					--i;
					continue;
				}

				// New line character -- skip to the next line
				if (ch == '\n')
				{
					if (x > maxX) maxX = x;
					x = 0;
					y += finalLineHeight;
					prev = 0;
					continue;
				}

				// Invalid character -- skip it
				if (ch < ' ')
				{
					prev = ch;
					continue;
				}

				// See if there is a symbol matching this text
				var symbol = useSymbols ? GetSymbol(ref text, i, textLength) : null;
				var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

				if (symbol != null)
				{
					var h = symbol.paddedHeight;
					if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
					var w = symbol.pixelPerfect ? symbol.advance : Mathf.Round(symbol.advance * scale * symbolScale);
					var mx = x + w;

					// Doesn't fit? Move down to the next line
					if (mx > maxWidth)
					{
						if (x == 0f) break;
						if (x > maxX) maxX = x;

						x = 0;
						y += finalLineHeight;
					}
					else if (mx > maxX) maxX = mx;

					x += w + finalSpacingX;
					i += symbol.length - 1;
					prev = 0;
				}
				else // No symbol present
				{
					var glyph = GetGlyph(ch, prev, bold, italic, scale);
					if (glyph == null) continue;

					prev = ch;
					var w = glyph.advance;

					if (sub != 0)
					{
						if (sub == 1)
						{
							var f = fontScale * fontSize * 0.4f;
							glyph.v0.y -= f;
							glyph.v1.y -= f;
						}
						else
						{
							var f = fontScale * fontSize * 0.05f;
							glyph.v0.y += f;
							glyph.v1.y += f;
						}
					}

					w += finalSpacingX;

					var mx = x + w;

					// Doesn't fit? Move down to the next line
					if (mx > maxWidth)
					{
						if (x == 0f) continue;

						x = 0;
						y += finalLineHeight;
					}
					else if (mx > maxX) maxX = mx;

					if (IsSpace(ch))
					{
						if (underline)
						{
							ch = '_';
						}
						else if (strikethrough)
						{
							ch = '-';
						}
					}

					// Advance the position
					x = mx;

					// Subscript may cause pixels to no longer be aligned
					if (sub != 0) x = Mathf.Round(x);

					// No need to continue if this is a space character
					if (IsSpace(ch)) continue;
				}
			}

			v.x = Mathf.Ceil(((x > maxX) ? x - finalSpacingX : maxX));
			v.y = Mathf.Ceil((y + finalLineHeight - yOffset));
		}
		return v;
	}

	static BetterList<float> mSizes = new BetterList<float>();

	/// <summary>
	/// Calculate the character index offset required to print the end of the specified text.
	/// </summary>

	static public int CalculateOffsetToFit (string text, bool prepare = true)
	{
		if (string.IsNullOrEmpty(text) || regionWidth < 1) return 0;

		if (prepare) Prepare(text);
		mColors.Clear();

		int textLength = text.Length, ch = 0, prev = 0;
		int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var symbolScale = NGUIText.symbolScale;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var fontScaleMult = 0f;

		for (int i = 0, imax = text.Length; i < imax; ++i)
		{
			if (encoding && ParseSymbol(text, ref i, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
				ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				--i;
				continue;
			}

			var symbol = useSymbols ? GetSymbol(ref text, i, textLength) : null;
			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

			if (symbol == null)
			{
				ch = text[i];
				float w = GetGlyphWidth(ch, prev, scale, bold, italic);
				if (w != 0f) mSizes.Add(finalSpacingX + w);
				prev = ch;
			}
			else
			{
				var h = symbol.paddedHeight;
				if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
				mSizes.Add(finalSpacingX + (symbol.pixelPerfect ? symbol.advance : Mathf.Round(symbol.advance * scale * symbolScale)));
				for (int b = 0, bmax = symbol.sequence.Length - 1; b < bmax; ++b) mSizes.Add(0);
				i += symbol.sequence.Length - 1;
				prev = 0;
			}
		}

		float remainingWidth = regionWidth;
		int currentCharacterIndex = mSizes.size;

		while (currentCharacterIndex > 0 && remainingWidth > 0)
			remainingWidth -= mSizes.buffer[--currentCharacterIndex];

		mSizes.Clear();

		if (remainingWidth < 0) ++currentCharacterIndex;
		return currentCharacterIndex;
	}

	/// <summary>
	/// Get the end of line that would fit into a field of given width.
	/// </summary>

	static public string GetEndOfLineThatFits (string text)
	{
		int textLength = text.Length;
		int offset = CalculateOffsetToFit(text);
		return text.Substring(offset, textLength - offset);
	}

	/// <summary>
	/// Text wrapping functionality. The 'width' and 'height' should be in pixels.
	/// </summary>

	static public bool WrapText (string text, out string finalText, bool wrapLineColors = false)
	{
		return WrapText(text, out finalText, false, wrapLineColors);
	}

	[System.NonSerialized]
	static StringBuilder mSB;

	/// <summary>
	/// Text wrapping functionality. The 'width' and 'height' should be in pixels.
	/// Returns 'true' if the requested text fits into the previously set dimensions.
	/// </summary>

	static public bool WrapText (string text, out string finalText, bool keepCharCount, bool wrapLineColors, bool useEllipsis = false)
	{
		if (regionWidth < 1 || regionHeight < 1 || finalLineHeight < 1f)
		{
			finalText = "";
			return false;
		}

		float height = (maxLines > 0) ? Mathf.Min(regionHeight, finalLineHeight * maxLines) : regionHeight;
		int maxLineCount = (maxLines > 0) ? maxLines : 1000000;
		maxLineCount = Mathf.FloorToInt(Mathf.Min(maxLineCount, height / finalLineHeight) + 0.01f);

		if (maxLineCount == 0)
		{
			finalText = "";
			return false;
		}

		if (string.IsNullOrEmpty(text)) text = " ";

		int textLength = text.Length;
		Prepare(text);
		mColors.Clear();

		if (mSB == null) mSB = new StringBuilder();
		else mSB.Length = 0;

		float maxWidth = regionWidth;
		float x = 0f;
		int start = 0, offset = 0, lineCount = 1, prev = 0;
		var lineIsEmpty = true;
		var fits = true;
		var eastern = false;

		Color c = tint;
		var sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var ellipsisWidth = useEllipsis ? (finalSpacingX + GetGlyphWidth('.', '.', fontScale, bold, italic)) * 3f : finalSpacingX;
		var symbolScale = NGUIText.symbolScale;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var lastValidChar = 0;
		var fontScaleMult = 0f;

		mColors.Add(c);

		if (!useSymbols) wrapLineColors = false;

		if (wrapLineColors)
		{
			mSB.Append("[");
			mSB.Append(NGUIText.EncodeColor(c));
			mSB.Append("]");
		}

		// Run through all characters
		for (; offset < textLength; ++offset)
		{
			var ch = text[offset];
			var space = IsSpace(ch);
			if (ch > 12287) eastern = true;

			// New line character -- start a new line
			if (ch == '\n')
			{
				if (lineCount == maxLineCount) break;
				x = 0f;

				// Add the previous word to the final string
				if (start < offset) mSB.Append(text, start, offset - start + 1);
				else mSB.Append(ch);

				if (wrapLineColors)
				{
					for (int i = 0; i < mColors.size; ++i)
						mSB.Insert(mSB.Length - 1, "[-]");

					for (int i = 0; i < mColors.size; ++i)
					{
						mSB.Append("[");
						mSB.Append(NGUIText.EncodeColor(mColors.buffer[i]));
						mSB.Append("]");
					}
				}

				lineIsEmpty = true;
				++lineCount;
				start = offset + 1;
				prev = 0;
				continue;
			}

			var lastLine = (lineIsEmpty || lineCount == maxLineCount);
			var previousSubscript = sub;

			// When encoded symbols such as [RrGgBb] or [-] are encountered, skip past them
			if (encoding && ParseSymbol(text, ref offset, mColors, premultiply, ref sub, ref fontScaleMult, ref bold, ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				// Append the previous word
				if (lastValidChar + 1 > offset)
				{
					mSB.Append(text, start, offset - start);
					start = offset;
					lastValidChar = offset;
				}

				if (wrapLineColors)
				{
					if (ignoreColor)
					{
						c = mColors.buffer[mColors.size - 1];
						c.a *= mAlpha * tint.a;
					}
					else
					{
						c = tint * mColors.buffer[mColors.size - 1];
						c.a *= mAlpha;
					}

					for (int b = 0, bmax = mColors.size - 2; b < bmax; ++b)
						c.a *= mColors.buffer[b].a;
				}

				// Append the symbol
				if (start < offset) mSB.Append(text, start, offset - start);
				else mSB.Append(ch);

				start = offset--;
				lastValidChar = start;
				continue;
			}

			// See if there is a symbol matching this text
			var symbol = useSymbols ? GetSymbol(ref text, offset, textLength) : null;

			// Calculate how wide this symbol or character is going to be
			float glyphWidth;
			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

			if (symbol == null)
			{
				// Find the glyph for this character
				float w = GetGlyphWidth(ch, prev, scale, bold, italic);
				if (w == 0f && !space) continue;
				glyphWidth = finalSpacingX + w;
			}
			else
			{
				var h = symbol.paddedHeight;
				if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
				glyphWidth = finalSpacingX + (symbol.pixelPerfect ? symbol.advance : Mathf.Round(symbol.advance * scale * symbolScale));
			}

			// Force pixel alignment
			if (sub != 0) glyphWidth = Mathf.Round(glyphWidth);

			// Reduce the width
			x += glyphWidth;
			prev = ch;
			var ew = (useEllipsis && lastLine) ? maxWidth - ellipsisWidth : maxWidth;

			// If this marks the end of a word, add it to the final string.
			if (space && !eastern && start < offset)
			{
				int end = offset - start;

				// Last word on the last line should not include an invisible character
				if (lineCount == maxLineCount && x >= ew && offset < textLength)
				{
					char cho = text[offset];
					if (cho < ' ' || IsSpace(cho)) --end;
				}

				// Adds "..." at the end of text that doesn't fit
				if (lastLine && useEllipsis && start < lastValidChar && x < maxWidth && x > ew)
				{
					if (lastValidChar > start) mSB.Append(text, start, lastValidChar - start + 1);
					if (sub != 0) mSB.Append("[/sub]");
					else if (fontScaleMult != 0f) mSB.Append("[/y]");
					mSB.Append("...");
					start = offset;
					break;
				}

				mSB.Append(text, start, end + 1);
				lineIsEmpty = false;
				start = offset + 1;
			}

			// Keep track of the last char that can still append an ellipsis
			if (useEllipsis && !space && x <= ew) lastValidChar = offset;

			// Doesn't fit?
			if (x > ew)
			{
				// Can't start a new line
				if (lastLine)
				{
					// Adds "..." at the end of text that doesn't fit
					if (useEllipsis && offset > 0)
					{
						if (lastValidChar > start) mSB.Append(text, start, lastValidChar - start + 1);
						if (sub != 0) mSB.Append("[/sub]");
						else if (fontScaleMult != 0f) mSB.Append("[/y]");
						if (symbolStyle == SymbolStyle.None) mSB.Append("...");
						else mSB.Append("[-][ff]...");
						start = offset;
						break;
					}

					// This is the first word on the line -- add it up to the character that fits
					mSB.Append(text, start, Mathf.Max(0, offset - start));
					if (!space && !eastern) fits = false;

					if (wrapLineColors && mColors.size > 0) mSB.Append("[-]");

					if (lineCount++ == maxLineCount)
					{
						start = offset;
						break;
					}

					if (keepCharCount) ReplaceSpaceWithNewline(ref mSB);
					else EndLine(ref mSB);

					if (wrapLineColors)
					{
						for (int i = 0; i < mColors.size; ++i)
							mSB.Insert(mSB.Length - 1, "[-]");

						for (int i = 0; i < mColors.size; ++i)
						{
							mSB.Append("[");
							mSB.Append(NGUIText.EncodeColor(mColors.buffer[i]));
							mSB.Append("]");
						}
					}

					// Start a brand-new line
					lineIsEmpty = true;

					if (space)
					{
						start = offset + 1;
						x = 0f;
					}
					else
					{
						start = offset;
						x = glyphWidth;
					}

					lastValidChar = offset;
					prev = 0;
				}
				else
				{
					// Skip spaces at the beginning of the line
					//while (start < offset && IsSpace(text[start])) ++start;
					while (start < textLength && IsSpace(text[start])) ++start;

					// Revert the position to the beginning of the word and reset the line
					lineIsEmpty = true;
					x = 0f;
					offset = start - 1;
					prev = 0;

					if (lineCount++ == maxLineCount) break;
					if (keepCharCount) ReplaceSpaceWithNewline(ref mSB);
					else EndLine(ref mSB);

					if (wrapLineColors)
					{
						// Negate previous colors prior to the newline character
						for (int i = 0; i < mColors.size; ++i)
							mSB.Insert(mSB.Length - 1, "[-]");

						// Add all the current colors before going forward
						for (int i = 0; i < mColors.size; ++i)
						{
							mSB.Append("[");
							mSB.Append(NGUIText.EncodeColor(mColors.buffer[i]));
							mSB.Append("]");
						}
					}
					continue;
				}
			}

			// Advance the offset past the symbol
			if (symbol != null)
			{
				offset += symbol.length - 1;
				prev = 0;
			}
		}

		if (start < offset) mSB.Append(text, start, offset - start);
		if (wrapLineColors && mColors.size > 0) mSB.Append("[-]");
		finalText = mSB.ToString();
		mColors.Clear();
		return fits && ((offset == textLength) || (maxLines != 0 ? lineCount == maxLineCount : lineCount == 0));
	}

	static Color s_c0, s_c1;

	/// <summary>
	/// Print the specified text into the buffers.
	/// </summary>

	static public void Print (string text, List<Vector3> verts, List<Vector2> uvs, List<Color> cols, List<Vector3> sverts = null, List<Vector2> suvs = null, List<Color> scols = null)
	{
		if (string.IsNullOrEmpty(text)) return;

		int indexOffset = verts.Count;
		var sIndexOffset = (sverts != null) ? sverts.Count : 0;
		Prepare(text);

		// Start with the white tint
		mColors.Clear();
		mColors.Add(Color.white);
		mAlpha = 1f;

		int ch = 0, prev = 0;
		float x = 0f, maxX = 0f;
		var y = Mathf.Round(spacingY * fontScale * 0.5f);

		Color gb = (tint * gradientBottom);
		Color gt = (tint * gradientTop);
		Color gc = tint;
		int textLength = text.Length;

		Rect uvRect = new Rect();
		float invX = 0f, invY = 0f;
		float sizePD = finalSize * pixelDensity;
		float v0x, v1x, v1y, v0y, prevX = 0f, maxWidth = regionWidth + 0.01f;

		// Advanced symbol support contributed by Rudy Pangestu.
		int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var clear = new Color(0f, 0f, 0f, 0f);
		var symbolScale = NGUIText.symbolScale;
		var symbolOffset = NGUIText.symbolOffset;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var fontScaleMult = 0f;

		if (dynamicFont == null && nguiFont != null)
		{
			uvRect = nguiFont.uvRect;
			invX = uvRect.width / nguiFont.texWidth;
			invY = uvRect.height / nguiFont.texHeight;
		}

		for (int i = 0; i < textLength; ++i)
		{
			ch = text[i];

			prevX = x;

			// Color changing symbol
			if (encoding && ParseSymbol(text, ref i, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
				ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				if (ignoreColor)
				{
					gc = mColors.buffer[mColors.size - 1];
					gc.a *= mAlpha * tint.a;
				}
				else
				{
					gc = tint * mColors.buffer[mColors.size - 1];
					gc.a *= mAlpha;
				}

				for (int b = 0, bmax = mColors.size - 2; b < bmax; ++b)
					gc.a *= mColors.buffer[b].a;

				if (gradient)
				{
					gb = (gradientBottom * gc);
					gt = (gradientTop * gc);
				}
				--i;
				continue;
			}

			// New line character -- skip to the next line
			if (ch == '\n')
			{
				if (x > maxX) maxX = x;

				if (alignment != Alignment.Left)
				{
					Align(verts, indexOffset, x - finalSpacingX);
					indexOffset = verts.Count;

					if (sverts != null)
					{
						Align(sverts, sIndexOffset, x - finalSpacingX);
						sIndexOffset = sverts.Count;
					}
				}

				x = 0;
				y += finalLineHeight;
				prev = 0;
				continue;
			}

			// Invalid character -- skip it
			if (ch < ' ')
			{
				prev = ch;
				continue;
			}

			// See if there is a symbol matching this text
			var symbol = useSymbols ? GetSymbol(ref text, i, textLength) : null;
			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

			if (symbol != null)
			{
				var h = symbol.paddedHeight;
				var mult = (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) ? (float)symbolMaxHeight / h : 1f;
				var fs = symbol.pixelPerfect ? 1f : fontScale * symbolScale * mult;
				v0x = x + symbol.offsetX * fs;
				v1x = v0x + symbol.width * fs;
				v1y = -(y + symbol.offsetY * fs) + symbolOffset;
				v0y = v1y - symbol.height * fs;
				var w = symbol.pixelPerfect ? symbol.advance : Mathf.Round(symbol.advance * scale * symbolScale * mult);

				if (symbolCentered)
				{
					var symH = Mathf.RoundToInt(symbol.height * fs);
					var fntH = Mathf.RoundToInt(fontScale * fontSize);
					var diff = (symH - fntH) / 2;

					v0y += diff;
					v1y += diff;
				}

				// Doesn't fit? Move down to the next line
				if (x + w > maxWidth)
				{
					if (x == 0f) return;

					if (alignment != Alignment.Left && indexOffset < verts.Count)
					{
						Align(verts, indexOffset, x - finalSpacingX);
						indexOffset = verts.Count;

						if (sverts != null)
						{
							Align(sverts, sIndexOffset, x - finalSpacingX);
							sIndexOffset = sverts.Count;
						}
					}

					v0x -= x;
					v1x -= x;
					v0y -= finalLineHeight;
					v1y -= finalLineHeight;

					x = 0;
					y += finalLineHeight;
					prevX = 0;
				}

				verts.Add(new Vector3(v0x, v0y));
				verts.Add(new Vector3(v0x, v1y));
				verts.Add(new Vector3(v1x, v1y));
				verts.Add(new Vector3(v1x, v0y));

				if (sverts != null)
				{
					sverts.Add(new Vector3(v0x, v0y));
					sverts.Add(new Vector3(v0x, v1y));
					sverts.Add(new Vector3(v1x, v1y));
					sverts.Add(new Vector3(v1x, v0y));
				}

				x += w + finalSpacingX;
				i += symbol.length - 1;
				prev = 0;

				if (uvs != null)
				{
					Rect uv = symbol.uvRect;

					float u0x = uv.xMin;
					float u0y = uv.yMin;
					float u1x = uv.xMax;
					float u1y = uv.yMax;

					if (suvs != null)
					{
						uvs.Add(new Vector2(1f, 1f));
						uvs.Add(new Vector2(1f, 1f));
						uvs.Add(new Vector2(1f, 1f));
						uvs.Add(new Vector2(1f, 1f));

						suvs.Add(new Vector2(u0x, u0y));
						suvs.Add(new Vector2(u0x, u1y));
						suvs.Add(new Vector2(u1x, u1y));
						suvs.Add(new Vector2(u1x, u0y));
					}
					else
					{
						uvs.Add(new Vector2(u0x, u0y));
						uvs.Add(new Vector2(u0x, u1y));
						uvs.Add(new Vector2(u1x, u1y));
						uvs.Add(new Vector2(u1x, u0y));
					}
				}

				if (cols != null)
				{
					if (symbolStyle == SymbolStyle.Colored || (symbolStyle == SymbolStyle.Normal && (forceSpriteColor || symbol.colored)))
					{
						if (scols != null)
						{
							for (int b = 0; b < 4; ++b)
							{
								cols.Add(clear);
								scols.Add(gc);
							}
						}
						else for (int b = 0; b < 4; ++b) cols.Add(gc);
					}
					else
					{
						var col = Color.white;

						if (symbolStyle == SymbolStyle.NoOutline)
						{
							col.r = -1f;
							col.a = 0f;
						}
						else col.a = gc.a;

						if (scols != null)
						{
							for (int b = 0; b < 4; ++b)
							{
								cols.Add(clear);
								scols.Add(col);
							}
						}
						else for (int b = 0; b < 4; ++b) cols.Add(col);
					}
				}
			}
			else // No symbol present
			{
				var glyph = GetGlyph(ch, prev, bold, italic, scale);
				if (glyph == null) continue;

				prev = ch;
				var w = glyph.advance;

				if (sub != 0)
				{
					if (sub == 1)
					{
						var f = fontScale * fontSize * 0.4f;
						glyph.v0.y -= f;
						glyph.v1.y -= f;
					}
					else
					{
						var f = fontScale * fontSize * 0.05f;
						glyph.v0.y += f;
						glyph.v1.y += f;
					}
				}
				else if (fontScaleMult != 0f)
				{
					// Centered vertical alignment of scaled text
					var f = fontScale * (1f - fontScaleMult) * fontSize * 0.5f;
					glyph.v0.y -= f;
					glyph.v1.y -= f;
				}

				w += finalSpacingX;

				v0x = glyph.v0.x + x;
				v0y = glyph.v0.y - y;
				v1x = glyph.v1.x + x;
				v1y = glyph.v1.y - y;

				// Doesn't fit? Move down to the next line
				if (x + w > maxWidth)
				{
					if (x == 0f) return;

					if (alignment != Alignment.Left && indexOffset < verts.Count)
					{
						Align(verts, indexOffset, x - finalSpacingX);
						indexOffset = verts.Count;

						if (sverts != null)
						{
							Align(sverts, sIndexOffset, x - finalSpacingX);
							sIndexOffset = sverts.Count;
						}
					}

					v0x -= x;
					v1x -= x;
					v0y -= finalLineHeight;
					v1y -= finalLineHeight;

					x = 0;
					y += finalLineHeight;
					prevX = 0;
				}

				if (IsSpace(ch))
				{
					if (underline)
					{
						ch = '_';
					}
					else if (strikethrough)
					{
						ch = '-';
					}
				}

				// Advance the position
				x += w;

				// Subscript may cause pixels to no longer be aligned
				if (sub != 0) x = Mathf.Round(x);

				// No need to continue if this is a space character
				if (IsSpace(ch)) continue;

				var useBold = bold && dynamicFont == null;

				// Texture coordinates
				if (uvs != null)
				{
					if (dynamicFont == null && nguiFont != null)
					{
						glyph.u0.x = uvRect.xMin + invX * glyph.u0.x;
						glyph.u2.x = uvRect.xMin + invX * glyph.u2.x;
						glyph.u0.y = uvRect.yMax - invY * glyph.u0.y;
						glyph.u2.y = uvRect.yMax - invY * glyph.u2.y;

						glyph.u1.x = glyph.u0.x;
						glyph.u1.y = glyph.u2.y;

						glyph.u3.x = glyph.u2.x;
						glyph.u3.y = glyph.u0.y;
					}

					for (int j = 0, jmax = (useBold ? 4 : 1); j < jmax; ++j)
					{
						uvs.Add(glyph.u0);
						uvs.Add(glyph.u1);
						uvs.Add(glyph.u2);
						uvs.Add(glyph.u3);
					}
				}

				// Vertex colors
				if (cols != null)
				{
					if (glyph.channel == 0 || glyph.channel == 15)
					{
						if (gradient)
						{
							float min = sizePD + glyph.v0.y / fontScale;
							float max = sizePD + glyph.v1.y / fontScale;

							min /= sizePD;
							max /= sizePD;

							s_c0 = Color.Lerp(gb, gt, min);
							s_c1 = Color.Lerp(gb, gt, max);

							for (int j = 0, jmax = (useBold ? 4 : 1); j < jmax; ++j)
							{
								cols.Add(s_c0);
								cols.Add(s_c1);
								cols.Add(s_c1);
								cols.Add(s_c0);
							}
						}
						else
						{
							for (int j = 0, jmax = (useBold ? 16 : 4); j < jmax; ++j)
								cols.Add(gc);
						}
					}
					else
					{
						// Packed fonts come as alpha masks in each of the RGBA channels.
						// In order to use it we need to use a special shader.
						//
						// Limitations:
						// - Effects (drop shadow, outline) will not work.
						// - Should not be a part of the atlas (eastern fonts rarely are anyway).
						// - Lower color precision

						Color col = gc;

						col *= 0.49f;

						switch (glyph.channel)
						{
							case 1: col.b += 0.51f; break;
							case 2: col.g += 0.51f; break;
							case 4: col.r += 0.51f; break;
							case 8: col.a += 0.51f; break;
						}

						for (int j = 0, jmax = (useBold ? 16 : 4); j < jmax; ++j)
							cols.Add(col);
					}
				}

				if (dynamicFont != null)
				{
					verts.Add(new Vector3(v0x, v0y));
					verts.Add(new Vector3(v0x, v1y));
					verts.Add(new Vector3(v1x, v1y));
					verts.Add(new Vector3(v1x, v0y));
				}
				else if (!bold) // Bold and italic contributed by Rudy Pangestu.
				{
					if (!italic)
					{
						verts.Add(new Vector3(v0x, v0y));
						verts.Add(new Vector3(v0x, v1y));
						verts.Add(new Vector3(v1x, v1y));
						verts.Add(new Vector3(v1x, v0y));
					}
					else // Italic
					{
						float slant = fontSize * 0.1f * ((v1y - v0y) / fontSize);
						verts.Add(new Vector3(v0x - slant, v0y));
						verts.Add(new Vector3(v0x + slant, v1y));
						verts.Add(new Vector3(v1x + slant, v1y));
						verts.Add(new Vector3(v1x - slant, v0y));
					}
				}
				else // Bold
				{
					for (int j = 0; j < 4; ++j)
					{
						float a = mBoldOffset[j * 2];
						float b = mBoldOffset[j * 2 + 1];

						float slant = (italic ? fontSize * 0.1f * ((v1y - v0y) / fontSize) : 0f);
						verts.Add(new Vector3(v0x + a - slant, v0y + b));
						verts.Add(new Vector3(v0x + a + slant, v1y + b));
						verts.Add(new Vector3(v1x + a + slant, v1y + b));
						verts.Add(new Vector3(v1x + a - slant, v0y + b));
					}
				}

				// Underline and strike-through contributed by Rudy Pangestu.
				if (underline || strikethrough)
				{
					var dash = GetGlyph(strikethrough ? '-' : '_', 0, false, false, scale);
					if (dash == null) continue;

					if (uvs != null)
					{
						if (dynamicFont == null && nguiFont != null)
						{
							dash.u0.x = uvRect.xMin + invX * dash.u0.x;
							dash.u2.x = uvRect.xMin + invX * dash.u2.x;
							dash.u0.y = uvRect.yMax - invY * dash.u0.y;
							dash.u2.y = uvRect.yMax - invY * dash.u2.y;
						}

						float cx = (dash.u0.x + dash.u2.x) * 0.5f;

						for (int j = 0, jmax = (useBold ? 4 : 1); j < jmax; ++j)
						{
							uvs.Add(new Vector2(cx, dash.u0.y));
							uvs.Add(new Vector2(cx, dash.u2.y));
							uvs.Add(new Vector2(cx, dash.u2.y));
							uvs.Add(new Vector2(cx, dash.u0.y));
						}
					}

					// Dash has a soft border, so using its dimensions as-is results in a very thick line.
					// To address this, I reduce the height of drawn strike-through line by 2 pixels.
					var height = Mathf.Round(dash.v0.y - dash.v1.y);
					height = Mathf.Max(height - 2f, 2f);

					v0y = -y + dash.v0.y - 1f;
					v1y = v0y - height;

					if (useBold)
					{
						for (int j = 0; j < 4; ++j)
						{
							float a = mBoldOffset[j * 2];
							float b = mBoldOffset[j * 2 + 1];

							verts.Add(new Vector3(prevX + a, v0y + b));
							verts.Add(new Vector3(prevX + a, v1y + b));
							verts.Add(new Vector3(x + a, v1y + b));
							verts.Add(new Vector3(x + a, v0y + b));
						}
					}
					else
					{
						verts.Add(new Vector3(prevX, v0y));
						verts.Add(new Vector3(prevX, v1y));
						verts.Add(new Vector3(x, v1y));
						verts.Add(new Vector3(x, v0y));
					}

					if (gradient)
					{
						float min = sizePD + dash.v0.y / scale;
						float max = sizePD + dash.v1.y / scale;

						min /= sizePD;
						max /= sizePD;

						s_c0 = Color.Lerp(gb, gt, min);
						s_c1 = Color.Lerp(gb, gt, max);

						for (int j = 0, jmax = (useBold ? 4 : 1); j < jmax; ++j)
						{
							cols.Add(s_c0);
							cols.Add(s_c1);
							cols.Add(s_c1);
							cols.Add(s_c0);
						}
					}
					else
					{
						for (int j = 0, jmax = (useBold ? 16 : 4); j < jmax; ++j)
							cols.Add(gc);
					}
				}
			}
		}

		if (alignment != Alignment.Left && indexOffset < verts.Count)
		{
			Align(verts, indexOffset, x - finalSpacingX);
			indexOffset = verts.Count;

			if (sverts != null)
			{
				Align(sverts, sIndexOffset, x - finalSpacingX);
				sIndexOffset = sverts.Count;
			}
		}
		mColors.Clear();
	}

	static float[] mBoldOffset = new float[]
	{
		-0.25f, 0f, 0.25f, 0f,
		0f, -0.25f, 0f, 0.25f
	};

	/// <summary>
	/// Print character positions and indices into the specified buffer. Meant to be used with the "find closest vertex" calculations.
	/// </summary>

	static public void PrintApproximateCharacterPositions (string text, List<Vector3> verts, List<int> indices)
	{
		if (string.IsNullOrEmpty(text)) text = " ";

		Prepare(text);
		mColors.Clear();

		float x = 0f, maxWidth = regionWidth + 0.01f;
		var y = Mathf.Round(spacingY * fontScale * 0.5f);
		int textLength = text.Length, indexOffset = verts.Count, ch = 0, prev = 0;

		int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var symbolScale = NGUIText.symbolScale;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var fontScaleMult = 0f;

		for (int i = 0; i < textLength; ++i)
		{
			ch = text[i];

			if (encoding && ParseSymbol(text, ref i, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
					ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				--i;
				continue;
			}

			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;
			var halfSize = scale * 0.5f;

			verts.Add(new Vector3(x, -y - halfSize));
			indices.Add(i);

			if (ch == '\n')
			{
				if (alignment != Alignment.Left)
				{
					Align(verts, indexOffset, x - finalSpacingX, 1);
					indexOffset = verts.Count;
				}

				x = 0;
				y += finalLineHeight;
				prev = 0;
				continue;
			}
			else if (ch < ' ')
			{
				prev = 0;
				continue;
			}

			// See if there is a symbol matching this text
			var symbol = useSymbols ? GetSymbol(ref text, i, textLength) : null;

			if (symbol == null)
			{
				var w = GetGlyphWidth(ch, prev, scale, bold, italic);

				if (w != 0f)
				{
					w += finalSpacingX;

					if (x + w > maxWidth)
					{
						if (x == 0f) return;

						if (alignment != Alignment.Left && indexOffset < verts.Count)
						{
							Align(verts, indexOffset, x - finalSpacingX, 1);
							indexOffset = verts.Count;
						}

						x = w;
						y += finalLineHeight;
					}
					else x += w;

					verts.Add(new Vector3(x, -y - halfSize));
					indices.Add(i + 1);
					prev = ch;
				}
			}
			else
			{
				var h = symbol.paddedHeight;
				if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
				float w = symbol.pixelPerfect ? symbol.advance + finalSpacingX : Mathf.Round(symbol.advance * scale * symbolScale + finalSpacingX);

				if (x + w > maxWidth)
				{
					if (x == 0f) return;

					if (alignment != Alignment.Left && indexOffset < verts.Count)
					{
						Align(verts, indexOffset, x - finalSpacingX, 1);
						indexOffset = verts.Count;
					}

					x = w;
					y += finalLineHeight;
				}
				else x += w;

				verts.Add(new Vector3(x, -y - halfSize));
				indices.Add(i + 1);
				i += symbol.sequence.Length - 1;
				prev = 0;
			}
		}

		if (alignment != Alignment.Left && indexOffset < verts.Count)
			Align(verts, indexOffset, x - finalSpacingX, 1);
	}

	/// <summary>
	/// Print character positions and indices into the specified buffer.
	/// This function's data is meant to be used for precise character selection, such as clicking on a link.
	/// There are 2 vertices for every index: Bottom Left + Top Right.
	/// </summary>

	static public void PrintExactCharacterPositions (string text, List<Vector3> verts, List<int> indices)
	{
		if (string.IsNullOrEmpty(text)) text = " ";

		Prepare(text);
		mColors.Clear();

		float x = 0f, maxWidth = regionWidth + 0.01f, fullSize = fontSize * fontScale;
		var y = Mathf.Round(spacingY * fontScale * 0.5f);
		int textLength = text.Length, indexOffset = verts.Count, ch = 0, prev = 0;

		int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var symbolScale = NGUIText.symbolScale;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var fontScaleMult = 0f;

		for (int i = 0; i < textLength; ++i)
		{
			ch = text[i];

			if (encoding && ParseSymbol(text, ref i, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
				ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				--i;
				continue;
			}

			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

			if (ch == '\n')
			{
				if (alignment != Alignment.Left)
				{
					Align(verts, indexOffset, x - finalSpacingX, 2);
					indexOffset = verts.Count;
				}

				x = 0;
				y += finalLineHeight;
				prev = 0;
				continue;
			}
			else if (ch < ' ')
			{
				prev = 0;
				continue;
			}

			// See if there is a symbol matching this text
			var symbol = useSymbols ? GetSymbol(ref text, i, textLength) : null;

			if (symbol == null)
			{
				var gw = GetGlyphWidth(ch, prev, scale, bold, italic);

				if (gw != 0f)
				{
					float w = gw + finalSpacingX;

					if (x + w > maxWidth)
					{
						if (x == 0f) return;

						if (alignment != Alignment.Left && indexOffset < verts.Count)
						{
							Align(verts, indexOffset, x - finalSpacingX, 2);
							indexOffset = verts.Count;
						}

						x = 0f;
						y += finalLineHeight;
						prev = 0;
						--i;
						continue;
					}

					indices.Add(i);
					verts.Add(new Vector3(x, -y - fullSize));
					verts.Add(new Vector3(x + w, -y));
					prev = ch;
					x += w;
				}
			}
			else
			{
				var h = symbol.paddedHeight;
				if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
				float w = symbol.pixelPerfect ? symbol.advance + finalSpacingX : Mathf.Round(symbol.advance * scale * symbolScale + finalSpacingX);

				if (x + w > maxWidth)
				{
					if (x == 0f) return;

					if (alignment != Alignment.Left && indexOffset < verts.Count)
					{
						Align(verts, indexOffset, x - finalSpacingX, 2);
						indexOffset = verts.Count;
					}

					x = 0f;
					y += finalLineHeight;
					prev = 0;
					--i;
					continue;
				}

				indices.Add(i);
				verts.Add(new Vector3(x, -y - fullSize));
				verts.Add(new Vector3(x + w, -y));
				i += symbol.sequence.Length - 1;
				x += w;
				prev = 0;
			}
		}

		if (alignment != Alignment.Left && indexOffset < verts.Count)
			Align(verts, indexOffset, x - finalSpacingX, 2);
	}

	/// <summary>
	/// Print the caret and selection vertices. Note that it's expected that 'text' has been stripped clean of symbols.
	/// </summary>

	static public void PrintCaretAndSelection (string text, int start, int end, List<Vector3> caret, List<Vector3> highlight)
	{
		if (string.IsNullOrEmpty(text)) text = " ";

		Prepare(text);
		mColors.Clear();

		int caretPos = end;

		if (start > end)
		{
			end = start;
			start = caretPos;
		}

		float x = 0f, fs = fontSize * fontScale;
		var y = Mathf.Round(spacingY * fontScale * 0.5f);
		int caretOffset = (caret != null) ? caret.Count : 0;
		int highlightOffset = (highlight != null) ? highlight.Count : 0;
		int textLength = text.Length, index = 0, ch = 0, prev = 0;
		bool highlighting = false, caretSet = false;

		int sub = 0;  // 0 = normal, 1 = subscript, 2 = superscript
		var bold = false;
		var italic = false;
		var underline = false;
		var strikethrough = false;
		var ignoreColor = false;
		var forceSpriteColor = false;
		var symbolScale = NGUIText.symbolScale;
		var symbolMaxHeight = NGUIText.symbolMaxHeight;
		var fontScaleMult = 0f;

		Vector2 last0 = Vector2.zero;
		Vector2 last1 = Vector2.zero;

		for (; index < textLength; ++index)
		{
			if (encoding && ParseSymbol(text, ref index, mColors, premultiply, ref sub, ref fontScaleMult, ref bold,
						ref italic, ref underline, ref strikethrough, ref ignoreColor, ref forceSpriteColor))
			{
				--index;
				continue;
			}

			var scale = (sub == 0) ? (fontScaleMult == 0f ? fontScale : fontScale * fontScaleMult) : fontScale * fontScaleMult;

			// Print the caret
			if (caret != null && !caretSet && caretPos <= index)
			{
				caretSet = true;
				caret.Add(new Vector3(x - 1f, -y - fs));
				caret.Add(new Vector3(x - 1f, -y));
				caret.Add(new Vector3(x + 1f, -y));
				caret.Add(new Vector3(x + 1f, -y - fs));
			}

			ch = text[index];

			if (ch == '\n')
			{
				// Align the caret
				if (caret != null && caretSet)
				{
					if (alignment != Alignment.Left) Align(caret, caretOffset, x - finalSpacingX);
					caret = null;
				}

				if (highlight != null)
				{
					if (highlighting)
					{
						// Close the selection on this line
						highlighting = false;
						highlight.Add(last1);
						highlight.Add(last0);
					}
					else if (start <= index && end > index)
					{
						// This must be an empty line. Add a narrow vertical highlight.
						highlight.Add(new Vector3(x, -y - fs));
						highlight.Add(new Vector3(x, -y));
						highlight.Add(new Vector3(x + 2f, -y));
						highlight.Add(new Vector3(x + 2f, -y - fs));
					}

					// Align the highlight
					if (alignment != Alignment.Left && highlightOffset < highlight.Count)
					{
						Align(highlight, highlightOffset, x - finalSpacingX);
						highlightOffset = highlight.Count;
					}
				}

				x = 0;
				y += finalLineHeight;
				prev = 0;
				continue;
			}
			else if (ch < ' ')
			{
				prev = 0;
				continue;
			}

			// See if there is a symbol matching this text
			var symbol = useSymbols ? GetSymbol(ref text, index, textLength) : null;
			float w;

			if (symbol != null)
			{
				var h = symbol.paddedHeight;
				if (!symbol.pixelPerfect && symbolMaxHeight != 0 && h > symbolMaxHeight) scale *= (float)symbolMaxHeight / h;
				w = symbol.pixelPerfect ? symbol.advance : Mathf.Round(symbol.advance * scale * symbolScale);
			}
			else w = GetGlyphWidth(ch, prev, scale, bold, italic);

			if (w != 0f)
			{
				float v0x = x;
				float v1x = x + w;
				float v0y = -y - fs;
				float v1y = -y;

				if (v1x + finalSpacingX > regionWidth)
				{
					if (x == 0f) return;

					// Align the caret
					if (caret != null && caretSet)
					{
						if (alignment != Alignment.Left) Align(caret, caretOffset, x - finalSpacingX);
						caret = null;
					}

					if (highlight != null)
					{
						if (highlighting)
						{
							// Close the selection on this line
							highlighting = false;
							highlight.Add(last1);
							highlight.Add(last0);
						}
						else if (start <= index && end > index)
						{
							// This must be an empty line. Add a narrow vertical highlight.
							highlight.Add(new Vector3(x, -y - fs));
							highlight.Add(new Vector3(x, -y));
							highlight.Add(new Vector3(x + 2f, -y));
							highlight.Add(new Vector3(x + 2f, -y - fs));
						}

						// Align the highlight
						if (alignment != Alignment.Left && highlightOffset < highlight.Count)
						{
							Align(highlight, highlightOffset, x - finalSpacingX);
							highlightOffset = highlight.Count;
						}
					}

					v0x -= x;
					v1x -= x;
					v0y -= finalLineHeight;
					v1y -= finalLineHeight;

					x = 0;
					y += finalLineHeight;
				}

				x += w + finalSpacingX;

				// Print the highlight
				if (highlight != null)
				{
					if (start > index || end <= index)
					{
						if (highlighting)
						{
							// Finish the highlight
							highlighting = false;
							highlight.Add(last1);
							highlight.Add(last0);
						}
					}
					else if (!highlighting)
					{
						// Start the highlight
						highlighting = true;
						highlight.Add(new Vector3(v0x, v0y));
						highlight.Add(new Vector3(v0x, v1y));
					}
				}

				// Save what the character ended with
				last0 = new Vector2(v1x, v0y);
				last1 = new Vector2(v1x, v1y);
				prev = ch;
			}
		}

		// Ensure we always have a caret
		if (caret != null)
		{
			if (!caretSet)
			{
				caret.Add(new Vector3(x - 1f, -y - fs));
				caret.Add(new Vector3(x - 1f, -y));
				caret.Add(new Vector3(x + 1f, -y));
				caret.Add(new Vector3(x + 1f, -y - fs));
			}

			if (alignment != Alignment.Left)
				Align(caret, caretOffset, x - finalSpacingX);
		}

		// Close the selection
		if (highlight != null)
		{
			if (highlighting)
			{
				// Finish the highlight
				highlight.Add(last1);
				highlight.Add(last0);
			}
			else if (start < index && end == index)
			{
				// Happens when highlight ends on an empty line. Highlight it with a thin line.
				highlight.Add(new Vector3(x, -y - fs));
				highlight.Add(new Vector3(x, -y));
				highlight.Add(new Vector3(x + 2f, -y));
				highlight.Add(new Vector3(x + 2f, -y - fs));
			}

			// Align the highlight
			if (alignment != Alignment.Left && highlightOffset < highlight.Count)
				Align(highlight, highlightOffset, x - finalSpacingX);
		}

		mColors.Clear();
	}

	/// <summary>
	/// Replace the specified link.
	/// </summary>

	static public bool ReplaceLink (ref string text, ref int index, string type, string prefix = null, string suffix = null)
	{
		if (index == -1) return false;
		index = text.IndexOf(type, index);
		if (index == -1) return false;

		if (index > 5)
		{
			var offset = index - 5;

			while (offset >= 0)
			{
				if (text[offset] == '[')
				{
					if (text[offset + 1] == 'u' && text[offset + 2] == 'r' && text[offset + 3] == 'l' && text[offset + 4] == '=')
					{
						index += type.Length;
						return ReplaceLink(ref text, ref index, type, prefix, suffix);
					}
					else if (text[offset + 1] == '/' && text[offset + 2] == 'u' && text[offset + 3] == 'r' && text[offset + 4] == 'l')
					{
						break;
					}
				}

				--offset;
			}
		}

		int domainStart = index + type.Length;
		int end = text.IndexOfAny(new char[] { ' ', '\n', (char)0x200a, (char)0x200b, '\u2009' }, domainStart);
		if (end == -1) end = text.Length;

		int domainEnd = text.IndexOfAny(new char[] { '/', ' ' }, domainStart);

		if (domainEnd == -1 || domainEnd == domainStart)
		{
			index += type.Length;
			return true;
		}

		string left = text.Substring(0, index);
		string link = text.Substring(index, end - index);
		string right = text.Substring(end);
		string urlName = text.Substring(domainStart, domainEnd - domainStart);

		if (!string.IsNullOrEmpty(prefix)) left += prefix;

		text = left + "[url=" + link + "][u]" + urlName + "[/u][/url]";
		index = text.Length;
		if (string.IsNullOrEmpty(suffix)) text += right;
		else text = text + suffix + right;
		return true;
	}

	/// <summary>
	/// Insert a hyperlink around the specified keyword.
	/// </summary>

	static public bool InsertHyperlink (ref string text, ref int index, string keyword, string link, string prefix = null, string suffix = null)
	{
		int patchStart = text.IndexOf(keyword, index, System.StringComparison.CurrentCultureIgnoreCase);
		if (patchStart == -1) return false;

		if (patchStart > 5)
		{
			var offset = patchStart - 5;

			while (offset >= 0)
			{
				if (text[offset] == '[')
				{
					if (text[offset + 1] == 'u' && text[offset + 2] == 'r' && text[offset + 3] == 'l' && text[offset + 4] == '=')
					{
						index = patchStart + keyword.Length;
						return InsertHyperlink(ref text, ref index, keyword, link, prefix, suffix);
					}
					else if (text[offset + 1] == '/' && text[offset + 2] == 'u' && text[offset + 3] == 'r' && text[offset + 4] == 'l')
					{
						break;
					}
				}

				--offset;
			}
		}

		string left = text.Substring(0, patchStart);
		string url = "[url=" + link + "][u]";
		string middle = text.Substring(patchStart, keyword.Length);

		if (!string.IsNullOrEmpty(prefix)) middle = prefix + middle;
		if (!string.IsNullOrEmpty(suffix)) middle += suffix;

		string right = text.Substring(patchStart + keyword.Length);

		text = left + url + middle + "[/u][/url]";
		index = text.Length;
		text += right;
		return true;
	}

	/// <summary>
	/// Helper function that replaces links within text with clickable ones.
	/// </summary>

	static public void ReplaceLinks (ref string text, string prefix = null, string suffix = null)
	{
		for (int index = 0; index < text.Length; )
		{
			if (!ReplaceLink(ref text, ref index, "http://", prefix, suffix)) break;
		}

		for (int index = 0; index < text.Length; )
		{
			if (!ReplaceLink(ref text, ref index, "https://", prefix, suffix)) break;
		}
	}
}
