# Merge Automation Notes

## 1) Reuse conflict resolutions

Enable conflict reuse once on your machine:

```bash
git config --global rerere.enabled true
```

This lets Git remember previously resolved conflict patterns and auto-apply them in future merges.

## 2) Run packaged sync helper

From repository root:

```bash
node scripts/sync_master_to_packaged.js --source origin/master --target packaged-version
```

Sample conflicts are always resolved from `master` (`theirs`) by design.

## 3) Validate branch constraints

```bash
node scripts/validate_packaged_branch.js
```

Checks:

- no `Assets/Live2D/Cubism/**` paths are tracked
- `Packages/com.live2d.cubism/package.json` includes URP dependency
- `Packages/com.live2d.cubism/Rendering/URP/CubismURPRenderer.asset` exists

## 4) UnityYAMLMerge setup (per developer machine)

Set up Unity Smart Merge locally for better `.unity`/`.prefab`/`.asset` merges.
The repository already maps these extensions to `merge=unityyamlmerge` in `.gitattributes`.

Use helper script (recommended):

```bash
npm run merge:setup-yamlmerge
```

It auto-detects UnityYAMLMerge from Unity Hub installs, writes git config, and
ensures `.gitattributes` includes UnityYAMLMerge mappings.

Optional flags:

```bash
node scripts/setup_unityyamlmerge.js --scope local
node scripts/setup_unityyamlmerge.js --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3.52f1/Editor/Data/Tools/UnityYAMLMerge.exe"
node scripts/setup_unityyamlmerge.js --dry-run
```
