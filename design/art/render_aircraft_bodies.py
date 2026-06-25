# THEATER — Blender turntable for the custom AIRCRAFT BODIES (b21body / jetbody / dronebody).
#
# This is the render pipeline the design docs always referenced but which had never actually been
# committed (the three body PNGs were rendered ad-hoc and only the finished strips landed in git).
# It is reproducible: run it INSIDE a connected Blender instance (Blender MCP `execute_blender_code`,
# or `blender --background --python render_aircraft_bodies.py`). It writes per-facing PNGs to /tmp,
# which the assembly step below packs into mods/theater/bits/<body>.png.
#
# ENGINE CONTRACT (verified against OpenRA.Mods.Common/Util.cs IndexFacing + Aircraft.FlyStep):
#   * OpenRA facing 0 = WAngle 0 = NORTH = screen-UP. At facing 0 a plane travels (0,-1024) = up the screen.
#   * Frame index increases CLOCKWISE. Frame 0 leftmost in the strip; PngSheetLoader slices L->R.
#   * So: model NOSE must point +Y (world) at frame 0, and we rotate the model -Z (clockwise from top)
#     by 360/N per frame. Camera looks from -Y/+Z at ~30deg elevation (RA 2:1 dimetric) so +Y = screen-up.
#
# WHY 32 FACINGS + VERTICAL FORM (the 2024 jank fix): the original bodies were 16-facing and nearly FLAT
# (height ~0.35 vs ~5 wide). A flat plate barely changes silhouette as it yaws at a 30deg camera, so
# rotation read as "sliding sideways". Fix = 32 baked facings (11.25deg steps) + real vertical form
# (raised fuselage spine, tail fins, centerbody) so heading reads at every angle. Keep InterpolatedFacings:64.
#
# ASSEMBLY (outside Blender, run from repo root):
#   python3 - <<'PY'
#   from PIL import Image
#   N,W=32,64
#   for body,src in [("dronebody","/tmp/drone"),("jetbody","/tmp/jet"),("b21body","/tmp/b21")]:
#       strip=Image.new("RGBA",(W*N,W),(0,0,0,0))
#       for i in range(N): strip.paste(Image.open(f"{src}/f{i:02d}.png"),(i*W,0))
#       strip.save(f"mods/theater/bits/{body}.png")
#   PY
#   # embed frame metadata (transient sidecar yaml -> PNG tEXt), then delete the sidecar:
#   for b in dronebody jetbody b21body; do
#     printf 'FrameSize: 64,64\nFrameAmount: 32\n' > mods/theater/bits/$b.yaml
#     ./utility.sh theater --png-sheet-import mods/theater/bits/$b.png
#     rm mods/theater/bits/$b.yaml
#   done
#   # sequence YAML (mods/theater/sequences/theater.yaml): Facings: 32, InterpolatedFacings: 64.

import bpy, math, bmesh, os

N = 32          # facings (power of 2, <=1024)
RES = 64        # output frame size in px
ORTHO = 6.4     # world units across a frame (shared scale for the whole roster)


def setup_scene():
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.render.film_transparent = True
    s.render.image_settings.file_format = 'PNG'
    s.render.image_settings.color_mode = 'RGBA'
    cam = bpy.data.objects['cam']
    cam.location = (0, -10.392, 6.0)                 # 30deg elevation toward +Y
    cam.rotation_euler = (math.radians(60), 0, 0)
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = ORTHO
    s.camera = cam
    sun = bpy.data.objects['sun']
    sun.rotation_euler = (math.radians(38), math.radians(8), math.radians(30))
    sun.data.energy = 4.0
    s.world.use_nodes = False
    s.world.color = (0.13, 0.14, 0.16)               # soft ambient so undersides aren't pure black


def mat(name, rgb, rough=0.7):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = False
    m.diffuse_color = (*rgb, 1.0)
    m.roughness = rough
    return m


def add_box(cx, cy, cz, sx, sy, sz, rx=0, ry=0, rz=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(cx, cy, cz))
    o = bpy.context.active_object
    o.scale = (sx, sy, sz)
    o.rotation_euler = (math.radians(rx), math.radians(ry), math.radians(rz))
    return o


def add_pyramid(cx, cy, cz, r, depth):
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=r, radius2=0, depth=depth, location=(cx, cy, cz))
    o = bpy.context.active_object
    o.rotation_euler = (math.radians(-90), math.radians(45), 0)   # tip -> +Y
    return o


def add_flat_poly(verts2d, z, thickness):
    me = bpy.data.meshes.new('poly')
    ob = bpy.data.objects.new('poly', me)
    bpy.context.collection.objects.link(ob)
    bm = bmesh.new()
    vs = [bm.verts.new((x, y, z)) for (x, y) in verts2d]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    verts = [e for e in r['geom'] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, 0, -thickness), verts=verts)
    bm.to_mesh(me)
    bm.free()
    return ob


def join_as(name, objs, material):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.data.materials.clear()
    o.data.materials.append(material)
    return o


def clear_meshes():
    bpy.ops.object.select_all(action='DESELECT')
    for o in list(bpy.data.objects):
        if o.type == 'MESH':
            o.select_set(True)
    if bpy.context.selected_objects:
        bpy.ops.object.delete()


def build_drone(body_mat):
    clear_meshes()
    p = []
    p.append(add_box(0, 0.10, 0.42, 0.46, 3.4, 0.55))       # raised fuselage spine
    p.append(add_pyramid(0, 2.05, 0.42, 0.30, 1.1))          # pointed nose -> +Y
    p.append(add_box(0, 1.45, 0.12, 0.42, 0.6, 0.34))        # sensor ball
    p.append(add_box(0, 0.35, 0.50, 5.4, 0.62, 0.12))        # long high-aspect wings
    p.append(add_box(-0.62, -1.65, 0.42, 0.13, 1.6, 0.13))   # tail booms
    p.append(add_box(0.62, -1.65, 0.42, 0.13, 1.6, 0.13))
    p.append(add_box(0, -2.42, 0.45, 1.5, 0.45, 0.10))       # tailplane
    p.append(add_box(-0.62, -2.30, 0.72, 0.10, 0.45, 0.55))  # vertical fins (heading cue)
    p.append(add_box(0.62, -2.30, 0.72, 0.10, 0.45, 0.55))
    return join_as('drone', p, body_mat)


def build_jet(body_mat, dark_mat):
    clear_meshes()
    p = []
    p.append(add_flat_poly([(0, 2.0), (-2.7, -1.4), (-0.45, -1.95), (0.45, -1.95), (2.7, -1.4)], 0.34, 0.14))
    p.append(add_box(0, 0.1, 0.42, 0.55, 3.4, 0.55))         # raised spine
    p.append(add_pyramid(0, 2.25, 0.40, 0.30, 1.5))          # sharp nose
    p.append(add_box(-0.55, -1.5, 0.62, 0.10, 0.85, 0.62, ry=22))   # twin canted tails
    p.append(add_box(0.55, -1.5, 0.62, 0.10, 0.85, 0.62, ry=-22))
    p.append(add_box(-0.28, -1.95, 0.40, 0.26, 0.4, 0.34))   # engine nozzles
    p.append(add_box(0.28, -1.95, 0.40, 0.26, 0.4, 0.34))
    jet = join_as('jet', p, body_mat)
    canopy = add_box(0, 1.1, 0.64, 0.34, 0.95, 0.24)         # dark cockpit accent
    canopy.data.materials.clear()
    canopy.data.materials.append(dark_mat)
    canopy.parent = jet
    return jet


def build_b21(b21_mat):
    clear_meshes()
    p = []
    p.append(add_flat_poly(
        [(0, 2.6), (-3.0, -0.6), (-1.6, -1.0), (-0.7, -1.9), (0, -1.2), (0.7, -1.9), (1.6, -1.0), (3.0, -0.6)],
        0.30, 0.16))                                          # B-2 style sawtooth flying wing (largest body)
    p.append(add_box(0, 0.55, 0.34, 1.5, 2.9, 0.42))         # raised centerbody
    p.append(add_box(0, 1.45, 0.46, 0.7, 0.9, 0.30))         # cockpit bump
    p.append(add_box(-0.62, 0.1, 0.50, 0.55, 1.3, 0.26))     # dorsal engine humps
    p.append(add_box(0.62, 0.1, 0.50, 0.55, 1.3, 0.26))
    return join_as('b21', p, b21_mat)


def render_facings(obj, outdir):
    os.makedirs(outdir, exist_ok=True)
    s = bpy.context.scene
    s.render.resolution_x = s.render.resolution_y = RES
    for i in range(N):
        obj.rotation_euler.z = math.radians(-i * 360.0 / N)   # frame 0 = nose up; CLOCKWISE as i grows
        bpy.context.view_layer.update()
        s.render.filepath = f'{outdir}/f{i:02d}.png'
        bpy.ops.render.render(write_still=True)


def main():
    setup_scene()
    body = mat('body_grey', (0.40, 0.41, 0.44))
    dark = mat('body_dark', (0.16, 0.17, 0.20))
    b21m = mat('b21_dark', (0.16, 0.17, 0.20))
    render_facings(build_drone(body), '/tmp/drone')
    render_facings(build_jet(body, dark), '/tmp/jet')
    render_facings(build_b21(b21m), '/tmp/b21')


if __name__ == '__main__':
    main()
