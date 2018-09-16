#pragma once

#define FGL_MAX_TEXTURES 16
#define FGL_VARYING_COUNT 3 // Must be more or equal to 1

#define FGL_API __declspec(dllexport)
#define FGL_INLINE __forceinline

// For a custom allocator, used by some stb libraries
// Can be a stub function if you don't plan on using
// the functions which actually allocate memory
//#define FGL_MALLOC
//#define FGL_REALLOC
//#define FGL_FREE

#ifndef FGL_API
#define FGL_API
#endif

#ifndef FGL_INLINE
#define FGL_INLINE
#endif

#ifdef FGL_IMPLEMENTATION
#define FGL_EXTERN
#else
#define FGL_EXTERN extern
#endif

#include <stdint.h>
#include <string.h>
#include <stdbool.h>
#include <cglm.h>

#ifdef FGL_IMPLEMENTATION
#define STB_IMAGE_IMPLEMENTATION
#endif

#ifdef FGL_MALLOC
#define STBI_MALLOC FGL_MALLOC
#endif
#ifdef FGL_REALLOC
#define STBI_REALLOC FGL_REALLOC
#endif
#ifdef FGL_FREE
#define STBI_FREE FGL_FREE
#endif

#define STBI_NO_STDIO
#define STBI_ONLY_PNG
#include <stb_image.h>