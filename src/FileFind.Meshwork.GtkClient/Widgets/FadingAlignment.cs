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
        private Color fill_color_a;
        private Color fill_color_b;
        
        public FadingAlignment(float xalign, float yalign, float xpadding, float ypadding) 
            : base(xalign, yalign, xpadding, ypadding)
        {
			AppPaintable = true;
        }
        
        public FadingAlignment() : this(0.0f, 0.5f, 1.0f, 0.0f)
        {            
        }
        
        protected override void OnStyleSet(Gtk.Style style)
        {
            base.OnStyleSet(style);
            
            fill_color_a = DrawingUtilities.GdkColorToCairoColor(Style.Background(StateType.Normal));
            
			var c = FileFind.Meshwork.GtkClient.GtkHelper.DarkenColor(Style.Background(StateType.Normal), 1);
			fill_color_b = DrawingUtilities.GdkColorToCairoColor(c);
        }
        
        protected override void OnSizeAllocated(Gdk.Rectangle rect)
        {
			bg_gradient = new Cairo.LinearGradient(rect.X, rect.Y, rect.X, rect.Y + rect.Height);
            bg_gradient.AddColorStop(0, fill_color_a);
            bg_gradient.AddColorStop(0.9, fill_color_b);
			    
            base.OnSizeAllocated(rect);
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsRealized) {
                return false;
            }
						
			Gdk.Rectangle rect = this.Allocation;
			
            using (Cairo.Context cr = Gdk.CairoHelper.Create(GdkWindow)) {            			
				cr.Pattern = bg_gradient;			
				cr.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
				cr.Fill();
			}			
			
			Gtk.Style.PaintHline(base.Style, base.GdkWindow, StateType.Normal, evnt.Area, this, 
			                     "hseparator", rect.X, rect.X + rect.Width - 1, rect.Y + rect.Height - 1);			
				
            return base.OnExposeEvent(evnt);
        }
    }
}
