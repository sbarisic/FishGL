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
			FGL.CreateRenderContext();
			FGL.CreateWindow(800, 600);

			int VertexCount = 3;
			int VertBuffer = FGL.CreateBuffer(sizeof(float) * 3 * VertexCount);

			Vector3* Verts = (Vector3*)FGL.MapBuffer(VertBuffer, FGL_BUFFER_FLAGS.ReadWrite);
			Verts[0] = new Vector3(150, 470, 0);
			Verts[1] = new Vector3(330, 100, 0);
			Verts[2] = new Vector3(500, 300, 0);

			Vector3 RotOrig = new Vector3(350, 200, 0);
			Matrix4x4 Mat = Matrix4x4.CreateTranslation(-RotOrig)
				* Matrix4x4.CreateFromYawPitchRoll(0, 0, 0.005f)
				* Matrix4x4.CreateTranslation(RotOrig);

			while (FGL.PollEvents()) {
				for (int i = 0; i < VertexCount; i++)
					Verts[i] = Vector3.Transform(Verts[i], Mat);

				FGL.Clear(new FGLColor(0, 0, 0));
				FGL.Draw(VertBuffer, VertexCount);
				FGL.Swap();



				Thread.Sleep(0);
				int FPS = (int)(1.0f / FrameTime);
				FGL.SetWindowTitle(string.Format("{0} FPS, {1} ms", FPS, FrameTime * 1000));
				FrameTime = SWatch.ElapsedMilliseconds / 1000.0f;
				SWatch.Restart();
			}
		}
	}
}
