#define FGL_INTERNAL
#include <FishGL.h>
#include <FishGLShader.h>

FGL_INLINE FglColor fglShaderSampleTexture(FglBuffer* TextureBuffer, int32_t X, int32_t Y) {
	if (X < 0 || X >= TextureBuffer->Width || Y < 0 || Y >= TextureBuffer->Height) {
		if (RenderState.TextureWrap == FglTextureWrap_BorderColor)
			return RenderState.BorderColor;
		else if (RenderState.TextureWrap == FglTextureWrap_Clamp) {
			if (X < 0)
				X = 0;
			else if (X >= TextureBuffer->Width)
				X = TextureBuffer->Width - 1;

			if (Y < 0)
				Y = 0;
			else if (Y >= TextureBuffer->Height)
				Y = TextureBuffer->Height - 1;
		}
		else
			return (FglColor) { 255, 50, 220, 255 };
	}

	return TextureBuffer->Pixels[Y * TextureBuffer->Width + X];
}

FGL_INLINE FglColor fglShaderSampleTextureUV(FglBuffer* TextureBuffer, vec2 UV) {
	return fglShaderSampleTexture(TextureBuffer, (int32_t)(UV[XElement] * TextureBuffer->Width), (int32_t)(UV[YElement] * TextureBuffer->Height));
}

FGL_INLINE FglVarying* fglShaderGetVarying(int32_t Num) {
	if (RenderState.CurShader == FglShaderType_Vertex)
		return &((FglVarying*)&RenderState.VarIn[Num])[RenderState.VertNum];
	else if (RenderState.CurShader == FglShaderType_Fragment)
		return &(RenderState.VarOut[Num]);

	return NULL;
}