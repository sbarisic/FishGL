using System;
using System.Numerics;
using System.Diagnostics;
using SFML.System;
using SFML.Window;
using SFML.Graphics;
using System.Threading;

namespace FishGL {
	class Program {
		//public static Bitmap RenderBitmap, DepthBitmap, TextureBitmap;

		static void Main(string[] args) {
			//Console.Title = "FishGL Console";

			VideoMode VMode = new VideoMode((uint)W, (uint)H);
			RenderWindow RWind = new RenderWindow(VMode, "FishGL", Styles.Close);
			RWind.SetVerticalSyncEnabled(false);
			RWind.SetFramerateLimit(0);
			RWind.Closed += (S, E) => RWind.Close();

			RWind.KeyReleased += (S, E) => {
				switch (E.Code) {
					case Keyboard.Key.F1:
						FishGL.EnableWireframe = !FishGL.EnableWireframe;
						break;

					case Keyboard.Key.F2:
						FishGL.EnableDepthTesting = !FishGL.EnableDepthTesting;
						break;

					case Keyboard.Key.F3:
						FishGL.EnableBackfaceCulling = !FishGL.EnableBackfaceCulling;
						break;

					case Keyboard.Key.F4:
						FishGL.EnableShading = !FishGL.EnableShading;
						break;
				}
			};

			Font DrawFont = new Font("C:\\Windows\\Fonts\\Consola.ttf");

			Text InfoText = new Text("Hello World!", DrawFont, 12);
			InfoText.Position = new Vector2f(1, 1);

			Text InfoText2 = new Text("F1 - Wireframe\nF2 - Depth testing\nF3 - Backface culling\nF4 - Shading", DrawFont, 12);
			InfoText2.Position = new Vector2f(1, 50);

			Texture Tex = new Texture(VMode.Width, VMode.Height);
			Sprite TexSprite = new Sprite(Tex);

			Stopwatch SWatch = new Stopwatch();
			float TPS = 1.0f / Stopwatch.Frequency;
			float FrameTime = 0;

			Init();

			while (RWind.IsOpen) {
				RWind.DispatchEvents();
				RWind.Clear(Color.Black);

				SWatch.Restart();
				Render();
				SWatch.Stop();

				Tex.Update(FishGL.ColorBuffer.Data);

				FrameTime = SWatch.ElapsedTicks * TPS;
				//SWatch.Restart();

				InfoText.DisplayedString = string.Format("{0:0.0000} ms; {1} FPS\n{2} tris",
					FrameTime * 1000.0f, 1.0f / FrameTime, Triangles.Length);
				RWind.Draw(TexSprite);
				RWind.Draw(InfoText);
				RWind.Draw(InfoText2);
				RWind.Display();
			}
		}

		static Tri[] Triangles;

		static int W = 800, H = 600;
		static Shadurr Shdr;
		static Stopwatch SWatch = Stopwatch.StartNew();

		static void Init() {
			Triangles = ObjLoader.Load("models\\diablo3_pose\\diablo3_pose.obj");

			FishGL.ColorBuffer = new FGLFramebuffer(W, H);
			FishGL.DepthBuffer = new FGLFramebuffer(W, H);
			FishGL.TEX0Buffer = FGLFramebuffer.FromFile("models\\diablo3_pose\\diablo3_pose_diffuse.png");

			//FGL.EnableTexturing = true;
			FishGL.EnableShading = true;
			FishGL.EnableBackfaceCulling = true;
			FishGL.EnableDepthTesting = true;
			FishGL.EnableWireframe = false;

			float S = Math.Min(W, H);
			Shdr = new Shadurr() {
				ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(90 * (float)Math.PI / 180, (float)W / H, 1, 1000),
				ViewMatrix = Matrix4x4.CreateScale(new Vector3(1, -1, 1) * S / 2) *
					Matrix4x4.CreateTranslation(new Vector3(W / 1.5f, H / 2, S))
			};
			FishGL.ShaderProgram = Shdr;
		}

		static void Render() {
			Shdr.ModelMatrix = Matrix4x4.CreateRotationY(SWatch.ElapsedMilliseconds / 2000.0f);

			FishGL.Fill(ref FishGL.ColorBuffer, FGLColor.Black);
			FishGL.Fill(ref FishGL.DepthBuffer, FGLColor.DepthZero);

			for (int i = 0; i < Triangles.Length; i++)
				FishGL.Triangle(Triangles[i]);
		}
	}

	class Shadurr : FGLShader {
		public Matrix4x4 ViewMatrix, ProjectionMatrix, ModelMatrix;

		public override void Vertex(ref Vector3 Vert) {
			Vert = Vector3.Transform(Vert, ModelMatrix * ViewMatrix * ProjectionMatrix);
		}

		public override void Pixel(ref FGLColor OutColor, float U, float V, int ScrX, int ScrY, float Depth, ref bool Discard) {
			FishGL.TEX0Buffer.Get(U, V, out OutColor);
			FGLColor.ScaleColor(ref OutColor, Math.Abs(Depth / -900));
		}
	}
}