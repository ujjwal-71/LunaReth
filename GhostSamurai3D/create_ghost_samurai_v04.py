"""Build a reference-faithful, depth-shaped Ghost Samurai study in Blender.

The original artwork remains the authority for the front-facing art style. The
front surface is bent in depth, while a modeled cloak, skull, flowers, vines,
and an armature provide real volume and a base for posing. This deliberately
does not add the kasa hat: the supplied current reference has no hat.
"""
import bpy, math, os
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(ROOT, 'reference_frame0000.png')
OUT = os.path.join(ROOT, 'GhostSamurai_Model_v04.blend')
PREVIEW = os.path.join(ROOT, 'GhostSamurai_Preview_v04.png')
ANGLE_PREVIEW = os.path.join(ROOT, 'GhostSamurai_DepthCheck_v04.png')

# Start a new scene/file; v01-v03 stay untouched.
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights, bpy.data.armatures):
    for block in list(datablocks):
        if block.users == 0: datablocks.remove(block)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1024; scene.render.resolution_y = 1152; scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'; scene.render.film_transparent = True
scene.world.color = (0,0,0)

def collection(name):
    c=bpy.data.collections.new(name); scene.collection.children.link(c); return c
parts = {n:collection(n) for n in ['GS | Artwork Surface','GS | Cloak Volume','GS | Skull Volume','GS | Flowers and Vines','GS | Skeleton Rig','GS | Presentation']}
def move(obj, col):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    parts[col].objects.link(obj); return obj
def material(name, color, rough=0.82):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(*color,1)
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=rough
    return m
INK=material('Ink black | linework',(0.012,0.018,0.01)); BONE=material('Skull bone | pale celadon',(0.82,0.88,0.72))
GREEN=material('Cloak core | deep moss',(0.10,0.30,0.035)); GREEN2=material('Leaf edge | vivid moss',(0.24,0.54,0.025))
RED=material('Cloak core | oxblood',(0.23,0.035,0.03)); WHITE=material('Flower | ivory',(0.94,0.93,0.82))
PINK=material('Flower | cherry pink',(0.96,0.25,0.39)); BLUE=material('Flower | indigo',(0.16,0.08,0.74)); GOLD=material('Flower | yellow',(1.0,0.53,0.025))

# Image coordinates (512 x 768) map to Blender X/Z. All front art is in front
# of the constructed volumes and follows a gently sculpted surface.
def px(x): return (x-256)*.01
def pz(y): return (768-y)*.01
def depth(x,y):
    # Rounded skull, shoulders and chest; sides roll backward toward the cloak.
    skull=0.25*math.exp(-(((x-322)/76)**2+((y-180)/78)**2))
    chest=0.18*math.exp(-(((x-272)/135)**2+((y-355)/180)**2))
    hem=0.10*math.exp(-(((x-250)/175)**2+((y-575)/120)**2))
    edge=0.20*(abs(x-256)/256)**2
    return -0.34-skull-chest-hem+edge

img=bpy.data.images.load(REF, check_existing=True)
img.pack(); img.filepath='//reference_frame0000.png'
surf=bpy.data.materials.new('Reference paint | original hand-drawn color and ink'); surf.use_nodes=True
nodes=surf.node_tree.nodes; links=surf.node_tree.links; bs=nodes.get('Principled BSDF')
tex=nodes.new('ShaderNodeTexImage'); tex.image=img; tex.interpolation='Linear'; tex.label='Original Krita-rendered reference'
links.new(tex.outputs['Color'],bs.inputs['Base Color']); links.new(tex.outputs['Alpha'],bs.inputs['Alpha'])
bs.inputs['Roughness'].default_value=1.0
surf.surface_render_method='DITHERED'
# Bent UV surface, not a flat image plane. Fine enough for the reference linework.
nx,ny=128,192; verts=[]; faces=[]; uvfaces=[]
for j in range(ny+1):
    y=768*j/ny
    for i in range(nx+1):
        x=512*i/nx; verts.append((px(x),depth(x,y),pz(y)))
for j in range(ny):
    for i in range(nx):
        # Winding faces the front (-Y) camera so the alpha ink texture renders cleanly.
        a=j*(nx+1)+i; faces.append((a,a+nx+1,a+nx+2,a+1))
me=bpy.data.meshes.new('GS_ReferenceSurface_Mesh'); me.from_pydata(verts,[],faces); me.materials.append(surf)
uv=me.uv_layers.new(name='Reference UV')
for poly in me.polygons:
    for li in poly.loop_indices:
        vi=me.loops[li].vertex_index; row=vi//(nx+1); col=vi%(nx+1)
        uv.data[li].uv=(col/nx,1-row/ny)
ob=bpy.data.objects.new('GS_ReferenceSurface | curved textured artwork',me); parts['GS | Artwork Surface'].objects.link(ob)
ob['Purpose']='Faithful front art surface, bent in depth over volumes; no hat.'

# Cloak core: changing elliptical sections create a tapered, rounded 3D mantle.
def loft(name, sections, mat, col, sides=24):
    v=[]; f=[]
    for z,cx,rx,ry,cy in sections:
        for i in range(sides):
            a=2*math.pi*i/sides; v.append((cx+rx*math.cos(a),cy+ry*math.sin(a),z))
    for r in range(len(sections)-1):
        for i in range(sides):
            a=r*sides+i; b=r*sides+(i+1)%sides; f.append((a,b,b+sides,a+sides))
    f.extend([tuple(range(sides-1,-1,-1)),tuple((len(sections)-1)*sides+i for i in range(sides))])
    mesh=bpy.data.meshes.new(name+'Mesh'); mesh.from_pydata(v,[],f); mesh.materials.append(mat)
    o=bpy.data.objects.new(name,mesh); parts[col].objects.link(o)
    for p in mesh.polygons:p.use_smooth=True
    return o
def alpha_run(img_y):
    row=767-img_y; runs=[]; start=None
    for x in range(512):
        a=img.pixels[(row*512+x)*4+3]
        if a>.2 and start is None: start=x
        if (a<=.2 or x==511) and start is not None:
            end=x if a<=.2 else x+1
            if end-start>=5:runs.append((start,end))
            start=None
    return max(runs,key=lambda r:r[1]-r[0]) if runs else None

sections=[]
for img_y in range(676,231,-18):
    bounds=alpha_run(img_y)
    if not bounds: continue
    left,right=bounds; half=(right-left)*.01*.47
    # The torso volume follows the largest opaque run at each height, inset
    # enough that the artwork remains the visible front surface.
    sections.append((pz(img_y),px((left+right)*.5),max(.08,half),max(.09,half*.42),.24))
sections.sort(key=lambda s:s[0])
cloak=loft('GS_CloakCore | tapered volume',sections,GREEN,'GS | Cloak Volume')
cloak['Design']='Volume beneath the painted, ragged cloak silhouette.'
# Distinct oxblood central mantle volume visible at turn angles.
sections2=[]
for z,cx,rx,ry,cy in sections:
    if 2.2<z<4.7: sections2.append((z,cx,rx*.46,ry*.56,.24))
loft('GS_OxbloodMantle | inner volume',sections2,RED,'GS | Cloak Volume')

def uv_sphere(name,loc,scale,mat,col,segments=24,rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    return move(o,col)
def curve(name,coords,radius,mat,col):
    cu=bpy.data.curves.new(name,'CURVE'); cu.dimensions='3D'; cu.resolution_u=16; cu.bevel_depth=radius; cu.bevel_resolution=3
    sp=cu.splines.new('BEZIER'); sp.bezier_points.add(len(coords)-1)
    for p,co in zip(sp.bezier_points,coords):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,cu); parts[col].objects.link(o); cu.materials.append(mat); return o

# Volumetric skull placed behind the exact illustrated skull in the source.
skull=uv_sphere('GS_Skull | cranium volume',(px(322),.05,pz(185)),(.39,.36,.43),BONE,'GS | Skull Volume',32,20)
jaw=uv_sphere('GS_Skull | jaw volume',(px(332),.07,pz(228)),(.29,.32,.21),BONE,'GS | Skull Volume',28,18)
# Deep sockets and nasal opening are sculptural relief, aligned behind painted marks.
for n,x,y,sx,sz in [('EyeSocket_L',301,178,.105,.11),('EyeSocket_R',358,179,.10,.12),('NasalCavity',328,211,.065,.09)]:
    uv_sphere('GS_'+n,(px(x),-.04,pz(y)),(sx,.04,sz),INK,'GS | Skull Volume',20,12)
# Individual small teeth relief; the drawing's teeth remain visible in the painted front.
for i in range(7):
    x=308+i*9
    uv_sphere('GS_Tooth_%02d'%i,(px(x),-.025,pz(233)),(.035,.03,.055),BONE,'GS | Skull Volume',12,8)

# Flower meshes are centered on the exact reference positions, with slightly
# cupped petals and rounded centers so they have real volume from oblique views.
def flower(name,x,y,petalmat,r=.16,count=5):
    cx,cz=px(x),pz(y); cy=depth(x,y)+.42
    for i in range(count):
        a=2*math.pi*i/count
        pet=uv_sphere(name+'_Petal_%02d'%i,(cx+math.cos(a)*r*.68,cy,cz+math.sin(a)*r*.68),(r*.48,.075,r*.66),petalmat,'GS | Flowers and Vines',20,12)
        pet.rotation_euler[1]=-a
    uv_sphere(name+'_Heart',(cx,cy-.07,cz),(.085,.065,.085),GOLD,'GS | Flowers and Vines',16,10)
flower('GS_Flower_Crown',277,143,WHITE,.13)
flower('GS_Flower_BlueShoulder',326,297,BLUE,.20)
flower('GS_Flower_PinkShoulder',232,320,PINK,.17)
flower('GS_Flower_WhiteChest',328,386,WHITE,.14)
flower('GS_Flower_PinkHem',201,531,PINK,.17)
flower('GS_Flower_YellowHem',112,590,GOLD,.14,6)
# Stems/vines track the illustration's broad arcs.
for idx,path in enumerate([
    [(240,390),(210,430),(182,480),(155,530),(118,580)],
    [(295,310),(270,355),(246,390),(220,430),(195,490)],
    [(330,315),(359,360),(370,414),(384,475),(395,545)],
    [(270,250),(236,275),(211,304),(185,337)],
]):
    coords=[(px(x),depth(x,y)+.38,pz(y)) for x,y in path]
    curve('GS_Vine_%02d'%idx,coords,.018,GREEN2,'GS | Flowers and Vines')

# Base rig: centerline and limb chains. Mesh surface weights are assigned by
# height for a usable first posing pass; later refinement can split cloak panels.
arm=bpy.data.armatures.new('GS_RigData'); rig=bpy.data.objects.new('GS_Rig | base posing skeleton',arm); parts['GS | Skeleton Rig'].objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None,connect=False):
    b=arm.edit_bones.new(name); b.head=head; b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]; b.use_connect=connect
bone('Root',(0,0,.25),(0,0,1.25))
bone('Spine_Lower',(0,0,1.25),(0,0,2.60),'Root')
bone('Spine_Upper',(0,0,2.60),(0,0,4.15),'Spine_Lower',True)
bone('Neck',(0,0,4.15),(px(320),-.2,pz(255)),'Spine_Upper')
bone('Head',(px(320),-.2,pz(255)),(px(320),-.2,pz(145)),'Neck')
bone('Arm_L_Shoulder',(-.40,0,3.95),(-.83,0,3.20),'Spine_Upper')
bone('Arm_L_Forearm',(-.83,0,3.20),(-1.08,0,2.55),'Arm_L_Shoulder',True)
bone('Arm_R_Shoulder',(.40,0,3.95),(.83,0,3.20),'Spine_Upper')
bone('Arm_R_Forearm',(.83,0,3.20),(1.08,0,2.55),'Arm_R_Shoulder',True)
bone('Leg_L',(-.28,0,1.30),(-.31,0,.45),'Root')
bone('Leg_R',(.28,0,1.30),(.31,0,.45),'Root')
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True; rig.display_type='WIRE'
rig['Readme']='Starter skeleton for posing. Painted surface is weighted to head/spine zones; split panels for final deformation.'
# Attach the curved artwork surface and assign smooth height-based weights.
mod=ob.modifiers.new('GS_Rig Deform','ARMATURE'); mod.object=rig
for name in ('Root','Spine_Lower','Spine_Upper','Head'):
    ob.vertex_groups.new(name=name)
for v in me.vertices:
    z=v.co.z; headw=max(0,min(1,(z-5.35)/.55)); upper=max(0,min(1,(z-2.9)/1.3))*(1-headw)
    lower=max(0,1-headw-upper)
    if z<1.4: ob.vertex_groups['Root'].add([v.index],max(.2,lower), 'REPLACE')
    ob.vertex_groups['Head'].add([v.index],headw,'REPLACE')
    ob.vertex_groups['Spine_Upper'].add([v.index],upper,'REPLACE')
    ob.vertex_groups['Spine_Lower'].add([v.index],lower,'REPLACE')

# Orthographic preview from exact reference angle.
camd=bpy.data.cameras.new('GS_Camera_Front'); cam=bpy.data.objects.new('GS_Camera_Front',camd); parts['GS | Presentation'].objects.link(cam)
cam.location=(0,-15,3.84); cam.rotation_euler=(math.radians(90),0,0); camd.type='ORTHO'; camd.ortho_scale=8.2; scene.camera=cam
ld=bpy.data.lights.new('GS_Softbox','AREA'); light=bpy.data.objects.new('GS_Softbox',ld); parts['GS | Presentation'].objects.link(light)
light.location=(-4,-7,8); ld.energy=900; ld.size=6
scene.render.filepath=PREVIEW
scene.view_settings.view_transform='Standard'; scene.view_settings.look='Medium High Contrast'; scene.view_settings.exposure=0; scene.view_settings.gamma=1
# Make the source-faithful front preview without hidden volume pieces leaking
# across transparent pixels. The saved .blend restores all modeled parts.
for name in ('GS | Cloak Volume','GS | Skull Volume','GS | Flowers and Vines','GS | Skeleton Rig'):
    parts[name].hide_render=True
bpy.ops.render.render(write_still=True)
for name in ('GS | Cloak Volume','GS | Skull Volume','GS | Flowers and Vines','GS | Skeleton Rig'):
    parts[name].hide_render=False
# Render the actual depth build from a mild three-quarter view for QA.
from mathutils import Vector
front_location=cam.location.copy(); front_rotation=cam.rotation_euler.copy()
cam.location=(4.0,-14.0,4.1); cam.rotation_euler=(Vector((0,0,3.8))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=ANGLE_PREVIEW
bpy.ops.render.render(write_still=True)
cam.location=front_location; cam.rotation_euler=front_rotation
scene.render.filepath=PREVIEW
bpy.ops.wm.save_as_mainfile(filepath=OUT)
print('SAVED',OUT); print('RENDERED',PREVIEW); print('DEPTH_CHECK',ANGLE_PREVIEW)
