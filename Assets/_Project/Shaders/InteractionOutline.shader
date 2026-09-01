Shader "TheShadowWood/InteractionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.9, 0.85, 0.55, 1)
        _OutlineWidth ("Outline Width", Range(0.0001, 0.05)) = 0.003
        _OutlineCenter ("Mesh Local Center", Vector) = (0, 0, 0, 0)
        _RadialWeight ("Radial Extrusion", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
        }

        Pass
        {
            Name "InteractionOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
                float4 _OutlineCenter;
                float _RadialWeight;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // Hard-surface meshes such as Unity's cube duplicate vertices per face.
                // Pure normal extrusion pulls those faces apart at the corners. Radial
                // extrusion keeps the fallback hull connected; organic meshes can blend
                // back towards their vertex normals with Radial Extrusion.
                float3 normalDirection = normalize(input.normalOS);
                float3 radialVector = input.positionOS.xyz - _OutlineCenter.xyz;
                float radialLengthSquared = dot(radialVector, radialVector);
                float3 radialDirection = radialLengthSquared > 0.000001
                    ? radialVector * rsqrt(radialLengthSquared)
                    : normalDirection;
                float3 extrusionDirection = normalize(lerp(normalDirection, radialDirection, _RadialWeight));
                float3 expandedPosition = input.positionOS.xyz + extrusionDirection * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(expandedPosition);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
