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
	public enum FGL_DEVICE_TYPE {
		CPU,
		GPU,
		Accelerator
	}

	public class FGLDevice {
		public string Extensions;
		public string Name;
		public string Profile;
		public string Vendor;
		public string Version;
		public FGL_DEVICE_TYPE DeviceType;

		internal cl_platform_id Platform;
		internal cl_device_id Device;

		public FGLDevice(string Extensions, string Name, string Profile, string Vendor, string Version, FGL_DEVICE_TYPE DeviceType, cl_platform_id Platform, cl_device_id Device) {
			this.Extensions = Extensions;
			this.Name = Name;
			this.Profile = Profile;
			this.Vendor = Vendor;
			this.Version = Version;
			this.DeviceType = DeviceType;
			this.Platform = Platform;
			this.Device = Device;
		}

		public override string ToString() {
			return string.Format("{0} - {1}, {2}, {3}", DeviceType, Vendor, Name, Profile);
		}
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

	interface ICLBuffer {
		void Delete();
	}

	unsafe class FGLBuffer : ICLBuffer {
		public FGLRenderContext Owner;
		public cl_mem Buffer;
		public int Length;
		public cl_mem_flags MemFlags;

		public void* MappedMemory;

		public FGLBuffer(FGLRenderContext Owner, cl_mem Buffer, int Length, cl_mem_flags MemFlags) {
			this.Owner = Owner;
			this.Buffer = Buffer;
			this.Length = Length;
			this.MemFlags = MemFlags;

			MappedMemory = null;
		}

		public void Delete() {
			FGL.CLCheckError(CL.clReleaseMemObject(Buffer));
		}
	}

	unsafe class FGLFramebuffer : ICLBuffer {
		public FGLTexture ColorTexture;
		public FGLTexture DepthTexture;

		public FGLFramebuffer() {
			ColorTexture = null;
			DepthTexture = null;
		}

		public void Delete() {
		}
	}

	unsafe class FGLTexture : ICLBuffer {
		public cl_mem TextureBuffer;
		public int Width;
		public int Height;
		public int Length;
		public FGL_TEXTURE_TYPE Type;
		public void* MappedMemory;


		public FGLTexture(cl_mem TextureBuffer, int Width, int Height, int Length, FGL_TEXTURE_TYPE Type, void* MappedMemory) {
			this.TextureBuffer = TextureBuffer;
			this.Width = Width;
			this.Height = Height;
			this.Length = Length;
			this.Type = Type;
			this.MappedMemory = MappedMemory;
		}

		public void Delete() {
			FGL.CLCheckError(CL.clReleaseMemObject(TextureBuffer));
		}
	}

	class FGLRenderContext {
		public FGLDevice Dev;
		public cl_context Ctx;
		public cl_command_queue Queue;
		public cl_device_id Device;
		public cl_program Program;
		public cl_kernel MainKernel;
		public uint WorkGroupSize;

		public FGLRenderContext(FGLDevice Dev, cl_context Ctx, cl_command_queue Queue, cl_device_id Device, cl_program Program, cl_kernel MainKernel, uint WorkGroupSize) {
			this.Dev = Dev;
			this.Ctx = Ctx;
			this.Queue = Queue;
			this.Device = Device;
			this.Program = Program;
			this.MainKernel = MainKernel;
			this.WorkGroupSize = WorkGroupSize;
		}
	}

	class FGLWindow {
		public int Width;
		public int Height;

		public IntPtr Wnd;
		public IntPtr Rnd;
		public IntPtr Tex;

		public uint WindowID;
		public bool WindowOpen;

		static FGLWindow() {
			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
		}

		public FGLWindow(int Width, int Height) {
			this.Width = Width;
			this.Height = Height;

			SDL.SDL_CreateWindowAndRenderer(Width, Height, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN, out Wnd, out Rnd);
			Tex = SDL.SDL_CreateTexture(Rnd, SDL.SDL_PIXELFORMAT_RGB24, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, Width, Height);

			WindowID = SDL.SDL_GetWindowID(Wnd);
			WindowOpen = true;
		}

		public void SetWindowTitle(string Title) {
			if (!WindowOpen)
				return;

			SDL.SDL_SetWindowTitle(Wnd, Title);
		}

		public void Close() {
			WindowOpen = false;

			SDL.SDL_DestroyTexture(Tex);
			SDL.SDL_DestroyRenderer(Rnd);
			SDL.SDL_DestroyWindow(Wnd);
		}
	}

	class FGLBufferList<T> where T : class {
		List<T> Buffers;

		public FGLBufferList() {
			Buffers = new List<T>();
		}

		public int Add(T Buffer) {
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

		public void Remove<T>(T Obj) {
			for (int i = 0; i < Buffers.Count; i++) {
				if (Buffers[i].Equals(Obj)) {
					Buffers[i] = null;
					return;
				}
			}

			throw new Exception("Object reference not found");
		}

		public T Get(int Handle) {
			if (Buffers[Handle] == null || Handle < 0 || Handle >= Buffers.Count)
				throw new Exception("Invalid FGL buffer object " + Handle);

			return Buffers[Handle];
		}

		public T2 Get<T2>(int Handle) where T2 : T {
			return (T2)Get(Handle);
		}

		public IEnumerable<T2> GetAll<T2>() where T2 : T {
			foreach (var Itm in Buffers)
				if (Itm is T2 ItmT2)
					yield return ItmT2;
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

	public enum FGL_FRAMEBUFFER_ATTACHMENT {
		Color,
		Depth,
	}

	public unsafe static class FGL {
		/*static cl_context CLContext;
		static cl_command_queue CLQueue;
		static cl_device_id CLDevice;
		static cl_program CLProgram;
		static cl_kernel CLKernel;
		static uint CLWorkGroupSize;*/

		/*static FGLBufferList<FGLBuffer> BufferHandles = new FGLBufferList<FGLBuffer>();
		static FGLBufferList<FGLFramebuffer> FramebufferHandles = new FGLBufferList<FGLFramebuffer>();
		static FGLBufferList<FGLTexture> TextureHandles = new FGLBufferList<FGLTexture>();*/

		static FGLBufferList<object> FGLObjects = new FGLBufferList<object>();

		public static int CreateWindow(int W, int H) {
			return FGLObjects.Add(new FGLWindow(W, H));
		}

		public static void DestroyWindow(int WindowHandle) {
			FGLObjects.Get<FGLWindow>(WindowHandle).Close();
		}

		public static void SetWindowTitle(int WindowHandle, string Title) {
			FGLObjects.Get<FGLWindow>(WindowHandle).SetWindowTitle(Title);
		}

		public static bool WindowOpen(int WindowHandle) {
			return FGLObjects.Get<FGLWindow>(WindowHandle).WindowOpen;
		}

		public static void PollEvents() {
			while (SDL.SDL_PollEvent(out SDL.SDL_Event E) != 0) {
				if (E.type == SDL.SDL_EventType.SDL_WINDOWEVENT && E.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) {
					FGLWindow[] Windows = FGLObjects.GetAll<FGLWindow>().ToArray();

					foreach (var Wnd in Windows)
						if (Wnd.WindowID == E.window.windowID)
							Wnd.Close();
				}
			}
		}

		static void ClearTexture(FGLTexture Tex, FGLColor ClearColor) {
			FGLColor* Pixels = (FGLColor*)Tex.MappedMemory;

			for (int i = 0; i < Tex.Width * Tex.Height; i++)
				Pixels[i] = ClearColor;
		}

		public static void Clear(int TextureHandle, FGLColor ClearColor) {
			ClearTexture(FGLObjects.Get<FGLTexture>(TextureHandle), ClearColor);
		}

		public static void ClearFramebuffer(int FramebufferHandle, FGLColor ClearColor) {
			FGLFramebuffer Framebuffer = FGLObjects.Get<FGLFramebuffer>(FramebufferHandle);
			ClearTexture(Framebuffer.ColorTexture, ClearColor);
		}

		public static void Finish(int ContextHandle) {
			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);
			CL.clFinish(Ctx.Queue);
		}

		static void Swap(FGLTexture Texture, int WindowHandle) {
			FGLWindow Window = FGLObjects.Get<FGLWindow>(WindowHandle);

			SDL.SDL_Rect Rect = new SDL.SDL_Rect();
			Rect.x = Rect.y = 0;
			Rect.w = Window.Width;
			Rect.h = Window.Height;

			SDL.SDL_UpdateTexture(Window.Tex, ref Rect, new IntPtr(Texture.MappedMemory), sizeof(FGLColor) * Window.Width);
			SDL.SDL_RenderClear(Window.Rnd);
			SDL.SDL_RenderCopy(Window.Rnd, Window.Tex, IntPtr.Zero, IntPtr.Zero);
			SDL.SDL_RenderPresent(Window.Rnd);
		}

		public static void SwapFramebuffer(int FramebufferHandle, int WindowHandle) {
			Swap(FGLObjects.Get<FGLFramebuffer>(FramebufferHandle).ColorTexture, WindowHandle);
		}

		/*static string CL_GetDeviceInfo(Device Dev) {
			Dictionary<DeviceInfo, string> DDD = new Dictionary<DeviceInfo, string>();

			DeviceInfo[] Infos = (DeviceInfo[])Enum.GetValues(typeof(DeviceInfo));
			foreach (var I in Infos) {
				DDD.Add(I, Cl.GetDeviceInfo(Dev, I, out ErrorCode Err).ToString());
			}

			return null;
		}*/

		internal static void CLCheckError(ErrorCode Err, cl_program? Prog = null, cl_device_id? Dev = null) {
			if (Err == ErrorCode.CL_BUILD_PROGRAM_FAILURE) {
				string Log = CL.clGetProgramBuildInfoStr(Prog.Value, Dev.Value, cl_program_build_info.CL_PROGRAM_BUILD_LOG, out ErrorCode Tmp);
				Console.WriteLine(Log);
			}

			if (Err != ErrorCode.CL_SUCCESS)
				throw new Exception("OpenCL error " + Err);
		}

		static cl_program CL_CreateProgram(cl_context Ctx, cl_device_id Dev, string[] Sources) {
			for (int i = 0; i < Sources.Length; i++)
				Sources[i] = Sources[i].Replace("\r", "");

			cl_program Prog = CL.clCreateProgramWithSource(Ctx, Sources, out ErrorCode Err);
			CLCheckError(Err, Prog, Dev);
			return Prog;
		}

		static FGLDevice CreateFGLDevice(cl_platform_id Plat, cl_device_id Dev, FGL_DEVICE_TYPE DevType) {
			ErrorCode err;

			string Ext = CL.clGetPlatformInfo(Plat, cl_platform_info.CL_PLATFORM_EXTENSIONS, out err);
			string Name = CL.clGetPlatformInfo(Plat, cl_platform_info.CL_PLATFORM_NAME, out err);
			string Profile = CL.clGetPlatformInfo(Plat, cl_platform_info.CL_PLATFORM_PROFILE, out err);
			string Vendor = CL.clGetPlatformInfo(Plat, cl_platform_info.CL_PLATFORM_VENDOR, out err);
			string Version = CL.clGetPlatformInfo(Plat, cl_platform_info.CL_PLATFORM_VERSION, out err);

			return new FGLDevice(Ext, Name, Profile, Vendor, Version, DevType, Plat, Dev);
		}

		public static FGLDevice[] GetAllDevices() {
			ErrorCode Err;
			List<FGLDevice> Devices = new List<FGLDevice>();

			cl_platform_id[] Platforms = CL.clGetPlatformIDs(out Err);
			CLCheckError(Err);

			for (int i = 0; i < Platforms.Length; i++) {
				cl_device_id[] DevicesGPU = CL.clGetDeviceIDs(Platforms[i], cl_device_type.CL_DEVICE_TYPE_GPU, out Err);
				cl_device_id[] DevicesCPU = CL.clGetDeviceIDs(Platforms[i], cl_device_type.CL_DEVICE_TYPE_CPU, out Err);
				cl_device_id[] DevicesAccelerators = CL.clGetDeviceIDs(Platforms[i], cl_device_type.CL_DEVICE_TYPE_ACCELERATOR, out Err);

				if (DevicesGPU != null)
					foreach (var GPU in DevicesGPU)
						Devices.Add(CreateFGLDevice(Platforms[i], GPU, FGL_DEVICE_TYPE.GPU));

				if (DevicesCPU != null)
					foreach (var CPU in DevicesCPU)
						Devices.Add(CreateFGLDevice(Platforms[i], CPU, FGL_DEVICE_TYPE.CPU));

				if (DevicesAccelerators != null)
					foreach (var Accel in DevicesAccelerators)
						Devices.Add(CreateFGLDevice(Platforms[i], Accel, FGL_DEVICE_TYPE.Accelerator));
			}

			return Devices.ToArray();
		}

		public static int CreateRenderContext(FGLDevice Dev) {
			const bool DisableOptimizations = false;
			string Include = "-I CL ";
			cl_device_id Device = Dev.Device;

			ErrorCode Err;
			cl_context Ctx = CL.clCreateContext(null, 1, &Device, null, null, out Err);
			cl_command_queue Queue = CL.clCreateCommandQueue(Ctx, Device, cl_command_queue_properties.NONE, out Err);

			cl_program Prog = CL_CreateProgram(Ctx, Device, new[] { File.ReadAllText("CL/kernel.cl"), File.ReadAllText("CL/vertex.cl"), File.ReadAllText("CL/fragment.cl") });
			CLCheckError(CL.clBuildProgram(Prog, 0, null, Include + (DisableOptimizations ? "-cl-opt-disable " : ""), null, null), Prog, Device);

			cl_kernel MainKernel = CL.clCreateKernel(Prog, "_main", out Err);
			CLCheckError(Err);

			ulong Val = 0;
			Err = CL.clGetKernelWorkGroupInfo(MainKernel, Device, cl_kernel_work_group_info.CL_KERNEL_WORK_GROUP_SIZE, sizeof(ulong), &Val, null);
			uint WorkGroupSize = (uint)Val;

			return FGLObjects.Add(new FGLRenderContext(Dev, Ctx, Queue, Device, Prog, MainKernel, WorkGroupSize));
		}

		public static void Draw(int ContextHandle, int FramebufferHandle, int VertexBuffer, int ColorBuffer, int VertexCount) {
			FGLFramebuffer Framebuffer = FGLObjects.Get<FGLFramebuffer>(FramebufferHandle);

			if (Framebuffer.ColorTexture == null)
				throw new Exception("Framebuffer has no color texture");

			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);

			CLCheckError(CL.clSetKernelArg<int>(Ctx.MainKernel, 0, Framebuffer.ColorTexture.Width));
			CLCheckError(CL.clSetKernelArg<int>(Ctx.MainKernel, 1, Framebuffer.ColorTexture.Height));
			CLCheckError(CL.clSetKernelArg<int>(Ctx.MainKernel, 2, VertexCount));
			CLCheckError(CL.clSetKernelArg(Ctx.MainKernel, 3, Framebuffer.ColorTexture.TextureBuffer));
			CLCheckError(CL.clSetKernelArg(Ctx.MainKernel, 4, FGLObjects.Get<FGLBuffer>(VertexBuffer).Buffer));
			CLCheckError(CL.clSetKernelArg(Ctx.MainKernel, 5, FGLObjects.Get<FGLBuffer>(ColorBuffer).Buffer));

			ulong[] GlobalWorkSet = new ulong[] { (ulong)Framebuffer.ColorTexture.Width, (ulong)Framebuffer.ColorTexture.Height };
			CLCheckError(CL.clEnqueueNDRangeKernel(Ctx.Queue, Ctx.MainKernel, 2, null, GlobalWorkSet, new ulong[] { 1, 1 }, 0, null, null));
		}

		public static int CreateBuffer(int ContextHandle, int Size, FGL_BUFFER_FLAGS Flags = FGL_BUFFER_FLAGS.ReadWrite) {
			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);

			cl_mem_flags MemFlags = (cl_mem_flags)Flags;
			cl_mem Buffer = CL.clCreateBuffer(Ctx.Ctx, MemFlags, (ulong)Size, null, out ErrorCode Err);
			CLCheckError(Err);

			return FGLObjects.Add(new FGLBuffer(Ctx, Buffer, Size, MemFlags));
		}

		public static int CreateSharedBuffer(int ContextHandle, int SharedBuffer) {
			FGLBuffer BufferObject = FGLObjects.Get<FGLBuffer>(SharedBuffer);
			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);
			MapBuffer(SharedBuffer, FGL_BUFFER_FLAGS.ReadWrite);

			cl_mem Buffer = CL.clCreateBuffer(Ctx.Ctx, BufferObject.MemFlags | cl_mem_flags.CL_MEM_USE_HOST_PTR, (ulong)BufferObject.Length, BufferObject.MappedMemory, out ErrorCode Err);
			CLCheckError(Err);

			return FGLObjects.Add(new FGLBuffer(Ctx, Buffer, BufferObject.Length, BufferObject.MemFlags));
		}

		public static void DeleteBuffer(int BufferHandle) {
			FGLObjects.Get<FGLBuffer>(BufferHandle).Delete();
			FGLObjects.Remove(BufferHandle);
		}

		public static void WriteBuffer(int BufferHandle, void* Data, int Len) {
			FGLBuffer Buffer = FGLObjects.Get<FGLBuffer>(BufferHandle);

			CLCheckError(CL.clEnqueueWriteBuffer(Buffer.Owner.Queue, Buffer.Buffer, true, 0, (ulong)Len, Data, 0, null, null));
		}

		public static void WriteBuffer<T>(int BufferHandle, T[] Data) where T : unmanaged {
			fixed (T* DataPtr = Data)
				WriteBuffer(BufferHandle, DataPtr, sizeof(T) * Data.Length);
		}

		public static void* MapBuffer(int BufferHandle, FGL_BUFFER_FLAGS Flags) {
			cl_map_flags MapFlags;

			if (Flags == FGL_BUFFER_FLAGS.ReadOnly)
				MapFlags = cl_map_flags.CL_MAP_READ;
			else if (Flags == FGL_BUFFER_FLAGS.WriteOnly)
				MapFlags = cl_map_flags.CL_MAP_WRITE;
			else if (Flags == FGL_BUFFER_FLAGS.ReadWrite)
				MapFlags = cl_map_flags.CL_MAP_READ | cl_map_flags.CL_MAP_WRITE;
			else
				throw new Exception("Invalid buffer map flags " + Flags);

			FGLBuffer Buffer = FGLObjects.Get<FGLBuffer>(BufferHandle);

			if (Buffer.MappedMemory != null)
				return Buffer.MappedMemory;

			void* Mem = CL.clEnqueueMapBuffer(Buffer.Owner.Queue, Buffer.Buffer, true, MapFlags, 0, (ulong)Buffer.Length, 0, null, null, out ErrorCode Err);
			CLCheckError(Err);

			Buffer.MappedMemory = Mem;
			return Mem;
		}

		public static void UnmapBuffer(int BufferHandle) {
			FGLBuffer Buffer = FGLObjects.Get<FGLBuffer>(BufferHandle);

			CLCheckError(CL.clEnqueueUnmapMemObject(Buffer.Owner.Queue, Buffer.Buffer, Buffer.MappedMemory, 0, null, null));
			Buffer.MappedMemory = null;
		}

		public static int CreateTexture(int ContextHandle, int Width, int Height, FGL_TEXTURE_TYPE Type) {
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

			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);

			cl_mem TextureBuffer = CL.clCreateBuffer(Ctx.Ctx, cl_mem_flags.CL_MEM_READ_WRITE | cl_mem_flags.CL_MEM_ALLOC_HOST_PTR, (ulong)Len, null, out ErrorCode Err);
			CLCheckError(Err);

			void* MappedMemory = CL.clEnqueueMapBuffer(Ctx.Queue, TextureBuffer, true, cl_map_flags.CL_MAP_READ | cl_map_flags.CL_MAP_WRITE, 0, (ulong)Len, 0, null, null, out Err);
			CLCheckError(Err);

			return FGLObjects.Add(new FGLTexture(TextureBuffer, Width, Height, Len, Type, MappedMemory));
		}

		public static void DeleteTexture(int ContextHandle, int TextureHandle) {
			FGLRenderContext Ctx = FGLObjects.Get<FGLRenderContext>(ContextHandle);
			FGLTexture Tex = FGLObjects.Get<FGLTexture>(TextureHandle);

			CLCheckError(CL.clEnqueueUnmapMemObject(Ctx.Queue, Tex.TextureBuffer, Tex.MappedMemory, 0, null, null));
			Tex.Delete();
			FGLObjects.Remove(TextureHandle);
		}

		public static int CreateFramebuffer() {
			return FGLObjects.Add(new FGLFramebuffer());
		}

		public static void DeleteFramebuffer(int FramebufferHandle) {
			FGLFramebuffer Framebuffer = FGLObjects.Get<FGLFramebuffer>(FramebufferHandle);
			Framebuffer.Delete();
			FGLObjects.Remove(FramebufferHandle);
		}

		public static void FramebufferAttachTexture(int FramebufferHandle, int TextureHandle, FGL_FRAMEBUFFER_ATTACHMENT Attachment) {
			FGLFramebuffer Framebuffer = FGLObjects.Get<FGLFramebuffer>(FramebufferHandle);
			FGLTexture Texture = FGLObjects.Get<FGLTexture>(TextureHandle);

			switch (Attachment) {
				case FGL_FRAMEBUFFER_ATTACHMENT.Color:
					Framebuffer.ColorTexture = Texture;
					break;

				case FGL_FRAMEBUFFER_ATTACHMENT.Depth:
					Framebuffer.DepthTexture = Texture;
					break;

				default:
					throw new Exception("Unknown framebuffer texture attachment " + Attachment);
			}
		}
	}
}
