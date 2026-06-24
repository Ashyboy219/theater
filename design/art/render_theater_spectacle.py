# THEATER — Blender turntable for the P4 SPECTACLE apex UNIT bodies (citadelbody / leviathanbody /
# archangelbody / tempestbody). These are the gargantuan "look at this HUGE thing" payoff units,
# rendered at DOUBLE size (128px frames) so they tower over the normal roster.
#
# Unlike the single-angle building bodies (render_theater_buildings.py), apex units ROTATE, so each
# body is a 32-facing turntable — same determinism contract as render_aircraft_bodies.py:
#   * OpenRA facing 0 = NORTH = screen-UP; at facing 0 the unit travels up the screen.
#   * Frame index increases CLOCKWISE; frame 0 leftmost; PngSheet slices L->R.
#   * So model FRONT points +Y at frame 0, and we rotate the pivot -Z by 360/N per frame.
#   * Camera looks from -Y/+Z at ~30deg elevation (RA 2:1 dimetric) -> +Y reads as up-screen.
#   * 32 baked facings + real VERTICAL form (turret/superstructure height) so heading reads at every
#     angle (a flat plate just "slides"); keep InterpolatedFacings:64 in the sequence YAML.
#
# Run INSIDE a connected Blender (MCP execute_blender_code, or blender --background --python). Writes
# 32 frames per body to /tmp/<body>/fNN.png. ASSEMBLY + the REQUIRED frame-metadata embed (PIL strips
# the PNG's FrameSize tEXt chunk on re-save, so the engine must be told the frame size again):
#   from PIL import Image
#   N,F=32,128
#   strip=Image.new('RGBA',(F*N,F),(0,0,0,0))
#   for i in range(N): strip.paste(Image.open(f'/tmp/citadel/f{i:02d}.png'),(i*F,0))
#   strip.save('mods/theater/bits/citadelbody.png')
#   # embed metadata (transient sidecar yaml -> PNG tEXt), then delete the sidecar:
#   #   printf 'FrameSize: 128,128\nFrameAmount: 32\n' > mods/theater/bits/citadelbody.yaml
#   #   ./utility.sh theater --png-sheet-import mods/theater/bits/citadelbody.png && rm mods/theater/bits/citadelbody.yaml
# ortho_scale 12.5 matches the existing citadel broadside (~102px content in the 128px frame); keep it
# so the rebuilt body is a drop-in (same in-world size). Sequence (mods/theater/sequences/theater.yaml)
# already declares Facings:32 + InterpolatedFacings:64 + a `muzzle` strip, so the strip is a drop-in.

import bpy, math, os

N = 32
F = 128


def node_mat(name, rgb, rough=0.6, metal=0.0, emit=None, estr=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs['Base Color'].default_value = (*rgb, 1.0)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metal
    if emit is not None:
        b.inputs['Emission Color'].default_value = (*emit, 1.0)
        b.inputs['Emission Strength'].default_value = estr
    return m


_made = []


def _box(loc, sc, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_b'; o.scale = sc
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def _cyl(loc, r, h, mat, v=16, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=r, depth=h, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_c'
    o.rotation_euler = tuple(math.radians(a) for a in rot)
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def build_citadel():
    """Apex super-heavy land dreadnought: long tracked hull, sloped glacis, massive central turret with
    TWIN main guns (front=+Y), side sponsons, AA cupola and a glowing forward sensor. Dark military green."""
    green = node_mat('GEN_green', (0.19, 0.23, 0.15), 0.7)
    green_d = node_mat('GEN_green_d', (0.12, 0.15, 0.10), 0.7)
    track = node_mat('GEN_track', (0.08, 0.08, 0.09), 0.6)
    metal = node_mat('GEN_metal', (0.26, 0.27, 0.28), 0.5, 0.4)
    gun = node_mat('GEN_gun', (0.16, 0.17, 0.18), 0.4, 0.5)
    red = node_mat('GEN_red', (0.7, 0.12, 0.10), 0.3, 0.0, emit=(0.9, 0.12, 0.05), estr=4.0)
    _box((-2.05, 0, 0.7), (1.0, 8.4, 1.4), track)
    _box((2.05, 0, 0.7), (1.0, 8.4, 1.4), track)
    _box((0, -0.2, 1.55), (3.3, 7.6, 1.5), green)
    _box((0, 3.5, 1.5), (3.3, 1.6, 1.3), green_d, rot=(38, 0, 0))           # sloped glacis (+Y front)
    _box((0, -0.2, 2.25), (3.5, 7.0, 0.4), green_d)
    _box((-2.0, -0.2, 1.5), (0.2, 7.2, 1.3), metal)
    _box((2.0, -0.2, 1.5), (0.2, 7.2, 1.3), metal)
    _box((0, -0.6, 3.1), (3.0, 3.6, 1.5), green)                            # turret
    _box((0, -0.6, 3.1), (3.7, 2.4, 1.4), green)
    _box((0, -0.6, 3.95), (2.4, 2.8, 0.4), green_d)
    _cyl((-0.7, 2.9, 3.1), 0.26, 5.2, gun, rot=(90, 0, 0))                  # twin main guns -> +Y
    _cyl((0.7, 2.9, 3.1), 0.26, 5.2, gun, rot=(90, 0, 0))
    _box((0, 1.2, 3.1), (1.9, 1.2, 0.7), metal)                            # mantlet
    for x in (-2.4, 2.4):
        _box((x, -1.8, 2.7), (0.9, 1.3, 0.8), green_d)                     # side sponsons
        _cyl((x, -1.0, 2.7), 0.12, 1.6, gun, rot=(90, 0, 0))
    _box((0.7, -2.2, 4.1), (0.9, 0.9, 0.5), metal)                         # cupola
    _cyl((0.7, -2.2, 4.5), 0.5, 0.3, green_d)
    _box((-0.9, -2.0, 4.2), (0.18, 1.0, 0.18), gun)                        # AA mg
    _box((0, 2.0, 3.5), (0.5, 0.12, 0.22), red)                            # forward sensor glow
    _box((0, -3.9, 2.0), (2.6, 0.5, 0.7), track)                          # rear exhaust
    return 12.5  # ortho_scale (matches existing broadside size)


BODIES = {'citadel': build_citadel}   # leviathan/archangel/tempest added as they are reworked


def turntable_cam(ortho):
    cam = bpy.data.objects.get('GEN_ttcam')
    if not cam:
        cd = bpy.data.cameras.new('GEN_ttcam'); cam = bpy.data.objects.new('GEN_ttcam', cd)
        bpy.context.scene.collection.objects.link(cam)
    cam.location = (0, -10.392, 6.0)                  # -Y/+Z, 30deg elevation toward +Y
    cam.rotation_euler = (math.radians(60), 0, 0)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho
    return cam


def render_body(name, builder):
    global _made
    _made = []
    ortho = builder()
    pivot = bpy.data.objects.new('GEN_pivot', None)
    bpy.context.scene.collection.objects.link(pivot)
    for o in _made:
        o.parent = pivot
    scene = bpy.context.scene
    scene.camera = turntable_cam(ortho)
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.resolution_x = scene.render.resolution_y = F
    outdir = f'/tmp/{name}'
    os.makedirs(outdir, exist_ok=True)
    for i in range(N):
        pivot.rotation_euler.z = math.radians(-i * 360.0 / N)   # frame0 front=+Y; CLOCKWISE
        bpy.context.view_layer.update()
        scene.render.filepath = f'{outdir}/f{i:02d}.png'
        bpy.ops.render.render(write_still=True)
    print(f'rendered {N} frames -> {outdir}')
    bpy.data.objects.remove(pivot, do_unlink=True)
    for o in list(_made):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.materials):
        if m.name.startswith('GEN_'):
            bpy.data.materials.remove(m)


def main():
    hidden = [o for o in bpy.data.objects if o.type == 'MESH' and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    prev_cam = bpy.context.scene.camera
    bpy.data.objects['Key'].data.energy = 3.6
    for name, builder in BODIES.items():
        render_body(name, builder)
    # restore
    for o in hidden:
        o.hide_render = False
    if prev_cam:
        bpy.context.scene.camera = prev_cam
    bpy.data.objects['Key'].data.energy = 4.2
    cam = bpy.data.objects.get('GEN_ttcam')
    if cam:
        bpy.data.objects.remove(cam, do_unlink=True)


if __name__ == '__main__':
    main()
