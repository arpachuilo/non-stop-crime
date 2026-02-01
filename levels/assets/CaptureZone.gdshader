shader_type spatial;
render_mode blend_mix, depth_prepass_alpha, cull_disabled;

uniform vec4 base_color : source_color = vec4(0.2, 0.8, 1.0, 1.0);
uniform vec4 capture_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float capture_progress = 0.0; // 0.0 to 1.0
uniform float glow_strength = 2.0;

/* Box dimensions */
uniform float box_height = 2.0;

/* Gradient */
uniform float bottom_solid_height = 0.25;
uniform float alpha_falloff_power = 1.5;

/* Noise */
uniform float noise_scale = 6.0;
uniform float noise_strength = 0.15;
uniform float noise_speed = 1.2;
uniform float noise_wobble = 0.35;
uniform float noise_pulse = 0.25;

/* Mesh-locked varyings */
varying float local_y;
varying vec2 local_xz;
varying vec3 local_normal;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(a, b, u.x) +
	       (c - a) * u.y * (1.0 - u.x) +
	       (d - b) * u.x * u.y;
}

void vertex() {
	local_y = (VERTEX.y / box_height) + 0.5;
	local_y = clamp(local_y, 0.0, 1.0);

	local_xz = VERTEX.xz;

	/* Object-space normal */
	local_normal = NORMAL;
}

void fragment() {
	/* Alpha gradient */
	float alpha;
	if (local_y < bottom_solid_height) {
		alpha = 1.0;
	} else {
		float t = (local_y - bottom_solid_height) / (1.0 - bottom_solid_height);
		alpha = pow(1.0 - t, alpha_falloff_power);
	}

	/* Side-only mask (kills top face noise) */
	float side_mask = 1.0 - abs(normalize(local_normal).y);
	side_mask = pow(side_mask, 2.0); // optional sharpening

	/* Oscillating noise */
	float t = TIME * noise_speed;

	vec2 wobble = vec2(
		sin(t * 1.3),
		cos(t * 1.1)
	) * noise_wobble;

	vec2 noise_uv = local_xz * noise_scale + wobble;
	float n = noise(noise_uv);

	float pulse = 1.0 + sin(t * 2.0) * noise_pulse;

	/* Apply noise only to sides */
	alpha += (n - 0.5) * noise_strength * pulse * side_mask;

	/* Capture progress fill effect */
	vec4 final_color = base_color;
	if (capture_progress > 0.0) {
		float fill_height = capture_progress;
		float fill_mask = smoothstep(fill_height - 0.05, fill_height + 0.05, local_y);
		final_color = mix(capture_color, base_color, fill_mask);
	}

	ALBEDO = final_color.rgb;
	ALPHA = alpha * final_color.a;
	EMISSION = final_color.rgb * glow_strength * alpha;
}
