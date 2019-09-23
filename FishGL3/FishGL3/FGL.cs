using OpenCL.Net;
using OpenCL.Net.Extensions;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CLProgram = OpenCL.Net.Program;

namespace FishGL3 {
	public class FGLWindow {
		public int Width;
		public int Height;

		public IntPtr Tex;
	}

	public struct FGLColor {
		public byte R;
		public byte G;
		public byte B;

		public FGLColor(byte R, byte G, byte B) {
			this.R = R;
			this.G = G;
			this.B = B;
		}
	}

	unsafe struct FGLGlobal {
		public int Width;
		public int Height;

		public int TriCount;

		public FGLGlobal(int Width, int Height, int TriCount = 0) {
			this.Width = Width;
			this.Height = Height;
			this.TriCount = TriCount;
		}
	}

	public unsafe static class FGL {
		static FGLGlobal Global;
		static IMem CLGlobal;

		static IntPtr Wnd;
		static IntPtr Rnd;
		static IntPtr Tex;

		static Context CLContext;
		static CommandQueue CLQueue;
		static Device CLDevice;
		static CLProgram CLProgram;
		static Kernel CLKernel;
		static uint CLWorkGroupSize;
		static uint CLWorkGroupMultiple;

		static int MemoryLen;
		static FGLColor* Memory;
		static IMem CLMemory;

		static IMem ClearBuffer;

		static List<IMem> BufferHandles = new List<IMem>();

		public static void CreateWindow(int W, int H) {
			Global = new FGLGlobal(W, H);

			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
			SDL.SDL_CreateWindowAndRenderer(W, H, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN, out Wnd, out Rnd);
			Tex = SDL.SDL_CreateTexture(Rnd, SDL.SDL_PIXELFORMAT_RGB24, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, W, H);

			MemoryLen = W * H * sizeof(FGLColor);
			Memory = (FGLColor*)Marshal.AllocHGlobal(MemoryLen);

			CLGlobal = Cl.CreateBuffer(CLContext, MemFlags.ReadOnly, sizeof(FGLGlobal), out ErrorCode Err);
			CLMemory = Cl.CreateBuffer(CLContext, MemFlags.WriteOnly, MemoryLen, out Err);
			ClearBuffer = Cl.CreateBuffer(CLContext, MemFlags.ReadWrite, MemoryLen, out Err);
		}

		public static void SetWindowTitle(string Title) {
			SDL.SDL_SetWindowTitle(Wnd, Title);
		}

		public static bool PollEvents() {
			while (SDL.SDL_PollEvent(out SDL.SDL_Event E) != 0) {
				if (E.type == SDL.SDL_EventType.SDL_QUIT)
					return false;
			}

			return true;
		}

		public static void PlotPixel(int X, int Y, FGLColor Clr) {
			Memory[Y * Global.Width + X] = Clr;
		}

		public static void ClearColor(byte R, byte G, byte B) {
			for (int i = 0; i < Global.Width * Global.Height; i++)
				Memory[i] = new FGLColor(R, G, B);

			Cl.EnqueueWriteBuffer(CLQueue, ClearBuffer, Bool.True, IntPtr.Zero, (IntPtr)MemoryLen, (IntPtr)Memory, 0, null, out Event Evt);
		}

		public static void Clear() {
			Cl.EnqueueCopyBuffer(CLQueue, ClearBuffer, CLMemory, IntPtr.Zero, IntPtr.Zero, (IntPtr)MemoryLen, 0, null, out Event Evt);
		}

		public static void Swap() {
			Cl.Finish(CLQueue);
			Cl.EnqueueReadBuffer(CLQueue, CLMemory, Bool.True, IntPtr.Zero, (IntPtr)MemoryLen, (IntPtr)Memory, 0, null, out Event Evt);

			SDL.SDL_Rect Rect = new SDL.SDL_Rect();
			Rect.x = Rect.y = 0;
			Rect.w = Global.Width;
			Rect.h = Global.Height;

			SDL.SDL_UpdateTexture(Tex, ref Rect, new IntPtr(Memory), sizeof(FGLColor) * Global.Width);
			SDL.SDL_RenderClear(Rnd);
			SDL.SDL_RenderCopy(Rnd, Tex, IntPtr.Zero, IntPtr.Zero);
			SDL.SDL_RenderPresent(Rnd);
		}

		static string CL_GetPlatformName(Platform P) {
			ErrorCode Err;

			string Ext = Cl.GetPlatformInfo(P, PlatformInfo.Extensions, out Err).ToString();
			string Name = Cl.GetPlatformInfo(P, PlatformInfo.Name, out Err).ToString();
			string Profile = Cl.GetPlatformInfo(P, PlatformInfo.Profile, out Err).ToString();
			string Vendor = Cl.GetPlatformInfo(P, PlatformInfo.Vendor, out Err).ToString();
			string Version = Cl.GetPlatformInfo(P, PlatformInfo.Version, out Err).ToString();

			return Name;
		}

		/*static string CL_GetDeviceInfo(Device Dev) {
			Dictionary<DeviceInfo, string> DDD = new Dictionary<DeviceInfo, string>();

			DeviceInfo[] Infos = (DeviceInfo[])Enum.GetValues(typeof(DeviceInfo));
			foreach (var I in Infos) {
				DDD.Add(I, Cl.GetDeviceInfo(Dev, I, out ErrorCode Err).ToString());
			}

			return null;
		}*/

		static void CLCheckError(ErrorCode Err) {
			if (Err == ErrorCode.BuildProgramFailure) {
				string Log = Cl.GetProgramBuildInfo(CLProgram, CLDevice, ProgramBuildInfo.Log, out ErrorCode Tmp).ToString();
				Console.WriteLine(Log);
			}

			if (Err != ErrorCode.Success)
				throw new Exception("OpenCL error " + Err);
		}

		static CLProgram CL_CreateProgram(string[] Sources) {
			for (int i = 0; i < Sources.Length; i++)
				Sources[i] = Sources[i].Replace("\r", "");

			CLProgram Prog = Cl.CreateProgramWithSource(CLContext, (uint)Sources.Length, Sources, Sources.Select(S => (IntPtr)S.Length).ToArray(), out ErrorCode Err);
			CLCheckError(Err);
			return Prog;
		}

		public static void CreateRenderContext(string PlatformName = "amd") {
			const bool DisableOptimizations = false;

			ErrorCode Err;

			Platform[] Platforms = Cl.GetPlatformIDs(out Err);
			Platform SelectedPlatform = Platforms[0];

			if (PlatformName != null) {
				foreach (var P in Platforms) {
					string Name = CL_GetPlatformName(P).ToLower();

					if (Name.Contains(PlatformName.ToLower())) {
						SelectedPlatform = P;
						break;
					}
				}
			}

			Device[] Devices = Cl.GetDeviceIDs(SelectedPlatform, DeviceType.Gpu, out Err);

			if (Devices.Length != 1)
				throw new Exception("Expected one GPU");

			CLDevice = Devices[0];

			//CL_GetDeviceInfo(Devices[0]);

			CLContext = Cl.CreateContext(null, 1, Devices, null, IntPtr.Zero, out Err);
			CLQueue = Cl.CreateCommandQueue(CLContext, Devices[0], CommandQueueProperties.None, out Err);

			CLProgram = CL_CreateProgram(new[] { File.ReadAllText("CL/kernel.cl") });
			CLCheckError(Cl.BuildProgram(CLProgram, 0, null, DisableOptimizations ? "-cl-opt-disable" : null, null, IntPtr.Zero));

			CLKernel = Cl.CreateKernel(CLProgram, "main", out Err);
			CLCheckError(Err);

			CLWorkGroupSize = Cl.GetKernelWorkGroupInfo(CLKernel, Devices[0], KernelWorkGroupInfo.WorkGroupSize, out Err).CastTo<uint>();
		}

		public static void Draw(int Triangles) {
			Event Evt;
			Global.TriCount = Triangles / 3;

			fixed (FGLGlobal* GlobalPtr = &Global) {
				Cl.EnqueueWriteBuffer(CLQueue, CLGlobal, Bool.True, IntPtr.Zero, (IntPtr)sizeof(FGLGlobal), (IntPtr)GlobalPtr, 0, null, out Evt);
			}

			Cl.SetKernelArg(CLKernel, 0, CLGlobal);
			Cl.SetKernelArg(CLKernel, 1, CLMemory);

			CLCheckError(Cl.EnqueueNDRangeKernel(CLQueue, CLKernel, 2, null, new IntPtr[] { (IntPtr)Global.Width, (IntPtr)Global.Height }, new IntPtr[] { (IntPtr)16, (IntPtr)16 }, 0, null, out Evt));
		}

		static IMem GetBuffer(int BufferObject) {
			if (BufferHandles[BufferObject] == null)
				throw new Exception("Trying to get non existing buffer");

			return BufferHandles[BufferObject];
		}

		public static int CreateBuffer(int Size, bool Read = true, bool Write = false) {
			MemFlags Flags = MemFlags.ReadOnly;

			if (Read && Write)
				Flags = MemFlags.ReadWrite;
			else if (!Read && Write)
				Flags = MemFlags.WriteOnly;

			IMem Buffer = Cl.CreateBuffer(CLContext, Flags, Size, out ErrorCode Err);
			CLCheckError(Err);

			for (int i = 0; i < BufferHandles.Count; i++) {
				if (BufferHandles[i] == null) {
					BufferHandles[i] = Buffer;
					return i;
				}
			}

			BufferHandles.Add(Buffer);
			return BufferHandles.Count - 1;
		}

		public static void DeleteBuffer(int BufferObject) {
			Cl.ReleaseMemObject(GetBuffer(BufferObject));
			BufferHandles[BufferObject] = null;
		}

		public static void WriteBuffer(int BufferObject, void* Data, int Len) {
			Cl.EnqueueWriteBuffer(CLQueue, GetBuffer(BufferObject), Bool.True, IntPtr.Zero, (IntPtr)Len, (IntPtr)Data, 0, null, out Event E);
		}

		public static void WriteBuffer<T>(int BufferObject, T[] Data) where T : unmanaged {
			fixed (T* DataPtr = Data)
				WriteBuffer(BufferObject, DataPtr, sizeof(T) * Data.Length);
		}

		public static void BindTriangleBuffer(int BufferObject) {
			Cl.SetKernelArg(CLKernel, 2, GetBuffer(BufferObject));
		}
	}
}
