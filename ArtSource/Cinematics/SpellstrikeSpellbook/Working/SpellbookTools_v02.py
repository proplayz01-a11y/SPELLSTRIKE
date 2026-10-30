import bpy, math, random
from mathutils import Vector
TAU=math.tau
def collection(name, parent=None):
    if name in bpy.data.collections: return bpy.data.collections[name]
    c=bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(c)
    return c
def link(obj,coll):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    coll.objects.link(obj)
    return obj
def material(name,color,metal=0.0,rough=.7,noise=False):
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    bs=m.node_tree.nodes.get("Principled BSDF")
    if bs:
        bs.inputs["Base Color"].default_value=(*color,1)
        bs.inputs["Metallic"].default_value=metal
        bs.inputs["Roughness"].default_value=rough
        if noise and not m.node_tree.nodes.get("SSB_LeatherNoise"):
            tex=m.node_tree.nodes.new("ShaderNodeTexNoise");tex.name="SSB_LeatherNoise";tex.inputs["Scale"].default_value=65
            bump=m.node_tree.nodes.new("ShaderNodeBump");bump.name="SSB_LeatherBump";bump.inputs["Strength"].default_value=.095;bump.inputs["Distance"].default_value=.035
            m.node_tree.links.new(tex.outputs["Fac"],bump.inputs["Height"])
            m.node_tree.links.new(bump.outputs["Normal"],bs.inputs["Normal"])
    return m
def cube(name, loc, size, mat, coll, parent=None, bevel=.0):
    bpy.ops.mesh.primitive_cube_add()
    o=bpy.context.object;o.name=name;link(o,coll)
    if parent:o.parent=parent
    o.location=loc;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if mat:o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new("Softened physical edges","BEVEL");mod.width=bevel;mod.segments=3
        o.modifiers.new("Weighted corner normals","WEIGHTED_NORMAL")
    return o
def empty(name, coll, parent=None, loc=(0,0,0)):
    o=bpy.data.objects.new(name,None);coll.objects.link(o);o.empty_display_size=.12
    if parent:o.parent=parent
    o.location=loc
    return o
def uv_sphere(name, loc, size,mat,coll,parent=None,segments=16,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings)
    o=bpy.context.object;o.name=name;link(o,coll)
    if parent:o.parent=parent
    o.location=loc;o.scale=size
    if mat:o.data.materials.append(mat)
    for p in o.data.polygons:p.use_smooth=True
    return o
def cylinder(name,loc,radius,depth,mat,coll,parent=None,vertices=32,rotate=(0,0,0),bevel=.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth)
    o=bpy.context.object;o.name=name;link(o,coll)
    if parent:o.parent=parent
    o.location=loc;o.rotation_euler=rotate
    if mat:o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new("Rounded rims","BEVEL");mod.width=bevel;mod.segments=3
        o.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
    return o
def tube(name, coords, mat, coll, parent=None, width=.009, cyclic=False):
    curve=bpy.data.curves.new(name,"CURVE");curve.dimensions="3D";curve.resolution_u=16
    curve.bevel_depth=width;curve.bevel_resolution=3
    sp=curve.splines.new("POLY");sp.points.add(len(coords)-1)
    for p,co in zip(sp.points,coords):p.co=(*co,1)
    sp.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(name,curve);coll.objects.link(ob)
    if parent:ob.parent=parent
    if mat:curve.materials.append(mat)
    return ob
def ring(name, center, radius, mat, coll, parent=None, width=.01, segments=96):
    coords=[(center[0]+radius*math.cos(i*TAU/segments),center[1]+radius*math.sin(i*TAU/segments),center[2]) for i in range(segments)]
    return tube(name,coords,mat,coll,parent,width,True)
def polygon(name,points,z,thick,mat,coll,parent=None):
    # Closed polygon extruded along Z, with stable face UVs 0..1.
    n=len(points)
    verts=[(x,y,z) for x,y in points]+[(x,y,z-thick) for x,y in points]
    faces=[tuple(range(n)),tuple(range(n*2-1,n-1,-1))]
    for i in range(n):
        j=(i+1)%n;faces.append((i,j,j+n,i+n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);coll.objects.link(ob)
    if parent:ob.parent=parent
    if mat:mesh.materials.append(mat)
    return ob
def page_mesh(name, number, count, mat, coll, pivot):
    # Each physical leaf is a distinct manifold mesh. Local x=0 at hinge.
    nx,ny=8,12;W,H=2.42,3.27;th=.011
    verts=[]
    for layer in [0,1]:
        for j in range(ny+1):
            y=-H/2+H*j/ny
            for i in range(nx+1):
                x=.04+(W-.04)*i/nx
                curl=.010*(i/nx)**5 + .005*(abs(y)/(H/2))**8
                ripple=.0018*math.sin((number+2)*2.7+y*9.3+x*8.2)
                z=curl+ripple + (th/2 if layer==0 else -th/2)
                verts.append((x,y,z))
    per=(nx+1)*(ny+1)
    faces=[];uvs=[]
    for j in range(ny):
        for i in range(nx):
            a=j*(nx+1)+i;b=a+1;c=a+(nx+1);d=c+1
            faces.append((a,b,d,c))
            u0=i/nx;u1=(i+1)/nx;v0=j/ny;v1=(j+1)/ny
            uvs.append(((u0,v0),(u1,v0),(u1,v1),(u0,v1)))
            faces.append((per+c,per+d,per+b,per+a))
            uvs.append(((1-u0,v1),(1-u1,v1),(1-u1,v0),(1-u0,v0)))
    for j in range(ny):
        a=j*(nx+1);b=(j+1)*(nx+1)
        faces.append((a,b,per+b,per+a));uvs.append(((0,0),(1,0),(1,1),(0,1)))
        a=j*(nx+1)+nx;b=(j+1)*(nx+1)+nx
        faces.append((b,a,per+a,per+b));uvs.append(((0,0),(1,0),(1,1),(0,1)))
    for i in range(nx):
        a=i;b=i+1
        faces.append((b,a,per+a,per+b));uvs.append(((0,0),(1,0),(1,1),(0,1)))
        a=ny*(nx+1)+i;b=a+1
        faces.append((a,b,per+b,per+a));uvs.append(((0,0),(1,0),(1,1),(0,1)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    uv=mesh.uv_layers.new(name="Page_Art_UV")
    for face,uv4 in zip(mesh.polygons,uvs):
        for li,uvcoord in zip(face.loop_indices,uv4):
            uv.data[li].uv=uvcoord
    mesh.materials.append(mat)
    ob=bpy.data.objects.new(name,mesh);coll.objects.link(ob);ob.parent=pivot
    ob["is_independent_page"]=True
    ob["page_index"]=number
    ob["page_front_texture_slot"]=0
    return ob
