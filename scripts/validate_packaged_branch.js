#!/usr/bin/env node
"use strict";

const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");

const repoRoot = path.resolve(__dirname, "..");
const packageJsonPath = path.join(
  repoRoot,
  "Packages",
  "com.live2d.cubism",
  "package.json"
);
const urpRendererAssetPath = path.join(
  repoRoot,
  "Packages",
  "com.live2d.cubism",
  "Rendering",
  "URP",
  "CubismURPRenderer.asset"
);

function runGit(args) {
  return execFileSync("git", args, {
    cwd: repoRoot,
    encoding: "utf8"
  });
}

function validateNoLegacyAssetsPaths() {
  const tracked = runGit(["ls-files"]).split(/\r?\n/).filter(Boolean);
  return tracked.filter((p) => p.startsWith("Assets/Live2D/Cubism/"));
}

function validateUrpDependency() {
  if (!fs.existsSync(packageJsonPath)) {
    return `Missing package file: ${packageJsonPath}`;
  }

  const packageData = JSON.parse(fs.readFileSync(packageJsonPath, "utf8"));
  const deps = packageData.dependencies || {};
  const urpVersion = deps["com.unity.render-pipelines.universal"];

  if (!urpVersion) {
    return (
      "Missing dependency 'com.unity.render-pipelines.universal' in " +
      "Packages/com.live2d.cubism/package.json"
    );
  }

  return null;
}

function validateUrpAssets() {
  if (!fs.existsSync(urpRendererAssetPath)) {
    return `Missing URP renderer asset: ${urpRendererAssetPath}`;
  }
  return null;
}

function main() {
  const errors = [];

  const forbidden = validateNoLegacyAssetsPaths();
  if (forbidden.length > 0) {
    const preview = forbidden
      .slice(0, 20)
      .map((p) => `  - ${p}`)
      .join("\n");
    const suffix =
      forbidden.length > 20 ? `\n  ... and ${forbidden.length - 20} more` : "";
    errors.push(
      "Legacy assets path found in repository (should stay package-only):\n" +
        preview +
        suffix
    );
  }

  const urpDependencyError = validateUrpDependency();
  if (urpDependencyError) {
    errors.push(urpDependencyError);
  }

  const urpAssetsError = validateUrpAssets();
  if (urpAssetsError) {
    errors.push(urpAssetsError);
  }

  if (errors.length > 0) {
    console.error("Packaged branch validation failed:");
    for (const error of errors) {
      console.error(`\n- ${error}`);
    }
    process.exit(1);
  }

  console.log("Packaged branch validation passed.");
}

main();
