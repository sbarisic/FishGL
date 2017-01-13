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
		public static Bitmap RenderBitmap, DepthBitmap, TextureBitmap;

		[STAThread]
		static void Main(string[] args) {
			Console.Title = "FishGL Console";
			Running = true;

			RenderBitmap = new Bitmap(800, 600);
			DepthBitmap = new Bitmap(RenderBitmap.Width, RenderBitmap.Height);
			TextureBitmap = new Bitmap(Image.FromFile("models\\diablo3_pose\\diablo3_pose_diffuse.png"));
			TextureBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

			DDta = DepthBitmap.LockBits(new Rectangle(0, 0, DepthBitmap.Width, DepthBitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			Texture = TextureBitmap.LockBits(new Rectangle(0, 0, TextureBitmap.Width, TextureBitmap.Height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			ObjLoader.Load(File.ReadAllLines("models\\diablo3_pose\\diablo3_pose.obj"), out Triangles);

			/*Triangles = new Vector3[][] {
				new Vector3[] { new Vector3(10, 70, 0), new Vector3(50, 160, 0), new Vector3(70, 80, 0) },
				new Vector3[] {new Vector3(180, 50, 0), new Vector3(150, 1, 0), new Vector3(70, 180, 0) },
				new Vector3[] { new Vector3(180, 150, 0), new Vector3(120, 160, 0), new Vector3(130, 180, 0) }
			};

			for (int i = 0; i < Triangles.Length; i++) {
				for (int j = 0; j < 3; j++)
					Triangles[i][j] /= 180.0f;
			}*/

			using (RenderForm RForm = new RenderForm()) {
				RForm.Show();



				while (Running) {
					Application.DoEvents();

					RForm.Refresh();
				}
			}

			//Console.WriteLine("Done!");
			//Console.ReadLine();
		}

		static Tri[] Triangles;
		static BitmapData DDta;

		public static BitmapData Texture;

		public static void Render(Graphics Gfx) {
			BitmapData Dta = RenderBitmap.LockBits(new Rectangle(0, 0, RenderBitmap.Width, RenderBitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			/*DDta = DepthBitmap.LockBits(new Rectangle(0, 0, DepthBitmap.Width, DepthBitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);*/

			FishGL.Clear(Dta, Color.Black);
			FishGL.Clear(DDta, Color.Black);
			
			Vector3 Scale = new Vector3((float)RenderBitmap.Width / 2, (float)RenderBitmap.Height / 2, 1);
			Vector3 Offset = new Vector3(1.0f, 1.0f, 1.0f);

			for (int i = 0; i < Triangles.Length; i++) {
				FishGL.Triangle(ref Triangles[i], (Triangles[i] + Offset) * Scale, Dta, DDta, Color.White);
			}

			RenderBitmap.UnlockBits(Dta);
			//DepthBitmap.UnlockBits(DDta);
			
			Gfx.DrawImageUnscaled(RenderBitmap, 0, 0);
		}
	}
}