sampler2D input1 : register(S0);

static float3x3 transformMatrix =
{
    0.3085892009682f, 0.3085892009682f, 0.3085892009682f,
	0.60939628462339098f, 0.60939628462339098f, 0.60939628462339098f,
	0.08199958210580662f, 0.08199958210580662f, 0.08199958210580662f
};

float4 MainPS(float2 uv : TEXCOORD) : COLOR
{
    float4 c = tex2D(input1, uv);
    return float4(mul(c.rgb, transformMatrix), c.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};