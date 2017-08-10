#pragma once

#include "global.h"
#include "Quad.h"
#include "Shader.h"
#include <random>

class SSAO
{

private:

	int downscaleFactor = 2;

	int kernelSize = 64;
	float kernelRadius = 0.05f;
	int samples = 24;
	float bias = 0.003f;
	float intensity = 0.05f;
	float power = 1.0f;
	
	std::vector<float> blurKernel;
	int blurKernelRadius = 10;
	float blurSigma = 10.0f;
	float blurBSigma = 0.1f;

	std::vector<glm::vec3> kernel;
	GLuint noiseTexId;
	GLuint fbo, fboCombined;
	Quad* quad;
	Shader* shader, * mixLayerShader, * upsampleShader;
	GLuint texWidth, texHeight, texWidthSmall, texHeightSmall;
	GLuint texSSAO, texFilterH, texFilterV1, texFilterV2, texCombined;

	GLuint positionMapId, normalMapId, colorMapId;

	float normpdf(float x, float s);
	void computeBlurKernel();

public:

	SSAO(int width, int height);
	~SSAO();

	void initializeShaders();
	void recompileShaders();
	void drawLayer(int layer, GLuint positionMapId, GLuint normalMapId, GLuint colorMapId);
	void drawCombined(GLuint colorMapId);

	GLuint getTextureLayer(int layer) const;
	int getKernelSize() const;
	float getKernelRadius() const;
	int getSamples() const;
	float getBias() const;
	float getIntensity() const;
	float getPower() const;
	int getBlurKernelRadius() const;
	float getBlurSigma() const;
	float getBlurBSigma() const;

	void setKernelSize(int value);
	void setKernelRadius(float value);
	void setSamples(int value);
	void setBias(float value);
	void setIntensity(float value);
	void setPower(float value);
	void setBlurKernelRadius(int value);
	void setBlurSigma(float value);
	void setBlurBSigma(float value);


};