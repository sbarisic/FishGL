using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

namespace FishGL {
	public struct Tri {
		public Vector3 A, B, C;
		public Vector2 A_UV, B_UV, C_UV;

		public static Tri operator +(Tri A, Vector3 B) {
			A.A += B;
			A.B += B;
			A.C += B;
			return A;
		}

		public static Tri operator *(Tri A, Vector3 B) {
			A.A *= B;
			A.B *= B;
			A.C *= B;
			return A;
		}
	}

	static unsafe class FishGL {
		public static void Swap<T>(ref T A, ref T B) {
			T Tmp = A;
			A = B;
			B = Tmp;
		}

		public static float Lerp(float A, float B, float Amt) {
			return A * Amt + B * (1 - Amt);
		}

		public static Color ScaleColor(Color Clr, float Scale) {
			return Color.FromArgb(Clr.A, (byte)(Clr.R * Scale), (byte)(Clr.G * Scale), (byte)(Clr.B * Scale));
		}

		public static Color Mix(Color Clr1, Color Clr2) {
			return Color.FromArgb((Clr1.A + Clr2.A) / 2, (Clr1.R + Clr2.R) / 2, (Clr1.G + Clr2.G) / 2, (Clr1.B + Clr2.B) / 2);
		}

		public static Color Shade(Color X, Color Y) {
			return Color.FromArgb(255, (byte)(X.R * (Y.R / 255.0f)), (byte)(X.G * (Y.G / 255.0f)), (byte)(X.B * (Y.B / 255.0f)));
		}

		public static Vector3 Barycentric(ref Tri T, Vector2 P) {
			Vector3 U = Vector3.Cross(new Vector3(T.C.X - T.A.X, T.B.X - T.A.X, T.A.X - P.X),
				new Vector3(T.C.Y - T.A.Y, T.B.Y - T.A.Y, T.A.Y - P.Y));

			if (Math.Abs(U.Z) < 1)
				return new Vector3(-1, 1, 1);

			return new Vector3(1.0f - (U.X + U.Y) / U.Z, U.Y / U.Z, U.X / U.Z);
		}

		/*public static Vector3 Barycentric(Vector3 A, Vector3 B, Vector3 C, Vector3 P) {
			return Barycentric(new Vector2(A.X, A.Y), new Vector2(B.X, B.Y), new Vector2(C.X, C.Y), P);
		}*/

		public static float Min(float A, float B, float C) {
			return Math.Min(A, Math.Min(B, C));
		}

		public static float Max(float A, float B, float C) {
			return Math.Max(A, Math.Max(B, C));
		}

		public static void BoundingBox(ref Tri T, out Vector3 Minimum, out Vector3 Maximum) {
			Minimum = new Vector3(Min(T.A.X, T.B.X, T.C.X), Min(T.A.Y, T.B.Y, T.C.Y), Min(T.A.Z, T.B.Z, T.C.Z));
			Maximum = new Vector3(Max(T.A.X, T.B.X, T.C.X), Max(T.A.Y, T.B.Y, T.C.Y), Max(T.A.Z, T.B.Z, T.C.Z));
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

		public static Color UVGet(BitmapData Dta, float U, float V) {
			return Get(Dta, (int)(U * Dta.Width), (int)(V * Dta.Width));
		}

		public static Color Get(BitmapData Dta, int X, int Y) {
			byte* Scan0 = (byte*)Dta.Scan0;

			if (X < 0 || X >= Dta.Width || Y < 0 || Y >= Dta.Height)
				return Color.Black;

			int Idx = Y * Dta.Stride + X * 4;
			return Color.FromArgb(Scan0[Idx + 3], Scan0[Idx + 2], Scan0[Idx + 1], Scan0[Idx + 0]);
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

		public static void Triangle(ref Tri T, BitmapData Dta, BitmapData DDta, Color Clr) {
			Vector3 Min, Max;
			BoundingBox(ref T, out Min, out Max);

			Vector2 P = new Vector2(Min.X, Min.Y);

			for (P.Y = Min.Y; P.Y < Max.Y; P.Y++)
				for (P.X = Min.X; P.X < Max.X; P.X++) {
					// TODO: Screen bounds check, besides triangle boundary

					Vector3 BC = Barycentric(ref T, P);
					if (BC.X < 0 || BC.Y < 0 || BC.Z < 0)
						continue;

					byte D = (byte)(((T.A.Z * BC.X) + (T.B.Z * BC.Y) + (T.C.Z * BC.Z)) * 134);

					if (Get(DDta, (int)P.X, (int)P.Y).R < D) {
						Set(DDta, (int)P.X, (int)P.Y, Color.FromArgb(255, D, D, D));

						Color FragClr = Clr;
						Color TexClr = UVGet(Program.Texture, (T.A_UV.X * BC.X) + (T.B_UV.X * BC.Y) + (T.C_UV.X * BC.Z),
							   (T.A_UV.Y * BC.X) + (T.B_UV.Y * BC.Y) + (T.C_UV.Y * BC.Z));

						FragClr = Shade(FragClr, TexClr);
						FragClr = ScaleColor(FragClr, D / 255.0f);

						Set(Dta, (int)P.X, (int)P.Y, FragClr);
					}
				}

			/*Line(A, B, Dta, Clr);
			Line(B, C, Dta, Clr);
			Line(C, A, Dta, Clr);*/
		}

		public static void Triangle(ref Tri World, Tri Screen, BitmapData Dta, BitmapData DDta, Color Clr) {
			Vector3 Cross = Vector3.Normalize(Vector3.Cross(World.C - World.A, World.B - World.A));
			if (Cross.Z > 0)
				return;

			float ShadeScale = Math.Abs(Cross.Z);
			Triangle(ref Screen, Dta, DDta, ScaleColor(Clr, ShadeScale));
		}
	}
}
