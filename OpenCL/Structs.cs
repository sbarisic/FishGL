using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using cl_int = System.Int32;
using cl_uint = System.UInt32;
using size_t = System.UInt64;

namespace OpenCL {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_platform_id {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_device_id {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_context {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_command_queue {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_mem {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_program {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_kernel {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_event {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_sampler {
		IntPtr Pointer;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_bool {
		uint Value;

		public static implicit operator bool(cl_bool B) {
			return B.Value != 0;
		}

		public static implicit operator cl_bool(bool B) {
			return new cl_bool() { Value = B ? 1u : 0u };
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct cl_context_properties {
		IntPtr Pointer;
	}
}
