# THEATER — Blender renderer for the custom BUILDING bodies (bankbody / alloyexbody / ...).
#
# Buildings are SINGLE-ANGLE (they never rotate), so unlike the 32-facing unit turntable
# (render_aircraft_bodies.py) each body is ONE render from OpenRA's fixed dimetric structure
# camera. This is the path actually used for the economy buildings (the gpt-image API route in
# the design docs was billing-capped at the time, and a 3D render matches the existing unit
# bodies' look far better than a flat illustration would).
#
# Run INSIDE a connected Blender (Blender MCP `execute_blender_code`, or
# `blender --background --python render_theater_buildings.py`). It is non-destructive: if a scene
# already holds other meshes it hides them for the render and restores them after. It writes a
# 512px transparent render per body to /tmp, which the ASSEMBLY step below grounds into a 64px
# mods/theater/bits/<body>.png (1x1 footprint).
#
# ENGINE CONTRACT: OpenRA draws structures from a high 3/4 dimetric angle, light from the upper
# left. The camera is ORTHO, ~30deg elevation toward +Y, so +Y reads as "up-screen / back". Each
# body is modelled grounded at z=0 with its front face toward -Y (the camera). EEVEE film is
# transparent, so the render carries its own alpha — no chroma key needed (cf. the magenta gpt-image
# route). Materials MUST be node-based (Principled BSDF Base Color): Blender 4.2+/EEVEE-Next ignores
# the legacy `diffuse_color` at render time and would render every surface white.
#
# ASSEMBLY (outside Blender, from repo root) — autocrop, scale to a target base width, and paste
# the body horizontally-centred with its base on a fixed baseline so grounding matches the old sprite:
#   python3 - <<'PY'
#   from PIL import Image
#   def ground(src, out, base_w, baseline=47, canvas=64, cx=32):
#       im = Image.open(src).convert('RGBA'); im = im.crop(im.getbbox())
#       sw, sh = im.size; scale = base_w / sw
#       if sh*scale > baseline: scale = baseline / sh      # keep a tall body within the canvas
#       nw, nh = max(1,round(sw*scale)), max(1,round(sh*scale))
#       im = im.resize((nw,nh), Image.LANCZOS)
#       cv = Image.new('RGBA',(canvas,canvas),(0,0,0,0)); cv.paste(im,(cx-nw//2, baseline-nh), im)
#       cv.save(out)
#   ground('/tmp/bank_raw.png',    'mods/theater/bits/bankbody.png',    base_w=36, baseline=47)
#   ground('/tmp/alloyex_raw.png', 'mods/theater/bits/alloyexbody.png', base_w=42, baseline=48)
#   PY
# The sequence YAML (mods/theater/sequences/theater.yaml) already keys idle/make -> <body>.png as a
# single frame (no FrameSize/Facings), so the grounded PNG is a drop-in.

import bpy, math, bmesh

RES = 512


def node_mat(name, rgb, rough=0.6, metal=0.0):
    """Principled-BSDF material — required for EEVEE-Next colour at render time."""
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs['Base Color'].default_value = (*rgb, 1.0)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    return m


def ensure_rig():
    """Reuse an existing Cam/Key/Fill rig if present (the tuned THEATER scene), else build the
    dimetric ortho rig from scratch so the script also runs in an empty file."""
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.render.film_transparent = True
    s.render.image_settings.file_format = 'PNG'
    s.render.image_settings.color_mode = 'RGBA'
    s.render.resolution_x = s.render.resolution_y = RES
    if not s.world:
        s.world = bpy.data.worlds.new('W')
    s.world.use_nodes = False
    s.world.color = (0.13, 0.14, 0.16)            # soft ambient so shadow sides aren't pure black
    cam = bpy.data.objects.get('Cam')
    if not cam:
        cd = bpy.data.cameras.new('Cam'); cam = bpy.data.objects.new('Cam', cd)
        s.collection.objects.link(cam)
        tgt = bpy.data.objects.new('CamTarget', None); tgt.location = (0.3, 0, 5.1)
        s.collection.objects.link(tgt)
        c = cam.constraints.new('TRACK_TO'); c.target = tgt
        c.track_axis = 'TRACK_NEGATIVE_Z'; c.up_axis = 'UP_Y'
    cam.location = (0, -43.3, 25.0)               # ~30deg elevation toward +Y (RA structure angle)
    cam.data.type = 'ORTHO'
    s.camera = cam
    if not bpy.data.objects.get('Key'):
        kd = bpy.data.lights.new('Key', 'SUN'); k = bpy.data.objects.new('Key', kd)
        s.collection.objects.link(k); k.rotation_euler = (math.radians(52), math.radians(8), math.radians(-42))
    if not bpy.data.objects.get('Fill'):
        fd = bpy.data.lights.new('Fill', 'SUN'); f = bpy.data.objects.new('Fill', fd)
        s.collection.objects.link(f); f.rotation_euler = (math.radians(-20), math.radians(-30), math.radians(140))
    bpy.data.objects['Key'].data.energy = 3.4
    bpy.data.objects['Fill'].data.energy = 1.4
    return cam


_made = []


def _box(loc, sc, mat):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_box'; o.scale = sc
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def _cyl(loc, r, h, mat, v=20, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=r, depth=h, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_cyl'
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def _cone(loc, r, h, mat, v=20):
    bpy.ops.mesh.primitive_cone_add(vertices=v, radius1=r, radius2=0, depth=h, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_cone'
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def build_bank():
    """Classical treasury: stone hall, 5-column portico, gold frieze, triangular pediment, gold dome."""
    stone = node_mat('GEN_stone', (0.52, 0.49, 0.43), 0.7)
    stone_d = node_mat('GEN_stone_d', (0.34, 0.32, 0.27), 0.7)
    trim = node_mat('GEN_trim', (0.66, 0.63, 0.56), 0.6)
    gold = node_mat('GEN_gold', (0.86, 0.62, 0.13), 0.35, 0.3)
    dark = node_mat('GEN_dark', (0.10, 0.13, 0.18), 0.3)
    H = 3.4
    _box((0, 0.7, H / 2), (7.2, 5.0, H), stone)
    _box((0, 0.7, 0.4), (7.9, 5.7, 0.8), stone_d)
    _box((0, 0.7, H + 0.05), (7.7, 5.5, 0.45), trim)
    _box((0, -2.6, 0.25), (5.2, 0.7, 0.5), stone_d)
    _box((0, -3.1, 0.12), (5.9, 0.7, 0.24), stone_d)
    for x in (-2.7, -1.35, 0.0, 1.35, 2.7):
        _cyl((x, -2.15, H / 2 + 0.1), 0.33, H, trim, 16)
        _box((x, -2.15, H - 0.05), (0.86, 0.86, 0.22), stone)
        _box((x, -2.15, 0.3), (0.86, 0.86, 0.32), stone)
    _box((0, -1.6, 1.55), (2.1, 0.3, 2.7), gold)
    _box((-2.5, -1.5, 1.7), (1.0, 0.2, 2.0), dark)
    _box((2.5, -1.5, 1.7), (1.0, 0.2, 2.0), dark)
    _box((0, -2.65, H + 0.05), (6.7, 0.2, 0.34), gold)
    # pediment: triangular gable (triangle in X-Z, extruded along +Y for depth)
    me = bpy.data.meshes.new('GEN_ped'); ped = bpy.data.objects.new('GEN_ped', me)
    bpy.context.scene.collection.objects.link(ped)
    bm = bmesh.new(); y0 = -2.75
    v1 = bm.verts.new((-3.5, y0, H + 0.25)); v2 = bm.verts.new((3.5, y0, H + 0.25)); v3 = bm.verts.new((0.0, y0, H + 1.45))
    f = bm.faces.new((v1, v2, v3))
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    bmesh.ops.translate(bm, vec=(0, 1.25, 0), verts=[e for e in r['geom'] if isinstance(e, bmesh.types.BMVert)])
    bm.to_mesh(me); bm.free()
    ped.data.materials.append(stone); _made.append(ped)
    # gold dome set back on the roof, tall enough to clear the pediment apex
    _cyl((0, 1.7, H + 0.1), 1.15, 0.6, trim, 24)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.2, location=(0, 1.7, H + 0.5), segments=24, ring_count=12)
    d = bpy.context.active_object; d.name = 'GEN_dome'; d.scale = (1, 1, 1.15)
    d.data.materials.clear(); d.data.materials.append(gold); _made.append(d)
    _box((0, 1.7, H + 2.1), (0.12, 0.12, 0.5), gold)
    return 13.0  # ortho_scale used for the bank


def build_alloyex():
    """Industrial metals extractor: twin hazard-banded storage tanks, processing block, smokestack, pipes."""
    steel = node_mat('GEN_steel', (0.50, 0.52, 0.55), 0.5, 0.2)
    steel_d = node_mat('GEN_steel_d', (0.33, 0.35, 0.38), 0.6, 0.1)
    rust = node_mat('GEN_rust', (0.55, 0.28, 0.12), 0.8)
    pipe = node_mat('GEN_pipe', (0.40, 0.42, 0.45), 0.4, 0.4)
    hazard = node_mat('GEN_hazard', (0.80, 0.62, 0.10), 0.6)
    steel_dk = node_mat('GEN_dark2', (0.16, 0.17, 0.19), 0.5)
    _box((0, 0.5, 0.2), (8.0, 6.0, 0.4), steel_d)
    for x in (-2.2, 1.7):
        _cyl((x, 0.9, 2.6), 1.5, 4.6, steel)
        _cone((x, 0.9, 5.4), 1.55, 1.0, steel_d)
        _cyl((x, 0.9, 3.4), 1.53, 0.4, hazard)
        _cyl((x, 0.9, 1.6), 1.53, 0.4, rust)
    _box((0.0, -2.2, 1.4), (3.4, 1.8, 2.8), steel_d)
    _box((0.0, -2.2, 2.95), (3.6, 2.0, 0.3), steel)
    _box((-0.9, -2.2, 3.4), (0.5, 0.5, 0.9), pipe)
    _box((0.6, -2.2, 3.3), (0.4, 0.4, 0.7), pipe)
    _cyl((3.0, 2.0, 4.0), 0.55, 8.0, steel)
    _cyl((3.0, 2.0, 7.4), 0.58, 0.7, rust)
    _cyl((-0.25, 0.9, 2.0), 0.28, 4.0, pipe, rot=(0, 90, 0))
    _cyl((1.7, -0.6, 1.5), 0.26, 3.0, pipe, rot=(90, 0, 0))
    _box((3.0, 2.0, 0.8), (0.9, 0.9, 1.4), steel_dk)
    return 14.0


BODIES = {'bank': build_bank, 'alloyex': build_alloyex}


def render_body(name, builder, cam):
    global _made
    _made = []
    ortho = builder()
    cam.data.ortho_scale = ortho
    s = bpy.context.scene
    s.render.filepath = f'/tmp/{name}_raw.png'
    bpy.ops.render.render(write_still=True)
    for o in list(_made):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.materials):
        if m.name.startswith('GEN_'):
            bpy.data.materials.remove(m)
    print(f'rendered /tmp/{name}_raw.png')


def main():
    # non-destructive: hide any pre-existing meshes for the clean render, restore afterwards
    hidden = [o for o in bpy.data.objects if o.type == 'MESH' and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    cam = ensure_rig()
    for name, builder in BODIES.items():
        render_body(name, builder, cam)
    for o in hidden:
        o.hide_render = False


if __name__ == '__main__':
    main()
