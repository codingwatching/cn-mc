Shader "Vertex color unlit" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	}
		Category{
			Tags { "RenderQueue" = "Geometry" }
			Lighting Off
			BindChannels {
				Bind "Color", color
				Bind "Vertex", vertex
				Bind "TexCoord", texcoord
			}

			SubShader {
				Tags { "RenderType" = "Transparent Cutout" }
				Pass {
					SetTexture[_MainTex] {
						Combine texture * primary DOUBLE
					}
				}
			}
	}
}