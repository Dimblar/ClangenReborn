sampler2D input1 : register(S0);

static float3x3 transformMatrix =
{
    0.86172147555016048f, 0.06172463884296048f, 0.06172463884296048f,
	0.121875957340278196f, 0.921876190201638196f, 0.121878218040278196f,
	0.016402096489561324f, 0.016398700335401324f, 0.816396672496761324f
};

float4 MainPS(float2 uv : TEXCOORD) : COLOR
{
    float4 c = tex2D(input1, uv);
    return float4(mul(c.rgb, transformMatrix) - 0.2f, c.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
};