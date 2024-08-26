// HLSL Shader for tweaking HSL elements
// TODO Look into .hlsl shaders (I do not understand this)
// ^ UPDATE I understand all but the technique pass stuff

float Hue : register(C0); // 0..360, default 0
float Saturation : register(C1); // 0..2, default 1
float Luminosity : register(C2); // -1..1, default 0

sampler2D input1 : register(S0);

static float3x3 matrixH =
{
	0.8164966f, 0, 0.5352037f,
	-0.4082483f, 0.70710677f, 1.0548190f,
	-0.4082483f, -0.70710677f, 0.1420281f
};

static float3x3 matrixH2 =
{
	0.84630f, -0.37844f, -0.37844f,
	-0.37265f, 0.33446f, -1.07975f,
	0.57735f, 0.57735f, 0.57735f
};

float4 MainPS(float2 uv : TEXCOORD) : COLOR
{
	float4 c = tex2D(input1, uv);

	float3x3 rotateZ =
	{
		cos(radians(Hue)), sin(radians(Hue)), 0,
		-sin(radians(Hue)), cos(radians(Hue)), 0,
		0, 0, 1
	};
	
	matrixH = mul(matrixH, rotateZ);
	matrixH = mul(matrixH, matrixH2);

	float i = 1 - Saturation;
	float3x3 matrixS =
	{
		i * 0.3086f + Saturation, i * 0.3086f, i * 0.3086f,
		i * 0.6094f, i * 0.6094f + Saturation, i * 0.6094f,
		i * 0.0820f, i * 0.0820f, i * 0.0820f + Saturation
	};
	
	matrixH = mul(matrixH, matrixS);

	float3 c1 = mul(c.rgb, matrixH);
	return float4(c1 + Luminosity, c.a);
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile ps_3_0 MainPS();
	}
};