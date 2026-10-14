#!/usr/bin/env python3
import pathlib,json,shutil,re,collections
root=pathlib.Path(__file__).resolve().parents[1];src=root/'SourceAssets~/FPSSample';repo=pathlib.Path('/workspace/upstream/FPSSample');audit=json.loads((root/'Reports/fpssample-source-audit.json').read_text());records=[]
for rel in audit['files']:
 p=src/rel;dest=root/rel
 if dest.exists():continue
 if p.suffix.lower() in ('.cs','.dll','.asmdef'):continue
 dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
 meta=pathlib.Path(str(p)+'.meta')
 if meta.exists():shutil.copy2(meta,pathlib.Path(str(dest)+'.meta'))
 records.append({'path':rel,'action':'copied original for supported Unity migration'})
for rel in ['Assets/Tools/MaterialPropertyOverride/MaterialPropertyOverride.cs','Assets/Tools/MaterialPropertyOverride/MaterialPropertyOverrideAsset.cs','Assets/ProjectLODLightmaps/ProjectLODLightmaps.cs','Assets/VFX/Script/VisualEffectInitialState.cs']:
 p=repo/rel;dest=root/rel;dest.parent.mkdir(parents=True,exist_ok=True);s=p.read_text(encoding='utf-8-sig').replace('UnityEngine.Experimental.VFX','UnityEngine.VFX');dest.write_text(s);shutil.copy2(str(p)+'.meta',str(dest)+'.meta');records.append({'path':rel,'action':'original source port; VFX namespace updated where needed'})
# Upstream importer has no game-framework dependency. Keep it in Editor scope.
p=repo/'Assets/Scripts/EditorTools/Editor/GeometryImporter.cs';dest=root/'Assets/FirstRep/Editor/OriginalGeometryImporter.cs';s=p.read_text(encoding='utf-8-sig').replace('importer.importMaterials = false','importer.materialImportMode = ModelImporterMaterialImportMode.None');dest.write_text(s)
# Preserve original layers and allocate unused slots for the local FPS controller.
s=(repo/'ProjectSettings/TagManager.asset').read_text();lines=s.splitlines();i=lines.index('  layers:');lines[i+1+10]='  - FirstRepPlayer';lines[i+1+11]='  - FirstRepWeapon';(root/'ProjectSettings/TagManager.asset').write_text('\n'.join(lines)+'\n')
p=root/'Assets/FirstRep/Runtime/SliceBootstrap.cs';s=p.read_text().replace('player.layer = 8','player.layer = 10').replace('t.gameObject.layer = 9','t.gameObject.layer = 11');p.write_text(s)
(root/'Reports/migration-staging.json').write_text(json.dumps(records,indent=2)+'\n');print('Staged',len(records))
