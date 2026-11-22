import bpy, math
from mathutils import Vector

def create_arcane_strike(key, label, rgb, path, preview_x, half_width, branch_indices):
    meshes = bpy.data.collections["Lightning_Meshes"]
    sources = bpy.data.collections["Editable_Bezier_Sources"]
    labels = bpy.data.collections["Preview_Labels"]
    name = "ArcaneSurge_" + key

    # Editable Blender Bezier source (the zig-zag creation technique from the video).
    curve = bpy.data.curves.new(name + "_BezierSource", 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 1
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(path)-1)
    for p,(x,z) in zip(spline.bezier_points,path):
        p.co = (x,0,z)
        p.handle_left_type = 'VECTOR'
        p.handle_right_type = 'VECTOR'
    src = bpy.data.objects.new(name + "_SOURCE_EDITABLE", curve)
    sources.objects.link(src)
    src["Modeling_Method"] = "Blender Bezier control points, vector handles, zigzag"
    src.hide_render = True

    mat = bpy.data.materials.get(name + "_Emissive")
    if mat is None:
        mat = bpy.data.materials.new(name + "_Emissive")
    mat.diffuse_color = (*rgb,1)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    output.location=(220,0)
    em = nodes.new('ShaderNodeEmission')
    em.location=(0,0)
    em.inputs['Color'].default_value=(*rgb,1)
    em.inputs['Strength'].default_value=3.0
    mat.node_tree.links.new(em.outputs['Emission'], output.inputs['Surface'])

    vertices = []
    faces = []
    uv_pairs = []
    def add_ribbon(points, basewidth, yoffset=0.0, fork=False):
        start = len(vertices)
        levels = len(points)
        for i,(x,z) in enumerate(points):
            if i == 0: dx,dz=points[1][0]-x,points[1][1]-z
            elif i == levels-1: dx,dz=x-points[i-1][0],z-points[i-1][1]
            else: dx,dz=points[i+1][0]-points[i-1][0],points[i+1][1]-points[i-1][1]
            mag = max(1e-6, math.hypot(dx,dz))
            nx,nz=dz/mag,-dx/mag
            if fork: 
                taper = (1.0 - 0.84 * i/(levels-1))
            else:
                t = max(0.0,min(1.0,z/10.0))
                taper = 0.16 + 0.84*t
            width = basewidth*taper
            vertices.extend(((x-nx*width,yoffset,z-nz*width),
                             (x+nx*width,yoffset,z+nz*width)))
        for i in range(levels-1):
            faces.append((start+2*i,start+2*i+1,start+2*i+3,start+2*i+2))
            v0 = 1.0 - i/(levels-1)
            v1 = 1.0 - (i+1)/(levels-1)
            uv_pairs.append(((0.0,v0),(1.0,v0),(1.0,v1),(0.0,v1)))

    add_ribbon(path,half_width)
    for branch_index,sign in branch_indices:
        ox,oz=path[branch_index]
        dest = [(ox,oz),
                (ox+sign*0.36,oz-0.23),
                (ox+sign*0.79,oz-0.57),
                (ox+sign*1.06,oz-0.92)]
        add_ribbon(dest,half_width*0.39,yoffset=-0.008,fork=True)

    mesh=bpy.data.meshes.new(name+"_Mesh")
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    uv=mesh.uv_layers.new(name="LightningUV")
    for polygon, quad_uv in zip(mesh.polygons,uv_pairs):
        for loop_index,coords in zip(polygon.loop_indices,quad_uv):
            uv.data[loop_index].uv = coords
    mesh.materials.append(mat)
    obj=bpy.data.objects.new(name,mesh)
    meshes.objects.link(obj)
    obj.location=(preview_x,0,0)
    obj["ArcaneSurge_BoltType"] = label
    obj["ArcaneSurge_ColorRGB"] = list(rgb)
    obj["ArcaneSurge_Pivot"] = "BOTTOM_CONTACT_AT_LOCAL_ORIGIN"
    obj["ArcaneSurge_HeightMeters"] = 10.0
    obj["ArcaneSurge_UV"] = "U across ribbon, V down full bolt"
    obj["ArcaneSurge_Tutorial"] = "Based on Gabriel Aguiar 05:26-08:50 zigzag Bezier / converted mesh / UV / bottom pivot"

    text_curve=bpy.data.curves.new(name+"_Label","FONT")
    text_curve.body=label.upper()
    text_curve.align_x='CENTER'
    text_curve.size=.38
    label_obj=bpy.data.objects.new(name+"_Label",text_curve)
    labels.objects.link(label_obj)
    label_obj.location=(preview_x,-0.18,-0.78)
    label_obj.rotation_euler=(math.pi/2,0,0)

    return dict(object=obj.name, verts=len(vertices), faces=len(faces), uv_faces=len(uv_pairs),
                origin="tip at local (0,0,0)", preview_x=preview_x,
                source=src.name, material=mat.name)
