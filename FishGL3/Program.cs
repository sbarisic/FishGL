using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FishGL3 {
	unsafe class Program {
		static Stopwatch Watch = Stopwatch.StartNew();
		static Stopwatch SWatch = Stopwatch.StartNew();
		static float FrameTime;

		static void Main(string[] args) {
			FGLDevice[] Devices = FGL.GetAllDevices();

			int Ctx = FGL.CreateRenderContext(Devices[0]);
			int Window = FGL.CreateWindow(800, 600);

			int Screen = FGL.CreateFramebuffer();
			int ColorTex = FGL.CreateTexture(Ctx, 800, 600, FGL_TEXTURE_TYPE.RGB);
			FGL.FramebufferAttachTexture(Screen, ColorTex, FGL_FRAMEBUFFER_ATTACHMENT.Color);

			int VertCount = 3;
			int VertBuffer = FGL.CreateBuffer(Ctx, sizeof(float) * 3 * VertCount);
			FGL.WriteBuffer(Ctx, VertBuffer, new[] { new Vector3(150, 470, 0), new Vector3(330, 100, 0), new Vector3(500, 300, 0) });

			while (FGL.PollEvents()) {
				RenderTriangle(Ctx, Window, Screen, VertBuffer, VertCount);


				//Thread.Sleep(0);
				int FPS = (int)(1.0f / FrameTime);
				FGL.SetWindowTitle(Window, string.Format("{0} FPS, {1} ms", FPS, FrameTime * 1000));
				FrameTime = SWatch.ElapsedMilliseconds / 1000.0f;
				SWatch.Restart();
			}
		}

		static void RunRender() {

		}

		static void RenderTriangle(int Context, int Window, int Framebuffer, int VertBuffer, int VertCount) {
			FGL.ClearFramebuffer(Framebuffer, new FGLColor(0, 0, 0));
			FGL.Draw(Context, Framebuffer, VertBuffer, VertCount);
			FGL.SwapFramebuffer(Context, Framebuffer, Window);
		}
	}
}
