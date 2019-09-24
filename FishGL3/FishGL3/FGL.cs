using OpenCL;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

	unsafe class FGLBuffer {
		public cl_mem Buffer;
		public void* Memory;
		public int Length;

		public void* MappedMemory;

		public FGLBuffer(cl_mem Buffer, int Length) {
			this.Buffer = Buffer;
			this.Length = Length;
		}

		public void Delete() {
			FGL.CLCheckError(CL.clReleaseMemObject(Buffer));
		}
	}

	class FGLBufferList {
		List<FGLBuffer> Buffers;

		public FGLBufferList() {
			Buffers = new List<FGLBuffer>();
		}

		public int Add(FGLBuffer Buffer) {
			for (int i = 0; i < Buffers.Count; i++) {
				if (Buffers[i] == null) {
					Buffers[i] = Buffer;
					return i;
				}
			}

			Buffers.Add(Buffer);
			return Buffers.Count - 1;
		}

		public void Remove(int Handle) {
			Get(Handle);
			Buffers[Handle] = null;
		}

		public FGLBuffer Get(int Handle) {
			if (Buffers[Handle] == null || Handle < 0 || Handle >= Buffers.Count)
				throw new Exception("Invalid FGL buffer object " + Handle);

			return Buffers[Handle];
		}
	}

	public enum FGL_BUFFER_FLAGS {
		ReadWrite = (int)cl_mem_flags.CL_MEM_READ_WRITE,
		WriteOnly = (int)cl_mem_flags.CL_MEM_WRITE_ONLY,
		ReadOnly = (int)cl_mem_flags.CL_MEM_READ_ONLY,
	}

	public enum FGL_TEXTURE_TYPE {
		RGB,
		Depth
	}

	public unsafe static class FGL {
		static FGLGlobal Global;
		static cl_mem CLGlobal;

		static IntPtr Wnd;
		static IntPtr Rnd;
		static IntPtr Tex;

		static cl_context CLContext;
		static cl_command_queue CLQueue;
		static cl_device_id CLDevice;
		static cl_program CLProgram;
		static cl_kernel CLKernel;
		static uint CLWorkGroupSize;

		static int ColorBuffer;

		static FGLBufferList BufferHandles = new FGLBufferList();

		public static void CreateWindow(int W, int H) {
			Global = new FGLGlobal(W, H);

			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
			SDL.SDL_CreateWindowAndRenderer(W, H, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN, out Wnd, out Rnd);
			Tex = SDL.SDL_CreateTexture(Rnd, SDL.SDL_PIXELFORMAT_RGB24, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, W, H);

			ErrorCode Err;
			CLGlobal = CL.clCreateBuffer(CLContext, cl_mem_flags.CL_MEM_READ_ONLY, (ulong)sizeof(FGLGlobal), null, out Err);

			ColorBuffer = CreateTexture(W, H, FGL_TEXTURE_TYPE.RGB);
			MapBuffer(ColorBuffer, FGL_BUFFER_FLAGS.ReadWrite);
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

		public static void Clear(FGLColor ClearColor) {
			int Len = Global.Width * Global.Height;
			FGLColor* Pixels = (FGLColor*)GetBuffer(ColorBuffer).MappedMemory;

			for (int i = 0; i < Len; i++)
				Pixels[i] = ClearColor;
		}

		public static void Swap() {
			CL.clFinish(CLQueue);

			SDL.SDL_Rect Rect = new SDL.SDL_Rect();
			Rect.x = Rect.y = 0;
			Rect.w = Global.Width;
			Rect.h = Global.Height;

			SDL.SDL_UpdateTexture(Tex, ref Rect, new IntPtr(GetBuffer(ColorBuffer).MappedMemory), sizeof(FGLColor) * Global.Width);
			SDL.SDL_RenderClear(Rnd);
			SDL.SDL_RenderCopy(Rnd, Tex, IntPtr.Zero, IntPtr.Zero);
			SDL.SDL_RenderPresent(Rnd);
		}

		static string CL_GetPlatformName(cl_platform_id P) {
			ErrorCode err;

			string Ext = CL.clGetPlatformInfo(P, cl_platform_info.CL_PLATFORM_EXTENSIONS, out err);
			string Name = CL.clGetPlatformInfo(P, cl_platform_info.CL_PLATFORM_NAME, out err);
			string Profile = CL.clGetPlatformInfo(P, cl_platform_info.CL_PLATFORM_PROFILE, out err);
			string Vendor = CL.clGetPlatformInfo(P, cl_platform_info.CL_PLATFORM_VENDOR, out err);
			string Version = CL.clGetPlatformInfo(P, cl_platform_info.CL_PLATFORM_VERSION, out err);

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

		internal static void CLCheckError(ErrorCode Err) {
			if (Err == ErrorCode.CL_BUILD_PROGRAM_FAILURE) {
				string Log = CL.clGetProgramBuildInfoStr(CLProgram, CLDevice, cl_program_build_info.CL_PROGRAM_BUILD_LOG, out ErrorCode Tmp);
				Console.WriteLine(Log);
			}

			if (Err != ErrorCode.CL_SUCCESS)
				throw new Exception("OpenCL error " + Err);
		}

		static cl_program CL_CreateProgram(string[] Sources) {
			for (int i = 0; i < Sources.Length; i++)
				Sources[i] = Sources[i].Replace("\r", "");

			cl_program Prog = CL.clCreateProgramWithSource(CLContext, Sources, out ErrorCode Err);
			CLCheckError(Err);
			return Prog;
		}

		public static void CreateRenderContext(string PlatformName = "amd") {
			const bool DisableOptimizations = true;

			string Include = "-I CL ";

			ErrorCode Err;

			cl_platform_id[] Platforms = CL.clGetPlatformIDs(out Err);
			cl_platform_id SelectedPlatform = Platforms[0];

			if (PlatformName != null) {
				foreach (var P in Platforms) {
					string Name = CL_GetPlatformName(P).ToLower();

					if (Name.Contains(PlatformName.ToLower())) {
						SelectedPlatform = P;
						break;
					}
				}
			}

			cl_device_id[] Devices = CL.clGetDeviceIDs(SelectedPlatform, cl_device_type.CL_DEVICE_TYPE_GPU, out Err);

			if (Devices.Length != 1)
				throw new Exception("Expected one GPU");

			CLDevice = Devices[0];

			//CL_GetDeviceInfo(Devices[0]);

			CLContext = CL.clCreateContext(null, 1, Devices, null, null, out Err);
			CLQueue = CL.clCreateCommandQueue(CLContext, Devices[0], cl_command_queue_properties.NONE, out Err);

			CLProgram = CL_CreateProgram(new[] { File.ReadAllText("CL/kernel.cl"), File.ReadAllText("CL/vertex.cl"), File.ReadAllText("CL/fragment.cl") });
			CLCheckError(CL.clBuildProgram(CLProgram, 0, null, Include + (DisableOptimizations ? "-cl-opt-disable " : ""), null, null));

			CLKernel = CL.clCreateKernel(CLProgram, "main", out Err);
			CLCheckError(Err);

			ulong Val = 0;
			Err = CL.clGetKernelWorkGroupInfo(CLKernel, Devices[0], cl_kernel_work_group_info.CL_KERNEL_WORK_GROUP_SIZE, sizeof(ulong), &Val, null);
			CLWorkGroupSize = (uint)Val;
		}

		public static void Draw(int TriangleBuffer, int Triangles) {
			Global.TriCount = Triangles / 3;

			fixed (FGLGlobal* GlobalPtr = &Global) {
				CL.clEnqueueWriteBuffer(CLQueue, CLGlobal, true, 0, (ulong)sizeof(FGLGlobal), GlobalPtr, 0, null, null);
			}

			CL.clSetKernelArg(CLKernel, 0, CLGlobal);
			CL.clSetKernelArg(CLKernel, 1, GetBuffer(ColorBuffer).Buffer);
			CL.clSetKernelArg(CLKernel, 2, GetBuffer(TriangleBuffer).Buffer);

			CLCheckError(CL.clEnqueueNDRangeKernel(CLQueue, CLKernel, 2, null, new ulong[] { (ulong)Global.Width, (ulong)Global.Height }, new ulong[] { (ulong)16, (ulong)16 }, 0, null, null));
		}

		static FGLBuffer GetBuffer(int BufferObject) {
			return BufferHandles.Get(BufferObject);
		}

		public static int CreateBuffer(int Size, FGL_BUFFER_FLAGS Flags = FGL_BUFFER_FLAGS.ReadWrite) {
			cl_mem_flags MemFlags = (cl_mem_flags)Flags | cl_mem_flags.CL_MEM_ALLOC_HOST_PTR;
			cl_mem Buffer = CL.clCreateBuffer(CLContext, MemFlags, (ulong)Size, null, out ErrorCode Err);
			CLCheckError(Err);

			return BufferHandles.Add(new FGLBuffer(Buffer, Size));
		}

		public static void DeleteBuffer(int BufferObject) {
			BufferHandles.Get(BufferObject).Delete();
			BufferHandles.Remove(BufferObject);
		}

		public static int CreateTexture(int Width, int Height, FGL_TEXTURE_TYPE Type) {
			int Len = Width * Height;

			switch (Type) {
				case FGL_TEXTURE_TYPE.RGB:
					Len *= sizeof(FGLColor);
					break;

				case FGL_TEXTURE_TYPE.Depth:
					Len *= sizeof(float);
					break;

				default:
					throw new Exception("Invalid texture type " + Type);
			}

			return CreateBuffer(Len);
		}

		public static void WriteBuffer(int BufferObject, void* Data, int Len) {
			CL.clEnqueueWriteBuffer(CLQueue, GetBuffer(BufferObject).Buffer, true, 0, (ulong)Len, Data, 0, null, null);
		}

		public static void WriteBuffer<T>(int BufferObject, T[] Data) where T : unmanaged {
			fixed (T* DataPtr = Data)
				WriteBuffer(BufferObject, DataPtr, sizeof(T) * Data.Length);
		}

		public static void* MapBuffer(int BufferObject, FGL_BUFFER_FLAGS Flags) {
			FGLBuffer Buffer = BufferHandles.Get(BufferObject);

			cl_map_flags MapFlags;

			if (Flags == FGL_BUFFER_FLAGS.ReadOnly)
				MapFlags = cl_map_flags.CL_MAP_READ;
			else if (Flags == FGL_BUFFER_FLAGS.WriteOnly)
				MapFlags = cl_map_flags.CL_MAP_WRITE;
			else if (Flags == FGL_BUFFER_FLAGS.ReadWrite)
				MapFlags = cl_map_flags.CL_MAP_READ | cl_map_flags.CL_MAP_WRITE;
			else
				throw new Exception("Invalid buffer map flags " + Flags);

			void* Mem = CL.clEnqueueMapBuffer(CLQueue, Buffer.Buffer, true, MapFlags, 0, (ulong)Buffer.Length, 0, null, null, out ErrorCode Err);
			CLCheckError(Err);

			Buffer.MappedMemory = Mem;
			return Mem;
		}

		public static void UnmapBuffer(int BufferObject) {
			FGLBuffer Buffer = BufferHandles.Get(BufferObject);

			CLCheckError(CL.clEnqueueUnmapMemObject(CLQueue, Buffer.Buffer, Buffer.MappedMemory, 0, null, null));

			Buffer.MappedMemory = null;
		}
	}
}
