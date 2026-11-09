"""Produce a real 240-frame Blender PNG sequence + H.264 P01 rough cut.

No source .blend or Unity files are modified. Creates the first complete,
inspectable animated video (no unapproved narration).
"""
from pathlib import Path
import subprocess
import bpy

HERE = Path(__file__).resolve().parent
SOURCE = HERE / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v03.blend"
OUT = HERE / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_8sec_v03.mp4"
FRAMES = HERE / "VideoFrames_v03"
if not SOURCE.is_file():
    raise FileNotFoundError(SOURCE)
if OUT.exists() or (FRAMES.exists() and any(FRAMES.iterdir())):
    raise FileExistsError("Refusing to overwrite MP4 or existing frame images")
FRAMES.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.frame_start, scene.frame_end = 1, 240
scene.render.fps = 30
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGB"
scene.render.filepath = str(FRAMES / "P01_frame_")
print("RENDERING_PANEL01_240_FRAMES_TO=" + str(FRAMES), flush=True)
bpy.ops.render.render(animation=True)
produced = sorted(FRAMES.glob("P01_frame_*.png"))
if len(produced) != 240:
    raise RuntimeError(f"Expected 240 PNGs, found {len(produced)}")
ffmpeg_binaries = list((Path.home() / "Documents/Codex/MocapLab/mixamo-llm-mocap/tools/ffmpeg").glob("*/bin/ffmpeg.exe"))
if len(ffmpeg_binaries) != 1:
    raise RuntimeError(f"Missing portable installed FFmpeg, found {len(ffmpeg_binaries)}")
print("ENCODING_P01_MP4=" + str(OUT), flush=True)
subprocess.run([
    str(ffmpeg_binaries[0]), "-hide_banner", "-loglevel", "error",
    "-framerate", "30", "-start_number", "1",
    "-i", str(FRAMES / "P01_frame_%04d.png"),
    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "21",
    "-movflags", "+faststart", "-n", str(OUT)
], check=True)
if not OUT.is_file() or OUT.stat().st_size < 100_000:
    raise RuntimeError("FFmpeg completed but MP4 missing or too small")
print("P01_FULL_VIDEO_DONE=" + str(OUT), flush=True)
print("P01_MP4_BYTES=" + str(OUT.stat().st_size), flush=True)
