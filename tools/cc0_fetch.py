#!/usr/bin/env python3
# Navigator 2.0 — downloads the CC0 (public domain) assets of the worlds, listed in
# NAVIGATOR_CC0_ASSETS.md and CREDITS.md. Re-run it any time: python3 tools/cc0_fetch.py
#   · rocks: Poly Haven models come as .gltf + .bin + textures; this packs them into one .glb
#     that Unity (glTFast) imports.
#   · ground: the photo is turned into a neutral grey detail map (mean 50 %, so the world's own
#     colour stays) plus its OpenGL normal map. Needs Pillow: pip install pillow
# Only CC0 sources are used: Poly Haven (polyhaven.com/license).
# Guacalita Inc. — SAL / Claude
import io, json, os, statistics, struct, sys, urllib.request

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "unity_project", "NavigatorUnity", "Assets")

# (Poly Haven id, output path under Assets)
MODELS = [
    ("moon_rock_03", "Models/Props/PR-02_MercuryBoulder_FINAL.glb"),
    ("rock_07",      "Models/Props/PR-04_MarsLayeredRock_FINAL.glb"),
    ("rock_09",      "Models/Props/PR-06_MarsBoulder_FINAL.glb"),
    ("moon_rock_01", "Models/Props/PR-10_TritonFrostRock_FINAL.glb"),
    # Variants: every _FINAL with the same code is scattered over the world
    ("moon_rock_02", "Models/Props/PR-02_MercuryBoulder_b_FINAL.glb"),
    ("moon_rock_04", "Models/Props/PR-02_MercuryBoulder_c_FINAL.glb"),
    ("moon_rock_06", "Models/Props/PR-02_MercuryBoulder_d_FINAL.glb"),
    ("namaqualand_boulder_04", "Models/Props/PR-06_MarsBoulder_b_FINAL.glb"),   # heavy: run optimize_models.mjs
    ("namaqualand_boulder_06", "Models/Props/PR-06_MarsBoulder_c_FINAL.glb"),   # heavy: run optimize_models.mjs
    ("moon_rock_05", "Models/Props/PR-10_TritonFrostRock_b_FINAL.glb"),
    ("moon_rock_07", "Models/Props/PR-10_TritonFrostRock_c_FINAL.glb"),
]

# (Poly Haven id, world) → Art/Ground/ground_<world>.jpg + ground_<world>_normal.jpg
GROUNDS = [
    ("moon_01",       "mercury"),
    ("dark_rock",     "venus"),
    ("gravelly_sand", "mars"),
    ("coast_sand_04", "titan"),
    ("snow_03",       "triton"),
]

UA = {"User-Agent": "Navigator-CC0-fetch/1.0"}

def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120) as r:
        return r.read()

def files(pid):
    return json.loads(get("https://api.polyhaven.com/files/" + pid))

def pad(b, fill=b"\0"):
    return b + fill * ((4 - len(b) % 4) % 4)

def pack_glb(gltf, blobs):
    """gltf dict + {relative uri: bytes} -> .glb bytes (one buffer, images embedded)."""
    blob = bytearray()
    srcs = [blobs[b["uri"]] for b in gltf["buffers"]]
    for v in gltf["bufferViews"]:
        start = v.get("byteOffset", 0)
        data = srcs[v["buffer"]][start:start + v["byteLength"]]
        v["byteOffset"] = len(blob)
        v["buffer"] = 0
        blob += pad(data)
    for img in gltf.get("images", []):
        uri = img.pop("uri")
        data = blobs[uri]
        gltf["bufferViews"].append({"buffer": 0, "byteOffset": len(blob), "byteLength": len(data)})
        img["bufferView"] = len(gltf["bufferViews"]) - 1
        img["mimeType"] = "image/png" if uri.lower().endswith(".png") else "image/jpeg"
        blob += pad(data)
    gltf["buffers"] = [{"byteLength": len(blob)}]
    js = pad(json.dumps(gltf, separators=(",", ":")).encode(), b" ")
    total = 12 + 8 + len(js) + 8 + len(blob)
    return (struct.pack("<III", 0x46546C67, 2, total) + struct.pack("<II", len(js), 0x4E4F534A) + js
            + struct.pack("<II", len(blob), 0x004E4942) + bytes(blob))

def model(pid, out):
    info = files(pid)["gltf"]["1k"]["gltf"]
    gltf = json.loads(get(info["url"]))
    blobs = {rel: get(f["url"]) for rel, f in info["include"].items()}
    dest = os.path.join(ROOT, out)
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    with open(dest, "wb") as fh:
        fh.write(pack_glb(gltf, blobs))
    print("model  ", out, os.path.getsize(dest) // 1024, "KB")

def ground(pid, world):
    try:
        from PIL import Image, ImageOps
    except ImportError:
        sys.exit("The ground textures need Pillow: pip install pillow")
    f = files(pid)
    dest = os.path.join(ROOT, "Art", "Ground")
    os.makedirs(dest, exist_ok=True)
    # URP multiplies the painted colour by detail × 2: a grey map with mean 50 % keeps the colour
    im = ImageOps.grayscale(Image.open(io.BytesIO(get(f["Diffuse"]["1k"]["jpg"]["url"]))))
    px = list(im.tobytes())   # one byte per pixel in grey ("L") mode
    mean, sd = statistics.mean(px), statistics.pstdev(px) or 1
    k = min(1.0, 34 / sd)   # keep the grain, cap the contrast
    im.point(lambda v: max(0, min(255, round(128 + (v - mean) * k)))).save(
        os.path.join(dest, "ground_%s.jpg" % world), quality=90)
    Image.open(io.BytesIO(get(f["nor_gl"]["1k"]["jpg"]["url"]))).convert("RGB").save(
        os.path.join(dest, "ground_%s_normal.jpg" % world), quality=88)
    print("ground ", "Art/Ground/ground_%s.jpg" % world, "(+ _normal)")

if __name__ == "__main__":
    only = sys.argv[1:]
    for m in MODELS:
        if not only or m[0] in only: model(*m)
    for g in GROUNDS:
        if not only or g[0] in only: ground(*g)
