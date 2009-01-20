/***************************************************************************
 *  FadingAlignment.cs
 *
 *  Copyright (C) 2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using Cairo;
using Gtk;

namespace Banshee.Widgets
{
    public class FadingAlignment : Alignment
    {
        private LinearGradient bg_gradient;
        private LinearGradient line_gradient;
        private Color fill_color_a;
        private Color fill_color_b;
        private Color fill_color_c;
        private Color fill_color_d;
        
        public FadingAlignment(float xalign, float yalign, float xpadding, float ypadding) 
            : base(xalign, yalign, xpadding, ypadding)
        {
        }
        
        public FadingAlignment() : base(0.0f, 0.5f, 1.0f, 0.0f)
        {
            AppPaintable = true;
        }
        
        protected override void OnStyleSet(Gtk.Style style)
        {
            base.OnStyleSet(style);
            
            fill_color_a = DrawingUtilities.GdkColorToCairoColor(Style.Background(StateType.Normal));
            fill_color_b = DrawingUtilities.GdkColorToCairoColor(Style.Mid(StateType.Normal));
            fill_color_b.A = 0.75;
                
            fill_color_c = DrawingUtilities.GdkColorToCairoColor(Style.Background(StateType.Normal));
            fill_color_d = DrawingUtilities.GdkColorToCairoColor(Style.Dark(StateType.Normal));
        }
        
        protected override void OnSizeAllocated(Gdk.Rectangle rect)
        {
            bg_gradient = new Cairo.LinearGradient(rect.X, rect.Y, rect.X, rect.Y + rect.Height);
            bg_gradient.AddColorStop(0, fill_color_a);
            bg_gradient.AddColorStop(0.9, fill_color_b);
            
            line_gradient = new Cairo.LinearGradient(rect.X, rect.Y, rect.X, rect.Y + rect.Height);
            line_gradient.AddColorStop(0.3, fill_color_c);
            line_gradient.AddColorStop(1, fill_color_d);
            
            base.OnSizeAllocated(rect);
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsRealized) {
                return false;
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create(GdkWindow);
            Draw(cr);
            ((IDisposable)cr).Dispose();
				
            return base.OnExposeEvent(evnt);
        }
        
        private void Draw(Cairo.Context cr)
        {
            cr.Pattern = bg_gradient;
            
            cr.Rectangle(Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
            cr.Fill();
            
	    /*
            cr.Antialias = Antialias.None;
            cr.LineWidth = 1.0;
         
            cr.Pattern = line_gradient;
            
            cr.MoveTo(Allocation.X + 1, Allocation.Y);
            cr.LineTo(Allocation.X + 1, Allocation.Y + Allocation.Height);
            cr.Stroke();
	    */
        }
    }
}
