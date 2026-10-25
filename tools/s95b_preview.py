#!/usr/bin/env python3
"""Stage a bounded S95B inspection/preview; remove only this run's staged files."""
import argparse
import ipaddress
import json
import shlex
from pathlib import Path, PurePosixPath
import subprocess
import sys
import uuid
from tv import ROOT, observed_model, private_path, save_private


def root_trial_succeeded(evidence):
    commands = evidence.get("commands", [])
    return (evidence.get("authenticated") is True and evidence.get("uid") == 0
            and evidence.get("euid") == 0 and evidence.get("staging_cleaned") is True
            and evidence.get("system_files_unchanged") is True and len(commands) == 1
            and commands[0].get("exit_code") == 0 and commands[0].get("timed_out") is False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", required=True)
    parser.add_argument("--sdb", required=True, type=Path)
    parser.add_argument("--root-cli", type=Path)
    parser.add_argument("--seconds", type=int, default=8)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--inspect", action="store_true")
    mode.add_argument("--controls", action="store_true", help="Enable remote-triggered input and picture changes")
    mode.add_argument("--restore", nargs=5, metavar=("KEY", "EXPECTED", "ORIGINAL", "SOURCE", "MODE"), help="Restore a numeric setting only if its source, mode and current value still match")
    parser.add_argument("--log", required=True, type=Path)
    args = parser.parse_args()
    ipaddress.IPv4Address(args.host)
    if not 1 <= args.seconds <= 25:
        parser.error("Trial duration must be 1–25 seconds")
    from samsung_tv_root.sdb import SdbClient

    client = SdbClient(args.sdb, args.host)
    args.log = private_path(args.log)
    if observed_model(client) != "QN55S95BAFXZA":
        raise RuntimeError("Not the verified QN55S95BAFXZA")
    fingerprint = client.capture("cat /etc/tizen-release").output
    if "BUILD_ID=T-PTMAKUC-REL-202602250225\n" not in fingerprint:
        raise RuntimeError("Firmware differs from the reviewed S95B build")
    remote = PurePosixPath("/home/owner/share/tmp/sdk_tools") / ("overlay-trial-" + uuid.uuid4().hex)
    build = ROOT / ".local/build/S95bOverlay"
    files = ["Overlay.dll", "Overlay.runtimeconfig.json", "Overlay.deps.json"]
    for name in files:
        if not (build / name).is_file():
            raise RuntimeError("Build first: missing " + str(build / name))
    client.capture(f"/bin/mkdir {remote}")
    try:
        for name in files:
            client.push(build / name, remote / name)
        action = "--inspect" if args.inspect else f"{'--controls' if args.controls else '--preview'} {args.seconds}"
        if args.restore:
            action = shlex.join(["--restore", *args.restore])
        command = f"/usr/bin/dotnet {remote}/Overlay.dll {action} 2>{remote}/ui.log"
        if args.root_cli:
            result = subprocess.run([
                str(args.root_cli), "--sdb", str(args.sdb), "-o", "json",
                "archive-root", "root", args.host, "--command", command
            ], capture_output=True, text=True, timeout=120)
            output = result.stdout + result.stderr
            evidence = json.loads(result.stdout) if result.stdout.strip().startswith("{") else {}
            succeeded = result.returncode == 0 and root_trial_succeeded(evidence)
        else:
            output = client.capture(command, timeout=args.seconds + 20).output
            succeeded = "[exit:0]" in output
        diagnostics = client.capture(f"cat {remote}/ui.log").output
        save_private(args.log, (output + "\nNative diagnostics:\n" + diagnostics).encode())
        print(output)
        print("Diagnostics:", diagnostics[-3000:])
        print("Log:", args.log)
        return 0 if succeeded else 1
    finally:
        paths = " ".join(str(remote / name) for name in files + ["ui.log"])
        cleanup = client.capture(f"/bin/rm -f {paths}; /bin/rmdir {remote}; test ! -e {remote}")
        if "[exit:0]" not in cleanup.output:
            raise RuntimeError("Trial cleanup failed: " + cleanup.output)


if __name__ == "__main__":
    sys.exit(main())
