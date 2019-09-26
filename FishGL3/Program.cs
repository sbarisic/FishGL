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
		public int VertCount;

		public ThreadArgs(int Ctx, int VertBuffer, int VertCount) {
			this.Ctx = Ctx;
			this.VertBuffer = VertBuffer;
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
			FGL.WriteBuffer(VertBuffer1, new[] { new Vector3(150, 470, 0), new Vector3(330, 100, 0), new Vector3(500, 300, 0) });
			int VertBuffer2 = FGL.CreateSharedBuffer(Ctx2, VertBuffer1);

			Vector3* Verts = (Vector3*)FGL.MapBuffer(VertBuffer1, FGL_BUFFER_FLAGS.ReadWrite);

			Thread T1 = new Thread(ThreadedRender);
			T1.IsBackground = true;
			T1.Start(new ThreadArgs(Ctx1, VertBuffer1, VertCount));

			Thread T2 = new Thread(ThreadedRender);
			T2.IsBackground = true;
			T2.Start(new ThreadArgs(Ctx2, VertBuffer2, VertCount));

			Matrix4x4 Mat = Matrix4x4.CreateFromYawPitchRoll(0, 0, 0.0000001f);

			while (true) {
				for (int i = 0; i < 3; i++) {
					Verts[i] = Vector3.Transform(Verts[i], Mat);
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
				FGL.Draw(Args.Ctx, Framebuffer, Args.VertBuffer, Args.VertCount);
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
