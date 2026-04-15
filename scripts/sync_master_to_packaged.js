#!/usr/bin/env node
"use strict";

const { spawnSync } = require("node:child_process");
const path = require("node:path");

const repoRoot = path.resolve(__dirname, "..");

function printUsage() {
  console.log(`Usage:
  node scripts/sync_master_to_packaged.js [--source <ref>] [--target <branch>]

Performs a merge from source into target and applies branch-specific conflict rules.
The script stops before commit so the result can be reviewed.
`);
}

function parseArgs(argv) {
  const args = {
    sourceRef: "origin/master",
    targetBranch: "packaged-version"
  };

  for (let i = 0; i < argv.length; i += 1) {
    const token = argv[i];
    if (token === "--source") {
      args.sourceRef = argv[++i] || "";
    } else if (token === "--target") {
      args.targetBranch = argv[++i] || "";
    } else if (token === "-h" || token === "--help") {
      printUsage();
      process.exit(0);
    } else {
      throw new Error(`Unknown argument: ${token}`);
    }
  }

  return args;
}

function runGit(args, { allowFailure = false, capture = false } = {}) {
  const result = spawnSync("git", args, {
    cwd: repoRoot,
    encoding: "utf8",
    stdio: capture ? ["ignore", "pipe", "pipe"] : "inherit"
  });

  if (result.error) {
    throw result.error;
  }
  if (!allowFailure && result.status !== 0) {
    throw new Error(`git ${args.join(" ")} failed with status ${result.status}`);
  }
  return result;
}

function getGitOutput(args) {
  const result = runGit(args, { capture: true });
  return (result.stdout || "").trim();
}

function getConflicts() {
  const output = getGitOutput(["diff", "--name-only", "--diff-filter=U"]);
  return output ? output.split(/\r?\n/).filter(Boolean) : [];
}

function main() {
  try {
    const args = parseArgs(process.argv.slice(2));
    const dirty = getGitOutput(["status", "--porcelain"]);
    if (dirty) {
      throw new Error("Working tree is not clean. Commit/stash changes before running sync.");
    }

    runGit(["fetch", "origin"]);
    runGit(["checkout", args.targetBranch]);
    runGit(["merge", "--no-ff", "--no-commit", args.sourceRef], { allowFailure: true });

    let conflicts = getConflicts();
    if (conflicts.length === 0) {
      console.log("No conflicts detected. Review and commit the merge.");
      return;
    }

    for (const filePath of conflicts) {
      if (filePath.startsWith("Assets/Live2D/Cubism/")) {
        // Keep packaged branch package-only layout.
        runGit(["rm", "-f", "--", filePath], { allowFailure: true });
        runGit(["add", "--", filePath], { allowFailure: true });
        continue;
      }

      if (filePath.startsWith("Packages/com.live2d.cubism/Samples~/")) {
        // Always keep sample conflicts from source (master).
        runGit(["checkout", "--theirs", "--", filePath], {
          allowFailure: true
        });
        runGit(["add", "--", filePath], { allowFailure: true });
      }
    }

    conflicts = getConflicts();
    console.log("\nRemaining conflicts after policy application:");
    if (conflicts.length === 0) {
      console.log("(none)");
    } else {
      for (const filePath of conflicts) {
        console.log(filePath);
      }
    }

    console.log("\nNext steps:");
    console.log("  1) Resolve remaining conflicts");
    console.log("  2) Run: node scripts/validate_packaged_branch.js");
    console.log("  3) Commit merge");
  } catch (error) {
    console.error(`Error: ${error.message}`);
    process.exit(1);
  }
}

main();
