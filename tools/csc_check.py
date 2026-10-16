"""Compile Assembly-CSharp outside Unity to check for errors (no output DLL kept)."""
import glob, os, re, subprocess, sys, tempfile

root = sys.argv[1]
U = r"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data"
proj = open(os.path.join(root, "Assembly-CSharp.csproj"), encoding="utf-8").read()

refs = re.findall(r"<HintPath>([^<]+)</HintPath>", proj)
for d in glob.glob(os.path.join(root, "Library", "ScriptAssemblies", "*.dll")):
    if not os.path.basename(d).startswith("Assembly-CSharp"):
        refs.append(d)
defines = re.search(r"<DefineConstants>([^<]*)", proj).group(1)
srcs = {os.path.normpath(os.path.join(root, s)) for s in re.findall(r'<Compile Include="([^"]+\.cs)"', proj)}
for f in glob.glob(os.path.join(root, "Assets", "_Game", "Scripts", "**", "*.cs"), recursive=True):
    if os.sep + "Editor" + os.sep not in f:
        srcs.add(os.path.normpath(f))

out = os.path.join(tempfile.gettempdir(), "acs_check.dll")
rsp = ["-nologo", "-noconfig", "-nostdlib+", "-target:library", "-langversion:9", "-nowarn:CS0618,CS0649,CS0414,CS0169",
       f"-define:{defines}", f"-out:{out}"]
rsp += [f'-r:"{r}"' for r in dict.fromkeys(refs)]
rsp += [f'"{s}"' for s in sorted(srcs)]
rf = os.path.join(tempfile.gettempdir(), "acs_check.rsp")
open(rf, "w", encoding="utf-8").write("\n".join(rsp))
p = subprocess.run([os.path.join(U, "DotNetSdk", "dotnet.exe"),
                    os.path.join(U, "DotNetSdk", "sdk", "8.0.318", "Roslyn", "bincore", "csc.dll"), f"@{rf}"],
                   capture_output=True, text=True)
errs = [l for l in p.stdout.splitlines() if ": error " in l]
print("\n".join(errs[:30]) or "NO ERRORS", f"\n({len(srcs)} files, exit {p.returncode})")
