using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using SFML;
using SFML.System;
using SFML.Graphics;
using SFML.Window;

namespace Test {
	unsafe class Program {
		[DllImport("FishGL2", CallingConvention = CallingConvention.Cdecl)]
		static extern void fglInit(IntPtr VideoMemory, int Width, int Height, int BPP, int Stride, int Order);

		[DllImport("FishGL2", CallingConvention = CallingConvention.Cdecl)]
		static extern void fglDebugLoop();

		static void Main(string[] Args) {
			int Width = 1366;
			int Height = 768;
			int BPP = 32;

			// Video memory
			byte[] VideoMemory = new byte[Width * Height * (BPP / 8)];
			IntPtr VideoMemoryPtr = GCHandle.Alloc(VideoMemory, GCHandleType.Pinned).AddrOfPinnedObject();

			// Test render window
			RenderWindow RWind = new RenderWindow(new VideoMode((uint)Width, (uint)Height), "FishGL 2", Styles.Close);
			RWind.Closed += (S, E) => RWind.Close();
			RWind.SetVerticalSyncEnabled(false);
			RWind.SetFramerateLimit(0);

			// Create texture and initialize stuff
			Texture Tex = new Texture((uint)Width, (uint)Height);
			Sprite TexSprite = new Sprite(Tex);
			fglInit(VideoMemoryPtr, Width, Height, BPP, 0, 1);

			// Debug stuff
			const int FntSize = 16;
			Font Fnt = new Font("C:/Windows/Fonts/consola.ttf");
			Text DebugText = new Text("", Fnt, (uint)FntSize);

			Stopwatch SWatch = Stopwatch.StartNew();
			float Dt = 0;

			Action<int, int, string> DrawText = (X, Y, Str) => {
				DebugText.Position = new Vector2f(X, Y);
				DebugText.DisplayedString = Str;
				RWind.Draw(DebugText);
			};

			while (RWind.IsOpen) {
				RWind.DispatchEvents();
				RWind.Clear(Color.Black);

				fglDebugLoop();
				Tex.Update(VideoMemory);
				RWind.Draw(TexSprite);

				DrawText(0, 0, string.Format("{0:F4} ms", Dt));
				DrawText(0, FntSize, string.Format("{0:F0} FPS", 1.0f / Dt));

				RWind.Display();
				Dt = (float)SWatch.ElapsedMilliseconds / 1000;
				SWatch.Restart();
			}
		}
	}
}
