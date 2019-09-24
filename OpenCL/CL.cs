using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using cl_bool = System.UInt32;
using cl_int = System.Int32;
using cl_uint = System.UInt32;
using size_t = System.UInt64;

namespace OpenCL {
	public unsafe static class CL {
		const string LibName = "opencl.dll";

		[DllImport(LibName)]
		public static extern ErrorCode clGetPlatformIDs(cl_uint num_entries, cl_platform_id* platforms, cl_uint* num_platforms);

		public static cl_platform_id[] clGetPlatformIDs(out ErrorCode err) {
			cl_uint PlatformCount = 0;
			err = clGetPlatformIDs(0, null, &PlatformCount);

			if (err != ErrorCode.CL_SUCCESS)
				return null;

			cl_platform_id[] Platforms = new cl_platform_id[PlatformCount];

			fixed (cl_platform_id* PlatformsPtr = Platforms)
				err = clGetPlatformIDs(PlatformCount, PlatformsPtr, null);

			return Platforms;
		}

		[DllImport(LibName)]
		public static extern ErrorCode clGetPlatformInfo(cl_platform_id platform, cl_platform_info param_name, size_t param_value_size, void* param_value, size_t* param_value_size_ret);

		public static string clGetPlatformInfo(cl_platform_id platform, cl_platform_info param_name, out ErrorCode err) {
			size_t size = 0;
			err = clGetPlatformInfo(platform, param_name, 0, null, &size);

			if (err != ErrorCode.CL_SUCCESS)
				return null;

			byte[] data = new byte[size];
			fixed (byte* dataptr = data) {
				err = clGetPlatformInfo(platform, param_name, size, dataptr, null);

				if (err != ErrorCode.CL_SUCCESS)
					return null;
			}

			return Encoding.UTF8.GetString(data);
		}

		[DllImport(LibName)]
		public static extern ErrorCode clFinish(cl_command_queue command_queue);

		[DllImport(LibName)]
		public static extern cl_mem clCreateBuffer(cl_context context, cl_mem_flags flags, size_t size, void* host_ptr, out ErrorCode errcode_ret);

		[DllImport(LibName)]
		public static extern ErrorCode clEnqueueReadBuffer(cl_command_queue command_queue, cl_mem buffer, cl_bool blocking_read, size_t offset, size_t cb, void* ptr, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt);

		[DllImport(LibName)]
		public static extern ErrorCode clEnqueueWriteBuffer(cl_command_queue command_queue, cl_mem buffer, cl_bool blocking_write, size_t offset, size_t cb, void* ptr, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt);

		[DllImport(LibName)]
		public static extern ErrorCode clSetKernelArg(cl_kernel kernel, cl_uint arg_index, size_t arg_size, void* arg_value);

		public static ErrorCode clSetKernelArg(cl_kernel kernel, cl_uint arg_index, cl_mem mem) {
			return clSetKernelArg(kernel, arg_index, (size_t)sizeof(cl_mem), &mem);
		}

		[DllImport(LibName)]
		public static extern ErrorCode clReleaseMemObject(cl_mem memobj);

		[DllImport(LibName)]
		public static extern ErrorCode clGetProgramBuildInfo(cl_program program, cl_device_id device, cl_program_build_info param_name, size_t param_value_size, void* param_value, size_t* param_value_size_ret);

		public static string clGetProgramBuildInfoStr(cl_program program, cl_device_id device, cl_program_build_info param_name, out ErrorCode err) {
			size_t size = 0;
			err = clGetProgramBuildInfo(program, device, param_name, 0, null, &size);
			if (err != ErrorCode.CL_SUCCESS)
				return null;

			byte[] chars = new byte[size];
			fixed (byte* charsptr = chars) {
				err = clGetProgramBuildInfo(program, device, param_name, size, charsptr, null);

				if (err != ErrorCode.CL_SUCCESS)
					return null;
			}

			return Encoding.UTF8.GetString(chars);
		}

		[DllImport(LibName)]
		public static extern ErrorCode clEnqueueNDRangeKernel(cl_command_queue command_queue, cl_kernel kernel, cl_uint work_dim, size_t* global_work_offset, size_t* global_work_size, size_t* local_work_size, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt);

		public static ErrorCode clEnqueueNDRangeKernel(cl_command_queue command_queue, cl_kernel kernel, cl_uint work_dim, size_t[] global_work_offset, size_t[] global_work_size, size_t[] local_work_size, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt) {
			fixed (size_t* global_work_offset_ptr = global_work_offset)
			fixed (size_t* global_work_size_ptr = global_work_size)
			fixed (size_t* local_work_size_ptr = local_work_size) {
				return clEnqueueNDRangeKernel(command_queue, kernel, work_dim, global_work_offset_ptr, global_work_size_ptr, local_work_size_ptr, num_events_in_wait_list, event_wait_list, evt);
			}
		}

		[DllImport(LibName)]
		public static extern cl_program clCreateProgramWithSource(cl_context context, cl_uint count, char** strings, size_t* lengths, out ErrorCode errcode_ret);

		public static cl_program clCreateProgramWithSource(cl_context context, string[] strings, out ErrorCode errcode_ret) {
			size_t[] lengths = new size_t[strings.Length];
			for (int i = 0; i < strings.Length; i++)
				lengths[i] = (size_t)strings[i].Length;

			char*[] stringsptr = new char*[strings.Length];

			fixed (size_t* lenptr = lengths) {
				for (int i = 0; i < strings.Length; i++) {
					byte[] stringbytes = Encoding.UTF8.GetBytes(strings[i]);
					byte* stringptr = (byte*)Marshal.AllocHGlobal(stringbytes.Length);

					fixed (byte* stringbytesptr = stringbytes)
						Buffer.MemoryCopy(stringbytesptr, stringptr, stringbytes.Length, stringbytes.Length);

					stringsptr[i] = (char*)stringptr;
				}

				fixed (char** stringsptrptr = stringsptr)
					return clCreateProgramWithSource(context, (cl_uint)strings.Length, stringsptrptr, lenptr, out errcode_ret);
			}
		}

		[DllImport(LibName)]
		public static extern cl_context clCreateContext(cl_context_properties* properties, cl_uint num_devices, cl_device_id* devices, void* pfn_notify, void* user_data, out ErrorCode errcode_ret);

		public static cl_context clCreateContext(cl_context_properties* properties, cl_uint num_devices, cl_device_id[] devices, void* pfn_notify, void* user_data, out ErrorCode errcode_ret) {
			fixed (cl_device_id* devicesptr = devices)
				return clCreateContext(properties, num_devices, devicesptr, pfn_notify, user_data, out errcode_ret);
		}

		[DllImport(LibName)]
		public static extern cl_command_queue clCreateCommandQueue(cl_context context, cl_device_id device, cl_command_queue_properties properties, out ErrorCode errcode_ret);

		[DllImport(LibName)]
		public static extern ErrorCode clBuildProgram(cl_program program, cl_uint num_devices, cl_device_id* device_list, char* options, void* pfn_notify, void* user_data);

		public static ErrorCode clBuildProgram(cl_program program, cl_uint num_devices, cl_device_id* device_list, string options, void* pfn_notify, void* user_data) {
			byte[] opts = Encoding.UTF8.GetBytes(options);
			fixed (byte* optsptr = opts) {
				return clBuildProgram(program, num_devices, device_list, (char*)optsptr, pfn_notify, user_data);
			}
		}

		[DllImport(LibName)]
		public static extern cl_kernel clCreateKernel(cl_program program, char* kernel_name, out ErrorCode errcode_ret);

		public static cl_kernel clCreateKernel(cl_program program, string kernel_name, out ErrorCode errcode_ret) {
			byte[] kernel_name_bytes = Encoding.UTF8.GetBytes(kernel_name);
			fixed (byte* kernel_name_ptr = kernel_name_bytes) {
				return clCreateKernel(program, (char*)kernel_name_ptr, out errcode_ret);
			}
		}

		[DllImport(LibName)]
		public static extern ErrorCode clGetKernelWorkGroupInfo(cl_kernel kernel, cl_device_id device, cl_kernel_work_group_info param_name, size_t param_value_size, void* param_value, size_t* param_value_size_ret);

		[DllImport(LibName)]
		public static extern ErrorCode clGetDeviceIDs(cl_platform_id platform, cl_device_type device_type, cl_uint num_entries, cl_device_id* devices, cl_uint* num_devices);

		public static cl_device_id[] clGetDeviceIDs(cl_platform_id platform, cl_device_type device_type, out ErrorCode err) {
			uint num_devices = 0;
			err = clGetDeviceIDs(platform, device_type, 0, null, &num_devices);

			if (err != ErrorCode.CL_SUCCESS)
				return null;

			cl_device_id[] devices = new cl_device_id[num_devices];
			fixed (cl_device_id* devicesptr = devices)
				err = clGetDeviceIDs(platform, device_type, num_devices, devicesptr, null);

			return devices;
		}

		[DllImport(LibName)]
		public static extern void* clEnqueueMapBuffer(cl_command_queue command_queue, cl_mem buffer, cl_bool blocking_map, cl_map_flags map_flags, size_t offset, size_t cb, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt, out ErrorCode errcode_ret);

		[DllImport(LibName)]
		public static extern ErrorCode clEnqueueUnmapMemObject(cl_command_queue command_queue, cl_mem memobj, void* mapped_ptr, cl_uint num_events_in_wait_list, cl_event* event_wait_list, cl_event* evt);
	}
}
