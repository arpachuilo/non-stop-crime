shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, unshaded;

uniform sampler2D fire_texture : source_color;
uniform float wiggle_strength : hint_range(0.0, 0.1) = 0.02;
uniform float wiggle_speed : hint_range(0.0, 5.0) = 1.0;
uniform vec2 wiggle_frequency = vec2(3.0, 4.0);
uniform bool billboard_enabled = true;

uniform vec3 emission : source_color;

instance uniform float scale = 1.0;

// Simple noise function
float noise(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

// Smooth noise
float smooth_noise(vec2 uv) {
    vec2 i = floor(uv);
    vec2 f = fract(uv);
    f = f * f * (3.0 - 2.0 * f);

    float a = noise(i);
    float b = noise(i + vec2(1.0, 0.0));
    float c = noise(i + vec2(0.0, 1.0));
    float d = noise(i + vec2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void vertex() {
    if (billboard_enabled) {
        mat4 mat_world = mat4(
            normalize(INV_VIEW_MATRIX[0]),
            normalize(INV_VIEW_MATRIX[1]),
            normalize(INV_VIEW_MATRIX[2]),
            MODEL_MATRIX[3]
        );
       mat_world = mat_world * mat4(
            vec4(length(MODEL_MATRIX[0].xyz) * scale, 0.0, 0.0, 0.0),
            vec4(0.0, length(MODEL_MATRIX[1].xyz) * scale, 0.0, 0.0),
            vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz) * scale, 0.0),
            vec4(0.0, 0.0, 0.0, 1.0)
        );
        MODELVIEW_MATRIX = VIEW_MATRIX * mat_world;
    } else {
	  VERTEX *= scale;
	}
}

void fragment() {
    vec2 uv = UV;

    // Create wiggle offset using animated noise
    float time_offset = TIME * wiggle_speed;
    vec2 noise_uv = uv * wiggle_frequency + vec2(time_offset, time_offset * 0.7);

    float noise_x = smooth_noise(noise_uv) * 2.0 - 1.0;
    float noise_y = smooth_noise(noise_uv + vec2(5.3, 2.1)) * 2.0 - 1.0;

    vec2 wiggle_offset = vec2(noise_x, noise_y) * wiggle_strength;

    // Sample fire texture with wiggle offset
    vec2 distorted_uv = uv + wiggle_offset;
    vec4 tex = texture(fire_texture, distorted_uv);

    ALBEDO = tex.rgb;
    ALPHA = tex.a;
}
