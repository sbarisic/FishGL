using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace FishGL {
	class RenderForm : Form {
		Stopwatch SWatch = new Stopwatch();

		public RenderForm() {
			Text = "FishGL";
			ClientSize = new Size(Program.RenderBitmap.Width, Program.RenderBitmap.Height);
			FormBorderStyle = FormBorderStyle.Fixed3D;
			StartPosition = FormStartPosition.CenterScreen;
			MaximizeBox = false;
			DoubleBuffered = true;

			SWatch.Start();
		}

		protected override void OnClosing(CancelEventArgs e) {
			Program.Running = false;
			base.OnClosing(e);
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			Program.Render(e.Graphics);

			e.Graphics.DrawString(string.Format("Time: {0} ms", SWatch.ElapsedMilliseconds), SystemFonts.CaptionFont, Brushes.White, 0, 0);
			SWatch.Restart();
		}
	}

	class Program {
		public static bool Running;
		public static Bitmap RenderBitmap;

		[STAThread]
		static void Main(string[] args) {
			Console.Title = "FishGL Console";
			Running = true;

			RenderBitmap = new Bitmap(800, 600);

			ObjLoader.Load(File.ReadAllLines("models\\diablo3_pose\\diablo3_pose.obj"), out Triangles);

			using (RenderForm RForm = new RenderForm()) {
				RForm.Show();

				RForm.Refresh();

				while (Running) {
					Application.DoEvents();
				}
			}

			//Console.WriteLine("Done!");
			//Console.ReadLine();
		}

		static Vector3[][] Triangles;
		static bool Rendered = false;

		public static void Render(Graphics Gfx) {
			//if (!Rendered) {
			//	Rendered = true;

				BitmapData Dta = RenderBitmap.LockBits(new Rectangle(0, 0, RenderBitmap.Width, RenderBitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				FishGL.Clear(Dta, Color.Black);

				/*FishGL.Line(new Vector2(50, 330), new Vector2(300, 50), Dta, Color.Red);
				FishGL.Line(new Vector2(300, 50), new Vector2(400, 300), Dta, Color.Green);
				FishGL.Line(new Vector2(400, 300), new Vector2(50, 330), Dta, Color.Blue);*/

				Vector3 Scale = new Vector3((float)RenderBitmap.Width / 2, (float)RenderBitmap.Height / 2, 1);
				Vector3 Offset = new Vector3(1.0f, 1.0f, 0.0f);

				for (int i = 0; i < Triangles.Length; i++) {
					//Console.WriteLine("{0} / {1}", i, Triangles.Length);
					
					Vector3[] Tri = Triangles[i];
					FishGL.Line((Tri[0] + Offset) * Scale, (Tri[1] + Offset) * Scale, Dta, Color.DarkCyan);
					FishGL.Line((Tri[1] + Offset) * Scale, (Tri[2] + Offset) * Scale, Dta, Color.DarkCyan);
					FishGL.Line((Tri[2] + Offset) * Scale, (Tri[0] + Offset) * Scale, Dta, Color.DarkCyan);
				}

				RenderBitmap.UnlockBits(Dta);
			//}

			RenderBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
			Gfx.DrawImageUnscaled(RenderBitmap, 0, 0);
		}
	}
}