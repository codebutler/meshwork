/*
 * Copyright (c) 2006 Idan Gazit <idan AT fastmail DOT fm>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included 
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using Cairo;
using Gdk;
using Gtk;

namespace Meshwork.Client.GtkClient.Widgets
{
	public class ZoomableCairoArea : DrawingArea {
		
		bool debug = false;

		private static Gdk.Pixbuf arrowUp, arrowDown, arrowLeft, arrowRight, zoomGlass;
		
		static ZoomableCairoArea ()
		{			
			arrowUp    = Gui.LoadIcon(22, "go-up");
			arrowDown  = Gui.LoadIcon(22, "go-down");
			arrowLeft  = Gui.LoadIcon(22, "go-previous");
			arrowRight = Gui.LoadIcon(22, "go-next");
			zoomGlass  = new Pixbuf (null, "Meshwork.Client.GtkClient.zoom.png");
		}
		
		private double mousePressedX, mousePressedY;
		private double mousePressedUserX, mousePressedUserY;
		private double currentMouseX, currentMouseY;
		private double lastMouseX, lastMouseY;
		private uint firstPressedButton;
		private double scrollUnits;
		private Cairo.Matrix transformMatrix;
		private bool drawZoomScale;
		private bool drawMotionIndicators;
		private bool mouseInWidget;
		private bool isTranslating, isScaling, isScrollWheelScaling;
		private bool[] pressedButtons;
		private const uint BUTTON_TRANSLATE = 2;
		private const uint BUTTON_ZOOM = 3;
		private Cairo.Color background;
		
		protected delegate void DrawGraphics (Cairo.Context gc);
		protected DrawGraphics render;
		protected DrawGraphics overlay;
		
		
		private double maxScale, minScale;
		
		public ZoomableCairoArea () {
			this.mousePressedX = -1;
			this.mousePressedY = -1;
			this.currentMouseX = -1;
			this.currentMouseY = -1;
			this.lastMouseX = -1;
			this.lastMouseY = -1;
			this.mousePressedUserX = -1;
			this.mousePressedUserY = -1;
			this.firstPressedButton = 0;
			this.scrollUnits = 0.1;
			
			this.maxScale = 1.0;
			this.minScale = 0.1;
			
			this.transformMatrix = new Matrix ();
			this.background = new Cairo.Color (0.3, 0.3, 0.3, 1.0);
			
			this.mouseInWidget = false;
			this.pressedButtons = new bool[] {false, false, false};
			this.isScaling = false;
			this.isTranslating = false;
			this.isScrollWheelScaling = false;
			this.drawZoomScale = true;
			
			this.Events = this.Events |
				Gdk.EventMask.ButtonPressMask |
				Gdk.EventMask.ButtonReleaseMask |
				Gdk.EventMask.PointerMotionMask |
				Gdk.EventMask.EnterNotifyMask |
				Gdk.EventMask.LeaveNotifyMask |
				Gdk.EventMask.ScrollMask;

			this.CanFocus = true;
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll es)
		{
			bool result = base.OnScrollEvent (es);
			if (!this.isScrollWheelScaling) {	
				this.isScrollWheelScaling = true;
				double mouseX = es.X;
				double mouseY = es.Y;
				DeviceToUser (ref mouseX, ref mouseY);
				this.mousePressedUserX = mouseX;
				this.mousePressedUserY = mouseY;
			}
			double scaleAdj = 1.0;
			if (es.Direction == ScrollDirection.Down) {
				scaleAdj -= this.scrollUnits;
			} else if (es.Direction == ScrollDirection.Up) {
				scaleAdj += this.scrollUnits;
			}
			ValidateScaleAdj (ref scaleAdj);
			if (scaleAdj != 1.0) {
				this.transformMatrix.Translate (mousePressedUserX, mousePressedUserY);
				this.transformMatrix.Scale (scaleAdj, scaleAdj);
				this.transformMatrix.Translate (-mousePressedUserX, -mousePressedUserY);
			}
			this.QueueDraw ();
			return result;
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing ec)
		{
			bool result = base.OnEnterNotifyEvent (ec);
			this.mouseInWidget = true;
			return result;
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ec)
		{
			bool result = base.OnLeaveNotifyEvent (ec);
			this.mouseInWidget = false;
			this.QueueDraw ();
			return result;
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion em)
		{
			bool result = base.OnMotionNotifyEvent (em);
		//	if (debug) Console.Out.WriteLine ("ZoomableCairoArea: OnMotionNotifyEvent at {0}, {1}", em.X, em.Y);
			this.currentMouseX = em.X;
			this.currentMouseY = em.Y;
			this.isScrollWheelScaling = false;
			if (this.isTranslating) {
				double deltaX, deltaY;
				deltaX = this.currentMouseX - this.lastMouseX;
				deltaY = this.currentMouseY - this.lastMouseY;
				this.transformMatrix.Translate (deltaX/this.ScaleFactor, deltaY/this.ScaleFactor);
			} else if (this.isScaling) {
				double scaleAdj = 1.0 - ((this.currentMouseY - this.lastMouseY) * 0.005);				
				ValidateScaleAdj (ref scaleAdj);
				this.transformMatrix.Translate (mousePressedUserX, mousePressedUserY);
				this.transformMatrix.Scale (scaleAdj, scaleAdj);
				this.transformMatrix.Translate (-mousePressedUserX, -mousePressedUserY);
			}
			this.lastMouseX = this.currentMouseX;
			this.lastMouseY = this.currentMouseY;
			this.QueueDraw();
			return result;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton eb)
		{
			bool result = base.OnButtonPressEvent (eb);
			base.GrabFocus ();

			if (eb.Button > pressedButtons.Length) return result;

			// We can't always rely on OnButtonReleaseEvent,
			// unfortunetly.
			for (int x = 0; x < pressedButtons.Length; x ++) {
				pressedButtons[x] = false;
			}

			if (debug) Console.Out.WriteLine ("ZoomableCairoArea: OnButtonPressEvent at {0}, {1}", eb.X, eb.Y);
			if (eb.Type == Gdk.EventType.ThreeButtonPress) {
				this.transformMatrix.InitIdentity ();
				this.QueueDraw ();
			} else if (!this.AreButtonsPressed) {
				//if (debug) Console.Out.WriteLine ("Button Pressed: {0}", eb.Button);
				pressedButtons[eb.Button - 1] = true;
				this.firstPressedButton = eb.Button;
				this.mousePressedX = eb.X;
				this.mousePressedY = eb.Y;
				this.lastMouseX = eb.X;
				this.lastMouseY = eb.Y;
				
				double mouseX = eb.X;
				double mouseY = eb.Y;
				DeviceToUser (ref mouseX, ref mouseY);
				this.mousePressedUserX = mouseX;
				this.mousePressedUserY = mouseY;
				
				if (this.firstPressedButton == BUTTON_TRANSLATE) {
					if ((eb.State & ModifierType.ControlMask) != 0)
						this.isScaling = true;
					else
						this.isTranslating = true;

					this.QueueDraw ();
				}
			}	
			return result;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton eb)
		{
			bool result = base.OnButtonReleaseEvent (eb);
			if (debug) Console.Out.WriteLine ("ZoomableCairoArea: OnButtonReleaseEvent at {0}, {1}", eb.X, eb.Y);
			if (eb.Button == this.firstPressedButton) {
				
				this.isTranslating = false;
				this.isScaling = false;
				if (eb.Button > pressedButtons.Length) return result;
				pressedButtons[eb.Button - 1] = false;
				this.firstPressedButton = 0;
				this.mousePressedX = -1;
				this.mousePressedY = -1;
				this.lastMouseX = -1;
				this.lastMouseY = -1;
				this.QueueDraw ();
			}
			return result;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ee)
		{	
			// get dimensions
			int width = this.Allocation.Width;
			int height = this.Allocation.Height;
			double centerx = (double) width / 2.0;
			double centery = (double) height / 2.0;
			
			// init cairo or die.
			Cairo.Context gc = Gdk.CairoHelper.Create (this.GdkWindow);
			
			// paint widget background
			gc.Rectangle (0, 0, width, height);
			gc.Color = background;
			gc.FillPreserve ();
			gc.Clip ();
			
			gc.NewPath ();
			gc.Save ();			
			gc.Matrix = transformMatrix;
			
			// draw the graphics.
			if (render != null)
				render (gc);
			
			gc.Restore ();
			
			if (overlay != null)
				overlay (gc);
			
			PaintZoomScale (gc, width, height, centerx, centery);
			PaintMotionIndicators (gc, width, height, centerx, centery);
			
			((IDisposable) gc.Target).Dispose ();
			((IDisposable) gc).Dispose ();
			return (true);
		}
		
		private void PaintMotionIndicators (Cairo.Context gc, double width, double height, double centerx, double centery)
		{
			if (this.isTranslating) {
				
				gc.Save ();
				gc.Matrix.InitIdentity ();
				
				
				RenderPixbufToSurf (gc, arrowUp,
				                        (width / 2.0) - (arrowUp.Width / 2.0), 10);
				RenderPixbufToSurf (gc, arrowDown,
				                        (width / 2.0) - (arrowDown.Width / 2.0), (height - 10 - arrowDown.Height));
				RenderPixbufToSurf (gc, arrowRight,
				                        (width - 10 - arrowRight.Width) + 0.5, (height / 2.0) - (arrowRight.Height / 2.0));
				RenderPixbufToSurf (gc, arrowLeft,
				                        10, (height / 2.0) - (arrowRight.Height / 2.0));
				
				
				gc.Restore ();
				return;
			}
			
			if (this.isScaling) {
				gc.Save ();
				
				CreateRoundedRectPath (gc, width - 104, 4, 100, 110, 30);
				gc.Color = new Cairo.Color (0,0,0, 0.5);
				gc.Fill ();
				RenderPixbufToSurf (gc, zoomGlass, width-102, 6);
								
				Pango.Layout layout = new Pango.Layout (this.PangoContext);
				layout.FontDescription = this.PangoContext.FontDescription.Copy ();
				layout.FontDescription.Size = Pango.Units.FromDouble (10.0);
				layout.SetText ("Zooming");
				
				Size te = GetTextSize(layout);
				gc.MoveTo (width - 52 - (te.Width / 2.0), 109);
				gc.Color = new Cairo.Color (1,1,1,0.7);
				
				Pango.CairoHelper.ShowLayout (gc, layout);
				
				gc.Restore ();
				return;
			}
		}
		
		
		
		private void PaintZoomScale (Cairo.Context gc, double width, double height, double centerx, double centery)
		{
			if (this.drawZoomScale) {
				gc.Save ();
				string s = string.Format ("x:{0:##0.0} y:{1:##0.0} *:{2:#0.0#}", 
			                             this.TranslatedX,
			                             this.TranslatedY,
			                             this.ScaleFactor);
				
				int textWidth, textHeight;
				
				Pango.Layout layout = new Pango.Layout (this.PangoContext);
				layout.FontDescription = this.PangoContext.FontDescription.Copy ();
				layout.FontDescription.Size = Pango.Units.FromDouble (12.0);
				layout.SetText (s);
				Size te = GetTextSize(layout);
			
				gc.Color = new Cairo.Color (0.0, 0.0, 0.0, 0.6);
				gc.Rectangle (width - 7.5d - te.Width, height - te.Height - 5.5d, 7.5d + te.Width, 5.5d + te.Height);
				gc.Fill ();
				gc.MoveTo (width - 4.5d - te.Width, height - te.Height - 4.5d);
				gc.Color = new Cairo.Color (1.0, 1.0, 1.0, 0.6);				
				
				Pango.CairoHelper.ShowLayout (gc, layout);
				
				gc.Fill ();
				gc.Restore ();
			}
		}

		
		
		private bool AreButtonsPressed {
			get {
				foreach (bool b in pressedButtons)
					if (b) return (true);
				// none are pressed
				return (false);
			}
		}
		
		public void DeviceToUser (ref double x, ref double y)
		{	   	
			Cairo.Matrix inv = (Cairo.Matrix) this.transformMatrix.Clone ();
			Cairo.Status cs = inv.Invert ();
			if (cs == Cairo.Status.Success)
				inv.TransformPoint (ref x, ref y);
			else
				throw new Exception ("Unable to transform device coordinates to user coordinates because the matrix was uninvertible (" + cs.ToString () + ").");
		}
		
		public double ScaleFactor {
			get {return this.transformMatrix.Xx;}
		}
		
		public double TranslatedX {
			get {return this.transformMatrix.X0;}
		}
		
		public double TranslatedY {
			get {return this.transformMatrix.Y0;}
		}
		
		public double ScrollUnits {
			get {return this.scrollUnits;}
			set {this.scrollUnits = value;}
		}
		
		public bool DrawZoomScale {
			get {return this.drawZoomScale;}
			set {this.drawZoomScale = value;}
		}
		
		public bool DrawMotionIndicators {
			get {return this.drawMotionIndicators;}
			set {this.drawMotionIndicators = value;}
		}

		
		private void ValidateScaleAdj (ref double scaleAdj) {
			if (this.ScaleFactor == this.minScale && scaleAdj < 1.0)
				scaleAdj = 1.0;
			else if ((this.ScaleFactor * scaleAdj) < minScale)
				scaleAdj = (minScale / this.ScaleFactor);
			else if (this.ScaleFactor == this.maxScale && scaleAdj > 1.0)
				scaleAdj = 1.0;
			else if ((this.ScaleFactor * scaleAdj) > maxScale)
				scaleAdj = (maxScale / this.ScaleFactor);
		}
		
		public Cairo.Color BackgroundColor {
			get {return this.background;}
			set {
				this.background = value;
				this.QueueDraw();
			}
		}
		
		public double MaxScale {
			get {return this.maxScale;}
			set {
				this.maxScale = value;
				if (this.ScaleFactor > maxScale) {
					double fixScale = maxScale / this.ScaleFactor;
					transformMatrix.Scale (fixScale, fixScale);
					this.QueueDraw ();
				}
			}
		}
		
		public double MinScale {
			get {return this.minScale;}
			set {
				this.minScale = value;
				if (this.ScaleFactor < minScale) {
					double fixScale = minScale / this.ScaleFactor;
					transformMatrix.Scale (fixScale, fixScale);
					this.QueueDraw ();
				}
			}
		}
		
		
		public static double GridAlign (double d) {
			return Math.Floor (d) + 0.5d;
		}
		
		public void CreateRoundedRectPath (Context gc, double x, double y, double width, double height, double radius)
		{	
			double x1, y1;
			x1 = x + width;
			y1 = y + height;
			if (width / 2 < radius) {
				if (height / 2 < radius) {
					gc.MoveTo (x, (y + y1) / 2);
					gc.CurveTo (x ,y, x, y, (x + x1) / 2, y);
					gc.CurveTo (x1, y, x1, y, x1, (y + y1) / 2);
					gc.CurveTo (x1, y1, x1, y1, (x1 + x) / 2, y1);
					gc.CurveTo (x, y1, x, y1, x, (y + y1) / 2);
				} else {
					gc.MoveTo (x, y + radius);
					gc.CurveTo (x, y, x, y, (x + x1) / 2, y);
					gc.CurveTo (x1, y, x1, y, x1, y + radius);
					gc.LineTo (x1 , y1 - radius);
					gc.CurveTo (x1, y1, x1, y1, (x1 + x) / 2, y1);
					gc.CurveTo (x, y1, x, y1, x, y1 - radius);
				}
			} else {
				if (height / 2 < radius) {
					gc.MoveTo (x, (y + y1) / 2);
					gc.CurveTo (x , y, x, y, x + radius, y);
					gc.LineTo (x1 - radius, y);
					gc.CurveTo (x1, y, x1, y, x1, (y + y1) / 2);
					gc.CurveTo (x1, y1, x1, y1, x1 - radius, y1);
					gc.LineTo (x + radius, y1);
					gc.CurveTo (x, y1, x, y1, x, (y + y1) / 2);
				} else {
					gc.MoveTo (x, y + radius);
					gc.CurveTo (x , y, x , y, x + radius, y);
					gc.LineTo (x1 - radius, y);
					gc.CurveTo (x1, y, x1, y, x1, y + radius);
					gc.LineTo (x1, y1 - radius);
					gc.CurveTo (x1, y1, x1, y1, x1 - radius, y1);
					gc.LineTo (x + radius, y1);
					gc.CurveTo (x, y1, x, y1, x, y1 - radius);
				}
			}
			gc.ClosePath ();
		}
		
		public void DrawRoundedRect (Context gc, double x, double y, double width,
						double height, double radius, double strokeWidth,
						Cairo.Color backgroundColor, Cairo.Color strokeColor)
						{
			gc.Save ();
			this.CreateRoundedRectPath (gc, x, y, width, height, radius);
			gc.LineWidth = strokeWidth;
			gc.Color = backgroundColor;
			gc.FillPreserve ();
			gc.Color = strokeColor;
			gc.Stroke ();
			gc.Restore ();							
		}
		
		public void DrawRoundedRectThumb (Context gc, double x, double y, double width,
						double height, double radius, double strokeWidth,
						Cairo.Color backgroundColor, Cairo.Color strokeColor,
						Gdk.Pixbuf image, double imageWidth, double imageHeight)
		{	
			gc.Save ();
			double imagex, imagey;
			//double x1 = x + width;
			//double y1 = y + height;
			double pixbufScale;
			if (image.Width > image.Height)
				pixbufScale = imageWidth / image.Width;
			else
				pixbufScale = imageHeight / image.Height;
			Gdk.Pixbuf scaledImage = image.ScaleSimple ((int)Math.Round (image.Width * pixbufScale),
									(int)Math.Round (image.Height * pixbufScale),
									InterpType.Bilinear);
			imagex = GridAlign ((x + (width / 2)) - ((double) scaledImage.Width / 2));
			imagey = GridAlign ((y + (height / 2)) - ((double) scaledImage.Height / 2));
			this.CreateRoundedRectPath (gc, x, y, width, height, radius);
			gc.Color = backgroundColor;
			gc.LineWidth = strokeWidth;
			gc.FillPreserve ();
			Gdk.CairoHelper.SetSourcePixbuf (gc, scaledImage, imagex, imagey);
			gc.FillPreserve ();
			gc.Color = strokeColor;
			gc.Stroke ();
			gc.Restore ();

			scaledImage.Dispose();
		}
		
		protected void RenderPixbufToSurf (Cairo.Context gc, Gdk.Pixbuf pixbuf, double x, double y)
		{
			gc.Save ();
			gc.Rectangle (x, y, pixbuf.Width, pixbuf.Height);
			Gdk.CairoHelper.SetSourcePixbuf (gc, pixbuf, x, y);
			gc.Fill ();
			gc.Restore ();
		}
		
		protected Size GetTextSize (Pango.Layout layout) 
		{
			int textWidth, textHeight;
			layout.GetSize(out textWidth, out textHeight);
			textWidth  = Pango.Units.ToPixels(textWidth);
			textHeight = Pango.Units.ToPixels(textHeight);
			return new Size(textWidth, textHeight);
		}
	}
		
		
}
