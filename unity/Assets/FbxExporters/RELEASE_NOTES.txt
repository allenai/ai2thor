RELEASE NOTES

**Version**: 1.3.0f1

**Version**: 1.3.0b3

FIXES
* ConvertToPrefab: fix Mesh Collider not pointing to exported mesh after converting
* FbxExporter: fix so "Compatible Naming" doesn't modify scene on export

**Version**: 1.3.0b2

NEW FEATURES
* Unity3dsMaxIntegration: Allow multi file import
* Unity3dsMaxIntegration: Allow multi file export by scene selection

**Version**: 1.3.0b1

NEW FEATURES
* FbxExporter: Export animation clips from Timeline
* FbxExportSettings: Added new UI to set export settings
* FbxExportSettings: Added option to transfer transform animation on export
* FbxExporterSettings: Added option to export model only
* FbxExporterSettings: Added option to export animation only
* FbxExporterSettings: Added option not to export animation on skinned meshes
* FbxExportSettings: Added option to export meshes without renderers
* FbxExportSettings: Added LOD export option
* UnityMayaIntegration: Allow multi file import
* UnityMayaIntegration: Allow multi file export by scene selection
* FbxPrefabAutoUpdater: new UI to help manage name changes

FIXES
* FbxExporter: link materials to objects connected to mesh instances
* FbxExporter: export meshes in model prefab instances as mesh instances in fbx
* ConvertToPrefab: Don't re-export fbx model instances
* FbxExportSettings: fix console error on Mac when clicking "Install Unity Integration"
* FbxExporter: fix so animating spot angle in Unity animates cone angle in Maya (not penumbra)
* FbxExporter: export correct rotation order (xyz) for euler rotation animations (previously would export as zxy)

**Version**: 1.3.0a1

NEW FEATURES
* FbxExporter: Added support for exporting Blendshapes
* FbxExporter: Added support for exporting SkinnedMeshes with legacy and generic animation
* FbxExporter: Added support for exporting Lights with animatable properties (Intensity, Spot Angle, Color)
* FbxExporter: Added support for exporting Cameras with animatable properties (Field of View)
* FbxExporter: added ability to export animation on transforms

FIXES
* fix Universal Windows Platform build errors

Error caused by UnityFbxSdk.dll being set as compatible with any platform instead of editor only.

**Version**: 1.2.0b1

NEW FEATURES
* Added Maya LT one button import/export
* Added Camera export support 

**Version**: 1.1.0b1

NEW FEATURES
* Added 3ds Max one button import/export

FIXES
* Fix so Object references aren't lost when using Convert to Linked Prefab Instance
* Fix Maya Integration dropdown not appearing in the Export Settings

**Version**: 1.0.0b1

NEW FEATURES
* Ability to export fbx files from Unity
* Convert to linked prefab to create a prefab that auto-updates with the linked fbx
* Maya one button import/export