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

		static Matrix4x4 CreateRot(Vector3 RotPos, float Angle) {
			return Matrix4x4.CreateTranslation(-RotPos) * Matrix4x4.CreateFromYawPitchRoll(0, 0, Angle) * Matrix4x4.CreateTranslation(RotPos);
		}

		static void Main(string[] args) {
			FGL.CreateRenderContext();
			FGL.CreateWindow(800, 600);
			FGL.ClearColor(83, 125, 185);

			Vector3[] Triangle = new Vector3[3];
			int TriangleBuffer = FGL.CreateBuffer(sizeof(float) * 3 * Triangle.Length);
			FGL.BindTriangleBuffer(TriangleBuffer);

			while (FGL.PollEvents()) {
				Matrix4x4 TransMat = CreateRot(new Vector3(350, 300, 0), Watch.ElapsedMilliseconds / 1000.0f);
				Triangle[0] = Vector3.Transform(new Vector3(150, 470, 0), TransMat);
				Triangle[1] = Vector3.Transform(new Vector3(330, 100, 0), TransMat);
				Triangle[2] = Vector3.Transform(new Vector3(500, 300, 0), TransMat);
				FGL.WriteBuffer(TriangleBuffer, Triangle);

				FGL.Clear();
				FGL.Draw(Triangle.Length);
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
