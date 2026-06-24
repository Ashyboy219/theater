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

import bpy, math, os, bmesh

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


def _pyr(loc, r, depth, mat):
    """4-sided pyramid with its tip pointing +Y (aircraft nose cone)."""
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=r, radius2=0, depth=depth, location=loc)
    o = bpy.context.active_object; o.name = 'GEN_p'
    o.rotation_euler = (math.radians(-90), math.radians(45), 0)
    o.data.materials.clear(); o.data.materials.append(mat); _made.append(o); return o


def _hull_poly(verts2d, ztop, depth, mat):
    """Extrude a 2D outline (XY) down by `depth` — used for the ship hull (pointed bow at +Y)."""
    me = bpy.data.meshes.new('GEN_hullm'); ob = bpy.data.objects.new('GEN_hull', me)
    bpy.context.scene.collection.objects.link(ob)
    bm = bmesh.new()
    vs = [bm.verts.new((x, y, ztop)) for (x, y) in verts2d]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    bmesh.ops.translate(bm, vec=(0, 0, -depth), verts=[e for e in r['geom'] if isinstance(e, bmesh.types.BMVert)])
    bm.to_mesh(me); bm.free()
    ob.data.materials.append(mat); _made.append(ob); return ob


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


def build_leviathan():
    """Apex naval arsenal dreadnought: bmesh ship hull (pointed bow +Y), twin-barrel main turrets fore
    and aft, amidships VLS cells with glowing hatches, central bridge island, funnel, radar mast with a
    phased-array panel + amber sensor, CIWS mounts. Haze-grey. ortho 18.5 -> ~76px broadside (drop-in)."""
    hull = node_mat('GEN_hull', (0.30, 0.33, 0.37), 0.6, 0.2)
    deck = node_mat('GEN_deck', (0.20, 0.22, 0.25), 0.7)
    sup = node_mat('GEN_sup', (0.42, 0.44, 0.47), 0.5)
    gun = node_mat('GEN_gun', (0.17, 0.18, 0.20), 0.4, 0.5)
    glow = node_mat('GEN_glow', (0.1, 0.5, 0.7), 0.3, 0.0, emit=(0.15, 0.8, 1.0), estr=5.0)
    amber = node_mat('GEN_amber', (0.7, 0.4, 0.05), 0.3, 0.0, emit=(1.0, 0.55, 0.05), estr=4.0)
    _hull_poly([(0, 5.6), (1.25, 3.8), (1.45, -3.8), (0, -4.7), (-1.45, -3.8), (-1.25, 3.8)], 1.1, 1.4, hull)
    _box((0, 0.4, 1.18), (2.3, 8.6, 0.12), deck)
    _box((0, 3.0, 1.5), (1.6, 1.4, 0.7), sup)                               # fwd turret
    _cyl((-0.3, 4.6, 1.6), 0.16, 2.6, gun, rot=(90, 0, 0)); _cyl((0.3, 4.6, 1.6), 0.16, 2.6, gun, rot=(90, 0, 0))
    _box((0, -2.6, 1.5), (1.6, 1.4, 0.7), sup)                              # aft turret
    _cyl((-0.3, -1.2, 1.6), 0.16, 2.6, gun, rot=(90, 0, 0)); _cyl((0.3, -1.2, 1.6), 0.16, 2.6, gun, rot=(90, 0, 0))
    for gy in (1.4, 0.6):                                                   # VLS cells
        for gx in (-0.5, 0.0, 0.5):
            _box((gx, gy, 1.32), (0.3, 0.3, 0.18), gun)
            _box((gx, gy, 1.42), (0.18, 0.18, 0.06), glow)
    _box((0, -0.3, 2.2), (1.5, 2.0, 1.6), sup)                             # bridge island
    _box((0, -0.3, 3.1), (1.0, 1.2, 0.8), sup)
    _box((0.0, -0.3, 3.0), (0.5, 0.06, 0.5), glow)                         # bridge windows
    _box((0, -1.4, 2.5), (0.7, 0.8, 1.2), deck)                            # funnel
    _cyl((0, -0.3, 4.2), 0.07, 2.2, gun)                                   # mast
    _box((0, -0.6, 4.9), (0.9, 0.12, 0.7), sup)                            # phased-array panel
    _box((0, -0.45, 5.2), (0.3, 0.1, 0.18), amber)
    for x in (-1.1, 1.1):                                                  # CIWS
        _cyl((x, 0.4, 1.5), 0.22, 0.5, sup); _cyl((x, 0.9, 1.55), 0.06, 0.7, gun, rot=(70, 0, 0))
    return 18.5


def build_archangel():
    """Apex heavy gunship flying-fortress: broad raised fuselage + nose cone (front=+Y), wide wing with
    four engine nacelles, side gun blisters with cannons, twin canted tail fins, wingtip missile pods,
    cyan cockpit + red chin sensor. Dark gunmetal. ortho 14 -> ~88px broadside."""
    body = node_mat('GEN_body', (0.22, 0.23, 0.26), 0.55, 0.1)
    body_d = node_mat('GEN_body_d', (0.14, 0.15, 0.17), 0.6)
    metal = node_mat('GEN_metal', (0.30, 0.31, 0.33), 0.4, 0.4)
    gun = node_mat('GEN_gun', (0.11, 0.12, 0.14), 0.4, 0.5)
    red = node_mat('GEN_red', (0.7, 0.12, 0.10), 0.3, 0.0, emit=(1.0, 0.12, 0.05), estr=5.0)
    cyan = node_mat('GEN_cyan', (0.1, 0.5, 0.7), 0.3, 0.0, emit=(0.2, 0.85, 1.0), estr=4.0)
    _box((0, 0.2, 0.55), (1.8, 7.2, 1.0), body)                            # fuselage
    _pyr((0, 4.2, 0.55), 0.9, 2.0, body_d)                                 # nose -> +Y
    _box((0, 1.4, 0.95), (1.0, 1.6, 0.5), metal)                           # cockpit hump
    _box((0, 1.7, 1.0), (0.6, 0.5, 0.28), cyan)                            # cockpit glass
    _box((0, 3.0, 0.4), (0.3, 0.5, 0.3), red)                              # chin sensor/gun
    _box((0, -0.2, 0.5), (11.0, 2.4, 0.32), body)                          # wing
    for x in (-3.4, -1.7, 1.7, 3.4):
        _cyl((x, 0.1, 0.18), 0.5, 1.9, body_d, rot=(90, 0, 0))             # engine nacelles
        _cyl((x, 1.15, 0.18), 0.42, 0.3, gun, rot=(90, 0, 0))
    for x in (-1.4, 1.4):
        _box((x, -1.6, 0.45), (0.9, 1.4, 0.7), body_d)                     # gun blister
        _cyl((x, -2.6, 0.35), 0.14, 1.8, gun, rot=(75, 0, 0))             # cannon
    _box((0, -3.2, 0.55), (1.2, 1.8, 0.7), body_d)
    _box((-0.7, -3.6, 1.2), (0.12, 1.0, 1.0), body, rot=(0, 24, 0))        # twin tails
    _box((0.7, -3.6, 1.2), (0.12, 1.0, 1.0), body, rot=(0, -24, 0))
    _box((0, -3.9, 0.6), (2.6, 0.7, 0.16), body)                          # tailplane
    for x in (-5.2, 5.2):
        _cyl((x, 0.2, 0.5), 0.18, 1.4, gun, rot=(90, 0, 0))               # wingtip pods
    return 14.0


def build_tempest():
    """Apex rocket-artillery (MLRS): 8-wheel sand chassis, armored cab (front=+Y), an elevated launcher
    pod tilted up-and-back with a 4x2 grid of rocket tubes (amber muzzle glow), hydraulic arms, rear
    stabiliser. Sand/tan (distinct from the green Citadel). ortho 14 -> ~81px broadside."""
    tan = node_mat('GEN_tan', (0.55, 0.50, 0.36), 0.7)
    tan_d = node_mat('GEN_tan_d', (0.39, 0.35, 0.25), 0.7)
    wheel = node_mat('GEN_wheel', (0.09, 0.09, 0.10), 0.7)
    metal = node_mat('GEN_metal', (0.28, 0.28, 0.29), 0.5, 0.4)
    tube = node_mat('GEN_tube', (0.13, 0.13, 0.14), 0.4, 0.5)
    amber = node_mat('GEN_amber', (0.7, 0.35, 0.04), 0.3, 0.0, emit=(1.0, 0.5, 0.05), estr=5.0)
    _box((0, -0.2, 1.15), (3.0, 7.4, 1.3), tan)                            # chassis
    _box((0, -0.2, 1.95), (3.2, 6.8, 0.4), tan_d)                          # deck
    for y in (2.5, 1.1, -1.1, -2.5):
        for x in (-1.6, 1.6):
            _cyl((x, y, 0.6), 0.62, 0.5, wheel, rot=(0, 90, 0))            # 8x8 wheels
    _box((0, 3.0, 2.0), (2.6, 1.6, 1.5), tan)                              # cab
    _box((0, 3.85, 2.1), (2.2, 0.3, 1.0), tube)                            # windscreen
    _box((0, -1.2, 2.3), (2.2, 1.4, 0.6), metal)                           # turntable
    _box((0, -1.9, 3.5), (2.8, 3.0, 1.7), tan_d, rot=(-32, 0, 0))          # launcher pod
    for tx in (-0.85, -0.28, 0.28, 0.85):
        for tz in (0, 1):
            _cyl((tx, -3.4, 4.3 + tz * 0.95), 0.22, 2.0, tube, rot=(58, 0, 0))         # rocket tubes
            _cyl((tx, -3.45, 4.32 + tz * 0.95), 0.13, 0.4, amber, rot=(58, 0, 0))      # muzzle glow
    for x in (-1.0, 1.0):
        _cyl((x, -0.4, 2.7), 0.12, 1.8, metal, rot=(40, 0, 0))            # hydraulic arms
    _box((0, -3.7, 1.0), (2.4, 0.5, 0.8), tan_d)                          # rear stabiliser
    return 14.0


BODIES = {'citadel': build_citadel, 'leviathan': build_leviathan,
          'archangel': build_archangel, 'tempest': build_tempest}   # full P4 apex set


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
