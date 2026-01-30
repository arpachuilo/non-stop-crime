shader_type spatial;
render_mode unshaded, depth_draw_never, cull_disabled;

uniform vec4 top_color : source_color = vec4(0.10, 0.14, 0.28, 1.0);
uniform vec4 bottom_color : source_color = vec4(0.03, 0.03, 0.04, 1.0);

uniform float boundary_y = 0.52;
uniform float arc_curvature = 0.18;
uniform float band_softness = 0.06;

uniform float wobble_amp = 0.015;
uniform float wobble_freq = 2.0;
uniform float wobble_speed = 0.01;

uniform float shimmer_amp = 0.006;
uniform float shimmer_freq = 18.0;
uniform float shimmer_speed = 1.2;

uniform float noise_amount = 0.75;

float hash21(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 34.345);
  return fract(p.x * p.y);
}

float value_noise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f * f * (3.0 - 2.0 * f);

  float a = hash21(i + vec2(0.0, 0.0));
  float b = hash21(i + vec2(1.0, 0.0));
  float c = hash21(i + vec2(0.0, 1.0));
  float d = hash21(i + vec2(1.0, 1.0));

  return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void fragment() {
  // Use SCREEN_UV for camera-relative shading, or UV for object-relative shading
  vec2 uv = UV;

  float x = uv.x - 0.5;
  float arc = boundary_y - arc_curvature * (x * x);

  float wobble = sin((uv.x * wobble_freq + TIME * wobble_speed) * 6.2831853) * wobble_amp;

  float shimmer_sin = sin((uv.x * shimmer_freq + TIME * shimmer_speed) * 6.2831853) * shimmer_amp;

  float n = value_noise(vec2(uv.x * 10.0, TIME * 0.5));
  float shimmer_noise = (n - 0.5) * 2.0 * shimmer_amp;

  float shimmer = mix(shimmer_sin, shimmer_noise, noise_amount);

  float boundary = arc + wobble + shimmer;

  float t = smoothstep(boundary - band_softness, boundary + band_softness, uv.y);

  // Top twilight at top, dark at bottom:
  vec3 col = mix(top_color.rgb, bottom_color.rgb, t);

  ALBEDO = col;
  ALPHA = 1.0;
}
