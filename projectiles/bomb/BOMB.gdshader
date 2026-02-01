shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, unshaded;

uniform sampler2D albedo_texture : source_color;
uniform float growth_duration = 1.0;
uniform float wobble_frequency = 5.0;
uniform float wobble_amplitude = 0.05;
uniform float wobble_segments = 8.0; // Higher = choppier wobble
uniform vec4 color_a : source_color = vec4(1.0, 0.5, 0.0, 1.0);
uniform vec4 color_b : source_color = vec4(0.0, 0.5, 1.0, 1.0);
uniform float color_cycle_speed = 2.0;
instance uniform float scale = 0.0;
varying float v_lifetime;

void vertex() {
    // Get radial distance from center
    vec2 centered_uv = UV - vec2(0.5, 0.5);
    float angle = atan(centered_uv.y, centered_uv.x);

    // Quantize angle for choppy wobble effect
    float quantized_angle = floor(angle / (TAU / wobble_segments)) * (TAU / wobble_segments);

    // Apply choppy radial wobble
    float wobble = sin(quantized_angle * wobble_frequency + TIME * 2.0) * wobble_amplitude;
    vec3 radial_dir = normalize(vec3(centered_uv.x, 0.0, centered_uv.y));

    // Apply growth scale and wobble
    VERTEX = VERTEX * scale + radial_dir * wobble;
}

void fragment() {
    // Sample texture
    vec4 tex_color = texture(albedo_texture, UV);

    // Continuous color cycling using sine wave
    float color_cycle = sin(TIME * color_cycle_speed) * 0.5 + 0.5;
    vec4 modulated_color = mix(color_a, color_b, color_cycle);

    ALBEDO = tex_color.rgb * modulated_color.rgb;
    ALPHA = tex_color.a * modulated_color.a;
}
