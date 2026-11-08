"""Create an editable 3D LunaReth Ghost Samurai with a reference-faithful front.

Run with Blender 5.x: blender --background --python create_ghost_samurai.py
All geometry is native Blender mesh/curve geometry with simple materials.
"""
import bpy
import math
import os
from mathutils import Vector

OUT = os.path.dirname(os.path.abspath(__file__)) + os.sep
VERSION = '02'

def clear_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for data in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for item in list(data):
            if item.users == 0:
                data.remove(item)

def mat(name, color, rough=0.82):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = rough
    return m

INK = mat('Ink | near black', (0.018, 0.025, 0.015))
SKULL = mat('Bone | pale ivory', (0.88, 0.91, 0.79))
GREEN = mat('Moss | cloak green', (0.20, 0.52, 0.025))
LIME = mat('Fern | bright green', (0.39, 0.72, 0.035))
DEEP = mat('Shadow | forest green', (0.045, 0.19, 0.055))
RED = mat('Cloak | oxblood', (0.28, 0.045, 0.035))
PINK = mat('Petal | cherry', (0.98, 0.28, 0.45))
WHITE = mat('Petal | ivory', (0.96, 0.96, 0.84))
BLUE = mat('Petal | indigo', (0.18, 0.08, 0.82))
YELLOW = mat('Flower | gold', (1.0, 0.60, 0.035))
STEEL = mat('Sword | dark steel', (0.30, 0.39, 0.37))

def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c
COL = {n: collection(n) for n in ['01 Body and cloak', '02 Skull and hat', '03 Flowers and foliage', '04 Sword equipment', '05 Stage']}

def move_to(obj, group):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    COL[group].objects.link(obj)

def mesh_poly(name, points, depth, material, group, bevel=0.025):
    # Extrude a side-view polygon through Y, retaining a dark ink outline as bevel.
    n = len(points); verts = [(x, y, z) for y in (-depth/2, depth/2) for x,z in points]
    faces = [tuple(range(n-1, -1, -1)), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    me = bpy.data.meshes.new(name+'Mesh'); me.from_pydata(verts, [], faces); me.materials.append(material); me.materials.append(INK)
    ob = bpy.data.objects.new(name, me); COL[group].objects.link(ob)
    if bevel:
        mod = ob.modifiers.new('Soft illustrated edge', 'BEVEL'); mod.width=bevel; mod.segments=1
        mod2 = ob.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
    return ob

def ellipsoid(name, loc, scale, material, group, outline=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=loc)
    ob=bpy.context.object; ob.name=name; ob.scale=scale; ob.data.materials.append(material)
    if outline:
        ob.data.materials.append(INK)
        sol=ob.modifiers.new('Ink rim', 'SOLIDIFY'); sol.thickness=0.018
        sol.material_offset=1
    move_to(ob, group); return ob

def line(name, coords, radius, material, group):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.bevel_depth=radius; cu.bevel_resolution=1
    sp=cu.splines.new('POLY'); sp.points.add(len(coords)-1)
    for p,co in zip(sp.points,coords): p.co=(*co,1)
    ob=bpy.data.objects.new(name,cu); COL[group].objects.link(ob); cu.materials.append(material); return ob

def flower(name, x,z, petalmat, group='03 Flowers and foliage', r=.18):
    for i in range(5):
        a=2*math.pi*i/5
        ellipsoid(name+'_Petal_'+str(i+1),(x+math.cos(a)*r*.62,-.34,z+math.sin(a)*r*.62),(r*.42,.055,r*.63),petalmat,group)
    ellipsoid(name+'_Heart',(x,-.43,z),(r*.28,.07,r*.28),YELLOW,group)

clear_scene()
# Back mantle and ragged leaf-like lower hem.
mesh_poly('GS_Cloak_Back', [(-.66,1.72),(-.82,1.28),(-1.02,.63),(-1.36,.02),(-1.10,.10),(-.93,-.13),(-.68,.02),(-.48,-.18),(-.26,.04),(.02,-.12),(.23,.05),(.44,-.10),(.62,.13),(.76,.11),(.55,.66),(.48,1.42)], .36, DEEP,'01 Body and cloak',.04)
# Main cloak with pointed lower edges and broad shoulder.
mesh_poly('GS_Cloak', [(-.39,2.13),(.36,2.15),(.68,1.78),(.73,1.15),(.62,.53),(.38,.16),(.20,.32),(.05,.03),(-.18,.29),(-.42,.08),(-.53,.36),(-.78,.24),(-.63,.89),(-.88,1.41),(-.65,1.86)], .5, GREEN,'01 Body and cloak',.045)
# Ox-blood front mantle panels.
mesh_poly('GS_Cloak_Front_L', [(-.43,2.18),(-.08,2.16),(-.14,1.73),(-.38,1.39),(-.47,.93),(-.75,.62),(-.54,1.31),(-.63,1.81)], .54, RED,'01 Body and cloak',.035)
mesh_poly('GS_Cloak_Front_R', [(-.04,2.16),(.30,2.12),(.48,1.72),(.55,1.12),(.40,.79),(.15,.70),(-.05,.85),(-.16,1.33)], .56, RED,'01 Body and cloak',.04)
# Mossy fringe motifs.
for i,(x,z) in enumerate([(-.73,.45),(-.48,.38),(-.18,.30),(.10,.28),(.38,.38),(.57,.53),(-.82,1.03),(.58,1.48)]):
    mesh_poly('GS_Fern_Fringe_%02d'%i,[(x-.10,z+.33),(x+.04,z+.28),(x+.12,z),(x+.02,z+.11),(x-.12,z+.04)],.58,LIME,'03 Flowers and foliage',.01)
# Skull: side-facing cranium with jaw and readable sockets/teeth.
ellipsoid('GS_Head_Skull',(.02,-.02,2.72),(.40,.31,.48),SKULL,'02 Skull and hat')
mesh_poly('GS_Skull_Jaw',[(-.22,2.66),(.33,2.64),(.37,2.42),(.20,2.32),(-.15,2.39)],.52,SKULL,'02 Skull and hat',.035)
ellipsoid('GS_EyeSocket',(.28,-.345,2.79),(.13,.045,.16),INK,'02 Skull and hat',False)
ellipsoid('GS_NasalCavity',(.39,-.34,2.60),(.06,.04,.09),INK,'02 Skull and hat',False)
line('GS_Skull_CheekSmile',[(.17,-.38,2.54),(.26,-.39,2.48),(.39,-.35,2.49)],.018,INK,'02 Skull and hat')
for i in range(4): line('GS_Tooth_%d'%i,[(.18+i*.055,-.39,2.47),(.18+i*.055,-.39,2.40)],.012,INK,'02 Skull and hat')
# Broad conical kasa, flattened brim and crown.
mesh_poly('GS_Hat_Crown',[(-.43,3.02),(-.31,3.33),(-.02,3.40),(.31,3.32),(.45,3.03),(.18,2.98),(-.22,2.98)],.62,DEEP,'02 Skull and hat',.025)
mesh_poly('GS_Hat_Brim',[(-.77,3.03),(-.48,3.11),(-.12,3.16),(.33,3.10),(.68,2.99),(.43,2.91),(-.14,2.96)],.72,GREEN,'02 Skull and hat',.035)
line('GS_Hat_Brim_Trim',[(-.70,-.40,3.01),(-.30,-.41,3.04),(.15,-.41,3.03),(.59,-.38,2.99)],.022,LIME,'02 Skull and hat')
# Arm and wrapped forearm, projecting on the near side.
mesh_poly('GS_LeftArm', [(-.53,1.98),(-.79,1.81),(-.72,1.46),(-.47,1.35),(-.31,1.55)],.42,RED,'01 Body and cloak',.04)
mesh_poly('GS_RightArm', [(.25,1.95),(.59,1.81),(.70,1.47),(.48,1.35),(.28,1.57)],.42,GREEN,'01 Body and cloak',.04)
line('GS_Sash', [(-.48,-.34,1.98),(-.18,-.37,1.85),(.18,-.37,1.67),(.49,-.34,1.48)],.055,LIME,'01 Body and cloak')
# Two separate sword objects: sheathed at hip and a spare crossing behind the mantle.
mesh_poly('GS_Sword_L', [(-.89,1.07),(-.80,1.16),(-.24,.76),(-.31,.66)],.20,STEEL,'04 Sword equipment',.012)
mesh_poly('GS_Sword_L_Handle',[(-.93,1.14),(-.87,1.22),(-.65,1.04),(-.70,.97)],.24,RED,'04 Sword equipment',.018)
mesh_poly('GS_Sword_R',[(.50,1.28),(.61,1.34),(.93,.78),(.84,.72)],.20,STEEL,'04 Sword equipment',.012)
mesh_poly('GS_Sword_R_Handle',[(.45,1.35),(.51,1.42),(.68,1.28),(.62,1.20)],.24,INK,'04 Sword equipment',.012)
# Flowers echo the source: white in hat and chest, blue/pink on shoulders, small hem blossoms.
flower('GS_Flower_Hat',-.34,3.22,WHITE,r=.16)
flower('GS_Flower_Shoulder',-.74,1.78,PINK,r=.17)
flower('GS_Flower_Collar',.22,1.94,BLUE,r=.16)
flower('GS_Flower_Chest',.36,1.32,WHITE,r=.15)
flower('GS_Flower_Hem',-.30,.62,PINK,r=.15)
flower('GS_Flower_Lower',-1.02,.35,YELLOW,r=.13)
# Thin foliage strokes with simple curve geometry.
for i,coords in enumerate([
    [(-.96,-.36,.30),(-.65,-.37,.62),(-.33,-.37,.76)],
    [(-.86,-.38,.38),(-.45,-.39,.87),(-.25,-.39,1.10)],
    [(.30,-.35,.36),(.45,-.36,.74),(.49,-.36,1.05)],
    [(-.55,-.36,1.58),(-.36,-.37,1.82),(-.15,-.37,1.97)],
]): line('GS_Vine_%02d'%i,coords,.018,LIME,'03 Flowers and foliage')

# Sculptural structure behind the illustrated front. These solid forms give
# the character meaningful depth when viewed from the side or back. The front
# reference is a curved, transparent surface, not a flat mirrored card.
def volume(name, loc, scale, material, group='01 Body and cloak'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20, location=loc)
    ob=bpy.context.object; ob.name=name; ob.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for p in ob.data.polygons: p.use_smooth=True
    ob.data.materials.append(material); move_to(ob,group)
    return ob

def bone_segment(name, a, b, radius, material=SKULL, group='01 Body and cloak'):
    mid=(Vector(a)+Vector(b))*0.5; vec=Vector(b)-Vector(a)
    ob=volume(name,mid,(radius,radius,vec.length*.58),material,group)
    ob.rotation_mode='QUATERNION'; ob.rotation_quaternion=vec.to_track_quat('Z','Y')
    return ob

# Torso, shoulder girdle, pelvis, arms and legs form a continuous hidden body
# volume beneath the cloak. A second, mirrored side gives the back real shape.
volume('GS_Body_Ribcage', (0,.02,1.70),(.46,.38,.68),DEEP)
volume('GS_Body_Pelvis', (0,.03,.91),(.42,.36,.39),DEEP)
for side in (-1,1):
    s=float(side)
    volume('GS_Shoulder_L' if side<0 else 'GS_Shoulder_R',(s*.43,-.01,1.99),(.28,.34,.29),GREEN)
    bone_segment(('GS_Humerus_L' if side<0 else 'GS_Humerus_R'),(s*.48,0,1.92),(s*.68,-.01,1.52),.15,RED)
    bone_segment(('GS_Forearm_L' if side<0 else 'GS_Forearm_R'),(s*.68,-.01,1.52),(s*.52,-.03,1.27),.12,SKULL)
    bone_segment(('GS_Femur_L' if side<0 else 'GS_Femur_R'),(s*.22,.02,.90),(s*.25,.02,.40),.16,SKULL)
    bone_segment(('GS_Tibia_L' if side<0 else 'GS_Tibia_R'),(s*.25,.02,.40),(s*.25,-.04,.08),.11,SKULL)
# Rib-like arcs under the cloak, visible from open/turnaround angles.
for z, width in [(1.88,.34),(1.68,.39),(1.48,.31)]:
    coords=[]
    for j in range(13):
        t=math.pi*j/12
        coords.append((width*math.cos(t),-.35*math.sin(t),z-.10*math.sin(t)))
    line('GS_Rib_Arc_%02d'%int(z*100),coords,.025,SKULL,'01 Body and cloak')

# Preserve the supplied 2D finish as an alpha-textured skin that follows a
# shallow bow across the front. The underlying mesh remains fully volumetric.
def add_curved_reference_skin():
    image=bpy.data.images.load(OUT+'reference_frame0000.png', check_existing=True)
    nx,nz=36,52; width,height=2.72,4.08; cx=-.02; cz=1.90
    verts=[]; faces=[]; uvs=[]
    for j in range(nz+1):
        v=j/nz
        for i in range(nx+1):
            u=i/nx; x=cx+(u-.5)*width
            y=-.73-.12*(2*u-1)**2
            verts.append((x,y,cz+(v-.5)*height))
    for j in range(nz):
        for i in range(nx):
            a=j*(nx+1)+i; b=a+1; d=a+nx+1; c=d+1
            faces.append((a,b,c,d))
            uvs.extend([(i/nx,j/nz),((i+1)/nx,j/nz),((i+1)/nx,(j+1)/nz),(i/nx,(j+1)/nz)])
    me=bpy.data.meshes.new('GS_ReferenceSkinMesh'); me.from_pydata(verts,[],faces); me.update()
    ob=bpy.data.objects.new('GS_ReferenceSkin_CurvedIllustration',me); COL['02 Skull and hat'].objects.link(ob)
    uv=me.uv_layers.new(name='Reference UV')
    for poly in me.polygons:
        for li in poly.loop_indices: uv.data[li].uv=uvs[li]
    image.pack()
    m=bpy.data.materials.new('Reference illustration | alpha color finish'); m.use_nodes=True
    nodes=m.node_tree.nodes; nodes.clear(); tex=nodes.new('ShaderNodeTexImage'); tex.image=image
    tex.interpolation='Closest'; tex.location=(-400,80)
    em=nodes.new('ShaderNodeEmission'); em.location=(-100,80)
    out=nodes.new('ShaderNodeOutputMaterial'); out.location=(120,80)
    transp=nodes.new('ShaderNodeBsdfTransparent'); transp.location=(-100,-100)
    mix=nodes.new('ShaderNodeMixShader'); mix.location=(100,-40)
    links=m.node_tree.links
    links.new(tex.outputs['Color'],em.inputs['Color']); links.new(tex.outputs['Alpha'],mix.inputs[0])
    links.new(transp.outputs[0],mix.inputs[1]); links.new(em.outputs[0],mix.inputs[2]); links.new(mix.outputs[0],out.inputs['Surface'])
    m.surface_render_method='DITHERED'; ob.data.materials.append(m)
    ob['purpose']='Curved transparent front finish; volumetric body and costume are modeled behind it.'
    return ob
add_curved_reference_skin()

# Side-view camera and soft illustrated lighting.
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.eevee.taa_render_samples=32
scene.render.resolution_x=640; scene.render.resolution_y=800; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'; scene.render.film_transparent=True
scene.world.color=(0.12,0.12,0.12)
cam_data=bpy.data.cameras.new('GS_Side_Camera'); cam=bpy.data.objects.new('GS_Side_Camera',cam_data); COL['05 Stage'].objects.link(cam)
cam.location=(0,-9,1.62); cam.rotation_euler=(math.radians(90),0,0); cam_data.type='ORTHO'; cam_data.ortho_scale=4.15; scene.camera=cam
light_data=bpy.data.lights.new('Large softbox','AREA'); light=bpy.data.objects.new('Large softbox',light_data); COL['05 Stage'].objects.link(light); light.location=(-3,-4,5); light_data.energy=450; light_data.size=5
light.rotation_euler=(Vector((0,0,1.7))-light.location).to_track_quat('-Z','Y').to_euler()
scene.view_settings.view_transform='Standard'; scene.view_settings.look='Medium High Contrast'; scene.view_settings.exposure=0; scene.view_settings.gamma=1
scene.render.filepath=OUT+'GhostSamurai_Preview_v'+VERSION+'.png'
# Set useful front/side viewport and select whole character collection's contents.
bpy.ops.object.select_all(action='DESELECT')
for ob in COL['01 Body and cloak'].objects: ob.select_set(True)
if bpy.context.selected_objects: bpy.context.view_layer.objects.active=bpy.context.selected_objects[0]
bpy.ops.wm.save_as_mainfile(filepath=OUT+'GhostSamurai_Model_v'+VERSION+'.blend')
# Render the front-view fidelity check without the unfinished blockout pieces
# showing through. Keep every modeled part in the saved, editable 3D scene.
front_skin=bpy.data.objects.get('GS_ReferenceSkin_CurvedIllustration')
hidden_for_front=[]
for ob in bpy.data.objects:
    if ob is not front_skin and ob.type not in {'CAMERA','LIGHT'}:
        hidden_for_front.append((ob,ob.hide_render))
        ob.hide_render=True
bpy.ops.render.render(write_still=True)
for ob,was_hidden in hidden_for_front:
    ob.hide_render=was_hidden
bpy.ops.wm.save_as_mainfile(filepath=OUT+'GhostSamurai_Model_v'+VERSION+'.blend')
print('SAVED:', OUT+'GhostSamurai_Model_v'+VERSION+'.blend')
print('RENDER:', OUT+'GhostSamurai_Preview_v'+VERSION+'.png')
