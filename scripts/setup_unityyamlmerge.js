#!/usr/bin/env node
"use strict";

const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { execFileSync } = require("node:child_process");

const REPO_ROOT = path.resolve(__dirname, "..");
const GITATTRIBUTES_PATH = path.join(REPO_ROOT, ".gitattributes");
const REQUIRED_GITATTRIBUTES_LINES = [
  "*.unity        text merge=unityyamlmerge eol=lf",
  "*.prefab       text merge=unityyamlmerge eol=lf",
  "*.asset        text merge=unityyamlmerge eol=lf",
  "*.anim         text merge=unityyamlmerge eol=lf",
  "*.mat          text merge=unityyamlmerge eol=lf",
  "*.meta         text merge=unityyamlmerge eol=lf"
];

function runGitConfig(args) {
  execFileSync("git", args, {
    cwd: REPO_ROOT,
    stdio: "inherit"
  });
}

function printUsage() {
  console.log(`Usage:
  node scripts/setup_unityyamlmerge.js [--unity-path <path>] [--scope <global|local>] [--dry-run]

Options:
  --unity-path <path>   Explicit path to UnityYAMLMerge executable.
  --scope <value>       Git config scope: global (default) or local.
  --dry-run             Print detected values without writing git config.
  -h, --help            Show this help message.
`);
}

function parseArgs(argv) {
  const args = {
    unityPath: process.env.UNITY_YAMLMERGE_PATH || "",
    scope: "global",
    dryRun: false
  };

  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];
    if (token === "--unity-path") {
      args.unityPath = argv[++i] || "";
    } else if (token === "--scope") {
      args.scope = argv[++i] || "";
    } else if (token === "--dry-run") {
      args.dryRun = true;
    } else if (token === "-h" || token === "--help") {
      printUsage();
      process.exit(0);
    } else {
      throw new Error(`Unknown argument: ${token}`);
    }
  }

  if (args.scope !== "global" && args.scope !== "local") {
    throw new Error(`Invalid --scope '${args.scope}'. Expected 'global' or 'local'.`);
  }

  return args;
}

function listSubdirs(parentDir) {
  if (!fs.existsSync(parentDir)) {
    return [];
  }
  return fs
    .readdirSync(parentDir, { withFileTypes: true })
    .filter((d) => d.isDirectory())
    .map((d) => d.name)
    .sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
}

function findUnityYamlMerge() {
  const platform = process.platform;
  let candidatePaths = [];

  if (platform === "win32") {
    const base = "C:/Program Files/Unity/Hub/Editor";
    candidatePaths = listSubdirs(base).map((v) =>
      path.join(base, v, "Editor", "Data", "Tools", "UnityYAMLMerge.exe")
    );
  } else if (platform === "darwin") {
    const base = "/Applications/Unity/Hub/Editor";
    candidatePaths = listSubdirs(base).map((v) =>
      path.join(base, v, "Unity.app", "Contents", "Tools", "UnityYAMLMerge")
    );
  } else {
    const base = path.join(os.homedir(), "Unity", "Hub", "Editor");
    candidatePaths = listSubdirs(base).map((v) =>
      path.join(base, v, "Editor", "Data", "Tools", "UnityYAMLMerge")
    );
  }

  const match = candidatePaths.find((p) => fs.existsSync(p));
  return match || "";
}

function escapeDoubleQuotes(value) {
  return value.replace(/"/g, '\\"');
}

function ensureGitAttributesContent() {
  const currentContent = fs.existsSync(GITATTRIBUTES_PATH)
    ? fs.readFileSync(GITATTRIBUTES_PATH, "utf8")
    : "";
  const normalized = currentContent.replace(/\r\n/g, "\n");
  const missing = REQUIRED_GITATTRIBUTES_LINES.filter(
    (line) => !normalized.includes(line)
  );

  if (missing.length === 0) {
    return { changed: false, missing };
  }

  const block =
    "\n# Unity text-based serialized assets.\n" +
    "# Requires local git config for \"unityyamlmerge\" driver.\n" +
    `${missing.join("\n")}\n`;
  const updated = normalized.endsWith("\n")
    ? `${normalized}${block}`
    : `${normalized}\n${block}`;

  fs.writeFileSync(GITATTRIBUTES_PATH, updated, "utf8");
  return { changed: true, missing };
}

function main() {
  try {
    const args = parseArgs(process.argv.slice(2));
    const detected = args.unityPath || findUnityYamlMerge();

    if (!detected) {
      throw new Error(
        "Could not auto-detect UnityYAMLMerge. Pass --unity-path or set UNITY_YAMLMERGE_PATH."
      );
    }

    if (!fs.existsSync(detected)) {
      throw new Error(`UnityYAMLMerge path does not exist: ${detected}`);
    }

    const quotedPath = `"${escapeDoubleQuotes(detected)}"`;
    const mergeName = "Unity SmartMerge (UnityYAMLMerge)";
    const mergeDriver = `${quotedPath} merge -p %O %A %B %A`;
    const mergeToolCmd = `${quotedPath} merge -p "$BASE" "$LOCAL" "$REMOTE" "$MERGED"`;

    console.log("Using UnityYAMLMerge path:");
    console.log(`  ${detected}`);
    console.log(`Git config scope: ${args.scope}`);

    if (args.dryRun) {
      console.log("\nDry run values:");
      console.log(`  merge.unityyamlmerge.name = ${mergeName}`);
      console.log(`  merge.unityyamlmerge.driver = ${mergeDriver}`);
      console.log(`  mergetool.unityyamlmerge.cmd = ${mergeToolCmd}`);
      const currentContent = fs.existsSync(GITATTRIBUTES_PATH)
        ? fs.readFileSync(GITATTRIBUTES_PATH, "utf8").replace(/\r\n/g, "\n")
        : "";
      const missing = REQUIRED_GITATTRIBUTES_LINES.filter(
        (line) => !currentContent.includes(line)
      );
      if (missing.length === 0) {
        console.log("  .gitattributes already contains UnityYAMLMerge mappings");
      } else {
        console.log("  .gitattributes missing mappings:");
        for (const line of missing) {
          console.log(`    ${line}`);
        }
      }
      return;
    }

    const scopeArg = args.scope === "global" ? "--global" : "--local";
    runGitConfig(["config", scopeArg, "merge.unityyamlmerge.name", mergeName]);
    runGitConfig(["config", scopeArg, "merge.unityyamlmerge.driver", mergeDriver]);
    runGitConfig(["config", scopeArg, "mergetool.unityyamlmerge.cmd", mergeToolCmd]);
    const gitattributesResult = ensureGitAttributesContent();

    console.log("\nUnityYAMLMerge git config updated successfully.");
    if (gitattributesResult.changed) {
      console.log(".gitattributes updated with UnityYAMLMerge mappings.");
    } else {
      console.log(".gitattributes already had UnityYAMLMerge mappings.");
    }
  } catch (error) {
    console.error(`Error: ${error.message}`);
    process.exit(1);
  }
}

main();
