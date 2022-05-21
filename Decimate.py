import bpy
import sys

argv = sys.argv
#Get only our arguments
argv = argv[argv.index("--") + 1:]
print(argv)
objPath = argv[0]
decimateValue = argv[1]

#Delete default objects:cube,camera,light
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

imported_object = bpy.ops.import_scene.obj(filepath=objPath)
obj_object = bpy.context.selected_objects[0]
decimate = obj_object.modifiers.new(name="Decimate", type = 'DECIMATE')
decimate.ratio = float(decimateValue)

#Export as obj
bpy.ops.export_scene.obj(filepath=objPath)