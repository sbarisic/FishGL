using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

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

	[StructLayout(LayoutKind.Explicit, Pack = 1)]
	public unsafe struct FGLColor {
		public static readonly FGLColor White = new FGLColor(255, 255, 255);
		public static readonly FGLColor Black = new FGLColor(0, 0, 0);
		public static readonly FGLColor DepthZero = new FGLColor(0.0f);

		[FieldOffset(0)]
		public byte R;

		[FieldOffset(1)]
		public byte G;

		[FieldOffset(2)]
		public byte B;

		[FieldOffset(3)]
		public byte A;

		[FieldOffset(0)]
		public int Int;

		[FieldOffset(0)]
		public float Float;

		public FGLColor(byte R, byte G, byte B, byte A) {
			Float = 0;
			Int = 0;

			this.R = R;
			this.G = G;
			this.B = B;
			this.A = A;
		}

		public FGLColor(byte R, byte G, byte B) : this(R, G, B, 255) {
		}

		public FGLColor(float Float) : this(0, 0, 0, 0) {
			this.Float = Float;
		}

		public static FGLColor ScaleColor(FGLColor Clr, float Scale) {
			return new FGLColor((byte)(Clr.R * Scale), (byte)(Clr.G * Scale), (byte)(Clr.B * Scale), Clr.A);
		}

		public static FGLColor ScaleColor(FGLColor Clr, FGLColor Scale) {
			return new FGLColor((byte)(Clr.R * (Scale.R / 255.0f)),
				(byte)(Clr.G * (Scale.G / 255.0f)), (byte)(Clr.B * (Scale.B / 255.0f)), Clr.A);
		}

		public static implicit operator FGLColor(Color Clr) {
			return new FGLColor(Clr.R, Clr.G, Clr.B, Clr.A);
		}
	}

	public unsafe struct FGLFramebuffer {
		public int Width, Height;
		public int ColorLen, Len;

		public byte[] Data;
		public FGLColor* DataPtr;

		GCHandle DataHandle;

		public FGLFramebuffer(int W, int H) {
			Width = W;
			Height = H;
			ColorLen = W * H;
			Len = ColorLen * sizeof(FGLColor);

			Data = new byte[Len];
			DataHandle = new GCHandle();
			DataPtr = null;

			Pin();
		}

		public void Pin() {
			DataHandle = GCHandle.Alloc(Data, GCHandleType.Pinned);
			DataPtr = (FGLColor*)DataHandle.AddrOfPinnedObject();
		}

		public void UnPin() {
			DataPtr = null;
			DataHandle.Free();
		}

		public void Get(float U, float V, out FGLColor Clr) {
			Clr = DataPtr[(int)(V * Height) * Width + (int)(U * Width)];
		}

		public static FGLFramebuffer FromFile(string Pth) {
			FGLFramebuffer FB;

			using (Bitmap Bmp = new Bitmap(Image.FromFile(Pth))) {
				Bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
				FB = new FGLFramebuffer(Bmp.Width, Bmp.Height);

				for (int Y = 0; Y < Bmp.Height; Y++)
					for (int X = 0; X < Bmp.Width; X++)
						FB.DataPtr[Y * FB.Width + X] = Bmp.GetPixel(X, Y);
			}

			return FB;
		}
	}
}
