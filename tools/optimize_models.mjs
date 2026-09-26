// Navigator 2.0 — tools/optimize_models.mjs
// Shrinks the Meshy .glb models so the phone app stays light: fewer polygons, smaller textures.
// Meshy exports 4–126 MB per model; a pod or a moon on a phone screen needs a fraction of that.
//
//   node tools/optimize_models.mjs            (from the repo root; needs Node 18+, uses npx)
//   node tools/optimize_models.mjs --dry      (only lists what it would do)
//
// Originals are copied first to unity_project/models_original/ (ignored by git), so nothing
// is lost: to undo, copy a file back. Output uses no glTF extensions, so glTFast reads it as-is.
// After running: open Unity, let it re-import, then Navigator → Setup Scene.
// Guacalita Inc. — SAL / Claude

import { spawnSync } from 'node:child_process';
import { copyFileSync, existsSync, mkdirSync, readdirSync, renameSync, statSync, unlinkSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const repo    = join(dirname(fileURLToPath(import.meta.url)), '..');
const models  = join(repo, 'unity_project', 'NavigatorUnity', 'Assets', 'Models');
const backups = join(repo, 'unity_project', 'models_original');
const dry     = process.argv.includes('--dry');
const MB      = 1024 * 1024;

// How much detail each kind of model needs on a phone screen
function profile(name) {
  if (/^(PD|MN)-/.test(name))            return { texture: 512,  ratio: 0.15, minMB: 2 };   // pods, moons: small on screen
  if (/^(OB|LV)-/.test(name))            return { texture: 1024, ratio: 0.25, minMB: 3 };   // probes, telescopes, asteroids
  if (/^PL-/.test(name))                 return { texture: 2048, ratio: 0.5,  minMB: 6 };   // planets fill the screen on arrival
  if (/^NV-/.test(name))                 return { texture: 1024, ratio: 0.5,  minMB: 6 };   // ships: always close to the camera
  return                                        { texture: 1024, ratio: 0.3,  minMB: 4 };
}

function* glbs(dir) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const p = join(dir, entry.name);
    if (entry.isDirectory()) yield* glbs(p);
    else if (entry.name.toLowerCase().endsWith('.glb')) yield p;
  }
}

// Git LFS pointer files (LFS not pulled) are ~130 bytes of text, not models
function isLfsPointer(path) {
  return statSync(path).size < 1024;
}

const npx = process.platform === 'win32' ? 'npx.cmd' : 'npx';
let before = 0, after = 0, done = 0;

for (const file of glbs(models)) {
  const name = file.split(/[\\/]/).pop();
  const size = statSync(file).size;
  const cfg  = profile(name);
  if (isLfsPointer(file)) { console.log(`skip  ${name}: LFS pointer (run "git lfs pull" first)`); continue; }
  if (size < cfg.minMB * MB) continue;

  console.log(`${dry ? 'would' : 'opt  '} ${name}  ${(size / MB).toFixed(1)} MB  (textures ≤${cfg.texture}px, keep ${cfg.ratio * 100}% vertices)`);
  if (dry) continue;

  const backup = join(backups, relative(models, file));
  mkdirSync(dirname(backup), { recursive: true });
  if (!existsSync(backup)) copyFileSync(file, backup);

  const tmp = file + '.tmp.glb';
  const r = spawnSync(npx, ['-y', '@gltf-transform/cli@4', 'optimize', file, tmp,
    '--compress', 'false',                   // no Draco/meshopt: glTFast would need extra packages
    '--texture-compress', 'auto',            // keep PNG/JPEG, just re-encoded
    '--texture-size', String(cfg.texture),
    '--simplify', 'true', '--simplify-ratio', String(cfg.ratio), '--simplify-error', '0.002',
  ], { stdio: 'inherit', shell: process.platform === 'win32' });

  if (r.status !== 0 || !existsSync(tmp)) {
    console.error(`FAILED ${name} (original untouched)`);
    if (existsSync(tmp)) unlinkSync(tmp);
    continue;
  }
  unlinkSync(file);
  renameSync(tmp, file);
  const out = statSync(file).size;
  before += size; after += out; done++;
  console.log(`      → ${(out / MB).toFixed(1)} MB`);
}

if (!dry) console.log(`\n${done} models: ${(before / MB).toFixed(0)} MB → ${(after / MB).toFixed(0)} MB. Originals in ${relative(repo, backups)}/`);
