//
// GtkHelper.cs: Some random code converted from C (Part of GTK+)
// 
// Authors:
// 	Eric Butler <eric@extermeboredom.net>
//
// Copyright (C) 2005 Meshwork Authors
// 

using System;

namespace Meshwork.Client.GtkClient
{
	public static class GtkHelper 
	{
		static void
			gtk_hls_to_rgb (ref double h,
				        ref double l,
				        ref double s)
		{
			double hue;
			double lightness;
			double saturation;
			double m1, m2;
			double r, g, b;
			
			lightness = l;
			saturation = s;
			
			if (lightness <= 0.5)
				m2 = lightness * (1 + saturation);
			else
				m2 = lightness + saturation - lightness * saturation;
			m1 = 2 * lightness - m2;
			
			if (saturation == 0) {
				h = lightness;
				l = lightness;
				s = lightness;
			} else {
				hue = h + 120;
				while (hue > 360)
					hue -= 360;
				while (hue < 0)
					hue += 360;
				
				if (hue < 60)
					r = m1 + (m2 - m1) * hue / 60;
				else if (hue < 180)
					r = m2;
				else if (hue < 240)
					r = m1 + (m2 - m1) * (240 - hue) / 60;
				else
					r = m1;
				
				hue = h;
				while (hue > 360)
					hue -= 360;
				while (hue < 0)
					hue += 360;
				
				if (hue < 60)
					g = m1 + (m2 - m1) * hue / 60;
				else if (hue < 180)
					g = m2;
				else if (hue < 240)
					g = m1 + (m2 - m1) * (240 - hue) / 60;
				else
					g = m1;
				
				hue = h - 120;
				while (hue > 360)
					hue -= 360;
				while (hue < 0)
					hue += 360;
				
				if (hue < 60)
					b = m1 + (m2 - m1) * hue / 60;
				else if (hue < 180)
					b = m2;
				else if (hue < 240)
					b = m1 + (m2 - m1) * (240 - hue) / 60;
				else
					b = m1;
				
				h = r;
				l = g;
				s = b;
			}
		}

		static void
		gtk_rgb_to_hls (ref double r,
				    ref double g,
				    ref double b)
		{
			double min;
			double max;
			double red;
			double green;
			double blue;
			double h, l, s;
			double delta;
			
			red = r;
			green = g;
			blue = b;
		  
			if (red > green) {
				if (red > blue)
					max = red;
				else
					max = blue;
				
				if (green < blue)
					min = green;
				else
					min = blue;
			} else {
				if (green > blue)
					max = green;
				else
					max = blue;
				
				if (red < blue)
					min = red;
				else
					min = blue;
			}
			
			l = (max + min) / 2;
			s = 0;
			h = 0;
			
			if (max != min) {
				if (l <= 0.5)
					s = (max - min) / (max + min);
				else
					s = (max - min) / (2 - max - min);
				
				delta = max -min;
				if (red == max)
					h = (green - blue) / delta;
				else if (green == max)
					h = 2 + (blue - red) / delta;
				else if (blue == max)
					h = 4 + (red - green) / delta;
				
				h *= 60;
				if (h < 0.0)
					h += 360;
			}
			
			r = h;
			g = l;
			b = s;
		}
		
		static void gtk_style_shade (ref Gdk.Color a,
						  ref Gdk.Color b,
						  double k)
		{
			double red;
			double green;
			double blue;

			red = (double) a.Red / 65535.0;
			green = (double) a.Green / 65535.0;
			blue = (double) a.Blue  / 65535.0;

			gtk_rgb_to_hls (ref red, ref green, ref blue);

			green *= k;
			if (green > 1.0)
				green = 1.0;
			else if (green < 0.0)
				green = 0.0;

			blue *= k;
			if (blue > 1.0)
				blue = 1.0;
			else if (blue < 0.0)
				blue = 0.0;

			gtk_hls_to_rgb (ref red, ref green, ref blue);

			b.Red = Convert.ToUInt16 (red * 65535.0);
			b.Green = Convert.ToUInt16 (green * 65535.0);
			b.Blue = Convert.ToUInt16 (blue * 65535.0);
		}
		
		public static void
		DarkenColor (ref Gdk.Color color, 
			     int darken_count)
		{
			color = DarkenColor (color, darken_count);
		}

		public static Gdk.Color
		DarkenColor (Gdk.Color color,
		             int darken_count)
		{
			Gdk.Color src = color;
			Gdk.Color shaded = Gdk.Color.Zero;

			while (darken_count > 0) {
				gtk_style_shade (ref src, ref shaded, 0.93);
				src = shaded;
				darken_count--;
			}
			return shaded;
		}
		public static Gdk.GC GetDarkenedGC (Gdk.Window window,
						    Gdk.Color color,
						    int darken_count)
		{
			DarkenColor (ref color, darken_count);
			Gdk.GC gc = new Gdk.GC (window);
			gc.RgbFgColor = color;
			return gc;
		}
	}
}
