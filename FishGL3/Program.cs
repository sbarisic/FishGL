using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FishGL3 {
	struct ThreadArgs {
		public int Ctx;
		public int VertBuffer;
		public int ColorBuffer;
		public int VertCount;

		public ThreadArgs(int Ctx, int VertBuffer, int ColorBuffer, int VertCount) {
			this.Ctx = Ctx;
			this.VertBuffer = VertBuffer;
			this.ColorBuffer = ColorBuffer;
			this.VertCount = VertCount;
		}
	}

	unsafe class Program {
		static Stopwatch Watch = Stopwatch.StartNew();

		static void Main(string[] args) {
			FGLDevice[] Devices = FGL.GetAllDevices();

			int Ctx1 = FGL.CreateRenderContext(Devices[0]);
			int Ctx2 = FGL.CreateRenderContext(Devices[2]);

			int VertCount = 3;
			int VertBuffer1 = FGL.CreateBuffer(Ctx1, sizeof(float) * 3 * VertCount);
			int VertBuffer2 = FGL.CreateSharedBuffer(Ctx2, VertBuffer1);
			FGL.WriteBuffer(VertBuffer1, new[] { new Vector3(330, 100, 0), new Vector3(500, 300, 0), new Vector3(150, 470, 0) });

			int ColorBuffer1 = FGL.CreateBuffer(Ctx1, sizeof(FGLColor) * VertCount);
			FGL.WriteBuffer(ColorBuffer1, new[] { new FGLColor(237, 76, 64), new FGLColor(91, 148, 47), new FGLColor(74, 162, 186) });

			int ColorBuffer2 = FGL.CreateBuffer(Ctx2, sizeof(FGLColor) * VertCount);
			FGL.WriteBuffer(ColorBuffer2, new[] { new FGLColor(255, 0, 0), new FGLColor(0, 255, 0), new FGLColor(0, 0, 255) });

			Thread T1 = new Thread(ThreadedRender);
			T1.IsBackground = true;
			T1.Start(new ThreadArgs(Ctx1, VertBuffer1, ColorBuffer1, VertCount));

			Thread T2 = new Thread(ThreadedRender);
			T2.IsBackground = true;
			T2.Start(new ThreadArgs(Ctx2, VertBuffer2, ColorBuffer2, VertCount));


			Vector3 RotPoint = new Vector3(400, 300, 0);
			Matrix4x4 Mat = Matrix4x4.CreateTranslation(-RotPoint) * Matrix4x4.CreateFromYawPitchRoll(0, 0, 0.01f) * Matrix4x4.CreateTranslation(RotPoint);
			Vector3* Verts = (Vector3*)FGL.MapBuffer(VertBuffer1, FGL_BUFFER_FLAGS.ReadWrite);

			while (true) {
				for (int i = 0; i < 3; i++) {
					Verts[i] = Vector3.Transform(Verts[i], Mat);
					Thread.Sleep(10);
				}
			}

			// T1.Join();
			// T2.Join();
		}

		static void ThreadedRender(object ThreadArgsObj) {
			ThreadArgs Args = (ThreadArgs)ThreadArgsObj;

			int Window = FGL.CreateWindow(800, 600);

			int Framebuffer = FGL.CreateFramebuffer();
			int ColorTex = FGL.CreateTexture(Args.Ctx, 800, 600, FGL_TEXTURE_TYPE.RGB);
			FGL.FramebufferAttachTexture(Framebuffer, ColorTex, FGL_FRAMEBUFFER_ATTACHMENT.Color);

			Stopwatch SWatch = Stopwatch.StartNew();
			float FrameTime = 0;

			while (FGL.WindowOpen(Window)) {
				FGL.PollEvents();

				FGL.ClearFramebuffer(Framebuffer, new FGLColor(0, 0, 0));
				FGL.Draw(Args.Ctx, Framebuffer, Args.VertBuffer, Args.ColorBuffer, Args.VertCount);
				FGL.Finish(Args.Ctx);
				FGL.SwapFramebuffer(Framebuffer, Window);

				Thread.Sleep(0);
				int FPS = (int)(1.0f / FrameTime);
				FGL.SetWindowTitle(Window, string.Format("{0} FPS, {1} ms", FPS, FrameTime * 1000));
				FrameTime = SWatch.ElapsedMilliseconds / 1000.0f;
				SWatch.Restart();
			}
		}
	}
}
