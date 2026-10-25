#!/usr/bin/env python3
"""Build locally; never redistribute Samsung reference assemblies."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", choices=["payloads", "launcher", "s95b-launcher", "s95b-overlay", "all"])
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    parser.add_argument("--framework", type=Path, default=ROOT / ".local/tv-framework")
    args = parser.parse_args()
    jobs = []
    if args.target in ("payloads", "all"):
        for name in ("SamsungTvArchiveRoot", "SamsungTvRootAgent"):
            jobs.append((ROOT / f"vendor/payload-source/{name}.csproj", "payloads", name, []))
    if args.target in ("launcher", "s95b-launcher", "s95b-overlay", "all"):
        for name in ("Tizen.NUI", "Tizen.Applications.Common", "Tizen.System", "Tizen"):
            if not (args.framework / f"{name}.dll").is_file():
                parser.error(f"Missing {name}.dll. Read docs/04-launcher.md; these are not bundled.")
    if args.target == "s95b-overlay":
        jobs.append((ROOT / "launcher/s95b-overlay/Overlay.csproj", "S95bOverlay", "S95bOverlay",
                      [f"-p:TV_FRAMEWORK_DIR={args.framework.resolve()}"]))
    if args.target == "s95b-launcher":
        jobs.append((ROOT / "launcher/src/Home.S95b.csproj", "S95bHome", "S95bHome",
                     [f"-p:TV_FRAMEWORK_DIR={args.framework.resolve()}"]))
    if args.target in ("launcher", "all"):
        for name in ("Home", "QuickLaunch"):
            jobs.append((ROOT / f"launcher/src/{name}.csproj", name, name,
                         [f"-p:TV_FRAMEWORK_DIR={args.framework.resolve()}"]))
        jobs.append((ROOT / "launcher/probe/Probe.csproj", "Probe", "Probe", []))
    for project, output, intermediate, extra in jobs:
        subprocess.run([
            args.dotnet, "build", str(project), "-c", "Release", "--nologo",
            "-o", str(ROOT / ".local/build" / output),
            f"-p:BaseIntermediateOutputPath={ROOT / '.local/obj' / intermediate}/",
            "-p:Deterministic=true",
            f"-p:PathMap={ROOT}=/_/quiet-tizen", *extra,
        ], check=True, env={**os.environ, "DOTNET_CLI_TELEMETRY_OPTOUT": "1"})
    if args.target in ("launcher", "s95b-launcher", "all"):
        s95b = args.target == "s95b-launcher"
        destination = ROOT / (".local/package-s95b" if s95b else ".local/package")
        destination.mkdir(parents=True, exist_ok=True)
        home = ROOT / ".local/build" / ("S95bHome" if s95b else "Home")
        shutil.copyfile(home / "BixbyPromotion.dll", destination / "BixbyPromotion.dll")
        if s95b:
            shutil.copyfile(home / "BixbyPromotion.runtimeconfig.json", destination / "BixbyPromotion.runtimeconfig.json")
        else:
            shutil.copyfile(ROOT / ".local/build/QuickLaunch/QuickLaunch.dll", destination / "QuickLaunch.dll")
            shutil.copyfile(ROOT / ".local/build/Probe/Probe.dll", destination / "Probe.dll")
        assets = destination / "quiet-home"
        if assets.exists():
            shutil.rmtree(assets)
        shutil.copytree(ROOT / "launcher/assets", assets)
        if s95b:
            shutil.copyfile(assets / "inputs.s95b.json", assets / "inputs.json")
        (assets / "inputs.s95b.json").unlink(missing_ok=True)
        local_catalog = ROOT / "launcher/assets/apps.local.json"
        if local_catalog.exists():
            shutil.copyfile(local_catalog, assets / "apps.json")
        (assets / "apps.local.json").unlink(missing_ok=True)
        print(f"Deployable files: {destination.relative_to(ROOT)}/. Review the catalog and input mapping before use.")


if __name__ == "__main__":
    main()
