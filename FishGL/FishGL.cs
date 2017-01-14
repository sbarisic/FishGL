﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FishGL {
	static class Helpers {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float A, float B, float C) {
			return Math.Min(A, Math.Min(B, C));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float A, float B, float C) {
			return Math.Max(A, Math.Max(B, C));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void BoundingBox(ref Tri T, out Vector3 Minimum, out Vector3 Maximum) {
			Minimum = new Vector3(Min(T.A.X, T.B.X, T.C.X), Min(T.A.Y, T.B.Y, T.C.Y), Min(T.A.Z, T.B.Z, T.C.Z));
			Maximum = new Vector3(Max(T.A.X, T.B.X, T.C.X), Max(T.A.Y, T.B.Y, T.C.Y), Max(T.A.Z, T.B.Z, T.C.Z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(ref Tri T, int PX, int PY, ref Vector3 Val) {
			Vector3 U = Vector3.Cross(new Vector3(T.C.X - T.A.X, T.B.X - T.A.X, T.A.X - PX),
				new Vector3(T.C.Y - T.A.Y, T.B.Y - T.A.Y, T.A.Y - PY));

			if (Math.Abs(U.Z) < 1) {
				Val.X = -1;
				return;
			}

			Val.X = 1.0f - (U.X + U.Y) / U.Z;
			Val.Y = U.Y / U.Z;
			Val.Z = U.X / U.Z;
		}
	}

	public abstract class FGLShader {
		public abstract void Vertex(ref Vector3 Vert);
		public abstract FGLColor? Pixel(float U, float V);
	}

	public static unsafe class FishGL {
		public static FGLFramebuffer ColorBuffer;
		public static FGLFramebuffer DepthBuffer;
		public static FGLFramebuffer TEX0Buffer;

		public static bool EnableTexturing = false;
		public static bool EnableShading = false;
		public static bool EnableBackfaceCulling = false;
		public static bool EnableDepthTesting = false;
		public static bool EnableWireframe = false;

		public static FGLColor DrawColor = FGLColor.White;
		public static FGLShader ShaderProgram;

		static Vector3 Bary;

		public static void Fill(ref FGLFramebuffer FB, FGLColor Clr) {
			fixed (byte* DataPtr = FB.Data) {
				FGLColor* ClrPtr = (FGLColor*)DataPtr;

				for (int i = 0; i < FB.ColorLen; i++)
					ClrPtr[i] = Clr;
			}
		}

		public static void Line(int X0, int Y0, int X1, int Y1) {
			bool Steep = false;

			if (Math.Abs(X0 - X1) < Math.Abs(Y0 - Y1)) {
				int Tmp = X0;
				X0 = Y0;
				Y0 = Tmp;

				Tmp = X1;
				X1 = Y1;
				Y1 = Tmp;

				Steep = true;
			}

			if (X0 > X1) {
				int Tmp = X0;
				X0 = X1;
				X1 = Tmp;

				Tmp = Y0;
				Y0 = Y1;
				Y1 = Tmp;
			}

			int DeltaX = X1 - X0;
			int DeltaY = Y1 - Y0;
			int DeltaError2 = Math.Abs(DeltaY) * 2;
			int Error2 = 0;
			int Y = Y0;

			for (int X = X0; X <= X1; X++) {
				if (Steep)
					ColorBuffer.DataPtr[X * ColorBuffer.Width + Y] = DrawColor;
				else
					ColorBuffer.DataPtr[Y * ColorBuffer.Width + X] = DrawColor;


				Error2 += DeltaError2;

				if (Error2 > DeltaX) {
					Y += (Y1 > Y0 ? 1 : -1);
					Error2 -= DeltaX * 2;
				}
			}
		}

		public static void Triangle(Tri Tri) {
			if (ShaderProgram != null) {
				ShaderProgram.Vertex(ref Tri.A);
				ShaderProgram.Vertex(ref Tri.B);
				ShaderProgram.Vertex(ref Tri.C);
			}

			Vector3 Cross = Vector3.Normalize(Vector3.Cross(Tri.C - Tri.A, Tri.B - Tri.A));

			// Backface culling
			if (EnableBackfaceCulling && Cross.Z < 0)
				return;

			Vector3 Min, Max;
			Helpers.BoundingBox(ref Tri, out Min, out Max);

			for (int Y = (int)Min.Y; Y < Max.Y; Y++)
				for (int X = (int)Min.X; X < Max.X; X++) {
					if (X < 0 || Y < 0 || X >= ColorBuffer.Width || Y >= ColorBuffer.Height)
						continue;

					Helpers.Barycentric(ref Tri, X, Y, ref Bary);
					if (Bary.X < 0 || Bary.Y < 0 || Bary.Z < 0)
						continue;

					int Idx = Y * ColorBuffer.Width + X;
					float D = ((Tri.A.Z * Bary.X) + (Tri.B.Z * Bary.Y) + (Tri.C.Z * Bary.Z));

					if (!EnableDepthTesting || (DepthBuffer.DataPtr[Idx].Float > D)) {
						// Calculate UV coordinates
						float TexU = (Tri.A_UV.X * Bary.X) + (Tri.B_UV.X * Bary.Y) + (Tri.C_UV.X * Bary.Z);
						float TexV = (Tri.A_UV.Y * Bary.X) + (Tri.B_UV.Y * Bary.Y) + (Tri.C_UV.Y * Bary.Z);

						FGLColor PixColor = DrawColor;
						if (ShaderProgram != null) {
							FGLColor? ShaderPixColor = ShaderProgram.Pixel(TexU, TexV);
							if (!ShaderPixColor.HasValue)
								continue; // Pixel was discarded

							PixColor = ShaderPixColor.Value;
						}

						if (EnableShading)
							PixColor = FGLColor.ScaleColor(PixColor, Math.Abs(Cross.Z));

						if (EnableTexturing) {
							// Scale UVs to texture size
							TexU *= TEX0Buffer.Width;
							TexV *= TEX0Buffer.Height;

							FGLColor TexClr = TEX0Buffer.DataPtr[(int)TexV * TEX0Buffer.Width + (int)TexU];
							PixColor = FGLColor.ScaleColor(PixColor, TexClr);
						}

						DepthBuffer.DataPtr[Idx].Float = D;
						ColorBuffer.DataPtr[Y * ColorBuffer.Width + X] = PixColor;
					}
				}

			if (EnableWireframe) {
				Line((int)Tri.A.X, (int)Tri.A.Y, (int)Tri.B.X, (int)Tri.B.Y);
				Line((int)Tri.B.X, (int)Tri.B.Y, (int)Tri.C.X, (int)Tri.C.Y);
				Line((int)Tri.C.X, (int)Tri.C.Y, (int)Tri.A.X, (int)Tri.A.Y);
			}
		}
	}
}