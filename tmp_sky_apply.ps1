$ErrorActionPreference='Stop'
$scenePath='Assets\Scenes\SampleScene.unity'
$imagePath='Assets\Art\Forest\ForestSky.jpg'
$ids=@{go='2146000001';transform='2146000002';renderer='2146000003'}
$source=[System.IO.File]::ReadAllText($scenePath)
foreach($id in $ids.Values){if([regex]::IsMatch($source,'(?m)^--- !u!\d+ &'+[regex]::Escape($id)+'$')){throw "Scene already contains file ID $id."}}
$guid=[guid]::NewGuid().ToString('N')
$spriteID=[guid]::NewGuid().ToString('N')
$meta=@"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 4096
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spritePixelsToUnits: 128
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    allowsAlphaSplitting: 0
    overridden: 0
    forceMaximumCompressionQuality_BC6_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 4096
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    allowsAlphaSplitting: 0
    overridden: 0
    forceMaximumCompressionQuality_BC6_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $spriteID
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
$goText=@"
--- !u!1 &$($ids.go)
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: $($ids.transform)}
  - component: {fileID: $($ids.renderer)}
  m_Layer: 0
  m_Name: ForestSkyBackground
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &$($ids.transform)
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($ids.go)}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 10}
  m_LocalScale: {x: 1.8, y: 1.8, z: 1}
  m_ConstrainProportionsScale: 1
  m_Children: []
  m_Father: {fileID: 519420032}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &$($ids.renderer)
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($ids.go)}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 0
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 0
  m_ReceiveGI: 0
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 0
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: -200
  m_Sprite: {fileID: 21300000, guid: $guid, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 17.796875, y: 10}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
"@
$backup='SceneBackup\SampleScene_beforeSkyBackground.unity'
if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $scenePath -Destination $backup}
$parts=[regex]::Split($source,'(?=--- !u!)')
$changed=@{cameraTransform=$false;camera=$false;oldBackground=$false}
$out=[System.Collections.Generic.List[string]]::new()
foreach($part in $parts){
    $updated=$part
    if($part -match '^--- !u!4 &519420032'){
        if($part -notmatch 'm_Children: \[\]'){throw 'Expected empty Camera children list.'}
        $replacement='  m_Children:'+[Environment]::NewLine+'  - {fileID: '+$ids.transform+'}'
        $updated=[regex]::Replace($part,'(?m)^  m_Children: \[\]$',$replacement,1)
        $changed.cameraTransform=$true
    } elseif($part -match '^--- !u!20 &519420031'){
        $updated=[regex]::Replace($part,'(?m)^  m_BackGroundColor:.*$','  m_BackGroundColor: {r: 0.25, g: 0.70, b: 0.94, a: 1}',1)
        $changed.camera=$true
    } elseif($part -match '^--- !u!114 &519420034' -and $part -match 'guid: 0a3b4fab973f4548aa5969ef5915bfe4'){
        $updated=[regex]::Replace($part,'(?m)^  m_Enabled: 1$','  m_Enabled: 0',1)
        $changed.oldBackground=$true
    }
    $out.Add($updated)
}
if($changed.Values -contains $false){throw "Required camera data not found: $($changed | ConvertTo-Json -Compress)"}
$updatedScene=[string]::Join('',$out)+$goText
$assetMetaPath=$imagePath+'.meta'
[System.IO.File]::WriteAllText($assetMetaPath,$meta,[System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($scenePath,$updatedScene,[System.Text.UTF8Encoding]::new($false))
[pscustomobject]@{BackgroundSpriteGuid=$guid;SpriteRendererId=$ids.renderer;CameraChildAdded=$changed.cameraTransform;OldGeneratedBackgroundDisabled=$changed.oldBackground;ImagePath=(Resolve-Path $imagePath).Path}

