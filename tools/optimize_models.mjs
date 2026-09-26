// Navigator 2.0 — tools/optimize_models.mjs
// Shrinks the Meshy .glb models so the phone app stays light, keeping how they look:
//   · textured models: 4096 px textures → 1024 (planets 2048; metal/roughness 512). Geometry untouched.
//   · untextured high-poly models (Meshy exports up to 5.5 million triangles): simplified to a
//     phone-sized count with a strict error limit (0.15 % of the model size), so the silhouette
//     and panels stay put.
// Result on the 2026 set: 1,387 MB → 133 MB. No glTF extensions are used: glTFast reads them as-is.
//
//   cd tools && npm install && node optimize_models.mjs
//
// Only *_FINAL.glb files are touched. Originals are copied to unity_project/models_original/
// (ignored by git) the first time, so nothing is lost; running it again is safe.
// Afterwards: open Unity, let it re-import, then Navigator → Setup Scene.
// Guacalita Inc. — SAL / Claude

import { NodeIO } from '@gltf-transform/core';
import { dedup, prune, weld, simplify, textureCompress } from '@gltf-transform/functions';
import { MeshoptSimplifier } from 'meshoptimizer';
import sharp from 'sharp';
import { readdirSync, statSync, copyFileSync, mkdirSync, existsSync } from 'node:fs';
import { join, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const repo   = join(dirname(fileURLToPath(import.meta.url)), '..');
const root   = join(repo, 'unity_project', 'NavigatorUnity', 'Assets', 'Models');
const backup = join(repo, 'unity_project', 'models_original');
const io = new NodeIO();
await MeshoptSimplifier.ready;

// Triangle budget and texture size per kind of model (by the Meshy file prefix)
function cfg(n) {
  if (/^PD-/.test(n))          return { tris: 60000,  tex: 512 };    // pods: small on screen
  if (/^MN-/.test(n))          return { tris: 50000,  tex: 1024 };   // moons
  if (/^OB-10|^OB-01/.test(n)) return { tris: 50000,  tex: 1024 };   // asteroids
  if (/^PL-/.test(n))          return { tris: 80000,  tex: 2048 };   // planets fill the screen on arrival
  return                              { tris: 150000, tex: 1024 };   // ships, probes, launcher, the rest
}

const tris = d => {
  let t = 0;
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    const i = p.getIndices();
    t += (i ? i.getCount() : p.getAttribute('POSITION').getCount()) / 3;
  }
  return t;
};
function* walk(d) {
  for (const e of readdirSync(d, { withFileTypes: true })) {
    const p = join(d, e.name);
    if (e.isDirectory()) yield* walk(p);
    else if (p.endsWith('_FINAL.glb')) yield p;
  }
}

let before = 0, after = 0;
for (const f of walk(root)) {
  const name = f.split(/[\\/]/).pop(), c = cfg(name), size = statSync(f).size;
  if (size < 1024) { console.log(`skip ${name}: Git LFS pointer (run "git lfs pull" first)`); continue; }
  let doc;
  try { doc = await io.read(f); } catch { console.log(`skip ${name}: broken or incomplete file, download it again`); continue; }
  const t0 = tris(doc);

  const bk = join(backup, relative(root, f));
  mkdirSync(dirname(bk), { recursive: true });
  if (!existsSync(bk)) copyFileSync(f, bk);

  const steps = [dedup(), weld()];
  if (t0 > c.tris) steps.push(simplify({ simplifier: MeshoptSimplifier, ratio: c.tris / t0, error: 0.0015, lockBorder: false }));
  steps.push(
    textureCompress({ encoder: sharp, resize: [512, 512],     slots: /metallicRoughness|occlusion/, quality: 85 }),
    textureCompress({ encoder: sharp, resize: [c.tex, c.tex], slots: /baseColor|normal|emissive/,   quality: 85 }),
    prune());
  await doc.transform(...steps);
  await io.write(f, doc);

  const s2 = statSync(f).size;
  before += size; after += s2;
  console.log(`${name.padEnd(28)} ${(size / 1048576).toFixed(1).padStart(6)} → ${(s2 / 1048576).toFixed(1).padStart(5)} MB   triangles ${(t0 / 1000).toFixed(0)}k → ${(tris(doc) / 1000).toFixed(0)}k`);
}
console.log(`TOTAL ${(before / 1048576).toFixed(0)} MB → ${(after / 1048576).toFixed(0)} MB`);
