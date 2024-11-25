sampler baseTarget : register(s0);
sampler velocityField : register(s1);
sampler turbulenceTexture : register(s2);
sampler densityField : register(s3);

float globalTime;
float deltaTime;
float gravity;
float2 simulationSize;
float2 stepSize;
float2 force;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return tex2D(baseTarget, coords);
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}