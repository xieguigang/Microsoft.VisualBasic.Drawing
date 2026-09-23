// The hlsl source of the 3d pipeline of the Scene3D namespace.
//
// This file is embedded into the Microsoft.VisualBasic.Drawing.DirectX assembly
// and compiled at run time by d3dcompiler_47.dll through D3DCompile, so the
// library has no build time dependency on the windows sdk. The entry points and
// the constant buffer layout below are the contract between this source and
// Scene3DShaders.vb (the SceneConstants structure and the Entry* constants), do
// not rename them without updating both sides.

cbuffer SceneConstants : register(b0)
{
    float4x4 worldViewProj;
    float4x4 worldRotation;
    float4   lightDirection;
    float4   lightColor;
    float4   shadingParams;
    float4   viewportScale;
    float4   unlitColor;
    float4   heatParams;
};

Texture2D    sceneTexture : register(t0);
SamplerState sceneSampler : register(s0);

struct SurfaceInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 color    : COLOR;
};

struct SurfaceOutput
{
    float4 position : SV_POSITION;
    float3 normal   : TEXCOORD0;
    float4 color    : COLOR;
};

struct PositionInput
{
    float3 position : POSITION;
};

struct PointInstanceInput
{
    float3 position : POSITION;
    float3 normal   : TEXCOORD0;
    float  heat     : TEXCOORD1;
    float4 color    : COLOR;
};

struct PointQuadInput
{
    float2 corner : TEXCOORD2;
};

struct PointOutput
{
    float4 position : SV_POSITION;
    float  heat     : TEXCOORD0;
    float4 color    : COLOR;
};

SurfaceOutput VS_Surface(SurfaceInput input)
{
    SurfaceOutput output;
    output.position = mul(worldViewProj, float4(input.position, 1));
    output.normal = input.normal;
    output.color = input.color;
    return output;
}

float4 PS_Surface(SurfaceOutput input) : SV_TARGET
{
    float3 n = input.normal;

    if (dot(n, n) < 1e-8f)
    {
        return input.color;
    }

    float3 lit = mul(worldRotation, float4(n, 0)).xyz;

    if (lit.z < 0)
    {
        lit = -lit;
    }

    float diffuse = max(0, dot(normalize(lit), normalize(lightDirection.xyz)));
    float ambient = lightColor.a;
    float factor = ambient + (1 - ambient) * diffuse;

    return float4(lerp(input.color.rgb, lightColor.rgb, factor), input.color.a);
}

float4 VS_Position(PositionInput input) : SV_POSITION
{
    return mul(worldViewProj, float4(input.position, 1));
}

float4 PS_Unlit(float4 position : SV_POSITION) : SV_TARGET
{
    return unlitColor;
}

PointOutput VS_Point(PointInstanceInput instance, PointQuadInput quad)
{
    PointOutput output;
    float4 clip = mul(worldViewProj, float4(instance.position, 1));

    if (clip.w <= 0.0001f)
    {
        output.position = float4(0, 0, -1, 1);
        output.heat = 0;
        output.color = instance.color;
        return output;
    }

    float heat = instance.heat;

    if (shadingParams.y > 0.5f)
    {
        float3 n = mul(worldRotation, float4(instance.normal, 0)).xyz;

        if (n.z < 0)
        {
            n = -n;
        }

        heat = lightColor.a + (1 - lightColor.a) * max(0, dot(normalize(n), normalize(lightDirection.xyz)));
    }

    clip.xy += quad.corner * shadingParams.x * viewportScale.xy * clip.w;
    output.position = clip;
    output.heat = heat;
    output.color = instance.color;
    return output;
}

float4 PS_Point(PointOutput input) : SV_TARGET
{
    if (shadingParams.y <= 0.5f && shadingParams.w > 0.5f && input.color.a > 0.0f)
    {
        return input.color;
    }

    float heat = (input.heat - heatParams.x) * heatParams.y;
    float levels = shadingParams.z;
    float index = floor(saturate(heat) * (levels - 1) + 0.5f);
    float u = (index + 0.5f) / levels;

    return sceneTexture.Sample(sceneSampler, float2(u, 0.5f));
}

struct BlitOutput
{
    float4 position : SV_POSITION;
    float2 uv       : TEXCOORD0;
};

BlitOutput VS_Blit(uint vertexId : SV_VertexID)
{
    BlitOutput output;
    float2 corner = float2((vertexId << 1) & 2, vertexId & 2);

    output.uv = corner;
    output.position = float4(corner * float2(2, -2) + float2(-1, 1), 0, 1);
    return output;
}

float4 PS_Blit(BlitOutput input) : SV_TARGET
{
    float4 color = sceneTexture.Sample(sceneSampler, input.uv);

    return float4(color.rgb, 1.0f);
}
