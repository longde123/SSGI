#pragma once

#include "global.h"
#include "Quad.h"
#include "Shader.h"
#include <OpenNI2\OpenNI.h>
#include <chrono>
#include <thread>
#include <atomic>

const GLuint maxDepth = 10000;

class DSensor
{

private:

	bool hasError = false, initOk = false, isRendering = true;;
	openni::Device device;
	openni::VideoStream depthStream, colorStream;
	openni::VideoStream** streams;
	GLuint texWidth, texHeight;
	openni::RGB888Pixel* texColorMap;
	uint16_t* texDepthMap;

	GLuint dsColorMap, dsDepthMap;
	int dsDepthMapLayerCounter = 1;

	GLuint minNumChunks(GLuint dataSize, GLuint chunkSize);
	GLuint minChunkSize(GLuint dataSize, GLuint chunkSize);
	void calculateHistogram(float* pHistogram, int histogramSize, const openni::VideoFrameRef& frame);

	glm::mat4 matProjection, matProjectionInverse;

	GLuint fbo, fbo2;
	GLuint outColorMap, outDepthMap, outPositionMap, outNormalMap;
	GLuint outColorMap2, outDepthMap2, outPositionMap2, outNormalMap2;
	Quad* quad;
	Shader* temporalMedianShader;
	Shader* medianShader;
	Shader* positionShader;
	Shader* blurShader;

	int tmfKernelRadius = 1;
	int tmfFrameLayers = 10;
	const int tmfMaxFrameLayers = 10;

	int fillKernelRadius = 5;
	int fillPasses = 11;

	std::vector<float> blurKernel;
	int blurKernelRadius = 22;
	float blurSigma = 32.0f;
	float blurBSigma = 1.0f;
	float blurBSigmaJBF = 0.00001f;
	float blurSThresh = 0.02f;

	float normpdf(float x, float s);
	void computeBlurKernel();

	std::vector<glm::vec3> customPositions;


public:

	DSensor();
	~DSensor();

	void initialize(int windowWidth, int windowHeight);
	void recompileShaders();
	void update();

	void launchUpdateThread();
	void updateThread(std::atomic<bool>& isRunning, unsigned int updateInterval);
	
	void toggleRendering();
	std::vector<glm::vec3>* getCustomPositions(int sampleRadius);

	GLuint getColorMapId() const;
	GLuint getDepthMapId() const;
	GLuint getPositionMapId() const;
	GLuint getNormalMapId() const;
	glm::mat4 getMatProjection() const;
	glm::mat4 getMatProjectionInverse() const;
	
	int getTMFKernelRadius() const;
	int getTMFFrameLayers() const;
	int getFillKernelRaidus() const;
	int getFillPasses() const;
	int getBlurKernelRadius() const;
	float getBlurSigma() const;
	float getBlurBSigma() const;

	void setTMFKernelRadius(int value);
	void setTMFFrameLayers(int value);
	void setFillKernelRaidus(int value);
	void setFillPasses(int value);
	void setBlurKernelRadius(int value);
	void setBlurSigma(float value);
	void setBlurBSigma(float value);

};