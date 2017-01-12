using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

namespace FishGL {
	static unsafe class FishGL {
		public static void Swap<T>(ref T A, ref T B) {
			T Tmp = A;
			A = B;
			B = Tmp;
		}

		public static void Set(BitmapData Dta, int X, int Y, Color Clr) {
			byte* Scan0 = (byte*)Dta.Scan0;

			if (X < 0 || X >= Dta.Width || Y < 0 || Y >= Dta.Height)
				return;

			int Idx = Y * Dta.Stride + X * 4;
			Scan0[Idx + 0] = Clr.B;
			Scan0[Idx + 1] = Clr.G;
			Scan0[Idx + 2] = Clr.R;
			Scan0[Idx + 3] = Clr.A;
		}

		public static void Clear(BitmapData Dta, Color Clr) {
			for (int Y = 0; Y < Dta.Height; Y++)
				for (int X = 0; X < Dta.Width; X++)
					Set(Dta, X, Y, Clr);
		}

		public static void Line(int X0, int Y0, int X1, int Y1, BitmapData Dta, Color Clr) {
			bool Steep = false;

			if (Math.Abs(X0 - X1) < Math.Abs(Y0 - Y1)) {
				Swap(ref X0, ref Y0);
				Swap(ref X1, ref Y1);

				Steep = true;
			}

			if (X0 > X1) {
				Swap(ref X0, ref X1);
				Swap(ref Y0, ref Y1);
			}

			int DeltaX = X1 - X0;
			int DeltaY = Y1 - Y0;
			int DeltaError2 = Math.Abs(DeltaY) * 2;
			int Error2 = 0;
			int Y = Y0;

			for (int X = X0; X <= X1; X++) {
				if (Steep)
					Set(Dta, Y, X, Clr);
				else
					Set(Dta, X, Y, Clr);

				Error2 += DeltaError2;

				if (Error2 > DeltaX) {
					Y += (Y1 > Y0 ? 1 : -1);
					Error2 -= DeltaX * 2;
				}
			}
		}

		public static void Line(Vector2 A, Vector2 B, BitmapData Dta, Color Clr) {
			Line((int)A.X, (int)A.Y, (int)B.X, (int)B.Y, Dta, Clr);
		}

		public static void Line(Vector3 A, Vector3 B, BitmapData Dta, Color Clr) {
			Line((int)A.X, (int)A.Y, (int)B.X, (int)B.Y, Dta, Clr);
		}
	}
}
