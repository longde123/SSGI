#pragma once

#include "global.h"
#include "Timer.h"
#include "Shader.h"
#include "CameraFPS.h"
#include "Quad.h"
#include "SSAO.h"
#include <nanogui\nanogui.h>

class Scene
{
	
private:

	double currentTime, previousTime = 0.0;

	GLuint gBuffer, gPosition, gNormal, gColor, gDepth;
	Shader* gPassShader, * lightingPassShader;
	SSAO* ssao;
	float ssaoKernelRadius = 0.35f;
	float ssaoSampleBias = 0.005f;

	Camera* camera;
	Quad* quad;
	Object* sponza;
	GLuint tex;

	GLuint uniform_CamMat;

	nanogui::Screen* guiScreen;
	nanogui::FormHelper *gui;
	GLuint fboGui, texGui;

public:

	Scene();
	~Scene();

	void initialize(nanogui::Screen* guiScreen);
	void update();
	void render();

	void keyCallback(int key, int action);
	void cursorPosCallback(double x, double y);
	void mouseCallback(int button, int action);
	void windowSizeCallback(int x, int y);

};